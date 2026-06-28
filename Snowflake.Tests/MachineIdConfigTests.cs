using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Snowflake.Redis;
using Snowflake.Redis.Cache;

namespace Snowflake.Tests;

/// <summary>
///     MachineIdConfig 测试：使用内存 FakeCache，不依赖真实 Redis。
///     覆盖 IP 哈希稳定性、注册、槽位冲突回退、全满抛异常、幂等、Dispose。
/// </summary>
public class MachineIdConfigTests
{
    /// <summary>内存缓存，模拟 SetNx/Exists/Get/Expire/Del，行为对齐 Redis 语义。</summary>
    private sealed class FakeCache : ICacheAsync
    {
        private readonly Dictionary<string, string> _store = new();
        public Task<bool> SetNxAsync(string key, object value)
        {
            lock (_store)
            {
                if (_store.ContainsKey(key)) return Task.FromResult(false);
                _store[key] = value?.ToString() ?? string.Empty;
                return Task.FromResult(true);
            }
        }
        public Task<bool> Expire(string key, int seconds) => Task.FromResult(true);
        public Task<bool> Exists(string key) { lock (_store) return Task.FromResult(_store.ContainsKey(key)); }
        public Task<string> Get(string key) { lock (_store) return Task.FromResult(_store.GetValueOrDefault(key)); }
        public Task<long> Del(string key) { lock (_store) { var n = _store.Remove(key) ? 1L : 0L; return Task.FromResult(n); } }
    }

    [Fact]
    public void HashToSlot_Deterministic()
    {
        int a = MachineIdConfig.HashToSlot("192.168.0.200", 32);
        int b = MachineIdConfig.HashToSlot("192.168.0.200", 32);
        Assert.Equal(a, b);
        Assert.InRange(a, 0, 31);
    }

    [Fact]
    public void HashToSlot_NoIpConcatCollision()
    {
        // 旧实现 Replace(".","") 会让这两者碰撞，FNV-1a 应区分开
        int a = MachineIdConfig.HashToSlot("192.168.0.200", 32);
        int b = MachineIdConfig.HashToSlot("192.16.80.200", 32);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task InitMachineId_FirstTime_Registers()
    {
        var cache = new FakeCache();
        var opt = new SnowflakeOptions { Name = "order", DataCenterId = 1 };
        var cfg = new MachineIdConfig(cache, opt);
        try
        {
            var sf = await cfg.InitMachineId();
            Assert.NotNull(sf);
            Assert.True(await cache.Exists($"order:1:{cfg.MachineId}"));
        }
        finally { cfg.Dispose(); }
    }

    [Fact]
    public async Task InitMachineId_SlotConflict_FallsBackToEmptySlot()
    {
        var cache = new FakeCache();
        var opt = new SnowflakeOptions { Name = "biz", DataCenterId = 2 };
        // 预占 0~31 中除 7 外全部槽位
        for (int i = 0; i < 32; i++) if (i != 7) await cache.SetNxAsync($"biz:2:{i}", "10.0.0." + i);
        var cfg = new MachineIdConfig(cache, opt);
        try
        {
            await cfg.InitMachineId();
            // 应落到空槽 7 或随机段 32~63
            Assert.True(cfg.MachineId == 7 || (cfg.MachineId >= 32 && cfg.MachineId < 64));
            Assert.True(await cache.Exists($"biz:2:{cfg.MachineId}"));
        }
        finally { cfg.Dispose(); }
    }

    [Fact]
    public async Task InitMachineId_AllSlotsFull_ThrowsNotStackOverflow()
    {
        var cache = new FakeCache();
        var opt = new SnowflakeOptions { Name = "full", DataCenterId = 3 };
        // 占满全部 0~63 槽，且 value 与本机 IP 不同
        for (int i = 0; i < 64; i++) await cache.SetNxAsync($"full:3:{i}", "9.9.9.9");
        var cfg = new MachineIdConfig(cache, opt);
        try
        {
            // 旧实现会无限递归栈溢出；现应抛 InvalidOperationException
            await Assert.ThrowsAsync<InvalidOperationException>(() => cfg.InitMachineId());
        }
        finally { cfg.Dispose(); }
    }

    [Fact]
    public async Task Dispose_DoesNotThrow()
    {
        var cache = new FakeCache();
        var opt = new SnowflakeOptions { Name = "disp", DataCenterId = 0 };
        var cfg = new MachineIdConfig(cache, opt);
        await cfg.InitMachineId();
        var ex = Record.Exception(() => cfg.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public async Task GetKey_Format_NameDcMachineId()
    {
        var cache = new FakeCache();
        var opt = new SnowflakeOptions { Name = "svc", DataCenterId = 5 };
        var cfg = new MachineIdConfig(cache, opt);
        try
        {
            await cfg.InitMachineId();
            Assert.Equal($"svc:5:{cfg.MachineId}", cfg.GetKey());
        }
        finally { cfg.Dispose(); }
    }

    [Fact]
    public async Task SameIp_Idempotent_SameSlot()
    {
        var cache = new FakeCache();
        var opt = new SnowflakeOptions { Name = "idem", DataCenterId = 1 };
        var cfg1 = new MachineIdConfig(cache, opt);
        await cfg1.InitMachineId();
        var m1 = cfg1.MachineId;
        cfg1.Dispose();
        // 第二个实例同 IP，应命中同槽（RegisterMachine 中 value==localIp 续期）
        var cfg2 = new MachineIdConfig(cache, opt);
        try
        {
            await cfg2.InitMachineId();
            Assert.Equal(m1, cfg2.MachineId);
        }
        finally { cfg2.Dispose(); }
    }
}