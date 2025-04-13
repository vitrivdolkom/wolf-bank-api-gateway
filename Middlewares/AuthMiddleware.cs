using System.Diagnostics;
using System.Net;
using Grpc.Core;
using Microsoft.AspNetCore.WebUtilities;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Middlewares
{
  public class AuthMiddleware
  {
    private readonly InternalUserService.InternalUserServiceClient _internalUserServiceClient;
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;
    private readonly HttpClient _httpClient;

    public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger, InternalUserService.InternalUserServiceClient internalUserServiceClient, HttpClient httpClient)
    {
      _next = next;
      _logger = logger;
      _internalUserServiceClient = internalUserServiceClient;
      _httpClient = httpClient;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      var endpoint = context.GetEndpoint();

      if (!string.IsNullOrEmpty(endpoint?.DisplayName) && (endpoint.DisplayName.Contains("Register") || endpoint.DisplayName.Contains("Token")))
      {
        await _next(context);
        return;
      }

      var token = context.Request.Headers["Authorization"].FirstOrDefault();
      if (!string.IsNullOrEmpty(token))
      {
        var authorizeRequest = new AuthorizeRequest { };
        var metadata = new Metadata
          {
            { "Authorization", token },
          };
        var authorizeResponse = await _internalUserServiceClient.AuthorizeAsync(authorizeRequest, metadata);

        if (authorizeResponse.IsValid)
        {
          context.Items["UserId"] = authorizeResponse.UserId;

          await _next(context);
          return;
        }
      }

      var url = QueryHelpers.AddQueryString("http://localhost:8082/v1/authorize", new Dictionary<string, string?>
        {
            { "client_id", "wem7LcxWDUArXEm-0e4nsEjkwsroaXU_" },
            { "redirect_uri", "http://localhost:3000/"},
            { "response_type", "code" }
        });
      var response = await _httpClient.GetAsync(url);
      var content = await response.Content.ReadAsStringAsync();
      _logger.LogInformation("#content: {content}", content);
      if (content.Contains("http://localhost:8082/login"))
      {
        context.Response.StatusCode = (int)response.StatusCode;
        var redirectUrl = QueryHelpers.AddQueryString("http://localhost:8082/login", new Dictionary<string, string?>
        {
            { "client_id", "wem7LcxWDUArXEm-0e4nsEjkwsroaXU_" },
            { "redirect_uri", "http://localhost:3000/"},
            { "response_type", "code" }
        });
        _logger.LogInformation("#redirectUrl: {redirectUrl}", redirectUrl);
        context.Response.StatusCode = 401;
        return;
      }


      await _next(context);
    }
  }
}