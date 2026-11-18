using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TaggregateStores;
using JetBrains.Annotations;

namespace Compze.Tests.Integration.CQRS;
#pragma warning disable CA1812 //Uninstantiated class (used via reflection)

class User : Taggregate<User, IUserTevent, UserTevent, IUserTevent<IUserTevent>, UserTevent<UserTevent>>
{
   static readonly TeventRouting<User, IUserTevent> Appliers = DeclareAppliers(it => it
     .For<IUserRegistered>(static (user, tevent) =>
      {
         user.Email = tevent.Email;
         user.Password = tevent.Password;
      })
     .For<IUserChangedEmail>(static (user, tevent) => user.Email = tevent.Email)
     .For<IMigratedBeforeUserRegisteredTevent>(static (_, _) => {})
     .For<IMigratedAfterUserChangedEmailTevent>(static (_, _) => {})
     .For<IMigratedReplaceUserChangedPasswordTevent>(static (_, _) => {})
     .For<IUserChangedPassword>(static (user, tevent) => user.Password = tevent.Password)
     .For<IUserDeleted>(static (_, _) => {}));

   protected override TeventRouting TeventAppliers => Appliers;

   public string Email { get; private set; } = "";
   public string Password { get; private set; } = "";

   public void Register(string email, string password, TaggregateId id) => Publish(new UserRegistered(id, email, password));

   public static User Register(ITaggregateStoreUpdater taggregates, string email, string password, TaggregateId id)
   {
      var user = new User();
      user.Register(email, password, id);
      taggregates.Save(user);
      return user;
   }

   public void ChangePassword(string password) => Publish(new UserChangedPassword(password));

   public void ChangeEmail(string email) => Publish(new UserChangedEmail(email));

   public void Delete() => Publish(new UserDeleted());
}

interface IUserTevent<out T> : ITaggregateTevent<T> where T : IUserTevent;
interface IUserTevent : ITaggregateTeventWithIdentity;

class UserTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IUserTevent<T> where T : IUserTevent;
abstract class UserTevent : TaggregateTeventWithIdentity, IUserTevent
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

interface IUserDeleted : IUserTevent, ITaggregateDeletedTevent;

class UserDeleted : UserTevent, IUserDeleted;

#pragma warning disable CA1812
interface IMigratedBeforeUserRegisteredTevent : IUserTevent, ITaggregateCreatedTevent;
[UsedImplicitly] class MigratedBeforeUserRegisteredTevent : UserTevent, IMigratedBeforeUserRegisteredTevent;

interface IMigratedAfterUserChangedEmailTevent : IUserTevent, ITaggregateCreatedTevent;
[UsedImplicitly] class MigratedAfterUserChangedEmailTevent : UserTevent, IMigratedAfterUserChangedEmailTevent;

interface IMigratedReplaceUserChangedPasswordTevent : IUserTevent, ITaggregateCreatedTevent;
[UsedImplicitly] class MigratedReplaceUserChangedPasswordTevent : UserTevent, IMigratedReplaceUserChangedPasswordTevent;

#pragma warning restore CA1812
