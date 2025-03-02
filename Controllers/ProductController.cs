using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
  private readonly ProductService.ProductServiceClient _grpcClient;

  public ProductController(IConfiguration configuration)
  {
    using var channel = GrpcChannel.ForAddress(configuration.GetConnectionString("ProductEngineGrpcConnection"));
    var client = new ProductService.ProductServiceClient(channel);
    _grpcClient = client;
  }

  [HttpPost("")]
  public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest body)
  {
    var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
    var clientId = HttpContext.Items["UserId"].ToString();
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
      { "Idempotency-Key", Request.Headers["Idempotency-Key"].FirstOrDefault() },
    };


    var request = new CreateProductRequest
    {
      IdempotencyKey = idempotencyKey,
      ClientId = clientId,
      Code = body.Code
    };
    await _grpcClient.CreateAsync(request, metadata);

    return NoContent();
  }

  [HttpGet("{code}")]
  public async Task<ActionResult<ProductDto>> GetProduct(string code)
  {
    var clientId = HttpContext.Items["UserId"].ToString();
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var request = new GetProductRequest { ClientId = clientId, Code = code };
    var response = await _grpcClient.GetAsync(request, metadata);

    return Ok(response);
  }

  [HttpPost("calculate")]
  public async Task<ActionResult<CalculateResponse>> Calculate([FromBody] CalculateRequest body)
  {
    var metadata = new Metadata
    {
      { "Authorization", Request.Headers["Authorization"].FirstOrDefault() },
    };

    var response = await _grpcClient.CalculateAsync(body, metadata);

    return Ok(response);
  }

  [HttpGet("")]
  public async Task<ActionResult<List<ProductDto>>> GetAllProducts([FromQuery] long? offset, [FromQuery] long? limit)
  {
    var clientId = HttpContext.Items["UserId"].ToString();
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
    var response = await _grpcClient.GetAllAsync(request, metadata);

    return Ok(response.Products);
  }
}