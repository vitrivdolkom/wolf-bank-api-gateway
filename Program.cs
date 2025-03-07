using StackExchange.Redis;
using Asp.Versioning;
using WolfBankGateway.Middlewares;
using Grpc.Net.Client;
using WolfBankGateway.Protos.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvc();
builder.Services.AddApiVersioning(options =>
 {
   options.DefaultApiVersion = new ApiVersion(1);
   options.ApiVersionReader = new UrlSegmentApiVersionReader();
 }).AddApiExplorer(options =>
 {
   options.GroupNameFormat = "'v'V";
   options.SubstituteApiVersionInUrl = true;
 });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

var productEngineGrpcConnectionString = builder.Configuration.GetConnectionString("ProductEngineGrpcConnection");
builder.Services.AddSingleton(GrpcChannel.ForAddress(productEngineGrpcConnectionString));
builder.Services.AddSingleton(provider =>
{
  var channel = provider.GetRequiredService<GrpcChannel>();
  return new BankAccountService.BankAccountServiceClient(channel);
});
builder.Services.AddSingleton(provider =>
{
  var channel = provider.GetRequiredService<GrpcChannel>();
  return new TransactionService.TransactionServiceClient(channel);
});
builder.Services.AddSingleton(provider =>
{
  var channel = provider.GetRequiredService<GrpcChannel>();
  return new ProductService.ProductServiceClient(channel);
});
builder.Services.AddSingleton(provider =>
{
  var channel = provider.GetRequiredService<GrpcChannel>();
  return new ApplicationService.ApplicationServiceClient(channel);
});
builder.Services.AddSingleton(provider =>
{
  var channel = provider.GetRequiredService<GrpcChannel>();
  return new CreditService.CreditServiceClient(channel);
});
builder.Services.AddSingleton(provider =>
{
  var channel = provider.GetRequiredService<GrpcChannel>();
  return new PaymentService.PaymentServiceClient(channel);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.MapControllers();

app.UseMiddleware<IdempotencyMiddleware>();
app.UseMiddleware<AuthMiddleware>();
app.UseMiddleware<RedisCacheMiddleware>();
app.UseMiddleware<GrpcExceptionHandlingMiddleware>();

app.Run();