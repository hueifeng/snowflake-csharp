using CSRedis;
using Microsoft.Extensions.DependencyInjection;
using Snowflake.Redis.Cache;
using System.Linq;

namespace Snowflake.Redis.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSnowflakeRedisService(this IServiceCollection services, string connectionString)
        {
            RedisHelper.Initialization(new CSRedisClient(connectionString));
            services.AddSingleton(typeof(ICacheAsync), typeof(RedisCacheAsync));
            var config= services.FirstOrDefault(d => d.ServiceType == typeof(MachineIdConfig));
            var machineIdConfig = (MachineIdConfig)services.FirstOrDefault(d => d.ServiceType == typeof(MachineIdConfig))?.ImplementationInstance;
            services.AddSingleton(typeof(SnowFlake), machineIdConfig.InitMachineId());

            return services;
        }
    }
}
