using AccountManagement.API;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting;

namespace AccountManagement.UserStories.Scenarios;

public class LoginScenario(IEndpoint clientEndpoint, string email, string password) : ScenarioBase<AccountResource.Tommand.LogIn.LoginAttemptResult>
{
   readonly IEndpoint _clientEndpoint = clientEndpoint;
   public string Password { get; set; } = password;
   public string Email { get; set; } = email;

   public LoginScenario WithEmail(string email)
   {
      Email = email;
      return this;
   }

   public LoginScenario WithPassword(string password)
   {
      Password = password;
      return this;
   }

   public static LoginScenario Create(IEndpoint clientEndpoint)
   {
      var registerAccountScenario = new RegisterAccountScenario(clientEndpoint);
      registerAccountScenario.Execute();
      return new LoginScenario(clientEndpoint, registerAccountScenario.Email, registerAccountScenario.Password);
   }

   public LoginScenario(IEndpoint clientEndpoint, AccountResource account, string password) : this(clientEndpoint, account.Email.ToString(), password) {}

   public override AccountResource.Tommand.LogIn.LoginAttemptResult Execute() => Api.Tommand.Login(Email, Password).ExecuteAsClientRequestOn(_clientEndpoint);
}