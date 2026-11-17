using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using JetBrains.Annotations;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Compze.Tests.Unit.CQRS.Taggregates;
#pragma warning disable CA1812 //Uninstantiated class (used via reflection)

class User : Taggregate<User,IUserTevent, UserTevent, IUserTevent<IUserTevent>, UserTevent<UserTevent>>, ITeventRouted
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
     .For<IUserChangedPassword>(static (user, tevent) => user.Password = tevent.Password));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   public string Email { get; private set; } = "";
   public string Password { get; private set; } = "";

   public void Register(string email, string password, TaggregateId id) => Publish(new UserRegistered(id, email, password));

   public void ChangePassword(string password) => Publish(new UserChangedPassword(password));

   public void ChangeEmail(string email) => Publish(new UserChangedEmail(email));
}

interface IUserTevent<out T> : ITaggregateTevent<T> where T : IUserTevent;
interface IUserTevent : ITaggregateTevent;

// ReSharper disable once ClassNeverInstantiated.Global
class UserTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IUserTevent<T> where T : IUserTevent;
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

interface IMigratedBeforeUserRegisteredTevent : IUserTevent, ITaggregateCreatedTevent;
[UsedImplicitly]
#pragma warning disable CA1812 // Class is instantiated via reflection during tevent deserialization
class MigratedBeforeUserRegisteredTevent : UserTevent, IMigratedBeforeUserRegisteredTevent;
#pragma warning restore CA1812

interface IMigratedAfterUserChangedEmailTevent : IUserTevent, ITaggregateCreatedTevent;
[UsedImplicitly]
#pragma warning disable CA1812 // Class is instantiated via reflection during tevent deserialization
class MigratedAfterUserChangedEmailTevent : UserTevent, IMigratedAfterUserChangedEmailTevent;
#pragma warning restore CA1812

interface IMigratedReplaceUserChangedPasswordTevent : IUserTevent, ITaggregateCreatedTevent;
[UsedImplicitly]
#pragma warning disable CA1812 // Class is instantiated via reflection during tevent deserialization
class MigratedReplaceUserChangedPasswordTevent : UserTevent, IMigratedReplaceUserChangedPasswordTevent;
#pragma warning restore CA1812