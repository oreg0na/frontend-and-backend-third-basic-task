using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Linq;

using AdminPanelService;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:8080");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
  c.SwaggerEndpoint("/swagger/v1/swagger.json", "AdminPanel API v1");
  c.RoutePrefix = "swagger";
});

string GetProductsFilePath()
{
  return Path.Combine("..", "SharedData", "Products.json");
}

List<Product> LoadProducts()
{
  var filePath = GetProductsFilePath();
  if (!File.Exists(filePath))
    return new List<Product>();

  var json = File.ReadAllText(filePath);
  return JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
}

void SaveProducts(List<Product> products)
{
  var filePath = GetProductsFilePath();
  var json = JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
  File.WriteAllText(filePath, json);
}

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

// PUT /products/{id} (Редактирование товара)
app.MapPut("/products/{id}", (int id, UpdateProductDto updatedDto) =>
{
  var products = LoadProducts();
  var product = products.FirstOrDefault(p => p.Id == id);
  if (product == null)
  {
    return Results.NotFound($"Продукт с id={id} не найден");
  }

  product.Name = updatedDto.Name;
  product.Price = updatedDto.Price;
  product.Description = updatedDto.Description;
  product.Categories = updatedDto.Categories;

  SaveProducts(products);
  return Results.Ok(product);
});

// DELETE /products/{id} (Удаление товара)
app.MapDelete("/products/{id}", (int id) =>
{
  var products = LoadProducts();
  var product = products.FirstOrDefault(p => p.Id == id);
  if (product == null)
  {
    return Results.NotFound($"Продукт с id={id} не найден");
  }

  products.Remove(product);
  SaveProducts(products);
  return Results.Ok($"Продукт с id={id} удален");
});

app.Run();
