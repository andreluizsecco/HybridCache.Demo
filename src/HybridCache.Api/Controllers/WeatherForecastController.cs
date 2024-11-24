using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace HybridCacheDemo.Api.Controllers;
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    [HttpGet("MemoryCache")]
    public IEnumerable<WeatherForecast> GetMemoryCache([FromServices] IMemoryCache memoryCache)
    {
        if (!memoryCache.TryGetValue("WeatherForecast", out IEnumerable<WeatherForecast> forecasts))
        {
            forecasts = GetData();
            memoryCache.Set("WeatherForecast", forecasts, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            });
        }
        return forecasts;
    }

    [HttpGet("DistributedCache")]
    public async Task<IEnumerable<WeatherForecast>> GetMemoryCache([FromServices] IDistributedCache distributedCache)
    {
        var forecastsString = await distributedCache.GetStringAsync("WeatherForecast");
        if (!string.IsNullOrWhiteSpace(forecastsString))
            return JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(forecastsString)!;

        var forecasts = GetData();

        await distributedCache.SetStringAsync("WeatherForecast",
            JsonSerializer.Serialize(forecasts),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            });

        return forecasts;
    }

    [HttpGet("GetOrCreate_HybridCache")]
    public async Task<IEnumerable<WeatherForecast>> GetHybridCache([FromServices] HybridCache hybridCache)
    {
        return await hybridCache.GetOrCreateAsync("WeatherForecast",
            cancellationToken => ValueTask.FromResult(GetData()),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(5),
                LocalCacheExpiration = TimeSpan.FromSeconds(5),
                //Flags = HybridCacheEntryFlags.DisableLocalCache
            },
            new[] { "tag1", "tag2" });
    }

    [HttpGet("Set_HybridCache")]
    public async Task SetHybridCache([FromServices] HybridCache hybridCache)
    {
        await hybridCache.SetAsync("WeatherForecast",
            GetData(),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(5),
            },
            new[] { "tag1", "tag2" });

        //Remove by tag
        //await hybridCache.RemoveByTagAsync("tag1");
    }

    [HttpGet("Remove_HybridCache")]
    public async Task RemoveHybridCache([FromServices] HybridCache hybridCache)
    {
        await hybridCache.RemoveAsync("WeatherForecast");
        
        //Remove by tag
        //await hybridCache.RemoveByTagAsync("tag1");
    }

    private IEnumerable<WeatherForecast> GetData()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Temperature = Random.Shared.Next(-20, 50),
            Humidity = Random.Shared.Next(5, 100)
        })
        .ToArray();
    }
}
