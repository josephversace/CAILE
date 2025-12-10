using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace IIM.Shared.Models
{
	public class ApplicationUser : IdentityUser
	{
		public bool IsActive { get; set; } = true;
		public bool RequireMfa { get; set; } = true;   // Default ON
		public DateTimeOffset? MfaEnrolledAt { get; set; }
		
		public string Organization { get; set; } = string.Empty;
	}

	public class ApplicationRole : IdentityRole
	{
		// For now this can be empty; add metadata if needed later
	}


	// =======================================================
	// =============== BASIC LOGIN / REGISTER =================
	// =======================================================

	public record RegisterRequest(
		string Email,
		string Password
	);



		public class LoginRequest
		{
			public string Email { get; set; } = "";      // must be settable
			public string Password { get; set; } = "";   // must be settable

			public LoginRequest() { }                    // required for Blazor forms
			public LoginRequest(string email, string password)
			{
				Email = email;
				Password = password;
			}
		}


	public class LoginResponse
	{
		public string? Token { get; set; }
		public bool MfaRequired { get; set; }
		public string? UserId { get; set; }

		public bool Success {get; set; }
	}


	public class MfaLoginRequest
		{
			public string UserId { get; set; } = "";     // must be settable
			public string Code { get; set; } = "";       // must be settable

			public MfaLoginRequest() { }                 // required for binding
			public MfaLoginRequest(string userId, string code)
			{
				UserId = userId;
				Code = code;
			}
		}


	// This is used when enabling/validating authenticator
	public record MfaVerifyRequest(
		string Code
	);



}
