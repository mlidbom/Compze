using System;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Registration;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting;

namespace AccountManagement.UserStories.Scenarios;

public class RegisterAccountScenario(IEndpoint clientEndpoint, string? email = null, string password = TestData.Passwords.ValidPassword) : ScenarioBase<(AccountResource.Tommand.Register.RegistrationAttemptResult Result, AccountResource? Account)>
{
   readonly IEndpoint _clientEndpoint = clientEndpoint;

   public AccountId AccountId = new();
   public string Email = email ?? TestData.Emails.CreateUnusedEmail();
   public string Password = password;


   public RegisterAccountScenario WithAccountId(AccountId acountId)
   {
      AccountId = acountId;
      return this;
   }

   public RegisterAccountScenario WithEmail(string email)
   {
      Email = email;
      return this;
   }

   public RegisterAccountScenario WithPassword(string password)
   {
      Password = password;
      return this;
   }

   public override (AccountResource.Tommand.Register.RegistrationAttemptResult Result, AccountResource? Account) Execute()
   {
      var registrationAttemptResult = _clientEndpoint.ExecuteClientRequest(Api.Tommand.Register(AccountId, Email, Password));

      return registrationAttemptResult.Status switch
      {
         RegistrationAttemptStatus.Successful => (registrationAttemptResult, Api.Tuery.AccountById(AccountId).ExecuteAsClientRequestOn(_clientEndpoint)),
         RegistrationAttemptStatus.EmailAlreadyRegistered => (registrationAttemptResult, null),
         _ => throw new ArgumentOutOfRangeException()
      };
   }
}