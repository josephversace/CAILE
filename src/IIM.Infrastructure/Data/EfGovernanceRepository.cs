using IIM.Shared.Interfaces;
using IIM.Shared.Models.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Data;

public class EfGovernanceRepository : IGovernanceRepository
{
    private readonly GovernanceDbContext _context;

    public EfGovernanceRepository(GovernanceDbContext context)
    {
        _context = context;
    }

    // --- Implementation of all interface methods ---

    public async Task<IEnumerable<ClassificationTag>> GetClassificationTagsAsync() => await _context.ClassificationTags.ToListAsync();
    public async Task<IEnumerable<StorageTier>> GetStorageTiersAsync() => await _context.StorageTiers.ToListAsync();
    public async Task<IEnumerable<AccessRole>> GetAccessRolesAsync() => await _context.AccessRoles.ToListAsync();
    public async Task<IEnumerable<AccessControlRule>> GetAccessControlRulesAsync() => await _context.AccessControlRules.Include(r => r.AccessRole).Include(r => r.ClassificationTag).ToListAsync();

    public async Task AddClassificationTagAsync(ClassificationTag tag) => await _context.ClassificationTags.AddAsync(tag);
    public async Task AddStorageTierAsync(StorageTier tier) => await _context.StorageTiers.AddAsync(tier);
    public async Task AddAccessRoleAsync(AccessRole role) => await _context.AccessRoles.AddAsync(role);
    public async Task AddAccessControlRuleAsync(AccessControlRule rule) => await _context.AccessControlRules.AddAsync(rule);

    public async Task<StorageTier> GetStorageTierForClassificationAsync(string classificationTag)
    {
        // In a real system, you would have a more complex mapping, but for now we assume a direct link
        // or a default. This is where the routing logic plugs in.
        // For this example, we find the policy tied to the classification.
        var rule = await _context.ClassificationTags
            .FirstOrDefaultAsync(t => t.Name == classificationTag);

        // This is a simplified lookup. A full implementation would query the PolicyRule table.
        // For now, let's assume a default or simple link.
        return await _context.StorageTiers.FirstOrDefaultAsync(); // Placeholder
    }

    public async Task<FilePermissions> GetPermissionsAsync(string roleName, string classificationTag)
    {
        var rule = await _context.AccessControlRules
            .Include(r => r.AccessRole)
            .Include(r => r.ClassificationTag)
            .FirstOrDefaultAsync(r => r.AccessRole.Name == roleName && r.ClassificationTag.Name == classificationTag);

        return rule?.Permissions ?? FilePermissions.None;
    }
}
