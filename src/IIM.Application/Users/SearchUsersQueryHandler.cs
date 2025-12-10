using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Application.Users
{
	using IIM.Shared.Interfaces;
    using IIM.Shared.Mediator;
    using IIM.Shared.Models;

    public class SearchUsersQueryHandler
		: IRequestHandler<SearchUsersQuery, IEnumerable<ApplicationUser>>
	{
		private readonly IUserRepository _users;

		public SearchUsersQueryHandler(IUserRepository users)
		{
			_users = users;
		}

		public async Task<IEnumerable<ApplicationUser>> Handle(
			SearchUsersQuery request,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Length < 2)
				return Enumerable.Empty<ApplicationUser>();

			return await _users.SearchAsync(request.Query);
		}
	}

}
