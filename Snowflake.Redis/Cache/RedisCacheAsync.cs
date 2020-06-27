using System.Threading.Tasks;

namespace Snowflake.Redis.Cache
{
    public class RedisCacheAsync : ICacheAsync
    {
        /// <summary>
        ///     只有在 key 不存在时设置 key 的值
        /// </summary>
        /// <param name="key">不含prefix前缀</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public Task<bool> SetNxAsync(string key, object value)
        {
            return RedisHelper.SetNxAsync(key, value);
        }

        /// <summary>
        ///     为给定 key 设置过期时间
        /// </summary>
        /// <param name="key">不含prefix前缀</param>
        /// <param name="seconds">过期时间</param>
        /// <returns></returns>
        public Task<bool> Expire(string key, int seconds)
        {
            return RedisHelper.ExpireAsync(key, seconds);
        }

        /// <summary>
        ///     检查给定 key 是否存在
        /// </summary>
        /// <param name="key">不含prefix前缀</param>
        /// <returns></returns>
        public Task<bool> Exists(string key)
        {
            return RedisHelper.ExistsAsync(key);
        }

        /// <summary>
        ///     获取key的值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<string> Get(string key)
        {
            return RedisHelper.GetAsync(key);
        }
    }
}
