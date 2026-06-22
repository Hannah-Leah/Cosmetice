using Cosmetice.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cosmetice.Controllers
{
    [Authorize]
    public class CustomListsController : Controller
    {
        private readonly CosmeticeContext _context;

        public CustomListsController(CosmeticeContext context)
        {
            _context = context;
        }

        // MY LISTS

        public async Task<IActionResult> Index()
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var lists = await _context.CustomLists
                .Include(l => l.CustomListItems)
                    .ThenInclude(i => i.Product)
                     .ThenInclude(p => p.ProductImages)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(lists);
        }


        // CREATE LIST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string name,
            string description,
            bool isPublic = false)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var list = new CustomList
            {
                UserId = userId,
                Name = name,
                Description = description,
                IsPublic = isPublic,
                CreatedAt = DateTime.UtcNow
            };

            _context.CustomLists.Add(list);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ADD PRODUCT TO LIST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(
            int customListId,
            int productId)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var list = await _context.CustomLists
                .FirstOrDefaultAsync(x =>
                    x.CustomListId == customListId &&
                    x.UserId == userId);

            if (list == null)
                return Unauthorized();

            bool exists =
                await _context.CustomListItems.AnyAsync(x =>
                    x.CustomListId == customListId &&
                    x.ProductId == productId);

            if (!exists)
            {
                _context.CustomListItems.Add(
                    new CustomListItem
                    {
                        CustomListId = customListId,
                        ProductId = productId,
                        AddedAt = DateTime.UtcNow
                    });
            }

            await _context.SaveChangesAsync();

            return Redirect(Request.Headers["Referer"].ToString());
        }

        // REMOVE PRODUCT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(
            int customListItemId)
        {
            var item =
                await _context.CustomListItems
                    .FindAsync(customListItemId);

            if (item != null)
            {
                _context.CustomListItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}