using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Snowflake.Redis.Sample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly SnowFlake _snowFlake;
        private readonly MachineIdConfig _machineIdConfig;

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, SnowFlake snowFlake,
            MachineIdConfig machineIdConfig)
        {
            _logger = logger;
            this._snowFlake = snowFlake;
            this._machineIdConfig = machineIdConfig;
        }

        [HttpGet]
        public string Get()
        {
            return _machineIdConfig.GetKey();
            //return  $"MachineId：{_snowFlake.GetMachineId()}，Id：{_snowFlake.NextId()}";
        }

        
        [HttpGet("list")]
        public IEnumerable<long> GetList()
        {
            List<long> list=new List<long>();
            for (int i = 0; i < 1000; i++)
            {
                list.Add(_snowFlake.NextId());
            }
            return list;
        } }
}