using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
  private readonly PublicUserService.PublicUserServiceClient _publicUserServiceClient;
  private readonly ILogger<AuthController> _logger;

  public AuthController(PublicUserService.PublicUserServiceClient publicUserServiceClient, ILogger<AuthController> logger)
  {
    _publicUserServiceClient = publicUserServiceClient;
    _logger = logger;
  }

  [HttpPost("login")]
  public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
  {
    var response = await _publicUserServiceClient.LoginAsync(request);

    return Ok(response);
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
