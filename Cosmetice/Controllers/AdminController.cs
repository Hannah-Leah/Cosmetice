using Cosmetice.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cosmetice.Controllers
{

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private readonly UserManager<IdentityUser> _userManager;
        private readonly CosmeticeContext _context;

        public AdminController(UserManager<IdentityUser> userManager, CosmeticeContext context)
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
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    // Handle deletion failure
                    ModelState.AddModelError("", "Failed to delete user.");
                }
            }
            else
            {
                // Handle user not found
                ModelState.AddModelError("", "User not found.");
            }
            return RedirectToAction("Index");
        }

        // toggle admin role

        [HttpPost]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
                    await _userManager.AddToRoleAsync(user, "User");
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(user, "User");
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
            }
            return RedirectToAction("Index");
        }

        // view user details

        [HttpPost]
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }
    }
}
