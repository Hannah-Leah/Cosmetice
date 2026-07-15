using System.ComponentModel.DataAnnotations;

namespace Cosmetice.ViewModels
{
    public class CreateReviewViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Please enter a review title.")]
        [StringLength(100)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please write your review.")]
        public string Content { get; set; }

        [Range(1, 5, ErrorMessage = "Please select a rating.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Please tell us what you liked.")]
        public string Pros { get; set; }

        [Required(ErrorMessage = "Please tell us what you didn't like.")]
        public string Cons { get; set; }

        [Required(ErrorMessage = "Please enter your skin type.")]
        public string SkinType { get; set; }
    }
}