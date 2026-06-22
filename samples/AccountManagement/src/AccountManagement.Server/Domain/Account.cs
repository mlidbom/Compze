using AccountManagement.API;
using AccountManagement.Domain.Passwords;
using AccountManagement.Domain.Registration;
using AccountManagement.Domain.Tevents;
using CommunityToolkit.Diagnostics;
using Compze.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Internals.Logging;
using Compze.Typermedia;

namespace AccountManagement.Domain;

///Completely encapsulates all the business logic for an account.  Should make it impossible for clients to use the class incorrectly.
class Account : Taggregate<Account, IAccountTevent, AccountTevent, IAccountTevent<IAccountTevent>, AccountTevent<AccountTevent>>, IAccountResourceData
{
   static readonly ILogger Log = CompzeLogger.For<Account>();
   public Email Email { get; private set; } = null!;       //Never public setters on an taggregate. AssertInvariantsAreMet guarantees not null status.
   public Password Password { get; private set; } = null!; //Never public setters on an taggregate. AssertInvariantsAreMet guarantees not null status.

   public override AccountId Id => new(base.Id.Value);

   //No public constructors please. Taggregates are created through domain verbs.
   //Expose named factory methods that ensure the instance is valid instead. See register method below.
   Account()
   {
      //Maintain correct state as tevents are raised or read from the store.
      //Use property updated tevents whenever possible. Changes to public state should be represented by property updated tevents.
      RegisterTeventAppliers()
        .For<IAccountTevent.PropertyUpdated.Email>(e => Email = e.Email)
        .For<IAccountTevent.PropertyUpdated.Password>(e => Password = e.Password)
        .IgnoreUnhandled<IAccountTevent.LoggedIn>()
        .IgnoreUnhandled<IAccountTevent.LoginFailed>();
   }

   //Ensure that the state of the instance is sane. If not throw an exception.
   //Called after every call to Publish.
   protected override void AssertInvariantsAreMet()
   {
      base.AssertInvariantsAreMet();
      Guard.IsNotNull(Email);
      Guard.IsNotNull(Password);
      Guard.IsNotNull(Id);
      Guard.IsNotDefault(Id.Value);
   }

   /// <summary><para>Used when a user manually creates an account themselves.</para>
   /// <para>Note how this design with a named static creation method: </para>
   /// <para> * makes it clearer what the caller intends.</para>
   /// <para> * makes it impossible to use the class incorrectly, such as forgetting to check for duplicates or save the new instance in the repository.</para>
   /// <para> * reduces code duplication since multiple callers are not burdened with saving the instance, checking for duplicates etc.</para>
   /// </summary>
   internal static (RegistrationAttemptStatus Status, Account? Registered) Register(AccountId accountId, Email email ,Password password, IInProcessTypermediaNavigator navigator)
   {
      //Ensure that it is impossible to call with invalid arguments.
      //Since all domain types should ensure that it is impossible to create a non-default value that is invalid we only have to disallow default values.
      Guard.IsNotNull(email);Guard.IsNotNull(password);

      //The email is the unique identifier for logging into the account so duplicates are forbidden.
      var existingAccount = navigator.Execute(InternalApi.Tueries.TryGetByEmail(email));
      if(existingAccount is not null)
      {
         Log.Warning($"Registration Failed. Email {email} is already registered.");
         return (RegistrationAttemptStatus.EmailAlreadyRegistered, null);
      }

      var newAccount = new Account();
      newAccount.Publish(new AccountTevent.UserRegistered(accountId: accountId, email: email, password: password));

      navigator.Execute(InternalApi.Tommands.Save(newAccount));

      return (RegistrationAttemptStatus.Successful, newAccount);
   }

   internal void ChangePassword(string oldPassword, Password newPassword)
   {
      Guard.IsNotNull(oldPassword); Guard.IsNotNull(newPassword);

      Password.AssertIsCorrectPassword(oldPassword);

      Publish(new AccountTevent.UserChangedPassword(newPassword));
   }

   internal void ChangeEmail(Email email)
   {
      Guard.IsNotNull(email);

      Publish(new AccountTevent.UserChangedEmail(email));
   }

   internal IAccountTevent.LoginAttempted Login(string logInPassword)
   {
      if(Password.IsCorrectPassword(logInPassword))
      {
         return Publish(new AccountTevent.LoggedIn(token: Guid.NewGuid().ToString()));
      } else
      {
         return Publish(new AccountTevent.LoginFailed());
      }
   }
}