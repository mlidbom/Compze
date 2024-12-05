using System;
using AccountManagement.API;
using AccountManagement.Domain.Registration;
using Compze.Messaging.Buses;

namespace AccountManagement.UserStories.Scenarios;

class RegisterAccountScenario(IEndpoint clientEndpoint, string email = null, string password = TestData.Passwords.ValidPassword) : ScenarioBase<(AccountResource.Command.Register.RegistrationAttemptResult Result, AccountResource Account)>
{
   readonly IEndpoint _clientEndpoint = clientEndpoint;

   public Guid AccountId = Guid.NewGuid();
   public string Email = email ?? TestData.Emails.CreateUnusedEmail();
   public string Password = password;


   public RegisterAccountScenario WithAccountId(Guid acountId)
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

   public override (AccountResource.Command.Register.RegistrationAttemptResult Result, AccountResource Account) Execute()
   {
      var registrationAttemptResult = _clientEndpoint.ExecuteClientRequest(Api.Command.Register(AccountId, Email, Password));

      return registrationAttemptResult.Status switch
      {
         RegistrationAttemptStatus.Successful => (registrationAttemptResult, Api.Query.AccountById(AccountId).ExecuteAsClientRequestOn(_clientEndpoint)),
         RegistrationAttemptStatus.EmailAlreadyRegistered => (registrationAttemptResult, null),
         _ => throw new ArgumentOutOfRangeException()
      };
   }
}