using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using SQE.API.DTO;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services;

public interface IAdminService
{
	Task<ServiceStatusDTO> GetDatabaseStatusAsync(UserInfo user);
	Task<ServiceStatusDTO> GetEmailStatusAsync(UserInfo    user);
}

public class AdminService(
		IAdminRepository admin
		, IEmailSender   emailSender
		, IUserService   userService) : IAdminService
{
	public async Task<ServiceStatusDTO> GetDatabaseStatusAsync(UserInfo user)
	{
		if (!user.SystemRoles.Contains(UserSystemRoles.USER_ADMIN))
			throw new StandardExceptions.NoSystemPermissionsException(user);

		const string dbServiceName = "database";

		try
		{
			var dbStatus = await admin.CheckDatabaseAccessibleAsync();

			return new ServiceStatusDTO
			{
					service = dbServiceName
					, isHealthy = dbStatus
					, statusMessage = ""
					,
			};
		}
		catch (Exception e)
		{
			return new ServiceStatusDTO
			{
					service = dbServiceName
					, isHealthy = false
					, statusMessage = e.Message
					,
			};
		}
	}

	public async Task<ServiceStatusDTO> GetEmailStatusAsync(UserInfo user)
	{
		if (!user.SystemRoles.Contains(UserSystemRoles.USER_ADMIN))
			throw new StandardExceptions.NoSystemPermissionsException(user);

		const string emailServiceName = "email";

		const string emailBody =
				"Dear $User,\r\n\r\n\tThe test of the system emailer was successfull";

		try
		{
			var userDetails = await userService.GetCurrentUser();

			await emailSender.SendEmailAsync(
					userDetails.email
					, "SQE Admin Email Test"
					, emailBody.Replace("$User", userDetails.forename));

			return new ServiceStatusDTO
			{
					service = emailServiceName
					, isHealthy = true
					, statusMessage = ""
					,
			};
		}
		catch (Exception e)
		{
			return new ServiceStatusDTO
			{
					service = emailServiceName
					, isHealthy = false
					, statusMessage = e.Message
					,
			};
		}
	}
}
