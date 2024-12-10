using Compze.Messaging.Buses;
using Compze.Persistence.EventStore;
using Compze.SystemCE;
using static System.Console;
using IEvent = Compze.Messaging.IEvent;

// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedType.Local
// ReSharper disable PartialTypeWithSinglePart

namespace Website.paradigms.semantic_events
{
   namespace NoCollision3
   {
      class Unhelpful
      {
         interface IName : IEvent;

         public void IllustrateEventListening()
         {
            MessageHandlerRegistrarWithDependencyInjectionSupport registrar = ((MessageHandlerRegistrarWithDependencyInjectionSupport)null!).NotNull();

            #region Unhelpful
            registrar.ForEvent<IName>(nameEvent => WriteLine($"Uhmm... What is happening here?"));
            #endregion

            #region helpful
            registrar
              .ForEvent<UserEvent.Profile.PropertyUpdated.IName>(clarity => WriteLine($"Ahh: {clarity.Name}"));
            #endregion

            #region helpful2
            registrar
              .ForEvent<IUserEvent.IProfileEvent.IPropertyUpdated.IName>(clarity => WriteLine($"Ahh: {clarity.Name}"));
            #endregion
         }
      }

      #region nested-events
      partial class UserEvent
      {
         internal interface IUserEvent : IAggregateEvent;

         internal static partial class Profile
         {
            internal interface IProfileEvent : IUserEvent;

            internal partial class PropertyUpdated
            {
               internal interface IName : IProfileEvent
               {
                  string Name { get; }
               }
            }
         }
      }
      #endregion

      #region nested-events2
      interface IUserEvent : IAggregateEvent
      {
         internal interface IProfileEvent : IUserEvent
         {
            internal interface IPropertyUpdated : IProfileEvent
            {
               internal interface IName : IPropertyUpdated
               {
                  string Name { get; }
               }
            }
         }
      }
      #endregion
   }
}