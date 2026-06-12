using Cosmetice.Models;

namespace Cosmetice.ViewModels
{
    public class ReplyViewModel
    {
        public ReviewReply Reply { get; set; }

        public Dictionary<string, ApplicationUser> Users { get; set; }
    }
}