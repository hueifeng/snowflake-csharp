using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Snowflake;

namespace Snowflake.Tests;

/// <summary>
///     SnowFlake 核心算法正确性测试：去重、位段、序列号、时钟回拨、构造校验。
/// </summary>
public class SnowFlakeTests
{
    private static long[] SplitId(long id) => new[] { id >> 24, (id >> 18) & 0x3F, (id >> 12) & 0x3F, id & 0xFFF };

    [Fact]
    public void Single_NextId_Returns_Quickly()
    {
        var sf = new SnowFlake(1, 1);
        var id = sf.NextId();
        Assert.True(id > 0);
    }

    [Fact]
    public async Task Concurrent_32Threads_640k_Unique()
    {
        var sf = new SnowFlake(1, 1);
        var ids = new ConcurrentBag<long>();
        var ts = Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 20_000; i++) ids.Add(sf.NextId());
        })).ToArray();
        await Task.WhenAll(ts);
        var arr = ids.ToArray();
        Assert.Equal(640000, arr.Length);
        Assert.Equal(640000, arr.Distinct().Count());
    }

    [Fact]
    public void Concurrent_BitSegments_Correct()
    {
        var sf = new SnowFlake(1, 1);
        var ids = new ConcurrentBag<long>();
        Parallel.For(0, 32, _ => { for (int i = 0; i < 20_000; i++) ids.Add(sf.NextId()); });
        Assert.All(ids, id =>
        {
            var s = SplitId(id);
            Assert.Equal(1, s[1]);
            Assert.Equal(1, s[2]);
            Assert.True(s[3] <= 4095);
        });
    }

    [Fact]
    public void SameMs_4096_SequenceNumbers_AllCovered()
    {
        long ms = 2_000_400_000_000L;
        var sf = new SnowFlake(1, 1);
        using (SnowflakeClock.StubCurrentTime(() => ms))
        {
            var ids = new List<long>();
            for (int i = 0; i < 4096; i++) ids.Add(sf.NextId());
            var seqs = new HashSet<long>(ids.Select(x => x & 0xFFF));
            Assert.Equal(4096, seqs.Count);
            // 同一毫秒内
            Assert.Single(ids.Select(x => x >> 24).Distinct());
        }
    }

    [Fact]
    public void CrossMs_TimestampAdvances()
    {
        long t = 2_000_500_000_000L;
        var sf = new SnowFlake(1, 1);
        using (SnowflakeClock.StubCurrentTime(() => Interlocked.Increment(ref t) / 2))
        {
            var ids = new List<long>();
            for (int i = 0; i < 1000; i++) ids.Add(sf.NextId());
            Assert.True(ids.Select(x => x >> 24).Distinct().Count() > 1);
        }
    }

    [Fact]
    public void FourInstances_GlobalUnique_100k()
    {
        var sf1 = new SnowFlake(0, 0); var sf2 = new SnowFlake(0, 1);
        var sf3 = new SnowFlake(1, 0); var sf4 = new SnowFlake(1, 1);
        var bag = new ConcurrentBag<long>();
        Parallel.Invoke(
            () => { for (int i = 0; i < 25_000; i++) bag.Add(sf1.NextId()); },
            () => { for (int i = 0; i < 25_000; i++) bag.Add(sf2.NextId()); },
            () => { for (int i = 0; i < 25_000; i++) bag.Add(sf3.NextId()); },
            () => { for (int i = 0; i < 25_000; i++) bag.Add(sf4.NextId()); });
        var arr = bag.ToArray();
        Assert.Equal(100000, arr.Length);
        Assert.Equal(100000, arr.Distinct().Count());
        Assert.All(arr, id => { var s = SplitId(id); Assert.True(s[1] <= 1 && s[2] <= 1); });
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(64, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 64)]
    public void Constructor_Rejects_OutOfRange(long dc, long mc)
    {
        Assert.Throws<ArgumentException>(() => new SnowFlake(dc, mc));
    }

    [Fact]
    public void Constructor_Accepts_Boundary_0_63()
    {
        var a = new SnowFlake(0, 0);
        var b = new SnowFlake(63, 63);
        Assert.Equal(0, a.MachineId);
        Assert.Equal(63, b.MachineId);
    }

    [Fact]
    public void Clock_ShortBackward_3ms_WaitsAndReturns()
    {
        long ms = 2_000_000_000_000L;
        var sf = new SnowFlake(1, 1);
        using (SnowflakeClock.StubCurrentTime(() => ms)) sf.NextId();
        long cur = ms - 3;
        using (SnowflakeClock.StubCurrentTime(() => Interlocked.Increment(ref cur)))
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var id = sf.NextId();
            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds < 1000);
            Assert.True(id > 0);
        }
    }

    [Fact]
    public void Clock_LongBackward_100ms_AtBatchBoundary_Throws()
    {
        long ms = 2_000_100_000_000L;
        var sf = new SnowFlake(1, 1);
        // 耗尽初始批次，使下次调用进入锁内回拨检测
        using (SnowflakeClock.StubCurrentTime(() => ms))
            for (int i = 0; i < 4096; i++) sf.NextId();
        using (SnowflakeClock.StubCurrentTime(() => ms - 100))
        {
            Assert.Throws<InvalidOperationException>(() => sf.NextId());
        }
    }

    [Fact]
    public void Clock_BoundaryBackward_5ms_DoesNotThrow()
    {
        long ms = 2_000_200_000_000L;
        var sf = new SnowFlake(1, 1);
        using (SnowflakeClock.StubCurrentTime(() => ms)) sf.NextId();
        long cur = ms - 5;
        using (SnowflakeClock.StubCurrentTime(() => Interlocked.Increment(ref cur)))
        {
            var id = sf.NextId();
            Assert.True(id > 0);
        }
    }

    [Fact]
    public void ConcurrentShortBackward_NoDuplicates()
    {
        long ms = 2_000_300_000_000L;
        var sf = new SnowFlake(1, 1);
        long t = ms;
        using (SnowflakeClock.StubCurrentTime(() => Interlocked.Increment(ref t)))
        {
            var warm = new ConcurrentBag<long>();
            Parallel.For(0, 10000, _ => warm.Add(sf.NextId()));
        }
        long cur = ms - 4;
        using (SnowflakeClock.StubCurrentTime(() => Interlocked.Increment(ref cur)))
        {
            var ids = new ConcurrentBag<long>();
            Parallel.For(0, 20000, _ =>
            {
                try { ids.Add(sf.NextId()); } catch (InvalidOperationException) { /* 可接受 */ }
            });
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
    }

    [Fact]
    public void OneMillion_Concurrent_Unique()
    {
        var sf = new SnowFlake(1, 1);
        var ids = new long[1_000_000];
        Parallel.For(0, 1_000_000, i => ids[i] = sf.NextId());
        Assert.Equal(1_000_000L, ids.Distinct().LongCount());
    }

    [Fact]
    public void TimestampSegment_NonDecreasing_SingleThread()
    {
        var sf = new SnowFlake(1, 1);
        long prev = -1;
        for (int i = 0; i < 100000; i++)
        {
            long ts = sf.NextId() >> 24;
            Assert.True(ts >= prev);
            prev = ts;
        }
    }

    [Fact]
    public void Concurrent_NoUnhandledException()
    {
        var sf = new SnowFlake(1, 1);
        var ex = Record.Exception(() => Parallel.For(0, 50000, _ => sf.NextId()));
        Assert.Null(ex);
    }

    [Fact]
    public void CrossBatchBoundary_800k_Unique()
    {
        var sf = new SnowFlake(1, 1);
        var ids = new ConcurrentBag<long>();
        Parallel.For(0, 16, _ => { for (int i = 0; i < 50_000; i++) ids.Add(sf.NextId()); });
        var arr = ids.ToArray();
        Assert.Equal(800000, arr.Length);
        Assert.Equal(800000, arr.Distinct().Count());
    }

    [Fact]
    public void BackwardDuringCachedBatch_NoDuplicates()
    {
        long ms = 2_000_600_000_000L;
        var sf = new SnowFlake(1, 1);
        var gen = new ConcurrentBag<long>();
        using (SnowflakeClock.StubCurrentTime(() => ms))
            for (int i = 0; i < 100; i++) gen.Add(sf.NextId());
        long cur = ms - 3;
        using (SnowflakeClock.StubCurrentTime(() => Interlocked.Increment(ref cur)))
        {
            try { for (int i = 0; i < 5000; i++) gen.Add(sf.NextId()); }
            catch (InvalidOperationException) { /* 短回拨追不上可接受 */ }
            var arr = gen.ToArray();
            Assert.Equal(arr.Length, arr.Distinct().Count());
        }
    }

    [Fact]
    public void FixedMs_4096_Unique_Then4097ThrowsSequenceExhausted()
    {
        long ms = 2_000_700_000_000L;
        var sf = new SnowFlake(1, 1);
        var ids = new List<long>();
        using (SnowflakeClock.StubCurrentTime(() => ms))
        {
            for (int i = 0; i < 4096; i++) ids.Add(sf.NextId());
            Assert.Equal(4096, ids.Distinct().Count());
            Assert.Throws<InvalidOperationException>(() => sf.NextId());
        }
    }
}