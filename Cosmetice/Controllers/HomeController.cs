using Cosmetice.Data;
using Cosmetice.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Cosmetice.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CosmeticeContext _context;
        private readonly ApplicationDbContext _identityContext;

        public HomeController(ILogger<HomeController> logger, CosmeticeContext context, ApplicationDbContext identityContext)
        {
            _logger = logger;
            _context = context;
            _identityContext = identityContext;
        }

        public async Task<IActionResult> Index(string searchString, int? pageNumber, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;

            var items = from s in _context.Products.Include(p => p.Category).Include(p => p.Brand).Include(p => p.ProductImages)
                        select s;

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                items = items.Where(p =>
                    p.Name.Contains(searchString) ||

                    (p.Brand != null &&
                     p.Brand.Name.Contains(searchString)) ||

                    (p.Category != null &&
                     p.Category.Name.Contains(searchString))
                );
            }

            // filtering
            ViewBag.CurrentSort = sortOrder;
            items = sortOrder switch
            {
                "rating" =>
                    items.OrderByDescending(p => p.AverageRating),

                "reviews" =>
                    items.OrderByDescending(p => p.ReviewCount),

                "newest" =>
                    items.OrderByDescending(p => p.ReleaseDate),

                "price-low" =>
                    items.OrderBy(p => p.Price),

                "price-high" =>
                    items.OrderByDescending(p => p.Price),

                _ =>
                    items.OrderByDescending(p => p.CreatedAt)
            };



            // pagination
            // items per page
            int pageSize = 12;
            int pageIndex = pageNumber ?? 1;

            var pagedItems = await items
                .AsNoTracking()
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // total count for page buttons
            int totalItems = await items.CountAsync();
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = pageIndex;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                ViewBag.FavoriteIds = await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.ProductId)
                    .ToListAsync();
            }
            else
            {
                ViewBag.FavoriteIds = new List<int>
                ();
            }

            // top products

            ViewBag.TopProducts = await _context.Products
    .Include(p => p.Brand)
    .Include(p => p.ProductImages)
    .OrderByDescending(p => p.AverageRating)
    .ThenByDescending(p => p.ReviewCount)
    .Take(5)
    .ToListAsync();



            return View(pagedItems);
        }

        public IActionResult AdminPage()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
