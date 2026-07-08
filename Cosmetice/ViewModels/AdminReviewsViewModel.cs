using Cosmetice.Models;

namespace Cosmetice.ViewModels
{
    public class AdminReviewsViewModel
    {
        public List<Review> Reviews { get; set; }

        public Dictionary<string, ApplicationUser> Users { get; set; }
    }
}
