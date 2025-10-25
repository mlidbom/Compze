using System;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time.Public;
using Compze.Tessaging.Teventive;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Aggregates;

class User : Aggregate<User,IUserTevent, UserTevent>
{
   public string Email { get; private set; } = "";
   public string Password { get; private set; } = "";


   public User():base(new DateTimeNowTimeSource())
   {
      RegisterEventAppliers()
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

   public void Register(string email, string password, Guid id) => Publish(new UserRegistered(id, email, password));

   public static User Register(IEventStoreUpdater aggregates, string email, string password, Guid id)
   {
      var user = new User();
      user.Register(email, password, id);
      aggregates.Save(user);
      return user;
   }

   public void ChangePassword(string password) => Publish(new UserChangedPassword(password));

   public void ChangeEmail(string email) => Publish(new UserChangedEmail(email));
}

interface IUserTevent : IAggregateTevent;

abstract class UserTevent : AggregateTevent, IUserTevent
{
   protected UserTevent() {}
   protected UserTevent(Guid aggregateId) : base(aggregateId) {}
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

interface IUserRegistered : IUserTevent, IAggregateCreatedTevent
{
   string Email { get; }
   string Password { get; }
}

class UserRegistered(Guid userId, string email, string password) : UserTevent(userId), IUserRegistered
{
   public string Email { get; private set; } = email;
   public string Password { get; private set; } = password;
}

interface IMigratedBeforeUserRegisteredTevent : IUserTevent, IAggregateCreatedTevent;
[UsedImplicitly]
#pragma warning disable CA1812 // Class is instantiated via reflection during event deserialization
class MigratedBeforeUserRegisteredTevent : UserTevent, IMigratedBeforeUserRegisteredTevent;
#pragma warning restore CA1812

interface IMigratedAfterUserChangedEmailTevent : IUserTevent, IAggregateCreatedTevent;
[UsedImplicitly]
#pragma warning disable CA1812 // Class is instantiated via reflection during event deserialization
class MigratedAfterUserChangedEmailTevent : UserTevent, IMigratedAfterUserChangedEmailTevent;
#pragma warning restore CA1812

interface IMigratedReplaceUserChangedPasswordTevent : IUserTevent, IAggregateCreatedTevent;
[UsedImplicitly]
#pragma warning disable CA1812 // Class is instantiated via reflection during event deserialization
class MigratedReplaceUserChangedPasswordTevent : UserTevent, IMigratedReplaceUserChangedPasswordTevent;
#pragma warning restore CA1812