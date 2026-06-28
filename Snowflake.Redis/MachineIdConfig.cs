using Snowflake.Redis.Cache;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Snowflake.Redis
{
    public class MachineIdConfig : IDisposable
    {
        /// <summary>机器ID槽位上限（受 MachineBit=6 限制，0~63）。</summary>
        private const int MaxMachineSlot = 64;
        /// <summary>第一段槽位数（基于 IP 哈希的主选区间）。</summary>
        private const int PrimarySlotCount = 32;
        /// <summary>注册最大重试次数，防止异常场景下无限循环。</summary>
        private const int MaxRegisterAttempts = 64;
        /// <summary>机器ID在 Redis 中的过期时间（秒）。</summary>
        private const int KeyTtlSeconds = 60 * 60 * 24;
        /// <summary>续期刷新间隔（秒）：短于 TTL，留出缓冲，避免一次刷新失败即丢槽。</summary>
        private const int RefreshIntervalSeconds = 60 * 60 * 12;

        private readonly ICacheAsync _cacheAsync;
        private readonly System.Timers.Timer _refreshTimer;
        private readonly Random _random = new Random();

        public MachineIdConfig(ICacheAsync cacheAsync, SnowflakeOptions options)
        {
            this._cacheAsync = cacheAsync;
            Name = options.Name;
            _datacenterId = options.DataCenterId;
            _refreshTimer = new System.Timers.Timer(RefreshIntervalSeconds * 1000L)
            {
                AutoReset = true,
                Enabled = false
            };
            _refreshTimer.Elapsed += Timer_Elapsed;
        }

        /// <summary>
        ///     数据中心Id
        /// </summary>
        private readonly long _datacenterId;

        /// <summary>
        ///     机器Id
        /// </summary>
        public long MachineId => _machineId;
        private long _machineId;

        /// <summary>
        ///     业务类型名称
        /// </summary>
        private string Name { get; set; }

        /// <summary>
        ///     本地IP地址
        /// </summary>
        public string LocalIp { get; private set; }

        public string GetKey()
        {
            return $"{Name}:{_datacenterId}:{_machineId}";
        }

        public string GetNameAndDataCenterId()
        {
            return $"{Name}:{_datacenterId}";
        }

        /// <summary>
        ///     获取本机 IPv4 地址（无 IPv4 时回退到 IPv6）。
        /// </summary>
        /// <returns></returns>
        private string GetIpAddress()
        {
            IPAddress ipv6Fallback = null;
            foreach (IPAddress ipAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ipAddress.ToString();
                }
                if (ipv6Fallback == null && ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    ipv6Fallback = ipAddress;
                }
            }
            return ipv6Fallback?.ToString() ?? string.Empty;
        }

        public async Task<SnowFlake> InitMachineId()
        {
            LocalIp = GetIpAddress();
            if (string.IsNullOrEmpty(LocalIp))
            {
                throw new InvalidOperationException("无法获取本机 IP 地址，机器ID分配失败");
            }

            _machineId = HashToSlot(LocalIp, PrimarySlotCount);
            await CreateMachineId();

            // 启动续期定时器
            _refreshTimer.Start();

            return new SnowFlake(_datacenterId, _machineId);
        }

        /// <summary>
        ///     将 IP 字符串映射到 [0, slotCount) 的稳定槽位（FNV-1a 32bit）。
        ///     避免使用 string.GetHashCode()（跨进程/架构不稳定）以及
        ///     "Replace('.',"")" 拼接导致的碰撞（如 192.168.0.200 与 192.16.80.200）。
        /// </summary>
        internal static int HashToSlot(string ip, int slotCount)
        {
            // FNV-1a 32-bit
            uint hash = 2166136261u;
            unchecked
            {
                foreach (byte b in System.Text.Encoding.UTF8.GetBytes(ip))
                {
                    hash ^= b;
                    hash *= 16777619u;
                }
            }
            return (int)(hash % (uint)slotCount);
        }

        /// <summary>
        ///     注册机器ID，最多重试 <see cref="MaxRegisterAttempts"/> 次，避免无限递归导致栈溢出。
        /// </summary>
        private async Task CreateMachineId()
        {
            int attempts = 0;
            while (attempts < MaxRegisterAttempts)
            {
                attempts++;
                try
                {
                    bool flag = await RegisterMachine(_machineId, LocalIp);
                    if (flag)
                    {
                        return;
                    }

                    // 注册失败：可能是该槽位已被其他 IP 占用。先扫描 0~31 找空槽。
                    if (await CheckIfCanRegister())
                    {
                        // 找到空槽，CheckIfCanRegister 已把 _machineId 设为该空槽，继续尝试注册
                        continue;
                    }

                    // 0~31 全满，回退到 32~63 随机并尝试注册
                    GetRandomMachineId();
                    continue;
                }
                catch (Exception)
                {
                    // Redis 异常等：回退到 32~63 随机，继续重试
                    GetRandomMachineId();
                }
            }

            throw new InvalidOperationException(
                $"机器ID注册失败，已重试 {MaxRegisterAttempts} 次。请检查 Redis 连接或槽位占用情况。");
        }

        /// <summary>
        ///     获取 32~63 之间的随机机器ID。
        /// </summary>
        private void GetRandomMachineId()
        {
            int id = _random.Next(PrimarySlotCount, MaxMachineSlot);
            Interlocked.Exchange(ref _machineId, id);
        }

        /// <summary>
        ///     检查 0~31 槽位中是否存在空位；若存在则把 _machineId 置为该空位并返回 true。
        /// </summary>
        /// <returns>true 表示找到空槽；false 表示已满。</returns>
        private async Task<bool> CheckIfCanRegister()
        {
            for (int i = 0; i < PrimarySlotCount; i++)
            {
                bool exists = await _cacheAsync.Exists($"{Name}:{_datacenterId}:{i}");
                if (!exists)
                {
                    Interlocked.Exchange(ref _machineId, i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     定时续期：检查 Redis 中存的 IP 是否仍是本机，是则续期；否则重新分配。
        /// </summary>
        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                bool b = await CheckIsLocalIp();
                if (b)
                {
                    await _cacheAsync.Expire(GetKey(), KeyTtlSeconds);
                }
                else
                {
                    // IP 冲突：当前槽位已被其他实例占用，重新分配机器ID
                    // 注意：已生成的 SnowFlake 单例（含旧 machineId）不会自动更新，
                    // 这里重新分配用于下一次注册，避免与占用者持续冲突。
                    GetRandomMachineId();
                    await CreateMachineId();
                }
            }
            catch (Exception)
            {
                // 续期失败忽略，下一轮再试；TTL 留有缓冲
            }
        }

        /// <summary>
        ///     检查 Redis 中对应 key 的 value 是否是本机 IP。
        /// </summary>
        private async Task<bool> CheckIsLocalIp()
        {
            string ip = await _cacheAsync.Get(GetKey());
            return LocalIp != null && LocalIp.Equals(ip);
        }

        /// <summary>
        ///     注册机器：用 SetNx 占槽，并设置过期时间。若槽已被本机 IP 占用则续期。
        /// </summary>
        /// <param name="machineId">0~63</param>
        /// <param name="localIp"></param>
        /// <returns></returns>
        private async Task<bool> RegisterMachine(long machineId, string localIp)
        {
            var key = GetKey();
            var result = await _cacheAsync.SetNxAsync(key, localIp);
            if (result)
            {
                await _cacheAsync.Expire(key, KeyTtlSeconds);
                return true;
            }

            // 如果 key 存在，判断 value 与当前 IP 是否一致，一致则续期返回 true
            var val = await _cacheAsync.Get(key);
            if (localIp.Equals(val))
            {
                await _cacheAsync.Expire(key, KeyTtlSeconds);
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            _refreshTimer.Elapsed -= Timer_Elapsed;
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
        }
    }
}