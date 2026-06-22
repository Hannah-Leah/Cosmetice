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
            var review = await _context.Reviews
                .Include(r => r.ReviewImages)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review == null)
                return NotFound();

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

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
                SkinType = review.SkinType,

                ExistingImages = review.ReviewImages.ToList()
            };

            return View(vm);
        }

        // POST: Reviews/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    EditReviewViewModel model, List<IFormFile> newImages)
        {
            var review = await _context.Reviews
                 .Include(r => r.ReviewImages)
                .FirstOrDefaultAsync(r =>
                    r.ReviewId == model.ReviewId);

            if (review == null)
                return NotFound();

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (review.UserId != userId)
                return Forbid();

            if (model.ImagesToDelete != null)
            {
                var imagesToRemove = review.ReviewImages
                    .Where(x => model.ImagesToDelete.Contains(x.ReviewImageId))
                    .ToList();

                foreach (var image in imagesToRemove)
                {
                    var fullPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        image.ImageUrl.TrimStart('/'));

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }

                    _context.ReviewImages.Remove(image);
                }
            }

            if (newImages != null && newImages.Any())
            {
                foreach (var imageFile in newImages)
                {
                    var fileName =
                        Guid.NewGuid() +
                        Path.GetExtension(imageFile.FileName);

                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/reviewimages",
                        fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    review.ReviewImages.Add(
                        new ReviewImage
                        {
                            ReviewId = review.ReviewId,
                            ImageUrl = "/reviewimages/" + fileName
                        });
                }
            }

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

        // POST: Reviews/Vote

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vote(int reviewId, bool isLike)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var review = await _context.Reviews
                .Include(r => r.ReviewVotes)
                .FirstOrDefaultAsync(r =>
                    r.ReviewId == reviewId);

            if (review == null)
                return NotFound();

            // Prevent voting on your own review
            if (review.UserId == userId)
            {
                TempData["Error"] =
                    "You cannot vote on your own review.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new { id = review.ProductId });
            }

            var existingVote = await _context.ReviewVotes
                .FirstOrDefaultAsync(v =>
                    v.ReviewId == reviewId &&
                    v.UserId == userId);

            if (existingVote == null)
            {
                // First vote

                _context.ReviewVotes.Add(new ReviewVote
                {
                    ReviewId = reviewId,
                    UserId = userId,
                    IsLike = isLike,
                    CreatedAt = DateTime.UtcNow
                });

                if (isLike)
                    review.LikesCount++;
                else
                    review.DislikesCount++;
            }
            else if (existingVote.IsLike != isLike)
            {
                // User switched vote

                if (isLike)
                {
                    review.LikesCount++;
                    review.DislikesCount--;
                }
                else
                {
                    review.LikesCount--;
                    review.DislikesCount++;
                }

                existingVote.IsLike = isLike;
            }
            else
            {
                // Clicking same vote removes it

                if (isLike)
                    review.LikesCount--;
                else
                    review.DislikesCount--;

                _context.ReviewVotes.Remove(existingVote);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "Products",
                new { id = review.ProductId });
        }

        // POST: Reviews/AddReply

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReply(
            int reviewId,
            int productId,
            string content,
            int? parentReplyId)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction(
                    "Details",
                    "Products",
                    new { id = productId });
            }

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reply = new ReviewReply
            {
                ReviewId = reviewId,
                UserId = userId,
                ParentReplyId = parentReplyId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReviewReplies.Add(reply);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "Products",
                new { id = productId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoteAjax(
    int reviewId,
    bool isLike)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var review = await _context.Reviews
                .Include(r => r.ReviewVotes)
                .FirstOrDefaultAsync(r =>
                    r.ReviewId == reviewId);

            if (review == null)
                return NotFound();

            if (review.UserId == userId)
            {
                return BadRequest();
            }

            var existingVote =
                await _context.ReviewVotes
                    .FirstOrDefaultAsync(v =>
                        v.ReviewId == reviewId &&
                        v.UserId == userId);

            if (existingVote == null)
            {
                _context.ReviewVotes.Add(
                    new ReviewVote
                    {
                        ReviewId = reviewId,
                        UserId = userId,
                        IsLike = isLike,
                        CreatedAt = DateTime.UtcNow
                    });

                if (isLike)
                    review.LikesCount++;
                else
                    review.DislikesCount++;
            }
            else if (existingVote.IsLike != isLike)
            {
                if (isLike)
                {
                    review.LikesCount++;
                    review.DislikesCount--;
                }
                else
                {
                    review.LikesCount--;
                    review.DislikesCount++;
                }

                existingVote.IsLike = isLike;
            }
            else
            {
                if (isLike)
                    review.LikesCount--;
                else
                    review.DislikesCount--;

                _context.ReviewVotes.Remove(existingVote);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                likes = review.LikesCount,
                dislikes = review.DislikesCount
            });
        }
    }
}