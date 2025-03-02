using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Data;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:3000");

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>().AddProjections();

var app = builder.Build();

app.UseRouting();

var frontendPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "..", "frontend");
if (Directory.Exists(frontendPath))
{
  app.UseDefaultFiles(new DefaultFilesOptions
  {
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath)
  });

  app.UseStaticFiles(new StaticFileOptions
  {
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath)
  });
}

app.MapGraphQL();

app.MapGet("/api/products", async () =>
{
  var filePath = System.IO.Path.Combine("..", "SharedData", "Products.json");
  if (!File.Exists(filePath))
    return Results.NotFound("Файл с продуктами не найден");

  var json = await File.ReadAllTextAsync(filePath);
  return Results.Text(json, "application/json");
});

app.Run();
