using System.Net;
using Grpc.Core;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Middlewares
{
  public class AuthMiddleware
  {
    private readonly InternalUserService.InternalUserServiceClient _internalUserServiceClient;
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;

    public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger, InternalUserService.InternalUserServiceClient internalUserServiceClient)
    {
      _next = next;
      _logger = logger;
      _internalUserServiceClient = internalUserServiceClient;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      var endpoint = context.GetEndpoint();

      if (!string.IsNullOrEmpty(endpoint?.DisplayName) && (endpoint.DisplayName.Contains("Login") || endpoint.DisplayName.Contains("Register")))
      {
        await _next(context);
        return;
      }

      var token = context.Request.Headers["Authorization"].FirstOrDefault();
      if (string.IsNullOrEmpty(token))
      {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized");
        return;
      }

      var authorizeRequest = new AuthorizeRequest { };
      var metadata = new Metadata {
        { "Authorization", token }
      };

      var authorizeResponse = await _internalUserServiceClient.AuthorizeAsync(authorizeRequest, metadata);

      if (!authorizeResponse.IsValid)
      {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized");
        return;
      }
      _logger.LogInformation("User {UserId} has been authorized with role {Role}", authorizeResponse.UserId, authorizeResponse.Role);
      context.Items["UserId"] = authorizeResponse.UserId;
      context.Items["Role"] = authorizeResponse.Role;

      await _next(context);
    }
  }
}