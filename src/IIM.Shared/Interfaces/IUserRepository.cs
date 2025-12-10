using System.Collections.Generic;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces;

public interface IUserRepository
{
	// Read
	Task<ApplicationUser?> GetByIdAsync(string id);
	Task<ApplicationUser?> GetByEmailAsync(string email);
	Task<ApplicationUser?> GetByUserNameAsync(string username);
	Task<List<ApplicationUser>> GetAllAsync();
	Task<List<ApplicationUser>> SearchAsync(string query);

	// Create
	Task<(bool Success, string[] Errors)> CreateAsync(ApplicationUser user, string password);

	// Update
	Task<(bool Success, string[] Errors)> UpdateAsync(ApplicationUser user);
	Task<(bool Success, string[] Errors)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
	Task<(bool Success, string[] Errors)> ResetPasswordAsync(string userId, string newPassword);
	Task<bool> SetActiveAsync(string userId, bool isActive);

	// Delete
	Task<bool> DeleteAsync(string userId);

	// Roles
	Task<IList<string>> GetRolesAsync(string userId);
	Task<bool> AddToRoleAsync(string userId, string roleName);
	Task<bool> RemoveFromRoleAsync(string userId, string roleName);
	Task<List<ApplicationUser>> GetUsersInRoleAsync(string roleName);

	// Validation
	Task<bool> CheckPasswordAsync(string userId, string password);
	Task<bool> ExistsAsync(string email);
}