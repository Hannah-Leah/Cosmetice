using Cosmetice.Data;
using Cosmetice.Models;
using Cosmetice.ViewModels;
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
        private readonly ApplicationDbContext _identityContext; 

        public CustomListsController(CosmeticeContext context, ApplicationDbContext identityContext)
        {
            _context = context;
            _identityContext = identityContext;
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

        // PUBLIC LISTS
        public async Task<IActionResult> PublicLists()
        {
            var lists = await _context.CustomLists
                .Include(l => l.CustomListItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.ProductImages)
                .Where(l => l.IsPublic == true)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var userIds = lists
                .Select(l => l.UserId)
                .Distinct()
                .ToList();

            var users = await _identityContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var vm = new PublicListsViewModel
            {
                Lists = lists,
                Users = users
            };

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var list = await _context.CustomLists
                .Include(l => l.CustomListItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Brand)
                .Include(l => l.CustomListItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(l => l.CustomListId == id);

            if (list == null)
                return NotFound();

            return View(list);
        }

        // delete list

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var list = await _context.CustomLists
                .Include(l => l.CustomListItems)
                .FirstOrDefaultAsync(l =>
                    l.CustomListId == id &&
                    l.UserId == userId);

            if (list == null)
                return Unauthorized();

            _context.CustomListItems.RemoveRange(
                list.CustomListItems);

            _context.CustomLists.Remove(list);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"List '{list.Name}' deleted.";

            return RedirectToAction(nameof(Index));
        }
    }
}