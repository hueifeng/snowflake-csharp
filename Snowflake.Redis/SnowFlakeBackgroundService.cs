using System.Threading;
using System.Threading.Tasks;
using CSRedis;
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
        private readonly SnowFlake snowFlake;
        private readonly SnowflakeOptions snowflakeOptions;

        public SnowFlakeBackgroundService(ILogger<SnowFlakeBackgroundService> logger,
            ICacheAsync cacheAsync, MachineIdConfig machineIdConfig, SnowFlake snowFlake,
            IOptions<SnowflakeOptions> options)
        {
            this._logger = logger;
            this._machineIdConfig = machineIdConfig;
            this._cacheAsync = cacheAsync;
            this.snowFlake = snowFlake;
            this.snowflakeOptions = options.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            RedisHelper.Initialization(new CSRedisClient(snowflakeOptions.ConnectionString));
            _logger.LogInformation($"###  SnowFlake background task is stopping. {_machineIdConfig.GetKey()}");
            await _cacheAsync.Del(_machineIdConfig.GetKey());
        }
    }
}