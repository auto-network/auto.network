using Microsoft.EntityFrameworkCore;
using AutoHost.Data;
using AutoHost.Services;
using AutoHost.Extensions;
using Fido2NetLib;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();


// Configure Entity Framework with SQLite
// Allow database path to be configured via command-line or environment variable
var databasePath = builder.Configuration["DatabasePath"] ?? "autohost.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

// Register services
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<PasskeyService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// Configure Fido2 for passkey support
builder.Services.AddSingleton<IFido2>(sp =>
{
    var fido2Configuration = new Fido2Configuration
    {
        ServerDomain = "localhost",
        ServerName = "Auto",
        Origins = new HashSet<string>
        {
            "http://localhost:5100",
            "https://localhost:5100",
            "http://localhost",
            "https://localhost"
        }
    };
    return new Fido2(fido2Configuration);
});

// Configure CORS to allow requests from any origin (for local development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// Ensure database is created and migrations are applied
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }
}
catch
{
    // Ignore errors during database creation (e.g., in test environment)
}

// Configure the HTTP request pipeline.
// Add custom request logging middleware
app.UseMiddleware<AutoHost.Middleware.RequestLoggingMiddleware>();

app.UseCors("AllowAll");

// Add custom authentication middleware
app.UseMiddleware<AutoHost.Middleware.TokenAuthMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.MapControllers();

// Let the application use the configured URLs (from launchSettings, environment, or command line)
app.Run();

// Make Program accessible to test project
public partial class Program { }
