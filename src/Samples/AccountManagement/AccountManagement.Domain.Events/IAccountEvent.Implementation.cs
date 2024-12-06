using System;
using AccountManagement.Domain.Passwords;
using CommunityToolkit.Diagnostics;
using Compze.Persistence.EventStore;
using Newtonsoft.Json;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming

namespace AccountManagement.Domain.Events;

//refactor: Consider using interfaces instead of static classes for nesting our events.
public static partial class AccountEvent
{
#pragma warning disable CA1724 // Type names should not match namespaces
   public static class Implementation
#pragma warning restore CA1724 // Type names should not match namespaces
   {
      public abstract class Root : AggregateEvent, AccountEvent.Root
      {
         protected Root() {}
         protected Root(Guid aggregateId) : base(aggregateId) {}
      }

      public class UserRegistered : Root, AccountEvent.UserRegistered
      {
#pragma warning disable IDE0051 // Remove unused private members
         [JsonConstructor] UserRegistered(Email email, Password password)
#pragma warning restore IDE0051 // Remove unused private members
         {
            Email = email;
            Password = password;
         }

         ///<summary>
         /// The constructor should guarantee that the event is correctly created.
         /// Once again we are saved from doing work here by using value objects for <see cref="Email"/> and <see cref="Password"/>
         /// The base class will ensure that the GUID is not empty.
         /// </summary>
         public UserRegistered(Guid accountId, Email email, Password password) : base(accountId)
         {
            Guard.IsNotNull(email);
            Guard.IsNotNull(password);

            Email = email;
            Password = password;
         }

         public Email Email { get; private set; }
         public Password Password { get; private set; }
      }

      public class UserChangedEmail(Email email) : Root, AccountEvent.UserChangedEmail
      {
         public Email Email { get; private set; } = email;
      }

      public class UserChangedPassword(Password password) : Root, AccountEvent.UserChangedPassword
      {
         public Password Password { get; private set; } = password;
      }

      public class LoggedIn(string token) : Root, AccountEvent.LoggedIn
      {
         public string AuthenticationToken { get; } = token;
      }

      public class LoginFailed : Root, AccountEvent.LoginFailed;
   }
}