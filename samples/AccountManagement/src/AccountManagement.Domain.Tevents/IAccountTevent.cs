using Compze.Teventive.Taggregates.Tevents.Public;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
#pragma warning disable CA1715 //The naming without I is very much a thought out design

namespace AccountManagement.Domain.Tevents;

public interface IAccountTevent<out T> : ITaggregateIdentifyingTevent<T> where T : IAccountTevent;

public interface IAccountTevent : ITaggregateTevent
{

   public interface Created : IAccountTevent, ITaggregateCreatedTevent;
      //Used in multiple places by the infrastructure and clients. Things WILL BREAK without this.
   //Taggregate: Sets the ID when such an tevent is raised.
   //Creates a viewmodel automatically when received by an SingleTaggregateQueryModelUpdater

   public interface UserRegistered :
      Created,
      PropertyUpdated.Email,
      PropertyUpdated.Password;

   public interface UserChangedEmail :
      PropertyUpdated.Email;

   public interface UserChangedPassword :
      PropertyUpdated.Password;

   public static class PropertyUpdated
   {
      public interface Password : IAccountTevent
      {
         Passwords.Password Password { get; /* Never add a setter! */ }
      }

      public interface Email : IAccountTevent
      {
         Domain.Email Email { get; /* Never add a setter! */ }
      }
   }

   public interface LoginAttempted : IAccountTevent;

   public interface LoggedIn : LoginAttempted
   {
      string AuthenticationToken { get; }
   }

   public interface LoginFailed : LoginAttempted;
}