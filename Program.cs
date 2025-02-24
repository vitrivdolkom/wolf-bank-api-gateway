using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using StackExchange.Redis;
using WolfBankGateway.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Configuration.AddJsonFile("ocelot.json");
builder.Services.AddSwaggerForOcelot(builder.Configuration);
builder.Services.AddOcelot();

var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseSwaggerForOcelotUI(opt =>
{
  opt.PathToSwaggerGenerator = "/swagger/docs";
});

app.UseMiddleware<IdempotencyMiddleware>();
app.UseMiddleware<AuthMiddleware>();
app.UseMiddleware<RedisCacheMiddleware>();

await app.UseOcelot();

app.Run();