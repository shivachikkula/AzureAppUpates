using AzureEnvManager.Api.Authorization;
using AzureEnvManager.Api.Data;
using AzureEnvManager.Api.Hubs;
using AzureEnvManager.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

const string AngularCorsPolicy = "AngularClient";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=azureenvmanager.db"));

builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// The SignalR JS client puts the access token on the query string for WebSocket/SSE transports
// (it cannot set an Authorization header on those), so accept it from there for the hub path only.
builder.Services.Configure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        options.Events ??= new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents();
        options.Events.OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/logs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppRoles.ManagerPolicy, policy => policy.RequireRole(AppRoles.Manager, AppRoles.Admin));
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

builder.Services.AddScoped<IAppAccessService, AppAccessService>();
builder.Services.AddScoped<IAzureApplicationLookup, AzureApplicationLookup>();
builder.Services.AddScoped<IChangeRequestService, ChangeRequestService>();
builder.Services.AddSingleton<IAzureAppServiceClient, AzureAppServiceClient>();
builder.Services.AddSingleton<ILogStreamBroker, AppServiceLogStreamBroker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:4200" };

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData.EnsureSeeded(db);
}

app.UseHttpsRedirection();
app.UseCors(AngularCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LogStreamHub>("/hubs/logs");

app.Run();
