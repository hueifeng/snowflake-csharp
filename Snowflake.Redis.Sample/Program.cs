using Snowflake;
using Snowflake.Redis;
using Snowflake.Redis.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSnowflakeRedisService("127.0.0.1:6379,allowAdmin=true",
    options => builder.Configuration.GetSection("snowFlake").Bind(options));

var app = builder.Build();
app.MapGet("/", (MachineIdConfig machineIdConfig) =>
{
    return machineIdConfig.GetKey();
});

app.MapGet("/list", (SnowFlake snowFlake) =>
{
    List<long> list = new List<long>();
    for (int i = 0; i < 1000; i++)
    {
        list.Add(snowFlake.NextId());
    }
    return list;
});


app.Run();