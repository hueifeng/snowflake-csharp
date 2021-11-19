using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Snowflake.Redis.Cache;

namespace Snowflake.Redis
{
    public class SnowFlakeBackgroundService : BackgroundService
    {
        private readonly ILogger<SnowFlakeBackgroundService> _logger;
        private readonly ICacheAsync _cacheAsync;
        private readonly MachineIdConfig _machineIdConfig;

        public SnowFlakeBackgroundService(ILogger<SnowFlakeBackgroundService> logger,
            ICacheAsync cacheAsync, MachineIdConfig machineIdConfig)
        {
            this._logger = logger;
            this._machineIdConfig = machineIdConfig;
            this._cacheAsync = cacheAsync;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("###  SnowFlake background task is stopping.");
            _cacheAsync.Del(_machineIdConfig.GetKey());
            return base.StopAsync(cancellationToken);
        }
    }
}