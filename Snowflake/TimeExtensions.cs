using System;

namespace Snowflake
{
    /// <summary>
    ///     提供当前毫秒时间戳，并支持测试期替换时间源。
    ///     注意：原类名 <c>System</c> 与 BCL 的 <c>System</c> 命名空间冲突，已重命名为 <see cref="SnowflakeClock"/>。
    /// </summary>
    public static class SnowflakeClock
    {
        public static Func<long> CurrentTimeFunc = InternalCurrentTimeMillis;

        public static long CurrentTimeMillis()
        {
            return CurrentTimeFunc();
        }

        public static IDisposable StubCurrentTime(Func<long> func)
        {
            CurrentTimeFunc = func;
            return new DisposableAction(() =>
            {
                CurrentTimeFunc = InternalCurrentTimeMillis;
            });
        }

        public static IDisposable StubCurrentTime(long millis)
        {
            CurrentTimeFunc = () => millis;
            return new DisposableAction(() =>
            {
                CurrentTimeFunc = InternalCurrentTimeMillis;
            });
        }

        private static readonly DateTime Jan1st1970 = new DateTime
            (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static long InternalCurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }
    }
}