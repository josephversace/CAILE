using System.Collections.Generic;

namespace IIM.Shared.Models;

public class IdentitySetupModel
{
	public AdminAccountModel Admin { get; set; } = new();
	public List<RoleDefinition> Roles { get; set; } = new();
	public List<UserModel> Users { get; set; } = new();
	public SsoConfig Sso { get; set; } = new();
}

public class AdminAccountModel
{
	public string UserName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
	public string ConfirmPassword { get; set; } = string.Empty;
}

public class RoleDefinition
{
	public string Name { get; set; } = string.Empty;
	public bool IsSystem { get; set; } // true for SystemAdmin, Viewer, etc.
}

public class UserModel
{
	public string UserName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;

	// Role names
	public List<string> Roles { get; set; } = new();
}

public class SsoConfig
{
	public string Provider { get; set; } = "None"; // None, AzureAD, Okta, etc.
	public Dictionary<string, string> Settings { get; set; } = new();
}
