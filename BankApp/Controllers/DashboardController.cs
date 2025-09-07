using System.Security.Claims;
using BankApp.Data;
using BankApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankApp.Controllers
{
    [Authorize(Roles = "User")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public DashboardController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var account = await EnsureUserBankAccount(userId);

            var recent = await _dbContext.Transactions
                .Where(t => t.BankAccountId == account.Id)
                .OrderByDescending(t => t.Date)
                .Take(5)
                .ToListAsync();

            ViewBag.AccountNumber = account.AccountNumber;
            ViewBag.Balance = account.Balance;
            return View(recent);
        }

        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(decimal amount)
        {
            if (amount <= 0)
            {
                ModelState.AddModelError(string.Empty, "Amount must be positive.");
                return View();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var account = await EnsureUserBankAccount(userId);

            account.Balance += amount;
            _dbContext.Transactions.Add(new Transaction
            {
                BankAccountId = account.Id,
                Amount = amount,
                Type = TransactionType.Deposit,
                Description = "Deposit"
            });
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Withdraw()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(decimal amount)
        {
            if (amount <= 0)
            {
                ModelState.AddModelError(string.Empty, "Amount must be positive.");
                return View();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var account = await EnsureUserBankAccount(userId);

            if (amount > account.Balance)
            {
                ModelState.AddModelError(string.Empty, "Insufficient balance.");
                return View();
            }

            account.Balance -= amount;
            _dbContext.Transactions.Add(new Transaction
            {
                BankAccountId = account.Id,
                Amount = amount,
                Type = TransactionType.Withdraw,
                Description = "Withdraw"
            });
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Transfer()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(string recipientAccountNumber, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(recipientAccountNumber))
            {
                ModelState.AddModelError(string.Empty, "Recipient account number is required.");
            }
            if (amount <= 0)
            {
                ModelState.AddModelError(string.Empty, "Amount must be positive.");
            }
            if (!ModelState.IsValid)
            {
                return View();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var sender = await EnsureUserBankAccount(userId);

            if (sender.AccountNumber.Equals(recipientAccountNumber, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "You cannot transfer to your own account.");
                return View();
            }

            var recipient = await _dbContext.BankAccounts
                .FirstOrDefaultAsync(a => a.AccountNumber == recipientAccountNumber);

            if (recipient == null)
            {
                ModelState.AddModelError(string.Empty, "Recipient account not found.");
                return View();
            }

            if (amount > sender.Balance)
            {
                ModelState.AddModelError(string.Empty, "Insufficient balance.");
                return View();
            }

            using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                sender.Balance -= amount;
                recipient.Balance += amount;

                _dbContext.Transactions.Add(new Transaction
                {
                    BankAccountId = sender.Id,
                    Amount = amount,
                    Type = TransactionType.Transfer,
                    Description = $"Transfer to {recipient.AccountNumber}"
                });
                _dbContext.Transactions.Add(new Transaction
                {
                    BankAccountId = recipient.Id,
                    Amount = amount,
                    Type = TransactionType.Transfer,
                    Description = $"Transfer from {sender.AccountNumber}"
                });

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Transfer failed. Please try again.");
                return View();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<BankAccount> EnsureUserBankAccount(string userId)
        {
            var account = await _dbContext.BankAccounts.FirstOrDefaultAsync(a => a.ApplicationUserId == userId);
            if (account != null)
            {
                return account;
            }

            // Create a unique account number: AC + 10-digit
            string newNumber = await GenerateUniqueAccountNumber();
            account = new BankAccount
            {
                ApplicationUserId = userId,
                AccountNumber = newNumber,
                Balance = 0m
            };
            _dbContext.BankAccounts.Add(account);
            //userId -> AppUser, AppUser.account = account
            await _dbContext.SaveChangesAsync();
            return account;
        }

        private async Task<string> GenerateUniqueAccountNumber()
        {
            var random = new Random();
            while (true)
            {
                var digits = string.Concat(Enumerable.Range(0, 10).Select(_ => random.Next(0, 10).ToString()));
                var number = $"AC{digits}";
                var exists = await _dbContext.BankAccounts.AnyAsync(a => a.AccountNumber == number);
                if (!exists)
                {
                    return number;
                }
            }
        }
    }
}


