using Cosmetice.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Cosmetice.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CosmeticeContext _context;

        public HomeController(ILogger<HomeController> logger, CosmeticeContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            var items = from s in _context.Products.Include(p => p.Category).Include(p => p.Brand).Include(p => p.ProductImages)
                        select s;

            if (!String.IsNullOrEmpty(searchString))
            {
                items = items.Where(s =>
                    (s.Brand != null && s.Brand.Name.Contains(searchString)) ||
                    (s.Category != null && s.Category.Name.Contains(searchString)) 
                );
            }

     

            // pagination
            // items per page
            int pageSize = 6;
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

            return View(pagedItems);
        }

        public IActionResult Privacy()
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
