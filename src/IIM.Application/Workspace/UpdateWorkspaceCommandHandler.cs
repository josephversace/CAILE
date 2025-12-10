using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models;

namespace IIM.Application.Workspaces
{
	public class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, bool>
	{
		private readonly IWorkspaceManager _workspaces;

		public UpdateWorkspaceCommandHandler(IWorkspaceManager workspaces)
		{
			_workspaces = workspaces;
		}

		public async Task<bool> Handle(UpdateWorkspaceCommand request, CancellationToken ct)
		{
			return await _workspaces.UpdateWorkspaceAsync(
				request.WorkspaceId,
				workspace =>
				{
					// BASIC FIELDS
					if (!string.IsNullOrWhiteSpace(request.Name))
						workspace.Name = request.Name;

					if (!string.IsNullOrWhiteSpace(request.Description))
						workspace.Description = request.Description;

					if (request.Type.HasValue)
						workspace.Type = request.Type.Value;

					if (request.IsPublic.HasValue)
						workspace.IsPublic = request.IsPublic.Value;

					if (request.OwnerId.HasValue)
						workspace.OwnerId = request.OwnerId.Value;

					// === USER ADD ===
					if (request.UsersToAdd?.Count > 0)
					{
						foreach (var add in request.UsersToAdd)
						{
							var existing = workspace.Users.FirstOrDefault(u => u.UserId == add.UserId);
							if (existing == null)
							{
								workspace.Users.Add(new WorkspaceUser
								{
									WorkspaceId = workspace.Id,
									UserId = add.UserId,
									Role = add.Role
								});
							}
						}
					}

					// === USER ROLE UPDATE ===
					if (request.UsersToUpdate?.Count > 0)
					{
						foreach (var upd in request.UsersToUpdate)
						{
							var wu = workspace.Users.FirstOrDefault(u => u.UserId == upd.UserId);
							if (wu != null)
								wu.Role = upd.Role;
						}
					}

					// === USER REMOVE ===
					if (request.UsersToRemove?.Count > 0)
					{
						var toRemove = workspace.Users
							.Where(u => request.UsersToRemove.Contains(u.UserId))
							.ToList();

						foreach (var u in toRemove)
							workspace.Users.Remove(u);
					}

					workspace.UpdatedAt = DateTimeOffset.UtcNow;
				},
				ct
			);
		}


	}
}
