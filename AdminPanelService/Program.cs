using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AdminPanelService;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:8080");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo
  {
    Title = "AdminPanel API",
    Version = "v1",
    Description = "API для управления товарами"
  });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => {
  c.SwaggerEndpoint("/swagger/v1/swagger.json", "AdminPanel API v1");
  c.RoutePrefix = "swagger";
});

var frontendPath = Path.Combine(builder.Environment.ContentRootPath, "..", "frontend");
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

string GetProductsFilePath() => Path.Combine("..", "SharedData", "Products.json");

List<Product> LoadProducts(){
  var filePath = GetProductsFilePath();
  if (!File.Exists(filePath)) return new List<Product>();
  var json = File.ReadAllText(filePath);
  return JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
}

void SaveProducts(List<Product> products){
  var filePath = GetProductsFilePath();
  var json = JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
  File.WriteAllText(filePath, json);
}

var adminCredentials = new { Username = "admin", Password = "1234" };

app.MapPost("/auth", async (HttpContext context) =>
{
  var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
  var loginData = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);

  if (loginData != null &&
      loginData.TryGetValue("username", out var username) &&
      loginData.TryGetValue("password", out var password) &&
      username == adminCredentials.Username &&
      password == adminCredentials.Password)
  {
    context.Response.StatusCode = 200;
    await context.Response.WriteAsync("OK");
  }
  else
  {
    context.Response.StatusCode = 401;
    await context.Response.WriteAsync("Unauthorized");
  }
});

app.MapPost("/products", (List<CreateProductDto> newProductsDto) =>
{
  var products = LoadProducts();
  var maxId = products.Count > 0 ? products.Max(p => p.Id) : 0;

  var createdProducts = new List<Product>();

  foreach (var dto in newProductsDto)
  {
    var newId = ++maxId;
    var newProduct = new Product
    {
      Id = newId,
      Name = dto.Name,
      Price = dto.Price,
      Description = dto.Description,
      Categories = dto.Categories
    };

    products.Add(newProduct);
    createdProducts.Add(newProduct);
  }

  SaveProducts(products);
  return Results.Ok(createdProducts);
});

app.MapPut("/products/{id}", (int id, UpdateProductDto updatedDto) =>
{
  var products = LoadProducts();
  var product = products.FirstOrDefault(p => p.Id == id);
  if (product == null)
    return Results.NotFound($"Продукт с id={id} не найден");

  product.Name = updatedDto.Name;
  product.Price = updatedDto.Price;
  product.Description = updatedDto.Description;
  product.Categories = updatedDto.Categories;

  SaveProducts(products);
  return Results.Ok(product);
});

app.MapDelete("/products/{id}", (int id) =>
{
  var products = LoadProducts();
  var product = products.FirstOrDefault(p => p.Id == id);
  if (product == null)
    return Results.NotFound($"Продукт с id={id} не найден");

  products.Remove(product);
  SaveProducts(products);
  return Results.Ok($"Продукт с id={id} удален");
});

var connectedClients = new List<(WebSocket socket, string role)>();
var clientsLock = new SemaphoreSlim(1, 1);

app.UseWebSockets();
app.Map("/ws", async context =>
{
  if (context.WebSockets.IsWebSocketRequest)
  {
    var webSocket = await context.WebSockets.AcceptWebSocketAsync();

    var isAdmin = context.Request.Query.ContainsKey("admin");
    var role = isAdmin ? "admin" : "user";

    await clientsLock.WaitAsync();
    try
    {
      connectedClients.Add((webSocket, role));
    }
    finally
    {
      clientsLock.Release();
    }

    await HandleWebSocketCommunication(webSocket, role);
  }
  else
  {
    context.Response.StatusCode = 400;
  }
});

app.Run();

async Task HandleWebSocketCommunication(WebSocket webSocket, string senderRole)
{
  var buffer = new byte[1024 * 4];
  var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

  while (!result.CloseStatus.HasValue)
  {
    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
    Console.WriteLine($"[WebSocket] {senderRole}: {message}");

    var targetRole = senderRole == "admin" ? "user" : "admin";
    var formattedMessage = senderRole == "admin" ? $"Админ: {message}" : $"Пользователь: {message}";

    await clientsLock.WaitAsync();
    try
    {
      foreach (var (socket, role) in connectedClients.ToList())
      {
        if (role == targetRole && socket.State == WebSocketState.Open)
        {
          var responseMessage = Encoding.UTF8.GetBytes(formattedMessage);
          await socket.SendAsync(new ArraySegment<byte>(responseMessage, 0, responseMessage.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);
        }
      }
    }
    finally
    {
      clientsLock.Release();
    }

    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
  }

  await clientsLock.WaitAsync();
  try
  {
    connectedClients.RemoveAll(c => c.socket == webSocket);
  }
  finally
  {
    clientsLock.Release();
  }

  await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}