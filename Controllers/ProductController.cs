using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
  private readonly ProductService.ProductServiceClient _productServiceClient;
  private readonly ILogger<ProductController> _logger;

  public ProductController(ProductService.ProductServiceClient productServiceClient, ILogger<ProductController> logger)
  {
    _productServiceClient = productServiceClient;
    _logger = logger;
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
    await _productServiceClient.CreateAsync(request, metadata);

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
    var response = await _productServiceClient.GetAsync(request, metadata);

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

    var response = await _productServiceClient.CalculateAsync(request, metadata);

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
      Limit = limit ?? 10
    };
    var response = await _productServiceClient.GetAllAsync(request, metadata);

    return Ok(response.Products);
  }
}