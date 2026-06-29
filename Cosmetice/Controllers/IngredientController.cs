using Cosmetice.Models.API;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Cosmetice.Controllers
{
    public class IngredientController : Controller
    {
        private readonly HttpClient _httpClient;

        public IngredientController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        // search products

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return View("Index");

            string url =
                $"https://world.openbeautyfacts.org/cgi/search.pl?search_terms={query}&search_simple=1&action=process&json=1";

            var response =
                await _httpClient.GetStringAsync(url);

            var data =
                JsonSerializer.Deserialize<OpenBeautyFactsResponse>(
                    response);

            return View("Results", data);
        }

        // ingredients of products

        [HttpGet]
        public async Task<IActionResult> Details(string code)
        {
            if (string.IsNullOrEmpty(code))
                return NotFound();

            string url =
                $"https://world.openbeautyfacts.org/api/v2/product/{code}.json";

            var response =
                await _httpClient.GetStringAsync(url);

            var data =
                JsonSerializer.Deserialize<ProductDetailsResponse>(
                    response);

            if (data?.Product == null)
                return NotFound();

            return View(data.Product);
        }
    }
}