using System.Threading.Tasks;

namespace Snowflake.Redis.Cache
{
    public class RedisCacheAsync : ICacheAsync
    {
        /// <inheritdoc />
        public Task<bool> SetNxAsync(string key, object value)
        {
            return RedisHelper.SetNxAsync(key, value);
        }

        /// <inheritdoc />
        public Task<bool> Expire(string key, int seconds)
        {
            return RedisHelper.ExpireAsync(key, seconds);
        }

        /// <inheritdoc />
        public Task<bool> Exists(string key)
        {
            return RedisHelper.ExistsAsync(key);
        }

        /// <inheritdoc />
        public Task<string> Get(string key)
        {
            return RedisHelper.GetAsync(key);
        }

        /// <inheritdoc />
        public Task<long> Del(string key)
        {
            return RedisHelper.DelAsync(key);
        }
    }
}