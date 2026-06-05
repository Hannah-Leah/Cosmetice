using Cosmetice.Models;
using Cosmetice.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System.Security.Claims;

namespace Cosmetice.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly CosmeticeContext _context;

        public ReviewsController(CosmeticeContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(CreateReviewViewModel model, List<IFormFile> imageFiles)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(
                    "Details",
                    "Products",
                    new { id = model.ProductId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r =>
                    r.ProductId == model.ProductId &&
                    r.UserId == userId);

            if (existingReview != null)
            {
                TempData["Error"] = "You have already reviewed this product.";

                return RedirectToAction(nameof(Details), new { id = model.ProductId });
            }

            var review = new Review
            {
                ProductId = model.ProductId,
                UserId = userId,
                Title = model.Title,
                Content = model.Content,
                Rating = model.Rating,
                Pros = model.Pros,
                Cons = model.Cons,
                SkinType = model.SkinType,
                LikesCount = 0,
                DislikesCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            await _context.SaveChangesAsync();

            // Upload images
            if (imageFiles != null && imageFiles.Any())
            {
                foreach (var imageFile in imageFiles)
                {
                    var fileName =
                        Guid.NewGuid() +
                        Path.GetExtension(imageFile.FileName);

                    var path = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/reviewimages",
                        fileName);

                    using var stream =
                        new FileStream(path, FileMode.Create);

                    await imageFile.CopyToAsync(stream);

                    _context.ReviewImages.Add(new ReviewImage
                    {
                        ReviewId = review.ReviewId,
                        ImageUrl = "/reviewimages/" + fileName
                    });
                }

                await _context.SaveChangesAsync();
            }

            await UpdateProductStatistics(model.ProductId);

            return RedirectToAction(
     "Details",
     "Products",
     new { id = model.ProductId });
        }

        private async Task UpdateProductStatistics(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return;

            product.ReviewCount = product.Reviews.Count;

            product.AverageRating = product.Reviews.Any()
                ? (decimal)product.Reviews.Average(r => r.Rating)
                : 0;

            await _context.SaveChangesAsync();
        }

        // edit review
        // GET: Reviews/Edit/5

        public async Task<IActionResult> Edit(int id)
        {
            var review = await _context.Reviews.FindAsync(id);

            if (review == null)
                return NotFound();

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (review.UserId != userId)
                return Forbid();

            var vm = new EditReviewViewModel
            {
                ReviewId = review.ReviewId,
                ProductId = review.ProductId,
                Title = review.Title,
                Content = review.Content,
                Rating = review.Rating,
                Pros = review.Pros,
                Cons = review.Cons,
                SkinType = review.SkinType
            };

            return View(vm);
        }

        // POST: Reviews/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    EditReviewViewModel model)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r =>
                    r.ReviewId == model.ReviewId);

            if (review == null)
                return NotFound();

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (review.UserId != userId)
                return Forbid();

            review.Title = model.Title;
            review.Content = model.Content;
            review.Rating = model.Rating;
            review.Pros = model.Pros;
            review.Cons = model.Cons;
            review.SkinType = model.SkinType;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await UpdateProductStatistics(
                review.ProductId);

            return RedirectToAction(
                "Details",
                "Products",
                new { id = review.ProductId });
        }

        // Delete review
        // GET: Reviews/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r =>
                    r.ReviewId == id);

            if (review == null)
                return NotFound();

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (review.UserId != userId)
                return Forbid();

            return View(review);
        }

        // POST: Reviews/Delete/5

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
    int id)
        {
            var review = await _context.Reviews
                .Include(r => r.ReviewImages)
                .FirstOrDefaultAsync(r =>
                    r.ReviewId == id);

            if (review == null)
                return NotFound();

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (review.UserId != userId)
                return Forbid();

            int productId = review.ProductId;

            // Delete physical images
            foreach (var image in review.ReviewImages)
            {
                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    image.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.ReviewImages.RemoveRange(
                review.ReviewImages);

            _context.Reviews.Remove(review);

            await _context.SaveChangesAsync();

            await UpdateProductStatistics(productId);

            return RedirectToAction(
                "Details",
                "Products",
                new { id = productId });
        }
    }
}