namespace Cosmetice.ViewModels
{
    public class CreateReviewViewModel
    {
        public int ProductId { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public int Rating { get; set; }

        public string Pros { get; set; }

        public string Cons { get; set; }

        public string SkinType { get; set; }
    }
}