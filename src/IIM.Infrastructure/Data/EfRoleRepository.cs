using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IIM.Infrastructure.Data;

public class EfRoleRepository : IRoleRepository
{
	private readonly RoleManager<ApplicationRole> _roleManager;

	public EfRoleRepository(RoleManager<ApplicationRole> roleManager)
	{
		_roleManager = roleManager;
	}

	public async Task<List<ApplicationRole>> GetAllAsync() =>
		await _roleManager.Roles.ToListAsync();

	public async Task<ApplicationRole?> GetByNameAsync(string name) =>
		await _roleManager.FindByNameAsync(name);

	public async Task<bool> CreateAsync(string roleName)
	{
		if (await _roleManager.RoleExistsAsync(roleName))
			return true;

		var result = await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
		return result.Succeeded;
	}

	public async Task<bool> DeleteAsync(string roleName)
	{
		var role = await _roleManager.FindByNameAsync(roleName);
		if (role == null)
			return false;

		var result = await _roleManager.DeleteAsync(role);
		return result.Succeeded;
	}

	public async Task<bool> ExistsAsync(string roleName) =>
		await _roleManager.RoleExistsAsync(roleName);
}