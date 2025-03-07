namespace WolfBankGateway.Middlewares
{
  public class AuthMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;

    public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger)
    {
      _next = next;
      _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      // TODO validate auth token with request to auth service
      var userId = "550e8400-e29b-41d4-a716-446655440000";
      var role = "client";
      context.Items["UserId"] = userId;
      context.Items["Role"] = role;

      await _next(context);
    }
  }
}
