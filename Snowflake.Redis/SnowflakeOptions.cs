namespace Snowflake.Redis
{
    public class SnowflakeOptions
    {
        public string ConnectionString { get; set; }

        /// <summary>
        ///     数据中心Id
        /// </summary>
        public long DataCenterId { get; set; }

        /// <summary>
        ///     业务类型名称
        /// </summary>
        public string Name { get; set; }

    }
}
