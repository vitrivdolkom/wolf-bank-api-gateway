using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Converters;
using WolfBankGateway.Models;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
  private readonly PaymentService.PaymentServiceClient _grpcClient;

  public PaymentController(IConfiguration configuration)
  {
    using var channel = GrpcChannel.ForAddress(configuration.GetConnectionString("ProductEngineGrpcConnection"));
    var client = new PaymentService.PaymentServiceClient(channel);
    _grpcClient = client;
  }

  [HttpPost("{bank_account_id}/deposit")]
  public async Task<ActionResult<DepositResponse>> Deposit(string bank_account_id, [FromBody] PaymentModel body)
  {
    var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
    var clientId = HttpContext.Items["UserId"].ToString();
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
      { "Idempotency-Key", Request.Headers["Idempotency-Key"].FirstOrDefault() },
    };

    var request = new DepositRequest
    {
      ClientId = clientId,
      BankAccountId = bank_account_id,
      IdempotencyKey = idempotencyKey,
      Amount = DecimalValueConverter.ToDecimalValue(body.Amount)
    };
    var response = await _grpcClient.DepositAsync(request, metadata);

    return Ok(response);
  }

  [HttpPost("{bank_account_id}/withdraw")]
  public async Task<ActionResult<WithdrawResponse>> Withdraw(string bank_account_id, [FromBody] PaymentModel body)
  {
    var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
    var clientId = HttpContext.Items["UserId"].ToString();
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
      { "Idempotency-Key", Request.Headers["Idempotency-Key"].FirstOrDefault() },
    };


    var request = new WithdrawRequest
    {
      IdempotencyKey = idempotencyKey,
      ClientId = clientId,
      BankAccountId = bank_account_id,
      Amount = DecimalValueConverter.ToDecimalValue(body.Amount)
    };
    var response = await _grpcClient.WithdrawAsync(request, metadata);

    return Ok(response);
  }

  [HttpGet("{bank_account_id}/credit")]
  public async Task<ActionResult<PayCreditResponse>> GetPayments(string bank_account_id)
  {
    var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
    var clientId = HttpContext.Items["UserId"].ToString();
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
      { "Idempotency-Key", Request.Headers["Idempotency-Key"].FirstOrDefault() },
    };


    var request = new PayCreditRequest
    {
      IdempotencyKey = idempotencyKey,
      ClientId = clientId
    };
    var response = await _grpcClient.PayCreditAsync(request, metadata);

    return Ok(response);
  }
}