using Cosmetice.Models;

namespace Cosmetice.ViewModels;

public class ProductDetailsViewModel
{
    public Product Product { get; set; }

    public Dictionary<string, ApplicationUser> Users { get; set; }
}