using Snowflake.Redis.Cache;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

namespace Snowflake.Redis
{
    public class MachineIdConfig
    {
        private readonly ICacheAsync _cacheAsync;

        public MachineIdConfig(ICacheAsync cacheAsync,SnowflakeOptions options)
        {
            this._cacheAsync = cacheAsync;
            Name = options.Name;
            _datacenterId = options.DataCenterId;
        }

        /// <summary>
        ///     数据中心Id
        /// </summary>
        private readonly long _datacenterId;

        /// <summary>
        ///     机器Id
        /// </summary>
        private long _machineId;

        /// <summary>
        ///     业务类型名称
        /// </summary>
        private string Name { get; set; }

        /// <summary>
        ///     本地IP地址
        /// </summary>
        private static string LocalIp { get; set; }

        /// <summary>
        ///     获取IP地址
        /// </summary>
        /// <returns></returns>
        private string GetIpAddress()
        {
            string addressIp = string.Empty;
            foreach (IPAddress ipAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ipAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    return ipAddress.ToString();
                }
            }
            return addressIp;
        }

        public async Task<SnowFlake> InitMachineId()
        {
            LocalIp = GetIpAddress();//192.168.0.200
            long ip = long.Parse(LocalIp.Replace(".", ""));//1921680200
            _machineId = ip.GetHashCode() % 32;//0-31
            //创建一个机器ID
            await CreateMachineId();

            return new SnowFlake(_datacenterId, _machineId);
        }

        /// <summary>
        /// 获取机器IP 并 % 32得到0-31
        /// 使用 业务名 + 组名 + IP 作为 Redis 的 key，机器IP作为 value，存储到Redis中
        /// </summary>
        /// <returns></returns>
        private async Task<long> CreateMachineId()
        {
            try
            {
                //向redis注册，并设置超时时间
                var flag = await RegisterMachine(_machineId, LocalIp);
                if (flag)
                {
                    UpdateExpTimeThread();
                    //返回机器ID
                    return _machineId;
                }

                //注册失败，可能 Hash%32 结果冲突
                if (!await CheckIfCanRegister())
                {
                    //如果0~31已经用完，使用32~64之间的随机ID
                    GetRandomMachineId();
                    await CreateMachineId();
                }
                else
                {
                    // 如果存在剩余的ID
                    await CreateMachineId();
                }

            }
            catch (Exception)
            {
                // 获取 32 - 63 之间的随机Id
                // 返回机器Id
                GetRandomMachineId();
                return _machineId;
            }
            return _machineId;
        }

        /// <summary>
        ///     获取32~63随机数
        /// </summary>
        private void GetRandomMachineId()
        {
            Random random = new Random();
            _machineId = (int)random.NextDouble() * 31 + 31;
        }

        /// <summary>
        ///     检查是否注册满了
        /// </summary>
        /// <returns></returns>
        private async Task<bool> CheckIfCanRegister()
        {
            //判断0~31这个区间段的机器IP是否被占满
            bool flag = true;
            for (int i = 0; i < 32; i++)
            {
                flag = await _cacheAsync.Exists(Name + _datacenterId + i);
                //如果不存在，设置机器ID为这个不存在的数字
                if (!flag)
                {
                    _machineId = i;
                    break;
                }
            }
            return !flag;
        }

        /// <summary>
        ///     1、更新超时时间
        /// 注意，更新前检查是否存在机器IP占用情况
        /// </summary>
        private void UpdateExpTimeThread()
        {
            var timer = new Timer();
            timer.Enabled = true;
            timer.Interval = 1000 * 60 * 60 * 23;
            timer.Start();

            timer.Elapsed += Timer_Elapsed;

        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            bool b = CheckIsLocalIp(_machineId.ToString()).Result;
            if (b)
            {
                _cacheAsync.Expire(Name + _datacenterId + _machineId, 60 * 60 * 24);
            }
            else
            {
                // IP冲突
                // 重新生成机器ID，并且更改雪花中的机器ID
                GetRandomMachineId();
                //重新生成并注册机器ID
                CreateMachineId().Wait();
                // 更改雪花中的机器ID
                SnowFlake.SetMachineId(_machineId);
            }

        }

        /// <summary>
        ///     检查Redis中对应key的val是否是本机IP
        /// </summary>
        /// <param name="machineId"></param>
        /// <returns></returns>
        private async Task<bool> CheckIsLocalIp(string machineId)
        {
            string ip = await _cacheAsync.Get(Name + _datacenterId + machineId);
            return LocalIp.Equals(ip);
        }

        /// <summary>
        ///     注册机器 设置超时时间
        /// </summary>
        /// <param name="machineId">0~31</param>
        /// <param name="localIp"></param>
        /// <returns></returns>
        private async Task<bool> RegisterMachine(long machineId, string localIp)
        {
            // key 业务号 + 数据中心ID + 机器ID value 机器IP
            var key = Name + _datacenterId + machineId;
            var result = await _cacheAsync.SetNxAsync(key, localIp);
            if (result)
            {
                //过期时间1天
                await _cacheAsync.Expire(key, 60 * 60 * 24);
                return true;
            }

            //如果key存在，判断val和当前IP是否一致，一致返回true
            var val = await _cacheAsync.Get(key);
            if (localIp.Equals(val))
            {
                //IP一致，注册机器ID成功
                await _cacheAsync.Expire(key, 60 * 60 * 24);
                return true;
            }
            return false;
        }

    }
}
