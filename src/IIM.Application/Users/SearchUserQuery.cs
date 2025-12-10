using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Mediator;
using IIM.Shared.Models;

namespace IIM.Application.Users
{

	public record SearchUsersQuery(string Query) : IQuery<IEnumerable<ApplicationUser>>;

}
