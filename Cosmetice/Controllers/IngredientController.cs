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
        public async Task<IActionResult> Search(
    string query,
    int page = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
                return View("Index");

            const int pageSize = 24;

            string url =
                $"https://world.openbeautyfacts.org/cgi/search.pl?" +
                $"search_terms={Uri.EscapeDataString(query)}" +
                $"&search_simple=1" +
                $"&action=process" +
                $"&json=1" +
                $"&page={page}" +
                $"&page_size={pageSize}";

            var response =
                await _httpClient.GetStringAsync(url);

            var data =
                JsonSerializer.Deserialize<OpenBeautyFactsResponse>(response);

            ViewBag.Query = query;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages =
    (int)Math.Ceiling((double)data.Count / pageSize);

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

            if (data == null ||
       data.Product == null )
            {
                TempData["Error"] =
                    "Sorry, this barcode could not be found in the Open Beauty Facts database.";

                return RedirectToAction(nameof(Scanner));
            }



            return View(data.Product);
        }

        public IActionResult Scanner()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ScanBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return RedirectToAction(nameof(Scanner));

            return RedirectToAction(nameof(Details),
                new { code = barcode });
        }
    }
}