using System.Text.Json;
using System.IO;
using AdminPanelService;
using HotChocolate;
using HotChocolate.Data;

public class Query
{
  [UseProjection]
  public List<Product> Products()
  {
    var filePath = System.IO.Path.Combine("..", "SharedData", "Products.json");
    if (!File.Exists(filePath)) return new List<Product>();

    var json = File.ReadAllText(filePath);
    return JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
  }
}