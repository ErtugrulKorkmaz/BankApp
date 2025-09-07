using BankApp.Data;
using BankApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankApp.Controllers
{
	[Authorize(Roles = "Admin")]
	public class AdminController : Controller
	{
		private readonly ApplicationDbContext _db;

		public AdminController(ApplicationDbContext db)
		{
			_db = db;
		}

		public async Task<IActionResult> Users()
		{
			var admins = await (from u in _db.Users
								  join ur in _db.UserRoles on u.Id equals ur.UserId
								  join r in _db.Roles on ur.RoleId equals r.Id
								  where r.Name == "Admin"
								  select u.Id).ToListAsync();

			var users = await _db.Users
				.Where(u => !admins.Contains(u.Id))
				.Select(u => new { u.Id, u.FullName, u.Email })
				.ToListAsync();

			return View(users);
		}

		public async Task<IActionResult> UserDetails(string id)
		{
			if (string.IsNullOrEmpty(id)) return NotFound();
			var user = await _db.Users.Include(u => u.BankAccount).FirstOrDefaultAsync(u => u.Id == id);
			if (user == null) return NotFound();
			return View(user);
		}
	}
}
