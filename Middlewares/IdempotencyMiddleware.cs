namespace WolfBankGateway.Middlewares
{
  public class IdempotencyMiddleware
  {
    private static readonly string[] NonIdempotentRequestMethods = ["POST", "PATCH"];
    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      string? idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();

      if (string.IsNullOrEmpty(idempotencyKey) && NonIdempotentRequestMethods.Contains(context.Request.Method))
      {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Idempotency-Key header is required.");
        return;
      }

      await _next(context);
    }
  }
}
