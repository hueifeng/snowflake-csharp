using System.Threading.Tasks;

namespace Snowflake.Redis.Cache
{
    public interface ICacheAsync
    {
        /// <summary>
        ///     只有在 key 不存在的时候设置key值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<bool> SetNxAsync(string key, object value);

        /// <summary>
        ///     为给定 key 设置过期时间
        /// </summary>
        /// <param name="key">不含prefix前辍</param>
        /// <param name="seconds">过期时间</param>
        /// <returns></returns>
        Task<bool> Expire(string key, int seconds);

        /// <summary>
        ///     检查给定 key 是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<bool> Exists(string key);

        /// <summary>
        ///     获取key的值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<string> Get(string key);
        
        /// <summary>
        /// 删除指定的key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<long> Del(string key);
    }
}
