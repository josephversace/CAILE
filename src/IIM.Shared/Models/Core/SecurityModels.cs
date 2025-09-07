// src/IIM.Shared/Models/Core/SecurityModels.cs
using System;
using System.Collections.Generic;

namespace IIM.Shared.Models.Core;

public class IIMUser
{
    public Guid Id { get; set; } // Internal system ID
    public string ExternalId { get; set; } // ID from the SSO provider
    public string AuthProvider { get; set; } // e.g., "AzureAD", "Local"
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public List<Role> Roles { get; set; } = new();
}

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } // e.g., "Investigator", "Attorney", "Admin"
    public string Description { get; set; }
    public List<Permission> Permissions { get; set; } = new();
}

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } // e.g., "case.create", "files.view.sensitive"
}