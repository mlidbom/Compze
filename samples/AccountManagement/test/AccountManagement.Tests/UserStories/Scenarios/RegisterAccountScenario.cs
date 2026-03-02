using System;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Registration;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting;

namespace AccountManagement.UserStories.Scenarios;

public class RegisterAccountScenario(IClient client, string? email = null, string password = TestData.Passwords.ValidPassword) : ScenarioBase<(AccountResource.Tommand.Register.RegistrationAttemptResult Result, AccountResource? Account)>
{
   readonly IClient _client = client;

   public AccountId AccountId { get; set; } = new();
   public string Email { get; set; } = email ?? TestData.Emails.CreateUnusedEmail();
   public string Password  { get; set; } = password;

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
      var registrationAttemptResult = _client.ExecuteRequest(Api.Tommand.Register(AccountId, Email, Password));

      return registrationAttemptResult.Status switch
      {
         RegistrationAttemptStatus.Successful             => (registrationAttemptResult, Api.Tuery.AccountById(AccountId).ExecuteRequestOn(_client)),
         RegistrationAttemptStatus.EmailAlreadyRegistered => (registrationAttemptResult, null),
         _                                                => throw new ArgumentOutOfRangeException()
      };
   }
}
