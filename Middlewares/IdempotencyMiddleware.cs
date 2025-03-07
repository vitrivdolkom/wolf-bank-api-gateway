using StackExchange.Redis;

namespace WolfBankGateway.Middlewares
{
  public class IdempotencyMiddleware
  {
    private static readonly string[] NonIdempotentRequestMethods = ["POST", "PATCH"];
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;

    public IdempotencyMiddleware(RequestDelegate next, IConnectionMultiplexer redis)
    {
      _next = next;
      _redis = redis;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      if (!NonIdempotentRequestMethods.Contains(context.Request.Method))
      {
        await _next(context);
        return;
      }

      string? idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
      if (string.IsNullOrEmpty(idempotencyKey))
      {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Idempotency-Key header is required.");
        return;
      }

      var db = _redis.GetDatabase();
      var cachedIdempotencyKey = await db.StringGetAsync(idempotencyKey);
      if (!cachedIdempotencyKey.IsNullOrEmpty)
      {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsync("Operation has already been processed.");
        return;
      }

      await _next(context);

      if (context.Response.StatusCode > 200 && context.Response.StatusCode < 300)
      {
        await db.StringSetAsync(idempotencyKey, idempotencyKey, TimeSpan.FromDays(1));
      }
    }
  }
}