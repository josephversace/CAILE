using System;
using System.Collections.Generic;

namespace IIM.Shared.Models.Core
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UserRole
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<RolePermission> Permissions { get; set; } = new();
    }

    public class RolePermission
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
