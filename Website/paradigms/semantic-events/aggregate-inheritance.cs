using System.Collections.Generic;
using Compze.Messaging.Buses;
using Compze.Persistence.EventStore;
using Compze.SystemCE;
using static System.Console;

// ReSharper disable once CheckNamespace
namespace Website.paradigms.semantic_events.InheritingAggregates;

#region noises1
interface IAnimalEvent : IAggregateEvent
{
   interface IBorn : IAnimalEvent, IAggregateCreatedEvent;
}

interface ICatEvent : IAnimalEvent;
interface IDogEvent : IAnimalEvent;
#endregion
#region noises1wrapped
interface IAnimalEvent<out T> : IAggregateWrapperEvent<T> where T : IAnimalEvent;
interface ICatEvent<out T> : IAnimalEvent<T> where T : IAnimalEvent;
interface IDogEvent<out T> : IAnimalEvent<T> where T : IAnimalEvent;
#endregion

class Examples
{
   public void Enumerables()
   {
      #region enumerable-type-compatibility
      IEnumerable<object> objects = [new object(), new object()];
      IEnumerable<string> strings = ["1", "2"];
      objects = strings;
      #endregion
   }

   public void Listeners()
   {
      MessageHandlerRegistrarWithDependencyInjectionSupport registrar = ((MessageHandlerRegistrarWithDependencyInjectionSupport)null!).NotNull();

      #region doglistener
      registrar
        .ForEvent<IDogEvent<IAnimalEvent.IBorn>>(born => WriteLine($"Dog Id:{born.Event.AggregateId} was born!"));
      #endregion
      #region catlistener
      registrar
        .ForEvent<ICatEvent<IAnimalEvent.IBorn>>(born => WriteLine($"Cat Id:{born.Event.AggregateId} was born!"));
      #endregion
      #region animallistener
      registrar
        .ForEvent<IAnimalEvent.IBorn>(born => WriteLine($"Animal Id:{born.AggregateId} was born!"));
      #endregion

      #region wrappedanimallistener
      registrar
        .ForEvent<IAnimalEvent<IAnimalEvent.IBorn>>(
            born => WriteLine($"{born.GetType().Name.Replace("Event", "")} Id: {born.Event.AggregateId}, was born!"));
      #endregion
   }
}
