<div> 
<p align="center">
    <image src="snowflake.png" width="250" height="250">
 </p>
 <p align="center">An ID Generator for C# based on Snowflake Algorithm (Twitter announced).</p>

  <p align="center">

<a href="https://www.nuget.org/packages/Snowflake.CSharp">
      <image src="https://img.shields.io/nuget/v/Snowflake.CSharp.svg?style=flat-square" alt="nuget">
</a>

<a href="https://www.nuget.org/stats/packages/Snowflake.CSharp?groupby=Version">
      <image src="https://img.shields.io/nuget/dt/Snowflake.CSharp.svg?style=flat-square" alt="stats">
</a>
</p>

</div>

## 说明

Twitter的雪花算法SnowFlake，使用csharp语言实现。


## 安装

```
PM> Install-Package Snowflake.Data -Version 1.1.2
```

## 使用

1. 指定数据中心ID及机器ID.

```csharp
SnowFlake snowFlake=new SnowFlake(datacenterId:1,machineId:1);
```

2. 生成ID

```csharp
var id=snowFlake.NextId();
```

## 高级

1. 用于分布式

```
PM> Install-Package Snowflake.Redis.CSharp
```

2. 在 ConfigureServices() 方法中添加如下代码

```csharp

public void ConfigureServices(IServiceCollection services)
{
  services.AddSnowflakeRedisService(connectionString:"127.0.0.1:6379,allowAdmin=true", 
      option 
            =>Configuration.GetSection("snowFlake").Bind(option)
     );
}
```

分布式雪花ID不同机器ID自动化配置

```
"snowFlake": {
  "dataCenterId": 1,
  "Name": "test"
} 
```

## License

Apache