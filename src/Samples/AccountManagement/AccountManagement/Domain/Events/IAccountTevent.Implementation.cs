using AccountManagement.Domain.Passwords;
using CommunityToolkit.Diagnostics;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Newtonsoft.Json;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming

namespace AccountManagement.Domain.Tevents;

//refactor: Consider using interfaces instead of static classes for nesting our tevents.
public partial interface IAccountTevent
{
#pragma warning disable CA1724 // Type names should not match namespaces
   public static class Implementation
#pragma warning restore CA1724 // Type names should not match namespaces
   {
      public abstract class Root : TaggregateTevent, IAccountTevent
      {
         protected Root() {}
         protected Root(AccountId accountId) : base(accountId) {}
      }

      public class UserRegistered : Root, IAccountTevent.UserRegistered
      {
#pragma warning disable IDE0051 // Remove unused private members
         [JsonConstructor] UserRegistered(Email email, Password password)
#pragma warning restore IDE0051 // Remove unused private members
         {
            Email = email;
            Password = password;
         }

         ///<summary>
         /// The constructor should guarantee that the tevent is correctly created.
         /// Once again we are saved from doing work here by using value objects for <see cref="Email"/> and <see cref="Password"/>
         /// The base class will ensure that the GUID is not empty.
         /// </summary>
         public UserRegistered(AccountId accountId, Email email, Password password) : base(accountId)
         {
            Guard.IsNotNull(email);
            Guard.IsNotNull(password);

            Email = email;
            Password = password;
         }

         public Email Email { get; private set; }
         public Password Password { get; private set; }
      }

      public class UserChangedEmail(Email email) : Root, IAccountTevent.UserChangedEmail
      {
         public Email Email { get; private set; } = email;
      }

      public class UserChangedPassword(Password password) : Root, IAccountTevent.UserChangedPassword
      {
         public Password Password { get; private set; } = password;
      }

      public class LoggedIn(string token) : Root, IAccountTevent.LoggedIn
      {
         public string AuthenticationToken { get; } = token;
      }

      public class LoginFailed : Root, IAccountTevent.LoginFailed;
   }
}