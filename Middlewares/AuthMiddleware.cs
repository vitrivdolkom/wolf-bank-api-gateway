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
      await _next(context);
    }
  }
}
