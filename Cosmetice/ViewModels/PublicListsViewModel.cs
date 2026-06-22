using Cosmetice.Models;

namespace Cosmetice.ViewModels
{
    public class PublicListsViewModel
    {
        public List<CustomList> Lists { get; set; } = new();

        public Dictionary<string, ApplicationUser> Users { get; set; }
            = new();
    }
}