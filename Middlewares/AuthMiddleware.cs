using System.Diagnostics;
using System.Net;
using Grpc.Core;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using WolfBankGateway.Invokers;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Middlewares
{
  public class AuthMiddleware
  {
    private readonly InternalUserService.InternalUserServiceClient _internalUserServiceClient;
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ResilienceInvoker _resilienceInvoker;
    private readonly IOptions<GatewayConnectionStrings> _options;

    public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger, InternalUserService.InternalUserServiceClient internalUserServiceClient, IHttpClientFactory httpClientFactory, ResilienceInvoker resilienceInvoker, IOptions<GatewayConnectionStrings> options)
    {
      _next = next;
      _logger = logger;
      _internalUserServiceClient = internalUserServiceClient;
      _httpClientFactory = httpClientFactory;
      _resilienceInvoker = resilienceInvoker;
      _options = options;
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

      var url = QueryHelpers.AddQueryString("/v1/authorize", new Dictionary<string, string?>
        {
            { "client_id", "wem7LcxWDUArXEm-0e4nsEjkwsroaXU_" },
            { "redirect_uri", _options.Value.FrontendUrl },
            { "response_type", "code" }
        });

      var userHttpClient = _httpClientFactory.CreateClient("User");
      var content = await _resilienceInvoker.ExecuteAsync(async () =>
      {
        var response = await userHttpClient.GetAsync(url);
        if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout)
        {
          throw new HttpRequestException($"Server error: {(int)response.StatusCode}");
        }

        return await response.Content.ReadAsStringAsync();
      });

      if (content.Contains("http://localhost:8082/login"))
      {
        context.Response.StatusCode = 401;
        return;
      }


      await _next(context);
    }
  }
}