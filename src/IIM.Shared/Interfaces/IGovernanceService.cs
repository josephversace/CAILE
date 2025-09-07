using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

// Defines the contract for managing the data governance entities.
public interface IGovernanceRepository
{
    Task<IEnumerable<ClassificationTag>> GetClassificationTagsAsync();
    Task<IEnumerable<StorageTier>> GetStorageTiersAsync();
    Task<IEnumerable<AccessRole>> GetAccessRolesAsync();
    Task<IEnumerable<AccessControlRule>> GetAccessControlRulesAsync();

    // Methods to be called by the AI wizard's approval command
    Task AddClassificationTagAsync(ClassificationTag tag);
    Task AddStorageTierAsync(StorageTier tier);
    Task AddAccessRoleAsync(AccessRole role);
    Task AddAccessControlRuleAsync(AccessControlRule rule);

    Task<StorageTier> GetStorageTierForClassificationAsync(string classificationTag);
    Task<FilePermissions> GetPermissionsAsync(string roleName, string classificationTag);
}


