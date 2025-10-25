using Compze.Abstractions.Tessaging.Teventive.Public;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
#pragma warning disable CA1715 //The naming without I is very much a thought out design

namespace AccountManagement.Domain.Tevents;

public static partial class AccountTevent
{
   public interface Root : ITaggregateTevent;

   public interface Created : Root, ITaggregateCreatedTevent;
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
      public interface Password : AccountTevent.Root
      {
         Passwords.Password Password { get; /* Never add a setter! */ }
      }

      public interface Email : AccountTevent.Root
      {
         Domain.Email Email { get; /* Never add a setter! */ }
      }
   }

   public interface LoginAttempted : AccountTevent.Root;

   public interface LoggedIn : LoginAttempted
   {
      string AuthenticationToken { get; }
   }

   public interface LoginFailed : LoginAttempted;
}