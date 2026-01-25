var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Yarp reverse proxy with service discovery
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCors();

// Map reverse proxy routes
app.MapReverseProxy();

app.Run();

// Make Program class accessible to tests
namespace ApiGateway
{
    public partial class Program { }
}

