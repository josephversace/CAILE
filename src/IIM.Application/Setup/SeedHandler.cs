using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Setup;

public class SeedHandler : IRequestHandler<SeedCommand, SeedResult>
{
	private readonly IConfiguration _config;
	private readonly IUserRepository _userRepository;
	private readonly IRoleRepository _roleRepository;
	private readonly ILogger<SeedHandler> _logger;

	public SeedHandler(
		IConfiguration config,
		IUserRepository userRepository,
		IRoleRepository roleRepository,
		ILogger<SeedHandler> logger)
	{
		_config = config;
		_userRepository = userRepository;
		_roleRepository = roleRepository;
		_logger = logger;
	}

	public async Task<SeedResult> Handle(SeedCommand request, CancellationToken ct)
	{
		// 1. Validate token
		if (_config.GetValue<bool>("Setup:Completed"))
			return new SeedResult { Success = false, Error = "Setup already completed" };

		var expectedToken = _config.GetValue<string>("Setup:Token");
		if (string.IsNullOrEmpty(expectedToken) || request.Token != expectedToken)
			throw new UnauthorizedAccessException("Invalid setup token");

		try
		{
			// 2. Create default roles
			await _roleRepository.CreateAsync("Admin");
			await _roleRepository.CreateAsync("User");
			await _roleRepository.CreateAsync("Viewer");
			_logger.LogInformation("Default roles created");

			// 3. Create admin user
			if (!await _userRepository.ExistsAsync(request.AdminEmail))
			{
				var adminUser = new ApplicationUser
				{
					UserName = request.AdminEmail.Split('@')[0],
					Email = request.AdminEmail,
					EmailConfirmed = true,
					IsActive = true,
					RequireMfa = false,
					Organization = "System"
				};

				var (success, errors) = await _userRepository.CreateAsync(adminUser, request.AdminPassword);

				if (!success)
				{
					_logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", errors));
					return new SeedResult { Success = false, Error = string.Join(", ", errors) };
				}

				await _userRepository.AddToRoleAsync(adminUser.Id, "Admin");
				_logger.LogInformation("Admin user created: {Email}", request.AdminEmail);
			}

			// 4. Mark setup complete
			await ClearSetupTokenAsync();

			_logger.LogInformation("Setup completed successfully");
			return new SeedResult { Success = true, Message = "Setup completed" };
		}
		catch (UnauthorizedAccessException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Seed failed");
			return new SeedResult { Success = false, Error = ex.Message };
		}
	}

	private async Task ClearSetupTokenAsync()
	{
		// Same as before - update appsettings.json
	}
}