using System.Collections.Generic;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces;

public interface IRoleRepository
{
	Task<List<ApplicationRole>> GetAllAsync();
	Task<ApplicationRole?> GetByNameAsync(string name);
	Task<bool> CreateAsync(string roleName);
	Task<bool> DeleteAsync(string roleName);
	Task<bool> ExistsAsync(string roleName);
}