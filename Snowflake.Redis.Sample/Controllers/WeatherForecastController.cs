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
        
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,SnowFlake snowFlake)
        {
            _logger = logger;
            this._snowFlake = snowFlake;
        }

        [HttpGet]
        public IEnumerable<long> Get()
        {
            List<long> list=new List<long>();
            for (int i = 0; i < 1000; i++)
            {
                list.Add(_snowFlake.NextId());
            }
            return list;
        }
    }
}
