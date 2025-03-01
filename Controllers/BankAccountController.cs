using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BankAccountsController : ControllerBase
{
  private readonly BankAccountService.BankAccountServiceClient _grpcClient;

  public BankAccountsController(BankAccountService.BankAccountServiceClient bankAccountServiceClient)
  {
    using var channel = GrpcChannel.ForAddress("https://localhost:7042");
    var client = new BankAccountService.BankAccountServiceClient(channel);
    _grpcClient = client;
  }

  // POST /api/bank-accounts
  [HttpPost]
  public async Task<IActionResult> CreateBankAccount([FromBody] CreateBankAccountRequest request)
  {
    var response = await _grpcClient.CreateAsync(request);
    return Ok(response);
  }

  // DELETE /api/bank-accounts/{bank_account_id}
  [HttpDelete("{bank_account_id}")]
  public async Task<IActionResult> DeleteBankAccount(string bank_account_id, [FromQuery] string client_id)
  {
    var request = new DeleteBankAccountRequest
    {
      BankAccountId = bank_account_id,
      ClientId = client_id
    };
    await _grpcClient.DeleteAsync(request);
    return Ok();
  }

  // GET /api/bank-accounts
  [HttpGet]
  public async Task<IActionResult> GetAllBankAccounts([FromQuery] string client_id, [FromQuery] long? offset, [FromQuery] long? limit)
  {
    var request = new GetAllBankAccountsRequest
    {
      ClientId = client_id,
      Offset = offset,
      Limit = limit
    };

    var response = await _grpcClient.GetAllAsync(request);
    return Ok(response.BankAccounts);
  }

  // GET /api/bank-accounts/{bank_account_id}
  [HttpGet("{bank_account_id}")]
  public async Task<IActionResult> GetBankAccount(string bank_account_id, [FromQuery] string client_id)
  {
    var request = new GetBankAccountRequest
    {
      BankAccountId = bank_account_id,
      ClientId = client_id
    };

    var response = await _grpcClient.GetAsync(request);
    return Ok(response);
  }
}
