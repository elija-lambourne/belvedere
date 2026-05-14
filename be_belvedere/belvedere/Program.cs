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
builder.Services.AddControllersWithViews(o => 
       { 
           o.ModelBinderProviders.Insert(0, new NodaTimeModelBinderProvider()); 
           // Globally require CSRF tokens for all state-changing requests (POST, PUT, DELETE, PATCH)
           o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
       })
       .AddJsonOptions(o => ConfigureJsonSerialization(o, isDev));
builder.Services.ConfigureAdditionalRouteConstraints();

builder.Services.AddAntiforgery(options =>
{
    // Industry standard header name for CSRF tokens (works natively with Axios)
    options.HeaderName = "X-XSRF-TOKEN";
    // We enforce SameSite=Strict for BFF to prevent CSRF.
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    options.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication(options =>
       {
           options.DefaultScheme = "Cookies";
           options.DefaultChallengeScheme = "OpenIdConnect";
       })
       .AddCookie("Cookies", options =>
       {
           options.Cookie.Name = "__Host-belvedere-session";
           options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
           options.Cookie.HttpOnly = true;
           options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
           // If running locally without HTTPS, this __Host- prefix will fail to set unless we are on secure localhost.
           // Removing the prefix to be safe for dev without HTTPS, but keeping secure defaults.
           if (isDev)
           {
               options.Cookie.Name = "belvedere-session";
               options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
           }
       })
       .AddOpenIdConnect("OpenIdConnect", options =>
       {
           options.Authority = GetRequiredConfigurationValue(builder.Configuration, "Keycloak:Authority");
           options.ClientId = GetRequiredConfigurationValue(builder.Configuration, "Keycloak:ClientId");
           options.ClientSecret = GetRequiredConfigurationValue(builder.Configuration, "Keycloak:ClientSecret");
           options.RequireHttpsMetadata = builder.Configuration.GetValue("Keycloak:RequireHttpsMetadata", false);
           options.ResponseType = "code"; // Enforces the Standard Flow
           options.SaveTokens = true; // Tells the BFF to hold onto the access/refresh tokens in the session
           options.GetClaimsFromUserInfoEndpoint = true;
           
           // Ensure the redirect URI is correct
           options.Events.OnRedirectToIdentityProvider = context =>
           {
               // Keycloak standard requires this to ensure we don't accidentally send cookies over HTTP when proxying
               return Task.CompletedTask;
           };
       });

var app = builder.Build();

// not using HTTPS, because all production backends _have_ to be behind a reverse proxy which will handle SSL termination

app.UseCors(Setup.CorsPolicyName);
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

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
