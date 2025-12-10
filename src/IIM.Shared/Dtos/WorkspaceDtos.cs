using System;
using System.Collections.Generic;
using IIM.Shared.Enums;
using IIM.Shared.Models;

namespace IIM.Shared.Dtos
{
	public class UpdateWorkspaceRequest
	{
		public string? Name { get; set; }
		public string? Description { get; set; }
		public WorkspaceType? Type { get; set; }
		public bool? IsPublic { get; set; }
		public Guid? OwnerId { get; set; }

		public List<UserRoleUpdate>? UsersToAdd { get; set; }
		public List<UserRoleUpdate>? UsersToUpdate { get; set; }
		public List<string>? UsersToRemove { get; set; }
	}

	public class UserRoleUpdate
	{
		public string UserId { get; set; }   // <-- FIXED
		public WorkspaceRole Role { get; set; }
	}



}
