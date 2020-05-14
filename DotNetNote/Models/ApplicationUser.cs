using Microsoft.AspNetCore.Identity;

namespace DotNetNote.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Address { get; set; }
    }
}
