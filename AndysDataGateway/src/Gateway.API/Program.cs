using Gateway.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Add API Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Inject our Infrastructure and Core services
// Make sure to add "GeminiApiKey" to your appsettings.json or User Secrets!
var geminiApiKey = builder.Configuration["GeminiApiKey"];
if (string.IsNullOrEmpty(geminiApiKey))
{
    throw new InvalidOperationException("Gemini API Key is missing from configuration.");
}

builder.Services.AddInfrastructureServices(geminiApiKey);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseDefaultFiles(); // Looks for index.html
app.UseStaticFiles();  // Serves files from the wwwroot folder

app.Run();