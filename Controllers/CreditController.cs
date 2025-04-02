using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class CreditController : ControllerBase
{
  private readonly CreditService.CreditServiceClient _creditServiceClient;
  private readonly ScoringService.ScoringServiceClient _scoringServiceClient;

  public CreditController(CreditService.CreditServiceClient creditServiceClient, ScoringService.ScoringServiceClient scoringServiceClient)
  {
    _creditServiceClient = creditServiceClient;
    _scoringServiceClient = scoringServiceClient;
  }

  [HttpGet("{agreementId}")]
  public async Task<ActionResult<GetCreditResponse>> GetCredit(string agreementId)
  {
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetCreditRequest
    {
      ClientId = clientId,
      AgreementId = agreementId
    };
    var response = await _creditServiceClient.GetAsync(request, metadata);

    return Ok(response);
  }

  [HttpGet]
  public async Task<ActionResult<List<GetCreditResponse>>> GetAllCredits([FromQuery] long? offset, [FromQuery] long? limit, [FromQuery] Guid? userId)
  {
    var clientId = userId.HasValue ? userId.Value.ToString() : HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetAllCreditRequest
    {
      ClientId = clientId,
      Offset = offset ?? 0,
      Limit = limit ?? 100
    };
    var response = await _creditServiceClient.GetAllAsync(request, metadata);

    return Ok(response.Credits);
  }

  [HttpGet("{agreementId}/payments")]
  public async Task<ActionResult<GetPaymentResponse>> GetPayments(string agreementId)
  {
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetPaymentsRequest
    {
      ClientId = clientId,
      AgreementId = agreementId
    };
    var response = await _creditServiceClient.GetPaymentsAsync(request, metadata);

    return Ok(response);
  }

  [HttpGet("rate")]
  public async Task<ActionResult<GetCreditRateResponse>> GetRate([FromQuery] Guid? userId)
  {
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetCreditRateRequest
    {
      ClientId = userId.HasValue ? userId.Value.ToString() : clientId,
    };
    var response = await _scoringServiceClient.GetRateAsync(request, metadata);

    return Ok(response);
  }
}