using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Invokers;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
  private readonly ProductService.ProductServiceClient _productServiceClient;
  private readonly ILogger<ProductController> _logger;
  private readonly ResilienceInvoker _resilienceInvoker;

  public ProductController(ProductService.ProductServiceClient productServiceClient, ILogger<ProductController> logger, ResilienceInvoker resilienceInvoker)
  {
    _productServiceClient = productServiceClient;
    _logger = logger;
    _resilienceInvoker = resilienceInvoker;
  }

  [HttpPost()]
  public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest body)
  {
    var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
      { "Idempotency-Key", Request.Headers["Idempotency-Key"].FirstOrDefault() },
    };

    var request = new CreateProductRequest
    {
      IdempotencyKey = idempotencyKey,
      ClientId = clientId,
      Code = body.Code,
      MinInterest = body.MinInterest,
      MaxInterest = body.MaxInterest,
      MinOriginationAmount = body.MinOriginationAmount,
      MaxOriginationAmount = body.MaxOriginationAmount,
      MinPrincipalAmount = body.MinPrincipalAmount,
      MaxPrincipalAmount = body.MaxPrincipalAmount,
      MinTerm = body.MinTerm,
      MaxTerm = body.MaxTerm,
    };
    _logger.LogInformation("CreateProduct {request}", request);
    await _resilienceInvoker.ExecuteAsync(
      () => _productServiceClient.CreateAsync(request, metadata).ResponseAsync
    );

    return NoContent();
  }

  [HttpGet("{code}")]
  public async Task<ActionResult<ProductDto>> GetProduct(string code)
  {
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetProductRequest { ClientId = clientId, Code = code };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _productServiceClient.GetAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }

  [HttpPost("calculate")]
  public async Task<ActionResult<CalculateResponse>> Calculate([FromBody] CalculateRequest body)
  {
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new CalculateRequest
    {
      ClientId = clientId,
      Amount = body.Amount,
      Term = body.Term
    };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _productServiceClient.CalculateAsync(request, metadata).ResponseAsync
    );

    return Ok(response);
  }

  [HttpGet("")]
  public async Task<ActionResult<List<ProductDto>>> GetAllProducts([FromQuery] long? offset, [FromQuery] long? limit)
  {
    var clientId = HttpContext.Items["UserId"]?.ToString() ?? "";
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetAllProductRequest
    {
      ClientId = clientId,
      Offset = offset ?? 0,
      Limit = limit ?? 100
    };
    var response = await _resilienceInvoker.ExecuteAsync(
      () => _productServiceClient.GetAllAsync(request, metadata).ResponseAsync
    );

    return Ok(response.Products);
  }
}