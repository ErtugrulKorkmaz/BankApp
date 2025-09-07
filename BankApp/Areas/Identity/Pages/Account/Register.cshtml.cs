using System.ComponentModel.DataAnnotations;
using BankApp.Data;
using BankApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BankApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            public string FullName { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email, FullName = Input.FullName, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                // Create bank account for the user
                var accountNumber = await GenerateUniqueAccountNumber();
                _db.BankAccounts.Add(new BankAccount
                {
                    ApplicationUserId = user.Id,
                    AccountNumber = accountNumber,
                    Balance = 0m
                });
                await _db.SaveChangesAsync();
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(Url.Content("~/"));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        private async Task<string> GenerateUniqueAccountNumber()
        {
            var random = new Random();
            while (true)
            {
                var digits = string.Concat(Enumerable.Range(0, 10).Select(_ => random.Next(0, 10).ToString()));
                var number = $"AC{digits}";
                var exists = await _db.BankAccounts.AnyAsync(a => a.AccountNumber == number);
                if (!exists)
                {
                    return number;
                }
            }
        }
    }
}


