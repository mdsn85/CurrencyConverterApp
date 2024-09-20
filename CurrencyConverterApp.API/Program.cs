using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using CurrencyConverterApp.API.Resources;
using CurrencyConverterApp.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ICurrencyConverterService, CurrencyConverterService>();
builder.Services.AddHttpClient(); // Register IHttpClientFactory



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add in-memory caching service
builder.Services.AddMemoryCache();  // This registers IMemoryCache

/*var redisSetting = builder.Configuration.GetSection("Redis");
var redisConnection = redisSetting.GetValue<String>("ConnectionString");

if (string.IsNullOrEmpty(redisConnection))
{
    throw new InvalidOperationException("Redis connection string is missing in the configuration.");
}

builder.Services.AddStackExchangeRedisCache(option =>
{
    option.Configuration = redisConnection;
});*/

var frankfurterApiSetting = builder.Configuration.GetSection("FrankfurterApiSetting").Get<FrankfurterApiSetting>();

if (frankfurterApiSetting == null || string.IsNullOrEmpty(frankfurterApiSetting.BaseUrl))
{
    throw new InvalidOperationException("Frankfurter API settings are missing or incomplete in the configuration file ");
}

//implement httpclient factory and centerlize configration 
//handle transient failure add rendom to the delay to prevent all retries from happening at the same time 
builder.Services.AddHttpClient("FrankfurterApi", client =>
{
    client.BaseAddress = new Uri(frankfurterApiSetting.BaseUrl);
    client.DefaultRequestHeaders.Add("accept", "application/json");

}).AddPolicyHandler(HttpPolicyExtensions
.HandleTransientHttpError()
.WaitAndRetryAsync(3, retryAttemp=>
TimeSpan.FromSeconds(Math.Pow(2,retryAttemp)+new Random().Next(0,1000)/1000.0)));

var app = builder.Build();

// Configure the HTTplease update code accordingly itit iP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
