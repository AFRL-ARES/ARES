using Ares.Core.Grpc;
using Ares.Messages;
using Ares.Messaging;
using ARESCore;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ARESService.Services.UserManagement;

public class UserManagementService : Ares.Messages.UserManagement.UserManagementBase
{
  private readonly RoleManager<IdentityRole> _roleManager;
  private readonly SignInManager<ARESUser> _signInManager;
  private readonly UserManager<ARESUser> _userManager;

  public UserManagementService(SignInManager<ARESUser> signInManager, UserManager<ARESUser> userManager, RoleManager<IdentityRole> roleManager)
  {
    _signInManager = signInManager;
    _userManager = userManager;
    _roleManager = roleManager;
  }

  [AuthorizeRoles(AresUserType.AresAdmin)]
  public override Task<ManagementResponse> Register(RegistrationRequest request, ServerCallContext context)
  {
    var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == request.UserInfo.UserName);
    if (existingUser is not null)
    {
      var response = new ManagementResponse
      {
        Success = false
      };

      response.Errors.Add($"User {request.UserInfo.UserName} already exists.");
      return Task.FromResult(response);
    }

    var registrantIsAdmin = context.GetHttpContext().User.IsInRole(AresUserType.AresAdmin.ToString());
    return RegisterNewUser(request, registrantIsAdmin);
  }

  public override Task<ManagementResponse> UpdateUser(RegistrationRequest request, ServerCallContext context)
  {
    var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == request.UserInfo.UserName);
    if (existingUser is null)
    {
      var response = new ManagementResponse { Success = false };
      response.Errors.Add($"Cannot update {request.UserInfo.UserName} as it does not exist.");
      return Task.FromResult(response);
    }

    var updaterIsAdmin = context.GetHttpContext().User.IsInRole(AresUserType.AresAdmin.ToString());
    return UpdateExistingUser(request, existingUser, updaterIsAdmin);
  }

  public override async Task<UsersResponse> GetUsers(Empty request, ServerCallContext context)
  {
    var response = new UsersResponse();
    var users = await _userManager.Users.ToArrayAsync();
    var requestingUser = context.GetHttpContext().User;
    if (!requestingUser.IsInRole(AresUserType.AresAdmin.ToString()))
      users = users.Where(user => user.UserName == requestingUser.Identity?.Name).ToArray();

    foreach (var aresUser in users)
    {
      var userInfo = new UserInfo { UserName = aresUser.UserName, Email = aresUser.Email };
      var roles = await _userManager.GetRolesAsync(aresUser);
      userInfo.Roles.AddRange(roles);
      response.Users.Add(userInfo);
    }

    return response;
  }

  public override async Task<UserResponse> GetUser(UserRequest request, ServerCallContext context)
  {
    var requestingUser = context.GetHttpContext().User;
    var getUserPermitted =
      requestingUser.IsInRole(AresUserType.AresAdmin.ToString())
      || requestingUser.Identity?.Name == request.UserName;

    if (!getUserPermitted)
      return new UserResponse { Success = false, Error = $"User unauthorized to get info about {request.UserName}" };

    var user = await _userManager.FindByNameAsync(request.UserName);
    if (user is null)
      return new UserResponse { Success = false, Error = $"Unable to find user {request.UserName}" };

    var userInfo = new UserInfo
    {
      UserName = user.UserName,
      Email = user.Email
    };

    userInfo.Roles.AddRange(await _userManager.GetRolesAsync(user));
    return new UserResponse { Success = true, User = userInfo };
  }

  private async Task<ManagementResponse> RegisterNewUser(RegistrationRequest request, bool isAdminRequest)
  {
    var newUser = new ARESUser
    {
      UserName = request.UserInfo.UserName,
      Email = request.UserInfo.Email
    };

    var serverRoles = await _roleManager.Roles.Select(role => role.Name).ToArrayAsync();
    var nonexistentRoles = request.UserInfo.Roles.Except(serverRoles).ToArray();
    // requested roles that don't actually exist on the server
    if (nonexistentRoles.Any())
    {
      var response = new ManagementResponse
      {
        Success = false
      };

      response.Errors.AddRange(nonexistentRoles.Select(s => $"Role {s} does not exist."));
      return response;
    }

    var userCreateResult = await _userManager.CreateAsync(newUser, request.Password);
    if (!userCreateResult.Succeeded)
      return userCreateResult.ToManagementResponse();

    if (isAdminRequest)
      await _userManager.AddToRolesAsync(newUser, request.UserInfo.Roles);
    else
      // if the registrant is not an admin, then just make the new user a simple ares user
      await _userManager.AddToRoleAsync(newUser, AresUserType.AresUser.ToString());

    return new ManagementResponse { Success = true };
  }

  private async Task<ManagementResponse> UpdateExistingUser(RegistrationRequest request, ARESUser user, bool isAdminRequest)
  {
    var serverRoles = await _roleManager.Roles.Select(role => role.Name).ToArrayAsync();
    var nonexistentRoles = request.UserInfo.Roles.Except(serverRoles).ToArray();
    // requested roles that don't actually exist on the server
    if (nonexistentRoles.Any())
    {
      var response = new ManagementResponse
      {
        Success = false
      };

      response.Errors.AddRange(nonexistentRoles.Select(s => $"Role {s} does not exist."));
      return response;
    }

    var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
    if (!passwordCheck.Succeeded && !string.IsNullOrEmpty(request.Password))
    {
      var passwordChanged = await ChangePasswordAdmin(user, request.Password);
      if (!passwordChanged.Succeeded)
        return passwordChanged.ToManagementResponse();
    }

    var currentEmail = await _userManager.GetEmailAsync(user);
    if (string.IsNullOrEmpty(currentEmail) || currentEmail != request.UserInfo.Email)
    {
      var emailSetResult = await _userManager.SetEmailAsync(user, request.UserInfo.Email);
      if (!emailSetResult.Succeeded)
        return emailSetResult.ToManagementResponse();
    }

    if (isAdminRequest)
    {
      var roleChange = await UpdateRoles(user, request.UserInfo.Roles.ToArray());
      if (!roleChange.Succeeded)
        return roleChange.ToManagementResponse();
    }

    return new ManagementResponse { Success = true };
  }

  public override async Task<ManagementResponse> DeleteUser(UserDeleteRequest request, ServerCallContext context)
  {
    var user = await _userManager.FindByNameAsync(request.UserName);
    if (user is null)
    {
      var response = new ManagementResponse { Success = false };
      response.Errors.Add($"User {request.UserName} not found.");
      return response;
    }

    if (context.GetHttpContext().User.Identity?.Name == user.UserName)
    {
      var response = new ManagementResponse { Success = false };
      response.Errors.Add(@"You shouldn't delete yourself ¯\_(ツ)_/¯");
      return response;
    }

    var otherUsers = _userManager.Users.Where(makerUser => makerUser.UserName != user.UserName).ToArray();
    if (!await AnyAdmins(otherUsers))
    {
      var response = new ManagementResponse { Success = false };
      response.Errors.Add($"At least one user with a role of {AresUserType.AresAdmin} has to exist on the system.");
      return response;
    }

    var result = await _userManager.DeleteAsync(user);
    return result.ToManagementResponse();
  }

  private Task<IdentityResult> ChangeEmail(ARESUser user, string email)
    => _userManager.SetEmailAsync(user, email);

  private async Task<IdentityResult> UpdateRoles(ARESUser user, string[] roles)
  {
    var existingRoles = await _userManager.GetRolesAsync(user);
    var rolesToAdd = roles.Except(existingRoles).ToArray();
    var rolesToRemove = existingRoles.Except(roles).ToArray();
    if (rolesToAdd.Any())
    {
      var result = await _userManager.AddToRolesAsync(user, rolesToAdd);
      if (!result.Succeeded)
        return result;
    }

    if (rolesToRemove.Any())
    {
      if (rolesToRemove.Contains(AresUserType.AresAdmin.ToString()))
      {
        var otherUsers = _userManager.Users.Where(makerUser => makerUser.UserName != user.UserName).ToArray();
        if (!await AnyAdmins(otherUsers))
          return IdentityResult.Failed(new IdentityError { Description = $"At least one user with a role of {AresUserType.AresAdmin} has to exist on the system." });
      }

      var result = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
      if (!result.Succeeded)
        return result;
    }

    return IdentityResult.Success;
  }

  private async Task<bool> AnyAdmins(ARESUser[] users)
  {
    foreach (var aresUser in users)
      if (await _userManager.IsInRoleAsync(aresUser, AresUserType.AresAdmin.ToString()))
        return true;

    return false;
  }

  private async Task<IdentityResult> ChangePasswordAdmin(ARESUser user, string password)
  {
    var changeToken = await _userManager.GeneratePasswordResetTokenAsync(user);
    return await _userManager.ResetPasswordAsync(user, changeToken, password);
  }

  private Task<IdentityResult> ChangePasswordUser(ARESUser user, string currentPassword, string newPassword)
    => _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
}
