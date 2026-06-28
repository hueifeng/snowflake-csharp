using CSRedis;
using Microsoft.Extensions.DependencyInjection;
using Snowflake.Redis.Cache;
using System;

namespace Snowflake.Redis.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSnowflakeRedisService(this IServiceCollection services,
            string connectionString, Action<SnowflakeOptions> action)
        {
            services.Configure(action);
            RedisHelper.Initialization(new CSRedisClient(connectionString));
            SnowflakeOptions snowflakeOptions = new SnowflakeOptions();
            action.Invoke(snowflakeOptions);
            return AddSnowflakeRedisServiceCore(services, snowflakeOptions);
        }

        public static IServiceCollection AddSnowflakeRedisService(this IServiceCollection services,
          Action<SnowflakeOptions> action)
        {
            services.Configure(action);
            SnowflakeOptions snowflakeOptions = new SnowflakeOptions();
            action.Invoke(snowflakeOptions);
            RedisHelper.Initialization(new CSRedisClient(snowflakeOptions.ConnectionString));
            return AddSnowflakeRedisServiceCore(services, snowflakeOptions);
        }

        private static IServiceCollection AddSnowflakeRedisServiceCore(IServiceCollection services,
            SnowflakeOptions snowflakeOptions)
        {
            services.AddSingleton<ICacheAsync>(new RedisCacheAsync());
            var machineIdConfig = new MachineIdConfig(new RedisCacheAsync(), snowflakeOptions);
            services.AddSingleton(machineIdConfig);

            // 启动期一次性初始化机器ID并构造 SnowFlake 单例。
            // 此处同步等待：注册发生在应用启动阶段（无同步上下文），避免运行期工厂 .Result 阻塞。
            SnowFlake snowFlake = machineIdConfig.InitMachineId().GetAwaiter().GetResult();
            services.AddSingleton(snowFlake);

            services.AddHostedService<SnowFlakeBackgroundService>();
            return services;
        }
    }
}