using System.Text.Json.Serialization;

namespace Cosmetice.Models.API
{
    public class ProductDetailsResponse
    {
        [JsonPropertyName("product")]
        public BeautyProductDto Product { get; set; }
    }
}