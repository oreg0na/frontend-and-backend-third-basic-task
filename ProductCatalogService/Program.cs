using Microsoft.Extensions.FileProviders;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:3000");
var app = builder.Build();

var frontendPath = Path.Combine(builder.Environment.ContentRootPath, "..", "frontend");

app.UseDefaultFiles(new DefaultFilesOptions
{
  FileProvider = new PhysicalFileProvider(frontendPath)
});

app.UseStaticFiles(new StaticFileOptions
{
  FileProvider = new PhysicalFileProvider(frontendPath)
});

app.MapGet("/api/products", async () =>
{
  var filePath = Path.Combine("..", "SharedData", "Products.json");
  if (!File.Exists(filePath))
    return Results.NotFound("Файл с продуктами не найден");

  var json = await File.ReadAllTextAsync(filePath);
  return Results.Text(json, "application/json");
});

app.Run();
