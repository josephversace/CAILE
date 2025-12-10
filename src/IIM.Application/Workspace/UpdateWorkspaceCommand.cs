using IIM.Shared.Dtos;
using IIM.Shared.Enums;
using IIM.Shared.Mediator;

public class UpdateWorkspaceCommand : IRequest<bool>
{
	public Guid WorkspaceId { get; set; }

	public string? Name { get; set; }
	public string? Description { get; set; }
	public WorkspaceType? Type { get; set; }
	public bool? IsPublic { get; set; }
	public Guid? OwnerId { get; set; }

	public List<UserRoleUpdate>? UsersToAdd { get; set; }
	public List<UserRoleUpdate>? UsersToUpdate { get; set; }
	public List<string>? UsersToRemove { get; set; }

	public string UpdatedBy { get; set; } = "system";  // ALSO string for Identity users
}
