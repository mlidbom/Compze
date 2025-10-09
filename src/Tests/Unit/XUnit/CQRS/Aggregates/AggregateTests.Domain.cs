using System;
using Compze.Abstractions.Internal.Time;
using Compze.Tessaging.Teventive;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.XUnit.CQRS.Aggregates;

class User : Aggregate<User,IUserEvent, UserEvent>
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
        .For<IMigratedBeforeUserRegisteredEvent>(_ => {})
        .For<IMigratedAfterUserChangedEmailEvent>(_ => {})
        .For<IMigratedReplaceUserChangedPasswordEvent>(_ => {})
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

interface IUserEvent : IAggregateEvent;

abstract class UserEvent : AggregateEvent, IUserEvent
{
   protected UserEvent() {}
   protected UserEvent(Guid aggregateId) : base(aggregateId) {}
}

interface IUserChangedEmail : IUserEvent
{
   string Email { get; }
}
class UserChangedEmail(string email) : UserEvent, IUserChangedEmail
{
   public string Email { get; private set; } = email;
}

interface IUserChangedPassword : IUserEvent
{
   string Password { get; }
}

class UserChangedPassword(string password) : UserEvent, IUserChangedPassword
{
   public string Password { get; private set; } = password;
}

interface IUserRegistered : IUserEvent, IAggregateCreatedEvent
{
   string Email { get; }
   string Password { get; }
}

class UserRegistered(Guid userId, string email, string password) : UserEvent(userId), IUserRegistered
{
   public string Email { get; private set; } = email;
   public string Password { get; private set; } = password;
}

interface IMigratedBeforeUserRegisteredEvent : IUserEvent, IAggregateCreatedEvent;
[UsedImplicitly]
#pragma warning disable CA1812 // Class is instantiated via reflection during event deserialization
class MigratedBeforeUserRegisteredEvent : UserEvent, IMigratedBeforeUserRegisteredEvent;
#pragma warning restore CA1812

interface IMigratedAfterUserChangedEmailEvent : IUserEvent, IAggregateCreatedEvent;
[UsedImplicitly]
#pragma warning disable CA1812 // Class is instantiated via reflection during event deserialization
class MigratedAfterUserChangedEmailEvent : UserEvent, IMigratedAfterUserChangedEmailEvent;
#pragma warning restore CA1812

interface IMigratedReplaceUserChangedPasswordEvent : IUserEvent, IAggregateCreatedEvent;
[UsedImplicitly]
#pragma warning disable CA1812 // Class is instantiated via reflection during event deserialization
class MigratedReplaceUserChangedPasswordEvent : UserEvent, IMigratedReplaceUserChangedPasswordEvent;
#pragma warning restore CA1812