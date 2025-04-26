using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Invokers;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
  private readonly InternalUserService.InternalUserServiceClient _internalUserServiceClient;
  private readonly ILogger<UserController> _logger;
  private readonly ResilienceInvoker _resilienceInvoker;

  public UserController(InternalUserService.InternalUserServiceClient internalUserServiceClient, ILogger<UserController> logger, ResilienceInvoker resilienceInvoker)
  {
    _internalUserServiceClient = internalUserServiceClient;
    _logger = logger;
    _resilienceInvoker = resilienceInvoker;
  }

  [HttpPost("{userId}/ban")]
  public async Task<ActionResult<BanUserResponse>> BanUser([FromRoute] string userId)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new BanUserRequest { UserId = userId };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _internalUserServiceClient.BanUserAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }

  [HttpGet]
  public async Task<ActionResult<ListUsersResponse>> GetUsers([FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? search)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new ListUsersRequest
    {
      Page = page ?? 1,
      PageSize = pageSize ?? 10,
      EmailFilter = search ?? ""
    };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _internalUserServiceClient.ListUsersAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }

  [HttpPost]
  public async Task<ActionResult<CreateUserResponse>> CreateUser([FromBody] CreateUserRequest request)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _internalUserServiceClient.CreateUserAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }

  [HttpGet("profile")]
  public async Task<ActionResult<GetProfileResponse>> GetProfile()
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetProfileRequest();
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _internalUserServiceClient.GetProfileAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }
}