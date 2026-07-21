using AccountManagement.Domain.Passwords;
using CommunityToolkit.Diagnostics;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using Newtonsoft.Json;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming

namespace AccountManagement.Domain.Tevents;

// ReSharper disable once ClassNeverInstantiated.Global
public class AccountTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IAccountTevent<T> where T : IAccountTevent;

public class AccountTevent : TaggregateTevent, IAccountTevent
{
   protected AccountTevent() {}
   AccountTevent(AccountId accountId) : base(accountId) {}

   public class UserRegistered : AccountTevent, IAccountTevent.UserRegistered
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

   public class UserChangedEmail(Email email) : AccountTevent, IAccountTevent.UserChangedEmail
   {
      public Email Email { get; private set; } = email;
   }

   public class UserChangedPassword(Password password) : AccountTevent, IAccountTevent.UserChangedPassword
   {
      public Password Password { get; private set; } = password;
   }

   public class LoggedIn(string token) : AccountTevent, IAccountTevent.LoggedIn
   {
      public string AuthenticationToken { get; } = token;
   }

   public class LoginFailed : AccountTevent, IAccountTevent.LoginFailed;
}
