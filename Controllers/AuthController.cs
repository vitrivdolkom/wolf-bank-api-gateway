using System.Text;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
  private readonly PublicUserService.PublicUserServiceClient _publicUserServiceClient;
  private readonly ILogger<AuthController> _logger;
  private readonly IHttpClientFactory _httpClientFactory;


  public AuthController(PublicUserService.PublicUserServiceClient publicUserServiceClient, ILogger<AuthController> logger, IHttpClientFactory httpClientFactory)
  {
    _publicUserServiceClient = publicUserServiceClient;
    _logger = logger;
    _httpClientFactory = httpClientFactory;
  }

  [HttpPost("token")]
  public async Task<ActionResult<TokenResponse>> Token([FromBody] TokenRequest body)
  {
    var userHttpClient = _httpClientFactory.CreateClient("User");
    var response = await userHttpClient.PostAsync("/v1/token", JsonContent.Create(body));
    var content = await response.Content.ReadFromJsonAsync<TokenResponse>();

    response.EnsureSuccessStatusCode();

    return Ok(content);
  }

  [HttpPost("register")]
  public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
  {
    var response = await _publicUserServiceClient.RegisterAsync(request);

    return Ok(response);
  }


  [HttpPost("logout")]

  public async Task<ActionResult<LogoutResponse>> Logout()
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new LogoutRequest { };
    var response = await _publicUserServiceClient.LogoutAsync(request, metadata);

    return Ok(response);
  }

  [HttpGet("refresh")]
  public async Task<ActionResult<RevalidateResponse>> Refresh()
  {
    var token = Request.Headers["Authorization"].FirstOrDefault();

    var metadata = new Metadata {
      { "Authorization", token }
    };

    var request = new RevalidateRequest { };
    var response = await _publicUserServiceClient.RevalidateAsync(request, metadata);

    return Ok(response);
  }
}
