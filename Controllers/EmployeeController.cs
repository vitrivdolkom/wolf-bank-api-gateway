using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Invokers;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class EmployeeController : ControllerBase
{
  private readonly InternalUserService.InternalUserServiceClient _internalUserServiceClient;
  private readonly ILogger<EmployeeController> _logger;
  private readonly ResilienceInvoker _resilienceInvoker;

  public EmployeeController(InternalUserService.InternalUserServiceClient internalUserServiceClient, ILogger<EmployeeController> logger, ResilienceInvoker resilienceInvoker)
  {
    _internalUserServiceClient = internalUserServiceClient;
    _logger = logger;
    _resilienceInvoker = resilienceInvoker;
  }

  [HttpPost]
  public async Task<ActionResult<CreateEmployeeResponse>> CreateEmployee([FromBody] CreateEmployeeRequest request)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _internalUserServiceClient.CreateEmployeeAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }
}