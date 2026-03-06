using AccountManagement.API;
using Compze.Typermedia;

namespace AccountManagement.UserStories.Scenarios;

public class LoginScenario(IRemoteTypermediaNavigator navigator, string email, string password) : ScenarioBase<AccountResource.Tommand.LogIn.LoginAttemptResult>
{
   readonly IRemoteTypermediaNavigator _navigator = navigator;
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

   public override AccountResource.Tommand.LogIn.LoginAttemptResult Execute() => _navigator.Navigate(Api.Tommand.Login(Email, Password));
}