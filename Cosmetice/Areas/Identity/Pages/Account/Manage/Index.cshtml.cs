// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Cosmetice.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Cosmetice.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {

            public string DisplayName { get; set; }

            public string SkinType { get; set; }

            public string Country { get; set; }

            public int? Age { get; set; }

            public string ProfilePictureUrl { get; set; }

            public IFormFile NewProfilePicture { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName =
                await _userManager.GetUserNameAsync(user);

            var phoneNumber =
                await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,

                DisplayName = user.DisplayName,
                SkinType = user.SkinType,
                Country = user.Country,
                Age = user.Age,
                ProfilePictureUrl = user.ProfilePictureUrl
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound(
                    $"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Phone number

            var phoneNumber =
                await _userManager.GetPhoneNumberAsync(user);

            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult =
                    await _userManager.SetPhoneNumberAsync(
                        user,
                        Input.PhoneNumber);

                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage =
                        "Unexpected error when trying to set phone number.";

                    return RedirectToPage();
                }
            }

            // Custom profile fields

            user.DisplayName = Input.DisplayName;
            user.SkinType = Input.SkinType;
            user.Country = Input.Country;
            user.Age = Input.Age;

            // Upload profile picture

            if (Input.NewProfilePicture != null)
            {
                var fileName =
                    Guid.NewGuid() +
                    Path.GetExtension(
                        Input.NewProfilePicture.FileName);

                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/profilepictures");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(
                        uploadsFolder);
                }

                var filePath =
                    Path.Combine(uploadsFolder, fileName);

                using (var stream =
                       new FileStream(filePath, FileMode.Create))
                {
                    await Input.NewProfilePicture
                        .CopyToAsync(stream);
                }

                user.ProfilePictureUrl =
                    "/profilepictures/" + fileName;
            }

            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);

            StatusMessage =
                "Your profile has been updated.";

            return RedirectToPage();
        }
    }
}
