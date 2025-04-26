using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Converters;
using WolfBankGateway.Invokers;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class BankAccountController : ControllerBase
{
  private readonly BankAccountService.BankAccountServiceClient _bankAccountClient;
  private readonly TransactionService.TransactionServiceClient _transactionClient;
  private readonly ILogger<BankAccountController> _logger;
  private readonly ResilienceInvoker _resilienceInvoker;

  public BankAccountController(BankAccountService.BankAccountServiceClient bankAccountClient, TransactionService.TransactionServiceClient transactionClient, ILogger<BankAccountController> logger, ResilienceInvoker resilienceInvoker)
  {
    _bankAccountClient = bankAccountClient;
    _transactionClient = transactionClient;
    _logger = logger;
    _resilienceInvoker = resilienceInvoker;
  }

  // POST /api/bank-accounts
  [HttpPost]
  public async Task<ActionResult<BankAccountDto>> CreateBankAccount()
  {
    var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";

    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
      { "Idempotency-Key", Request.Headers["Idempotency-Key"].FirstOrDefault() }
    };

    var request = new CreateBankAccountRequest
    {
      IdempotencyKey = idempotencyKey,
      ClientId = clientId
    };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _bankAccountClient.CreateAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }

  // DELETE /api/bank-accounts/{bank_account_id}
  [HttpDelete("{bank_account_id}")]

  public async Task<ActionResult> CloseBankAccount(string bank_account_id)
  {
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new CloseBankAccountRequest
    {
      BankAccountId = bank_account_id,
      ClientId = clientId
    };
    await _resilienceInvoker.ExecuteAsync(
      () => _bankAccountClient.CloseAsync(request, metadata).ResponseAsync
    );

    return NoContent();
  }

  // GET /api/bank-accounts
  [HttpGet]
  public async Task<ActionResult<List<BankAccountDto>>> GetMyBankAccounts([FromQuery] long? offset, [FromQuery] long? limit, [FromQuery] Guid? userId)
  {
    var clientId = userId.HasValue ? userId.Value.ToString() : HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() ?? "" },
    };
    _logger.LogInformation("metadata {metadata}", metadata);
    var request = new GetAllBankAccountsRequest
    {
      ClientId = clientId,
      Offset = offset ?? 0,
      Limit = limit ?? 100
    };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _bankAccountClient.GetAllAsync(request, metadata).ResponseAsync
    );

    return Ok(response.BankAccounts);
  }

  [HttpGet("all")]
  public async Task<ActionResult<List<BankAccountDto>>> GetAllBankAccounts([FromQuery] long? offset, [FromQuery] long? limit, [FromQuery] Guid? userId)
  {
    var clientId = userId.HasValue ? userId.Value.ToString() : HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() ?? "" },
    };
    _logger.LogInformation("metadata {metadata}", metadata);
    var request = new GetAllBankAccountsRequest
    {
      ClientId = clientId,
      Offset = offset ?? 0,
      Limit = limit ?? 100
    };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _bankAccountClient.GetAllAccountsAsync(request, metadata).ResponseAsync
    );

    return Ok(response.BankAccounts);
  }

  // GET /api/bank-accounts/{bank_account_id}
  [HttpGet("{bank_account_id}")]

  public async Task<ActionResult<BankAccountDto>> GetBankAccount(string bank_account_id)
  {
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetBankAccountRequest
    {
      BankAccountId = bank_account_id,
      ClientId = clientId
    };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _bankAccountClient.GetAsync(request, metadata).ResponseAsync
    );
    _logger.LogInformation("Bank account balance: {Response}", DecimalValueConverter.ToDecimal(response.Balance));

    return Ok(response);
  }

  [HttpGet("{bank_account_id}/history")]

  public async Task<ActionResult<List<Transaction>>> GetHistory(string bank_account_id, [FromQuery] long? offset, [FromQuery] long? limit, [FromQuery] Guid? userId)
  {
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetHistoryRequest
    {
      BankAccountId = bank_account_id,
      ClientId = clientId,
      Offset = offset ?? 0,
      Limit = limit ?? 100
    };
    _logger.LogInformation("Get history request: {Request}", request);
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _transactionClient.GetHistoryAsync(request, metadata).ResponseAsync
    );
    _logger.LogInformation("Response: {Response}", response);
    return Ok(response.Transactions);
  }
}
