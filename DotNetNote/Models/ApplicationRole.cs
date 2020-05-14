using Microsoft.AspNetCore.Identity;

namespace DotNetNote.Models
{
    public class ApplicationRole : IdentityRole
    {
        public string Description { get; set; }
    }
}
