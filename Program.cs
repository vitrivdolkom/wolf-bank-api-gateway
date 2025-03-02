using StackExchange.Redis;
using Asp.Versioning;
using WolfBankGateway.Middlewares;

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

app.Run();