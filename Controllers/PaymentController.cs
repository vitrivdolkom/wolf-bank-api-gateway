using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Converters;
using WolfBankGateway.Models;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
  private readonly PaymentService.PaymentServiceClient _paymentServiceClient;
  private readonly ILogger<PaymentController> _logger;

  public PaymentController(PaymentService.PaymentServiceClient paymentServiceClient, ILogger<PaymentController> logger)
  {
    _paymentServiceClient = paymentServiceClient;
    _logger = logger;
  }

  [HttpPost("{bank_account_id}/deposit")]
  public async Task<ActionResult<DepositResponse>> Deposit(string bank_account_id, [FromBody] PaymentModel body)
  {
    var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
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
    var response = await _paymentServiceClient.DepositAsync(request, metadata);

    return Ok(response);
  }

  [HttpPost("{bank_account_id}/withdraw")]
  public async Task<ActionResult<WithdrawResponse>> Withdraw(string bank_account_id, [FromBody] PaymentModel body)
  {
    var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
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
      Amount = DecimalValueConverter.ToDecimalValue(body.Amount),
      ToBankAccountId = body.ToBankAccountId
    };
    var response = await _paymentServiceClient.WithdrawAsync(request, metadata);

    return Ok(response);
  }

  [HttpPost("{agreement_id}/credit")]
  public async Task<ActionResult<PayCreditResponse>> PayCredit(string agreement_id)
  {
    var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
      { "Idempotency-Key", Request.Headers["Idempotency-Key"].FirstOrDefault() },
    };

    var request = new PayCreditRequest
    {
      IdempotencyKey = idempotencyKey,
      ClientId = clientId,
      AgreementId = agreement_id
    };
    var response = await _paymentServiceClient.PayCreditAsync(request, metadata);

    return Ok(response);
  }
}