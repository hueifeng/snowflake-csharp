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

## License

Apache