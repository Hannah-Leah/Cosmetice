using Cosmetice.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cosmetice.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly CosmeticeContext _context;

        public FavoritesController(CosmeticeContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var favorite = _context.Favorites
                .FirstOrDefault(f =>
                    f.ProductId == productId &&
                    f.UserId == userId);

            if (favorite == null)
            {
                _context.Favorites.Add(new Favorite
                {
                    ProductId = productId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                _context.Favorites.Remove(favorite);
            }

            await _context.SaveChangesAsync();

            return Redirect(Request.Headers["Referer"].ToString());
        }

        public async Task<IActionResult> Index()
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Brand)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Category)
                .Include(f => f.Product)
                    .ThenInclude(p => p.ProductImages)
                .ToListAsync();

            ViewBag.FavoriteIds = favorites
    .Select(f => f.ProductId)
    .ToList();

            return View(favorites);
        }
    }
}