using Newtonsoft.Json;
using StackExchange.Redis;

namespace WolfBankGateway.Middlewares
{
  public class RedisCacheValue
  {
    public int StatusCode { get; set; }
    public string Body { get; set; }
  }

  public class RedisCacheMiddleware
  {
    // TODO add requests to cache
    private static readonly string[] CacheKeysToHandle = [
      GetCacheKey("GET", "WolfBankGateway.Controllers.ApplicationController.GetApplication")
    ];
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheMiddleware> _logger;

    public RedisCacheMiddleware(RequestDelegate next, IConnectionMultiplexer redis, ILogger<RedisCacheMiddleware> logger)
    {
      _next = next;
      _redis = redis;
      _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      var endpoint = context.GetEndpoint();
      var cacheKey = GetCacheKey(context.Request.Method, endpoint?.DisplayName ?? "");

      if (!CacheKeysToHandle.Contains(cacheKey))
      {
        await _next(context);
        return;
      }

      var db = _redis.GetDatabase();
      var cachedResponse = await db.StringGetAsync(cacheKey);
      if (!cachedResponse.IsNullOrEmpty)
      {
        var cachedResponseJson = JsonConvert.DeserializeObject<RedisCacheValue>(cachedResponse);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = cachedResponseJson.StatusCode;
        await context.Response.WriteAsync(cachedResponseJson.Body);
        return;
      }

      Stream originalBodyStream = context.Response.Body;
      using (var memoryStream = new MemoryStream())
      {
        context.Response.Body = memoryStream;
        await _next(context);

        memoryStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
        memoryStream.Seek(0, SeekOrigin.Begin);

        var responseCacheObject = new RedisCacheValue
        {
          StatusCode = context.Response.StatusCode,
          Body = responseBody
        };

        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
          var responseCacheObjectJson = JsonConvert.SerializeObject(responseCacheObject);
          await db.StringSetAsync(cacheKey, responseCacheObjectJson, TimeSpan.FromDays(1));
        }

        await memoryStream.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
      }
    }

    private static string GetCacheKey(string method, string endpointDisplayName) => method + endpointDisplayName;
  }
}
