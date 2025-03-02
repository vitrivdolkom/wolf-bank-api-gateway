using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class BankAccountController : ControllerBase
{
  private readonly BankAccountService.BankAccountServiceClient _bankAccountClient;
  private readonly TransactionService.TransactionServiceClient _transactionClient;

  public BankAccountController(IConfiguration configuration)
  {
    using var channel = GrpcChannel.ForAddress(configuration.GetConnectionString("ProductEngineGrpcConnection"));
    var bankAccountClient = new BankAccountService.BankAccountServiceClient(channel);
    var transactionClient = new TransactionService.TransactionServiceClient(channel);
    _bankAccountClient = bankAccountClient;
    _transactionClient = transactionClient;
  }

  // POST /api/bank-accounts
  [HttpPost]
  public async Task<ActionResult<BankAccountDto>> CreateBankAccount()
  {
    var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
    var clientId = HttpContext.Items["UserId"].ToString();
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
    var response = await _bankAccountClient.CreateAsync(request, metadata);

    return Ok(response);
  }

  // DELETE /api/bank-accounts/{bank_account_id}
  [HttpDelete("{bank_account_id}")]
  public async Task<ActionResult> DeleteBankAccount(string bank_account_id)
  {
    var clientId = HttpContext.Items["UserId"].ToString();
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new DeleteBankAccountRequest
    {
      BankAccountId = bank_account_id,
      ClientId = clientId
    };
    await _bankAccountClient.DeleteAsync(request, metadata);

    return NoContent();
  }

  // GET /api/bank-accounts
  [HttpGet]
  public async Task<ActionResult<List<BankAccountDto>>> GetAllBankAccounts([FromQuery] long? offset, [FromQuery] long? limit)
  {
    var clientId = HttpContext.Items["UserId"].ToString();
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetAllBankAccountsRequest
    {
      ClientId = clientId,
      Offset = offset,
      Limit = limit
    };
    var response = await _bankAccountClient.GetAllAsync(request, metadata);

    return Ok(response.BankAccounts);
  }

  // GET /api/bank-accounts/{bank_account_id}
  [HttpGet("{bank_account_id}")]
  public async Task<ActionResult<BankAccountDto>> GetBankAccount(string bank_account_id)
  {
    var clientId = HttpContext.Items["UserId"].ToString();
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetBankAccountRequest
    {
      BankAccountId = bank_account_id,
      ClientId = clientId
    };
    var response = await _bankAccountClient.GetAsync(request, metadata);

    return Ok(response);
  }

  [HttpGet("{bank_account_id}/history")]
  public async Task<IActionResult> GetHistory(string bank_account_id, [FromQuery] long? offset, [FromQuery] long? limit)
  {
    var clientId = HttpContext.Items["UserId"].ToString();
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetHistoryRequest
    {
      BankAccountId = bank_account_id,
      ClientId = clientId,
      Offset = offset ?? 0,
      Limit = limit ?? 10
    };
    var response = await _transactionClient.GetHistoryAsync(request, metadata);

    return Ok(response);
  }
}
