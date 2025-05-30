using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Invokers;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ApplicationController : ControllerBase
{
  private readonly ApplicationService.ApplicationServiceClient _applicationServiceClient;
  private readonly ResilienceInvoker _resilienceInvoker;

  public ApplicationController(ApplicationService.ApplicationServiceClient applicationServiceClient, ResilienceInvoker resilienceInvoker)
  {
    _applicationServiceClient = applicationServiceClient;
    _resilienceInvoker = resilienceInvoker;
  }

  [HttpGet("{id}")]
  public async Task<ActionResult<ApplicationResponse>> GetApplication(string id)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetApplicationRequest { Id = id };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _applicationServiceClient.GetAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }

  [HttpPost]
  public async Task<ActionResult<ApplicationResponse>> CreateApplication([FromBody] CreateApplicationRequest body)
  {
    var userId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
      { "Idempotency-Key", Request.Headers["Idempotency-Key"].FirstOrDefault() }
    };

    var request = new CreateApplicationRequest
    {
      DisbursementAmount = body.DisbursementAmount,
      OriginationAmount = body.OriginationAmount,
      ProductCode = body.ProductCode,
      ProductVersion = body.ProductVersion,
      Status = body.Status,
      Term = body.Term,
      ToBankAccountId = body.ToBankAccountId,
      Interest = body.Interest,
      UserId = userId
    };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _applicationServiceClient.CreateAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }

  [HttpPut]
  public async Task<ActionResult<ApplicationResponse>> UpdateApplication([FromBody] UpdateApplicationRequest request)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
      { "Idempotency-Key", Request.Headers["Idempotency-Key"].FirstOrDefault() }
    };

    var response = await _resilienceInvoker.ExecuteAsync(
      () => _applicationServiceClient.UpdateAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteApplication(string id)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new DeleteApplicationRequest { Id = id };
    await _resilienceInvoker.ExecuteAsync(
      () => _applicationServiceClient.DeleteAsync(request, metadata).ResponseAsync
    );

    return NoContent();
  }

  [HttpGet]
  public async Task<ActionResult<ListApplicationResponse>> ListApplications([FromQuery] uint page, [FromQuery] uint pageSize, [FromQuery] ApplicationStatus[] status, [FromQuery] Guid? userId)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new ListApplicationRequest
    {
      Page = page,
      PageSize = pageSize,
      UserId = userId.HasValue ? userId.Value.ToString() : HttpContext.Items["UserId"].ToString()
    };
    request.Status.AddRange(status);
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _applicationServiceClient.ListAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }
}
