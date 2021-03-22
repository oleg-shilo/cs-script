using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

public class WeatherForecast
{
	public DateTime Date { get; set; }

	public int TemperatureC { get; set; }

	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

	public string Summary { get; set; }
}

	
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
	static string[] Summaries = new[]
	{
		"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
	};

	 ILogger<WeatherForecastController> _logger;

	public WeatherForecastController(ILogger<WeatherForecastController> logger)
	{
		_logger = logger;
	}

	[HttpGet]
	public IEnumerable<WeatherForecast> Get()
	{
		_logger.LogInformation("Get-WeatherForecast");
		
		var rng = new Random();
		return Enumerable.Range(1, 5).Select(index => new WeatherForecast
		{
			Date = DateTime.Now.AddDays(index),
			TemperatureC = rng.Next(-20, 55),
			Summary = Summaries[rng.Next(Summaries.Length)]
		});
	}
}
