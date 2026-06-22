using Compze.Abstractions.Public;
using Compze.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Tessaging.Teventive.TeventStore.Public;
using JetBrains.Annotations;

namespace Compze.Tests.Integration.CQRS;
#pragma warning disable CA1812 //Uninstantiated class (used via reflection)

class User : Taggregate<User, IUserTevent, UserTevent, IUserTevent<IUserTevent>, UserTevent<UserTevent>>
{
   public string Email { get; private set; } = "";
   public string Password { get; private set; } = "";

   public User()
   {
      RegisterTeventAppliers()
        .For<IUserRegistered>(e =>
         {
            Email = e.Email;
            Password = e.Password;
         })
        .For<IUserChangedEmail>(e => Email = e.Email)
        .For<IMigratedBeforeUserRegisteredTevent>(_ => {})
        .For<IMigratedAfterUserChangedEmailTevent>(_ => {})
        .For<IMigratedReplaceUserChangedPasswordTevent>(_ => {})
        .For<IUserChangedPassword>(e => Password = e.Password);
   }

   public void Register(string email, string password, TaggregateId id) => Publish(new UserRegistered(id, email, password));

   public static User Register(ITeventStoreUpdater taggregates, string email, string password, TaggregateId id)
   {
      var user = new User();
      user.Register(email, password, id);
      taggregates.Save(user);
      return user;
   }

   public void ChangePassword(string password) => Publish(new UserChangedPassword(password));

   public void ChangeEmail(string email) => Publish(new UserChangedEmail(email));
}

interface IUserTevent<out T> : ITaggregateIdentifyingTevent<T> where T : IUserTevent;
interface IUserTevent : ITaggregateTevent;

class UserTevent<T>(T tevent) : TaggregateIdentifyingTevent<T>(tevent), IUserTevent<T> where T : IUserTevent;
abstract class UserTevent : TaggregateTevent, IUserTevent
{
   protected UserTevent() {}
   protected UserTevent(TaggregateId taggregateId) : base(taggregateId) {}
}

interface IUserChangedEmail : IUserTevent
{
   string Email { get; }
}
class UserChangedEmail(string email) : UserTevent, IUserChangedEmail
{
   public string Email { get; private set; } = email;
}

interface IUserChangedPassword : IUserTevent
{
   string Password { get; }
}

class UserChangedPassword(string password) : UserTevent, IUserChangedPassword
{
   public string Password { get; private set; } = password;
}

interface IUserRegistered : IUserTevent, ITaggregateCreatedTevent
{
   string Email { get; }
   string Password { get; }
}

class UserRegistered(TaggregateId userId, string email, string password) : UserTevent(userId), IUserRegistered
{
   public string Email { get; private set; } = email;
   public string Password { get; private set; } = password;
}

#pragma warning disable CA1812
interface IMigratedBeforeUserRegisteredTevent : IUserTevent, ITaggregateCreatedTevent;
[UsedImplicitly] class MigratedBeforeUserRegisteredTevent : UserTevent, IMigratedBeforeUserRegisteredTevent;

interface IMigratedAfterUserChangedEmailTevent : IUserTevent, ITaggregateCreatedTevent;
[UsedImplicitly] class MigratedAfterUserChangedEmailTevent : UserTevent, IMigratedAfterUserChangedEmailTevent;

interface IMigratedReplaceUserChangedPasswordTevent : IUserTevent, ITaggregateCreatedTevent;
[UsedImplicitly] class MigratedReplaceUserChangedPasswordTevent : UserTevent, IMigratedReplaceUserChangedPasswordTevent;

#pragma warning restore CA1812
