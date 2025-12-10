using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IIM.Infrastructure.Data;

public class EfUserRepository : IUserRepository
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly AuthDbContext _db;

	public EfUserRepository(
		UserManager<ApplicationUser> userManager,
		AuthDbContext db)
	{
		_userManager = userManager;
		_db = db;
	}

	// ─────────────────────────────────────────────────────────────
	// READ
	// ─────────────────────────────────────────────────────────────

	public async Task<ApplicationUser?> GetByIdAsync(string id) =>
		await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

	public async Task<ApplicationUser?> GetByEmailAsync(string email) =>
		await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);

	public async Task<ApplicationUser?> GetByUserNameAsync(string username) =>
		await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserName == username);

	public async Task<List<ApplicationUser>> GetAllAsync() =>
		await _db.Users.AsNoTracking().ToListAsync();

	public async Task<List<ApplicationUser>> SearchAsync(string query)
	{
		if (string.IsNullOrWhiteSpace(query))
			return new List<ApplicationUser>();

		query = query.ToLower();

		return await _db.Users
			.AsNoTracking()
			.Where(u =>
				u.UserName!.ToLower().Contains(query) ||
				u.Email!.ToLower().Contains(query) ||
				u.Organization.ToLower().Contains(query))
			.Take(25)
			.ToListAsync();
	}

	public async Task<bool> ExistsAsync(string email) =>
		await _db.Users.AnyAsync(u => u.Email == email);

	// ─────────────────────────────────────────────────────────────
	// CREATE
	// ─────────────────────────────────────────────────────────────

	public async Task<(bool Success, string[] Errors)> CreateAsync(ApplicationUser user, string password)
	{
		var result = await _userManager.CreateAsync(user, password);
		return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
	}

	// ─────────────────────────────────────────────────────────────
	// UPDATE
	// ─────────────────────────────────────────────────────────────

	public async Task<(bool Success, string[] Errors)> UpdateAsync(ApplicationUser user)
	{
		var existing = await _userManager.FindByIdAsync(user.Id);
		if (existing == null)
			return (false, new[] { "User not found" });

		existing.UserName = user.UserName;
		existing.Email = user.Email;
		existing.PhoneNumber = user.PhoneNumber;
		existing.Organization = user.Organization;
		existing.IsActive = user.IsActive;
		existing.RequireMfa = user.RequireMfa;

		var result = await _userManager.UpdateAsync(existing);
		return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
	}

	public async Task<(bool Success, string[] Errors)> ChangePasswordAsync(
		string userId,
		string currentPassword,
		string newPassword)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null)
			return (false, new[] { "User not found" });

		var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
		return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
	}

	public async Task<(bool Success, string[] Errors)> ResetPasswordAsync(string userId, string newPassword)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null)
			return (false, new[] { "User not found" });

		var token = await _userManager.GeneratePasswordResetTokenAsync(user);
		var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
		return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
	}

	public async Task<bool> SetActiveAsync(string userId, bool isActive)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null)
			return false;

		user.IsActive = isActive;
		var result = await _userManager.UpdateAsync(user);
		return result.Succeeded;
	}

	// ─────────────────────────────────────────────────────────────
	// DELETE
	// ─────────────────────────────────────────────────────────────

	public async Task<bool> DeleteAsync(string userId)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null)
			return false;

		var result = await _userManager.DeleteAsync(user);
		return result.Succeeded;
	}

	// ─────────────────────────────────────────────────────────────
	// ROLES
	// ─────────────────────────────────────────────────────────────

	public async Task<IList<string>> GetRolesAsync(string userId)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null)
			return new List<string>();

		return await _userManager.GetRolesAsync(user);
	}

	public async Task<bool> AddToRoleAsync(string userId, string roleName)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null)
			return false;

		var result = await _userManager.AddToRoleAsync(user, roleName);
		return result.Succeeded;
	}

	public async Task<bool> RemoveFromRoleAsync(string userId, string roleName)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null)
			return false;

		var result = await _userManager.RemoveFromRoleAsync(user, roleName);
		return result.Succeeded;
	}

	public async Task<List<ApplicationUser>> GetUsersInRoleAsync(string roleName)
	{
		var users = await _userManager.GetUsersInRoleAsync(roleName);
		return users.ToList();
	}

	// ─────────────────────────────────────────────────────────────
	// VALIDATION
	// ─────────────────────────────────────────────────────────────

	public async Task<bool> CheckPasswordAsync(string userId, string password)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null)
			return false;

		return await _userManager.CheckPasswordAsync(user, password);
	}
}