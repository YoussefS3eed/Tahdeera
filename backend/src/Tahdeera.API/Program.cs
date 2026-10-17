using Tahdeera.Application;
using Tahdeera.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Clean Architecture layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger UI at the root path (http://localhost:5000/)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tahdeera API v1");
    c.RoutePrefix = string.Empty; // Sets Swagger UI as the default homepage
});

// Redirect /swagger to root
app.MapGet("/swagger", () => Results.Redirect("/"));

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new 
{ 
    status = "Healthy", 
    service = "Tahdeera.API",
    timestamp = DateTime.UtcNow 
}));

app.Run();
