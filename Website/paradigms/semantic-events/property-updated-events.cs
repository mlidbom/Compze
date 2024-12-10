using Compze.Messaging.Buses;
using Compze.Persistence.EventStore;
using Compze.SystemCE;
using static System.Console;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Website.paradigms.semantic_events
{
   namespace NoCollision
   {
      #region IUserCreated
      interface IUserRegistered : IUserEvent, IAggregateCreatedEvent;
      interface IUserChangedEmail : IUserEvent;
      #endregion
   }

   namespace NoCollision1
   {
      #region IUserEmailEventsNaive
      interface IUserRegistered : IUserEvent, IAggregateCreatedEvent
      {
         Email Email { get; }
      }

      interface IUserChangedEmail : IUserEvent
      {
         Email Email { get; }
      }
      #endregion
   }

   namespace NoCollision2
   {
      #region IUserEmailEventsWorking
      interface IUserEmailPropertyUpdated : IUserEvent
      {
         Email Email { get; }
      }

      interface IUserRegistered : IUserEmailPropertyUpdated, IAggregateCreatedEvent;
      interface IUserChangedEmail : IUserEmailPropertyUpdated;
      #endregion

      class Examples
      {
         public void IllustrateEventListening()
         {
            MessageHandlerRegistrarWithDependencyInjectionSupport eventHandlerRegistrar = ((MessageHandlerRegistrarWithDependencyInjectionSupport)null!).NotNull();

            #region EmailPropertyUpdatedListener
            eventHandlerRegistrar
              .ForEvent<IUserEmailPropertyUpdated>(emailUpdated => WriteLine($"User: {emailUpdated.AggregateId} Email: {emailUpdated.Email}"));
            #endregion
         }
      }
   }
}
