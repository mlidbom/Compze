using System;
using Compze.Messaging.Buses;
using Compze.Persistence.EventStore;
using Compze.SystemCE;
using static System.Console;

namespace Website.paradigms.semantic_events
{
   #region IUserEvent
   interface IUserEvent : IAggregateEvent;
   #endregion

   namespace Introduction
   {
      namespace HideThisStuff
      {
         #region IEvent
         public interface IEvent;
         #endregion

         #region IAggregateEvent
         public interface IAggregateEvent : IEvent
         {
            Guid AggregateId { get; }
         }
         #endregion

         #region IAggregateCreatedEvent
         public interface IAggregateCreatedEvent : IAggregateEvent;
         #endregion
      }

      #region UserEvents1
      interface IUserCreated : IUserEvent, IAggregateCreatedEvent;
      interface IUserRegistered : IUserCreated;
      interface IUserImported : IUserRegistered;
      #endregion

      class Examples
      {
         public void IllustrateEventListening()
         {
            MessageHandlerRegistrarWithDependencyInjectionSupport registrar = ((MessageHandlerRegistrarWithDependencyInjectionSupport)null!).NotNull();

            #region UserEventRegistration
            registrar
              .ForEvent<IUserEvent>(userEvent => WriteLine($"User: {userEvent.AggregateId} something happened"))
              .ForEvent<IUserRegistered>(userRegistered => WriteLine($"User: {userRegistered.AggregateId} registered"))
              .ForEvent<IUserImported>(userImported => WriteLine($"User: {userImported.AggregateId} imported"));
            #endregion
         }
      }
   }
}
