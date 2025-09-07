using Microsoft.AspNetCore.Identity;

namespace BankApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public virtual BankAccount? BankAccount { get; set; }
    }
}


