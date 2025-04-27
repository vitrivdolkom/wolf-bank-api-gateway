using StackExchange.Redis;
using Asp.Versioning;
using WolfBankGateway.Middlewares;
using WolfBankGateway.Protos.Services;
using Serilog;
using WolfBankGateway.Invokers;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WolfBankGateway;

var outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] 🐺 [TraceId:{TraceId}] {Message:lj}{NewLine}{Exception}";
Log.Logger = new LoggerConfiguration()
.Enrich.FromLogContext()
.WriteTo.Console(Serilog.Events.LogEventLevel.Information, outputTemplate)
.CreateLogger();

try
{
  var builder = WebApplication.CreateBuilder(args);
  builder.Host.UseSerilog();

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
  builder.Services.AddHttpClient();
  builder.Services.AddHttpContextAccessor();
  builder.Services.AddSingleton<ResilienceInvoker>();
  builder.Services.Configure<GatewayConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));

  var jaegerConnectionString = builder.Configuration.GetConnectionString("JaegerConnection");
  builder.Services.AddOpenTelemetry().WithTracing(b =>
  {
    b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("gateway"))
   .AddAspNetCoreInstrumentation()
   .AddHttpClientInstrumentation()
   .AddGrpcClientInstrumentation()
   .AddOtlpExporter(opts =>
   {
     opts.Endpoint = new Uri(jaegerConnectionString);
   });
  });

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

  var creditOriginationGrpcConnectionConnectionString = builder.Configuration.GetConnectionString("CreditOriginationGrpcConnection");
  builder.Services.AddGrpcClient<ApplicationService.ApplicationServiceClient>(options =>
  {
    options.Address = new Uri(creditOriginationGrpcConnectionConnectionString);
  });

  var scoringGrpcConnectionConnectionString = builder.Configuration.GetConnectionString("ScoringGrpcConnection");
  builder.Services.AddGrpcClient<ScoringService.ScoringServiceClient>(options =>
  {
    options.Address = new Uri(scoringGrpcConnectionConnectionString);
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

  var userHttpConnectionString = builder.Configuration.GetConnectionString("UserHttpConnection");
  builder.Services.AddHttpClient("User", httpClient =>
  {
    httpClient.BaseAddress = new Uri(userHttpConnectionString);
  });

  var app = builder.Build();

  if (app.Environment.IsDevelopment())
  {
    app.UseSwagger();
    app.UseSwaggerUI();
  }

  app.UseMiddleware<TraceLoggingMiddleware>();
  app.UseSerilogRequestLogging();
  app.MapControllers();

  app.UseMiddleware<AuthMiddleware>();
  app.UseMiddleware<IdempotencyMiddleware>();
  app.UseMiddleware<RedisCacheMiddleware>();
  app.UseMiddleware<GrpcExceptionHandlingMiddleware>();

  app.Run();
}
catch (Exception exception)
{
  Log.Fatal(exception.Message);
}
finally
{
  Log.Information("Stopped");
  Log.CloseAndFlush();
}
