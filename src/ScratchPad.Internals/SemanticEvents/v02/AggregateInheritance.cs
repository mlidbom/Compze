// ReSharper disable All

using System;

#pragma warning disable IDE0059 // Unnecessary assignment of a value
#pragma warning disable IDE0051 // Remove unused private members

//When persisting event we would only persist the wrapped part. Thus changing from unwrapped-uninheritable to inheritable does not break storage and maybe one could even move events between clases in the hierarchy?
namespace ScratchPad.SemanticEvents.v02;

interface IEvent {}

interface IExactlyOnceEvent : IEvent
{
   Guid EventId { get; }
}

interface IAggregateEvent : IExactlyOnceEvent
{
   Guid AggregateId { get; }
}

interface IEvent<out TEventInterface> : IEvent
{
   TEventInterface Event { get; }
}

interface IExactlyOnceEvent<out TEventInterface> : IEvent<TEventInterface> where TEventInterface : IExactlyOnceEvent {}
interface IAggregateEvent<out TAggregateEventInterface> : IExactlyOnceEvent<TAggregateEventInterface> where TAggregateEventInterface : IAggregateEvent {}

//Urgent: Removing the rule that the inner event be an IUserEvent feels a bit icky, but is there any way around it? 
interface IUserEvent<out TUserEventInterface> : IAggregateEvent<TUserEventInterface> where TUserEventInterface : IUserEvent {}
interface IManagerEvent<out TIBirdEventInterface> : IUserEvent<TIBirdEventInterface> where TIBirdEventInterface : IUserEvent {}


interface IUserEvent : IAggregateEvent {}
interface IUserRegisteredEvent : IUserEvent {}
interface IManagerEvent : IUserEvent {}
interface IManagerHiredEvent : IManagerEvent {}

public class AggregateInheritance
{
   public void DemonstrateSemanticRelationships()
   {
      IUserEvent<IUserEvent> wrapperUserEvent = null!;
      IUserEvent<IUserRegisteredEvent> wrapperUserRegisteredEvent = null!;

      IManagerEvent<IUserEvent> wrapperManagerEvent = null!;
      IManagerEvent<IUserRegisteredEvent> wrapperManagerUserRegisteredEvent = null!;
      IManagerEvent<IManagerHiredEvent> wrappedManagerHiredEven = null!;
      wrapperUserEvent = wrapperUserRegisteredEvent = wrapperManagerUserRegisteredEvent;
      wrapperUserEvent = wrapperManagerEvent = wrapperManagerUserRegisteredEvent;
      wrapperUserEvent = wrappedManagerHiredEven;
   }
}

interface IAddressEvent {}
interface IAddressUpdatedEvent : IAddressEvent {}
interface IMovedEvent : IAddressUpdatedEvent {}

//Should it be a specific IUserAddressEvent<T> or IUserComponent<T> for all component events? Or should IUserAddressEvent<T> inherit IUserComponent<T>  
interface IUserAddressEvent<out TAddressEventInterface> : IEvent<TAddressEventInterface>, IUserEvent {}
interface IManagerAddressEvent<out TAddressEventInterface> : IUserAddressEvent<TAddressEventInterface> {}

public class ReUsableAggregateComponentsInInheritableAggregates
{
   static void DemonstrateSemanticRelationships()
   {
      IUserEvent<IUserAddressEvent<IAddressEvent>> userAddressEvent = null!;
      IUserEvent<IUserAddressEvent<IAddressUpdatedEvent>> userAddressUpdatedEvent = null!;
      IUserEvent<IUserAddressEvent<IMovedEvent>> userMovedEvent = null!;

      IManagerEvent<IManagerAddressEvent<IAddressEvent>> managerAddressEvent = null!;
      IManagerEvent<IManagerAddressEvent<IAddressUpdatedEvent>> managerAddressUpdatedEvent = null!;
      IManagerEvent<IManagerAddressEvent<IMovedEvent>> managerMovedEvent = null!;

      //Semantic relationships are maintained.
      userAddressEvent = userAddressUpdatedEvent = userMovedEvent = managerMovedEvent;
      managerAddressEvent = managerAddressUpdatedEvent = managerMovedEvent;

      userAddressEvent = managerAddressEvent = managerAddressUpdatedEvent = managerMovedEvent;
      userAddressUpdatedEvent = managerAddressUpdatedEvent;
   }
}
