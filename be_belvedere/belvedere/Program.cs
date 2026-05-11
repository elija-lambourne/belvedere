using belvedere;
using belvedere.Shared;
using belvedere.Util;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

bool isDev = builder.Environment.IsDevelopment();
var configurationManager = builder.Configuration;
var settings = builder.Services.LoadAndConfigureSettings(configurationManager);

builder.AddLogging();
builder.Services.AddApplicationServices(configurationManager, isDev);
builder.Services.AddOpenApi();
builder.Services.AddCors(settings);
builder.Services.AddControllers(o => { o.ModelBinderProviders.Insert(0, new NodaTimeModelBinderProvider()); })
       .AddJsonOptions(o => ConfigureJsonSerialization(o, isDev));
builder.Services.ConfigureAdditionalRouteConstraints();
builder.Services.AddAuthentication(options =>
       {
           options.DefaultScheme = "Cookies";
           options.DefaultChallengeScheme = "oidc";
       })
       .AddCookie("Cookies")
       .AddOpenIdConnect("oidc", options =>
       {
           options.Authority = GetRequiredConfigurationValue(builder.Configuration, "Keycloak:Authority");
           options.ClientId = GetRequiredConfigurationValue(builder.Configuration, "Keycloak:ClientId");
           options.ClientSecret = GetRequiredConfigurationValue(builder.Configuration, "Keycloak:ClientSecret");
           options.RequireHttpsMetadata = builder.Configuration.GetValue("Keycloak:RequireHttpsMetadata", false);
           options.ResponseType = "code"; // Enforces the Standard Flow
           options.SaveTokens = true; // Tells the BFF to hold onto the access/refresh tokens in the session
       });

var app = builder.Build();

// not using HTTPS, because all production backends _have_ to be behind a reverse proxy which will handle SSL termination

app.UseCors(Setup.CorsPolicyName);
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "OpenAPI V1");
    });
}

await app.RunAsync();

return;

static void ConfigureJsonSerialization(JsonOptions options, bool isDev)
{
    JsonConfig.ConfigureJsonSerialization(options.JsonSerializerOptions, isDev);
}

static string GetRequiredConfigurationValue(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException($"Missing required configuration value: {key}");

// used for integration testing
public partial class Program { }
