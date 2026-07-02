using System.Text.Json.Serialization;

namespace Cosmetice.Models.API
{
    public class OpenBeautyFactsResponse
    {
        [JsonPropertyName("products")]
        public List<BeautyProductDto> Products { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }
    }

    public class BeautyProductDto
    {

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; }

        [JsonPropertyName("brands")]
        public string Brands { get; set; }

        [JsonPropertyName("ingredients_text")]
        public string Ingredients { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }
    }
}