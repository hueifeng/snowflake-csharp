<div> 
<p align="center">
    <image src="snowflake.png" width="250" height="250">
 </p>
 <p align="center">An ID Generator for C# based on Snowflake Algorithm (Twitter announced).</p>

  <p align="center">

<a href="https://www.nuget.org/packages/Snowflake.CSharp">
      <image src="https://img.shields.io/nuget/v/Snowflake.CSharp.svg?style=flat-square" alt="nuget">
</a>
    
<a href="https://github.com/hueifeng/snowflake-csharp/workflows/.NET%20Core/badge.svg">
      <image src="https://github.com/hueifeng/snowflake-csharp/workflows/.NET%20Core/badge.svg" alt="NETCore">
</a>
    
<a href="https://www.nuget.org/stats/packages/Snowflake.CSharp?groupby=Version">
      <image src="https://img.shields.io/nuget/dt/Snowflake.CSharp.svg?style=flat-square" alt="stats">
</a>
    
<a href="https://raw.githubusercontent.com/hueifeng/snowflake-csharp/master/LICENSE">
    <image src="https://img.shields.io/badge/license-Apache%202-blue.svg" alt="LICENSE">
</a>
</p>

</div>

## Description

Language: English | [中文](README.zh-cn.md)

Twitter's SnowFlake algorithm is implemented using C# language.


## Installation

```
PM> Install-Package Snowflake.CSharp
```

## Useage

1. Specify the data center ID and machine ID.

```csharp
SnowFlake snowFlake = new SnowFlake(datacenterId:1,machineId:1);
```

2. Generate Id.

```csharp
var id = snowFlake.NextId();
```

## Advanced

1. Used in distributed

```
PM> Install-Package Snowflake.Redis.CSharp
```

2. Add the following code at ConfigureServices() method

```csharp

public void ConfigureServices(IServiceCollection services)
{
  services.AddSnowflakeRedisService(connectionString:"127.0.0.1:6379,allowAdmin=true", 
      option 
            =>Configuration.GetSection("snowFlake").Bind(option)
     );
}
```

Distributed snowflake ID automated configuration of different machine ids

```
"snowFlake": {
  "dataCenterId": 1,
  "Name": "test"
} 
```

## License

[Apache](https://github.com/hueifeng/snowflake-csharp/blob/master/LICENSE)

