using Cosmetice.Models;

namespace Cosmetice.ViewModels
{
    public class EditReviewViewModel
    {
        public int ReviewId { get; set; }

        public int ProductId { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public int Rating { get; set; }

        public string Pros { get; set; }

        public string Cons { get; set; }

        public string SkinType { get; set; }

        public List<ReviewImage> ExistingImages { get; set; }
    = new();

        public List<int> ImagesToDelete { get; set; }
            = new();
    }
}