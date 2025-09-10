
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Defines the contract for managing all data governance entities, including
    /// individual rules and the versioned framework itself.
    /// </summary>
    public interface IGovernanceRepository
    {
        #region Framework Management

        /// <summary>
        /// Retrieves the currently active and approved governance framework.
        /// </summary>
        Task<GovernanceFramework?> GetCurrentGovernanceFrameworkAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing governance framework entity.
        /// </summary>
        Task UpdateAsync(GovernanceFramework framework, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves a new, complete governance framework, including all its rules, in a single transaction.
        /// This typically involves archiving the old framework and inserting the new set of rules.
        /// </summary>
        Task SaveNewFrameworkAsync(ApproveGovernanceFrameworkCommand command, CancellationToken cancellationToken = default);

        #endregion

        #region Rule Management

        Task<IEnumerable<ClassificationTag>> GetClassificationTagsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<StorageTier>> GetStorageTiersAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<AccessRole>> GetAccessRolesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<AccessControlRule>> GetAccessControlRulesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<DataHandlingRule>> GetDataHandlingRulesAsync(CancellationToken cancellationToken = default);

        Task AddClassificationTagAsync(ClassificationTag tag, CancellationToken cancellationToken = default);
        Task AddStorageTierAsync(StorageTier tier, CancellationToken cancellationToken = default);
        Task AddAccessRoleAsync(AccessRole role, CancellationToken cancellationToken = default);
        Task AddAccessControlRuleAsync(AccessControlRule rule, CancellationToken cancellationToken = default);

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets the required storage tier for a given classification.
        /// </summary>
        Task<StorageTier?> GetStorageTierForClassificationAsync(string classificationTag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the combined permissions for a user's role on a specific data classification.
        /// </summary>
        Task<FilePermissions> GetPermissionsAsync(string roleName, string classificationTag, CancellationToken cancellationToken = default);

        #endregion
    }
}

