using StackExchange.Redis;
using Asp.Versioning;
using WolfBankGateway.Middlewares;
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

var publicUserGrpcConnectionConnectionString = builder.Configuration.GetConnectionString("PublicUserGrpcConnection");
builder.Services.AddGrpcClient<PublicUserService.PublicUserServiceClient>(options =>
{
  options.Address = new Uri(publicUserGrpcConnectionConnectionString);
});

var internalUserGrpcConnectionConnectionString = builder.Configuration.GetConnectionString("InternalUserGrpcConnection");
builder.Services.AddGrpcClient<InternalUserService.InternalUserServiceClient>(options =>
{
  options.Address = new Uri(internalUserGrpcConnectionConnectionString);
});

var productEngineGrpcConnectionString = builder.Configuration.GetConnectionString("ProductEngineGrpcConnection");
builder.Services.AddGrpcClient<BankAccountService.BankAccountServiceClient>(options =>
{
  options.Address = new Uri(productEngineGrpcConnectionString);
});
builder.Services.AddGrpcClient<TransactionService.TransactionServiceClient>(options =>
{
  options.Address = new Uri(productEngineGrpcConnectionString);
});
builder.Services.AddGrpcClient<ProductService.ProductServiceClient>(options =>
{
  options.Address = new Uri(productEngineGrpcConnectionString);
});
builder.Services.AddGrpcClient<CreditService.CreditServiceClient>(options =>
{
  options.Address = new Uri(productEngineGrpcConnectionString);
});
builder.Services.AddGrpcClient<PaymentService.PaymentServiceClient>(options =>
{
  options.Address = new Uri(productEngineGrpcConnectionString);
});

builder.Services.AddGrpcClient<CreditService.CreditServiceClient>(options =>
{
  options.Address = new Uri(productEngineGrpcConnectionString);
});

// TODO add applications grpc
// var applicationsGrpcConnectionString = builder.Configuration.GetConnectionString("ApplicationsGrpcConnection");

// builder.Services.AddGrpcClient<CreditService.CreditServiceClient>(options =>
// {
//   options.Address = new Uri(applicationsGrpcConnectionString);
// });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.MapControllers();

app.UseMiddleware<AuthMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseMiddleware<RedisCacheMiddleware>();
app.UseMiddleware<GrpcExceptionHandlingMiddleware>();

app.Run();