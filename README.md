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

## Description

Language: English | [中文](README.zh-cn.md)

Twitter's SnowFlake algorithm is implemented using C# language.


## Installation

```
PM> Install-Package Snowflake.Data -Version 1.1.2
```

## Useage

1. Specify the data center ID and machine ID.

```csharp
SnowFlake snowFlake=new SnowFlake(datacenterId:1,machineId:1);
```

2. Generate Id.

```csharp
var id=snowFlake.NextId();
```

## License

Apache