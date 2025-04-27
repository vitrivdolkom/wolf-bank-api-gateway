using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Invokers;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class FirebaseController : ControllerBase
{
  private readonly FirebaseService.FirebaseServiceClient _firebaseServiceClient;
  private readonly ILogger<FirebaseController> _logger;
  private readonly ResilienceInvoker _resilienceInvoker;

  public FirebaseController(FirebaseService.FirebaseServiceClient firebaseServiceClient, ILogger<FirebaseController> logger, ResilienceInvoker resilienceInvoker)
  {
    _firebaseServiceClient = firebaseServiceClient;
    _logger = logger;
    _resilienceInvoker = resilienceInvoker;
  }

  [HttpPost]
  public async Task<ActionResult<FirebaseRegisterResponse>> Register([FromBody] FirebaseRegisterRequest request)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _firebaseServiceClient.FirebaseRegisterAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }
}