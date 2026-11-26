using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

#pragma warning disable // Documentation example code: a small example domain, shown in regions.
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

// ReSharper disable once CheckNamespace
namespace Website.Teventive.Taggregates.WritingATaggregate;

#region tevents
interface IUserTevent : ITaggregateTevent
{
   interface IPropertyUpdated : IUserTevent
   {
      interface IEmail : IPropertyUpdated
      {
         string Email { get; }
      }
   }

   interface IRegistered : IUserTevent, ITaggregateCreatedTevent, IPropertyUpdated.IEmail;
   interface IChangedEmail : IUserTevent, IPropertyUpdated.IEmail;
}
#endregion

#region wrapper-tevent
interface IUserTevent<out T> : ITaggregateTevent<T> where T : IUserTevent;
#endregion

#region tevent-classes
abstract class UserTevent : TaggregateTevent, IUserTevent
{
   protected UserTevent() {}
   UserTevent(TaggregateId userId) : base(userId) {}

   internal class Registered(TaggregateId userId, string email) : UserTevent(userId), IUserTevent.IRegistered
   {
      public string Email { get; } = email;
   }

   internal class ChangedEmail(string email) : UserTevent, IUserTevent.IChangedEmail
   {
      public string Email { get; } = email;
   }
}

class UserTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IUserTevent<T> where T : IUserTevent;
#endregion

#region user
class User : Taggregate<User, IUserTevent, UserTevent, IUserTevent<IUserTevent>, UserTevent<UserTevent>>
{
   static readonly TeventRouting<User, IUserTevent> Appliers = DeclareAppliers(it => it
     .For<IUserTevent.IPropertyUpdated.IEmail>(static (user, updated) => user.Email = updated.Email));

   protected override TeventRouting TeventAppliers => Appliers;

   User() {}

   public string Email { get; private set; } = "";

   public static User Register(string email)
   {
      var user = new User();
      user.Publish(new UserTevent.Registered(TaggregateId.New(), email));
      return user;
   }

   public void ChangeEmail(string email) => Publish(new UserTevent.ChangedEmail(email));
}
#endregion

static class TaggregatesExample
{
   internal static User Run()
   {
      #region using
      var user = User.Register("ada@example.com");
      user.ChangeEmail("ada.lovelace@example.com");
      //user.Email is now "ada.lovelace@example.com", and user.Version is 2.
      #endregion
      return user;
   }
}
