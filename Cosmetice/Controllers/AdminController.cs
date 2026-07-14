using Cosmetice.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Cosmetice.ViewModels;

namespace Cosmetice.Controllers
{

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CosmeticeContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, CosmeticeContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // show users 

        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // delete user

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user is an admin
            bool isAdmin =
                await _userManager.IsInRoleAsync(user, "Admin");

            if (isAdmin)
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");

                if (admins.Count <= 1)
                {
                    TempData["Error"] =
                        "You cannot delete the last administrator.";

                    return RedirectToAction(nameof(Index));
                }
            }


            var currentUserId =
                _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["Error"] =
                    "You cannot delete your own account.";

                return RedirectToAction(nameof(Index));
            }

            // Favorites
            var favorites = await _context.Favorites
                .Where(x => x.UserId == id)
                .ToListAsync();

            _context.Favorites.RemoveRange(favorites);

            // Review votes
            var votes = await _context.ReviewVotes
                .Where(x => x.UserId == id)
                .ToListAsync();

            _context.ReviewVotes.RemoveRange(votes);

            // Reviews
            var reviews = await _context.Reviews
      .Include(r => r.ReviewImages)
      .Where(r => r.UserId == id)
      .ToListAsync();

            foreach (var review in reviews)
            {
                foreach (var image in review.ReviewImages)
                {
                    var path = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        image.ImageUrl.TrimStart('/'));

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }
            }

            // replies on reviews

            var reviewIds = reviews
    .Select(r => r.ReviewId)
    .ToList();

            var repliesOnReviews = await _context.ReviewReplies
                .Where(r => reviewIds.Contains(r.ReviewId))
                .ToListAsync();

            _context.ReviewReplies.RemoveRange(repliesOnReviews);
            _context.Reviews.RemoveRange(reviews);

            // Custom Lists
            var lists = await _context.CustomLists
     .Include(l => l.CustomListItems)
     .Where(l => l.UserId == id)
     .ToListAsync();

            var listItems = lists
                .SelectMany(l => l.CustomListItems)
                .ToList();

            _context.CustomListItems.RemoveRange(listItems);
            _context.CustomLists.RemoveRange(lists);

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    string.Join(", ", result.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["Success"] =
                    $"User '{user.UserName}' was deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        // toggle admin role

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = _userManager.GetUserId(User);

            // Prevent an admin from demoting themselves
            if (user.Id == currentUserId)
            {
                TempData["Error"] =
                    "You cannot change your own administrator role.";

                return RedirectToAction(nameof(Index));
            }

            bool isAdmin =
                await _userManager.IsInRoleAsync(user, "Admin");

            if (isAdmin)
            {
                var admins =
                    await _userManager.GetUsersInRoleAsync("Admin");

                if (admins.Count <= 1)
                {
                    TempData["Error"] =
                        "You cannot remove the last administrator.";

                    return RedirectToAction(nameof(Index));
                }

                await _userManager.RemoveFromRoleAsync(user, "Admin");

                if (!await _userManager.IsInRoleAsync(user, "User"))
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                TempData["Success"] =
                    $"{user.UserName} was demoted to User.";
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "Admin");

                TempData["Success"] =
                    $"{user.UserName} was promoted to Admin.";
            }

            return RedirectToAction(nameof(Index));
        }

        // view user details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            return View(user);
        }

        public async Task<IActionResult> Reviews(string sort = "recent")
        {
            var reviews = _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.ReviewImages)
                .Include(r => r.ReviewReplies)
                    .ThenInclude(r => r.InverseParentReply)
                .AsQueryable();

            reviews = sort switch
            {
                "liked" => reviews.OrderByDescending(r => r.LikesCount),
                "rating" => reviews.OrderByDescending(r => r.Rating),
                _ => reviews.OrderByDescending(r => r.CreatedAt)
            };

            var reviewList = await reviews.ToListAsync();

            var userIds = reviewList
                .Select(r => r.UserId)
                .Concat(reviewList
                    .SelectMany(r => r.ReviewReplies)
                    .Select(r => r.UserId))
                .Distinct()
                .ToList();

            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var vm = new AdminReviewsViewModel
            {
                Reviews = reviewList,
                Users = users
            };

            ViewBag.Sort = sort;

            return View(vm);
        }
    }
}
