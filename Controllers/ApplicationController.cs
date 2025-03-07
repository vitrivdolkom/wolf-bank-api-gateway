using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ApplicationController : ControllerBase
{
  private readonly ApplicationService.ApplicationServiceClient _applicationServiceClient;

  public ApplicationController(ApplicationService.ApplicationServiceClient applicationServiceClient)
  {
    _applicationServiceClient = applicationServiceClient;
  }

  [HttpGet("{id}")]
  public async Task<ActionResult<ApplicationResponse>> GetApplication(string id)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetApplicationRequest { Id = id };
    var response = await _applicationServiceClient.GetAsync(request, metadata);

    return Ok(response);
  }

  [HttpPost]
  public async Task<ActionResult<ApplicationResponse>> CreateApplication([FromBody] CreateApplicationRequest request)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
      { "Idempotency-Key", Request.Headers["Idempotency-Key"].FirstOrDefault() }
    };

    var response = await _applicationServiceClient.CreateAsync(request, metadata);

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

    var response = await _applicationServiceClient.UpdateAsync(request, metadata);

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
    await _applicationServiceClient.DeleteAsync(request, metadata);

    return NoContent();
  }

  [HttpGet]
  public async Task<ActionResult<ListApplicationResponse>> ListApplications([FromQuery] uint page, [FromQuery] uint pageSize, [FromQuery] ApplicationStatus[] status)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new ListApplicationRequest
    {
      Page = page,
      PageSize = pageSize,
    };
    request.Status.AddRange(status);
    var response = await _applicationServiceClient.ListAsync(request, metadata);

    return Ok(response);
  }
}
