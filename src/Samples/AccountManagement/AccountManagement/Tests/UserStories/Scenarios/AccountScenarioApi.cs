using AccountManagement.API;
using AccountManagement.Domain;
using Compze.Core.Tessaging.Hosting.Public;

namespace AccountManagement.UserStories.Scenarios;

public class AccountScenarioApi
{
   readonly IEndpoint _clientEndpoint;
   public AccountScenarioApi(IEndpoint clientEndpoint) => _clientEndpoint = clientEndpoint;

   public RegisterAccountScenario Register => new(_clientEndpoint);

   public ChangeAccountEmailScenario ChangeEmail() => ChangeAccountEmailScenario.Create(_clientEndpoint);
   public ChangeAccountEmailScenario ChangeEmail(AccountResource account) => new(_clientEndpoint, account);

   public ChangePasswordScenario ChangePassword() => ChangePasswordScenario.Create(_clientEndpoint);
   public ChangePasswordScenario ChangePassword(AccountResource account, string oldPassword, string newPassword) => new(_clientEndpoint, account, oldPassword: oldPassword, newPassword: newPassword);

   public LoginScenario Login() => LoginScenario.Create(_clientEndpoint);
   public LoginScenario Login(RegisterAccountScenario registrationScenario) => new(_clientEndpoint, registrationScenario.Email, registrationScenario.Password);
   public LoginScenario Login(Email email, string password) => new(_clientEndpoint, email: email.StringValue, password: password);
   public LoginScenario Login(string email, string password) => new(_clientEndpoint, email: email, password: password);
}