using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Registration;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Abstractions.Tessaging.Typermedia.Public;

namespace AccountManagement.UserStories.Scenarios;

public class RegisterAccountScenario(IRemoteTypermediaNavigator navigator, string? email = null, string password = TestData.Passwords.ValidPassword) : ScenarioBase<(AccountResource.Tommand.Register.RegistrationAttemptResult Result, AccountResource? Account)>
{
   readonly IRemoteTypermediaNavigator _navigator = navigator;

   public AccountId AccountId { get; private set; } = new();
   public string Email { get; private set; } = email ?? TestData.Emails.CreateUnusedEmail();
   public string Password  { get; private set; } = password;

   internal RegisterAccountScenario WithAccountId(AccountId acountId)
   {
      AccountId = acountId;
      return this;
   }

   internal RegisterAccountScenario WithEmail(string email)
   {
      Email = email;
      return this;
   }

   internal RegisterAccountScenario WithPassword(string password)
   {
      Password = password;
      return this;
   }

   public override (AccountResource.Tommand.Register.RegistrationAttemptResult Result, AccountResource? Account) Execute()
   {
      var registrationAttemptResult = _navigator.Navigate(Api.Tommand.Register(AccountId, Email, Password));

      return registrationAttemptResult.Status switch
      {
         RegistrationAttemptStatus.Successful             => (registrationAttemptResult, _navigator.Navigate(Api.Tuery.AccountById(AccountId))),
         RegistrationAttemptStatus.EmailAlreadyRegistered => (registrationAttemptResult, null),
         _                                                => throw new ArgumentOutOfRangeException()
      };
   }
}
