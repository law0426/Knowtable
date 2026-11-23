using Knowtable.Api.Services;
using Knowtable.Api.Storage;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IProjectStorage, FileProjectStorage>();
builder.Services.AddSingleton<ICardStorage, FileCardStorage>();
builder.Services.AddScoped<StorageOrchestrator>();
builder.Services.AddScoped<CardOrchestrator>();
builder.Services.AddHttpClient<IOllamaEmbeddingService, OllamaEmbeddingService>(client =>
{
    var baseUrl = builder.Configuration["Ollama:BaseUrl"]
        ?? "http://localhost:11434/";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(2);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
