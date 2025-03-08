using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class EmployeeController : ControllerBase
{
  private readonly InternalUserService.InternalUserServiceClient _internalUserServiceClient;
  private readonly ILogger<EmployeeController> _logger;

  public EmployeeController(InternalUserService.InternalUserServiceClient internalUserServiceClient, ILogger<EmployeeController> logger)
  {
    _internalUserServiceClient = internalUserServiceClient;
    _logger = logger;
  }

  [HttpPost]
  public async Task<ActionResult<CreateEmployeeResponse>> CreateEmployee([FromBody] CreateEmployeeRequest request)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var response = await _internalUserServiceClient.CreateEmployeeAsync(request, metadata);

    return Ok(response);
  }
}