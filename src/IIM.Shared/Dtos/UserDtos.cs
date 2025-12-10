using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Dtos
{
	public class UserDto
	{
		public string Id { get; set; } = "";
		public string DisplayName { get; set; } = "";
		public string Email { get; set; } = "";
		public bool IsActive { get; set; }
	}

	public class UserSearchRequest
	{
		public string Query { get; set; } = "";
	}
}
