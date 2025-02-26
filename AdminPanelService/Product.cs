using System.Text.Json.Serialization;

namespace AdminPanelService;

public record Product
{
  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("name")]
  public string? Name { get; set; }

  [JsonPropertyName("price")]
  public decimal Price { get; set; }

  [JsonPropertyName("description")]
  public string? Description { get; set; }

  [JsonPropertyName("categories")]
  public List<string>? Categories { get; set; }
}

public record CreateProductDto
{
  [JsonPropertyName("name")]
  public string? Name { get; set; }

  [JsonPropertyName("price")]
  public decimal Price { get; set; }

  [JsonPropertyName("description")]
  public string? Description { get; set; }

  [JsonPropertyName("categories")]
  public List<string>? Categories { get; set; }
}

public record UpdateProductDto
{
  [JsonPropertyName("name")]
  public string? Name { get; set; }

  [JsonPropertyName("price")]
  public decimal Price { get; set; }

  [JsonPropertyName("description")]
  public string? Description { get; set; }

  [JsonPropertyName("categories")]
  public List<string>? Categories { get; set; }
}