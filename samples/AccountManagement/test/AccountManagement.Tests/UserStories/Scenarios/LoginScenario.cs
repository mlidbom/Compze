using AccountManagement.API;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting;

namespace AccountManagement.UserStories.Scenarios;

public class LoginScenario(IClient client, string email, string password) : ScenarioBase<AccountResource.Tommand.LogIn.LoginAttemptResult>
{
   readonly IClient _client = client;
   string Password { get; set; } = password;
   string Email { get; set; } = email;

   internal LoginScenario WithEmail(string email)
   {
      Email = email;
      return this;
   }

   internal LoginScenario WithPassword(string password)
   {
      Password = password;
      return this;
   }

   public override AccountResource.Tommand.LogIn.LoginAttemptResult Execute() => Api.Tommand.Login(Email, Password).ExecuteRequestOn(_client);
}