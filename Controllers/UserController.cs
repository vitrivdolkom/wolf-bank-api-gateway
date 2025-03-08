using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
  private readonly InternalUserService.InternalUserServiceClient _internalUserServiceClient;
  private readonly ILogger<UserController> _logger;

  public UserController(InternalUserService.InternalUserServiceClient internalUserServiceClient, ILogger<UserController> logger)
  {
    _internalUserServiceClient = internalUserServiceClient;
    _logger = logger;
  }

  [HttpPost("{userId}/ban")]
  public async Task<ActionResult<BanUserResponse>> BanUser([FromRoute] string userId)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new BanUserRequest { UserId = userId };
    var response = await _internalUserServiceClient.BanUserAsync(request, metadata);

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
    var response = await _internalUserServiceClient.ListUsersAsync(request, metadata);

    return Ok(response);
  }

  [HttpPost]
  public async Task<ActionResult<CreateUserResponse>> CreateUser([FromBody] CreateUserRequest request)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var response = await _internalUserServiceClient.CreateUserAsync(request, metadata);

    return Ok(response);
  }
}