using System.Threading.Tasks;

namespace Snowflake.Redis.Cache
{
    public interface ICacheAsync
    {
        /// <summary>
        ///     仅在 key 不存在时设置其值。
        /// </summary>
        /// <param name="key">不含前缀的缓存键。</param>
        /// <param name="value">缓存值。</param>
        /// <returns>是否设置成功。</returns>
        Task<bool> SetNxAsync(string key, object value);

        /// <summary>
        ///     为给定 key 设置过期时间。
        /// </summary>
        /// <param name="key">不含前缀的缓存键。</param>
        /// <param name="seconds">过期秒数。</param>
        /// <returns>是否设置成功。</returns>
        Task<bool> Expire(string key, int seconds);

        /// <summary>
        ///     检查给定 key 是否存在。
        /// </summary>
        /// <param name="key">不含前缀的缓存键。</param>
        /// <returns>是否存在。</returns>
        Task<bool> Exists(string key);

        /// <summary>
        ///     获取 key 的值。
        /// </summary>
        /// <param name="key">不含前缀的缓存键。</param>
        /// <returns>key 对应的值；不存在时为 null。</returns>
        Task<string> Get(string key);

        /// <summary>
        ///     删除指定的 key。
        /// </summary>
        /// <param name="key">不含前缀的缓存键。</param>
        /// <returns>实际删除的 key 数量。</returns>
        Task<long> Del(string key);
    }
}