using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snowflake.Redis.Cache;

namespace Snowflake.Redis
{
    public class SnowFlakeBackgroundService : BackgroundService
    {
        private readonly ILogger<SnowFlakeBackgroundService> _logger;
        private readonly ICacheAsync _cacheAsync;
        private readonly MachineIdConfig _machineIdConfig;
        private readonly SnowflakeOptions snowflakeOptions;

        public SnowFlakeBackgroundService(ILogger<SnowFlakeBackgroundService> logger,
            ICacheAsync cacheAsync, MachineIdConfig machineIdConfig,
            IOptions<SnowflakeOptions> options)
        {
            this._logger = logger;
            this._machineIdConfig = machineIdConfig;
            this._cacheAsync = cacheAsync;
            this.snowflakeOptions = options.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"###  SnowFlake background task is stopping. {_machineIdConfig.GetKey()}");
            try
            {
                // 释放续期定时器
                _machineIdConfig.Dispose();
                // 删除 Redis 中机器ID 占用，便于其他实例复用该槽位
                await _cacheAsync.Del(_machineIdConfig.GetKey());
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "SnowFlake stop cleanup failed.");
            }
        }
    }
}