﻿// ReSharper disable All

using System;

#pragma warning disable IDE0059 // Unnecessary assignment of a value
#pragma warning disable IDE0051 // Remove unused private members

//When persisting event we would only persist the wrapped part. Thus changing from unwrapped-uninheritable to inheritable does not break storage and maybe one could even move events between clases in the hierarchy?
namespace ScratchPad.SemanticEvents.v02;

//todo: There can only be one EventId per event. So nested entities will NOT raise IExactlyOnceEvents directly.
//But then, what about publishing non-aggregate events on the bus? Is there a real issue or am I just imagining things?
interface IExactlyOnceEvent
{
   Guid EventId { get; }
}

interface IAggregateEvent : IExactlyOnceEvent
{
   Guid AggregateId { get; }
}

interface IWrapperEvent<out TWrappedEvent>
{
   TWrappedEvent Event { get; }
}

interface IInheritableAggregateEvent<out TInheritorEvent> : IWrapperEvent<TInheritorEvent> where TInheritorEvent : IAggregateEvent {}
interface IAnimalWrapperEvent<out TInheritorEvent> : IInheritableAggregateEvent<TInheritorEvent> where TInheritorEvent : IAnimalEvent {}
interface IBirdWrapperEvent<out TInheritorEvent> : IAnimalWrapperEvent<TInheritorEvent> where TInheritorEvent : IAnimalEvent {}
interface IAnimalEvent : IAggregateEvent {}
interface IAnimalBorn : IAnimalEvent {}
interface IBirdEvent : IAnimalEvent {}
interface IBirdChirpsEvent : IBirdEvent {}

public class AggregateInheritance
{
   public void DemonstrateSemanticRelationships()
   {
      IAnimalWrapperEvent<IAnimalEvent> animalEventAnimalWrapped = null!;
      IAnimalWrapperEvent<IAnimalBorn> animalBornEventAnimalWrapped = null!;

      IBirdWrapperEvent<IAnimalEvent> animalEventBirdWrapped = null!;
      IBirdWrapperEvent<IAnimalBorn> animalBornEventBirdWrapped = null!;
      IBirdWrapperEvent<IBirdChirpsEvent> birdChirpsEventBirdWrapped = null!;

      //Semantic relationships and unique type identity for events is maintained without having to recreate the inheritance hierarchy for each inheritor.
      //An inheritable aggregate would publish the inner event just like now, it would be automatically wrapped by the framework.
      //Would that happen within the aggregate, or only once the event has been published?
      //We would only persist the inner event in the store and bus. Thus changing ones mind in either direction would not break persisted data.
      animalEventAnimalWrapped = animalBornEventAnimalWrapped = animalBornEventBirdWrapped;
      animalEventAnimalWrapped = animalEventBirdWrapped = animalBornEventBirdWrapped;
      animalEventAnimalWrapped = birdChirpsEventBirdWrapped;

      //For registering handlers we could enable registering via the wrapped type so that handlers need not always do the unwrapping.
      //Listeners could listen to either the wrapped or the unwrapped event. They only _have_ to use the wrapped event if they want to get only inheritor events, and not the base types events.
      //Thus no code breaks when you decide to make your aggregate inheritable. All existing listeners still work just fine.
   }
}

interface IAnimalComponentEvent<out TComponentEvent> : IAnimalEvent {}
interface IBirdComponentEvent<out TComponentEvent> : IAnimalComponentEvent<TComponentEvent> {}

public class ReUsableAggregateComponentsInInheritableAggregates
{
   static void DemonstrateSemanticRelationships()
   {
      IAnimalWrapperEvent<IAnimalComponentEvent<IComponentEventBase>> componentEventBaseAnimalWrapped = null!;
      IAnimalWrapperEvent<IAnimalComponentEvent<IComponentEvent1>> componentEvent1AnimalWrapped = null!;
      IAnimalWrapperEvent<IAnimalComponentEvent<IComponentEvent2>> componentEvent2AnimalWrapped = null!;

      IBirdWrapperEvent<IBirdComponentEvent<IComponentEventBase>> componentEventBaseBirdWrapped = null!;
      IBirdWrapperEvent<IBirdComponentEvent<IComponentEvent1>> componentEvent1BirdWrapped = null!;
      IBirdWrapperEvent<IBirdComponentEvent<IComponentEvent2>> componentEvent2BirdWrapped = null!;

      //Semantic relationships are maintained.
      componentEventBaseAnimalWrapped = componentEvent1AnimalWrapped = componentEvent2AnimalWrapped;
      componentEventBaseBirdWrapped = componentEvent1BirdWrapped = componentEvent2BirdWrapped;

      componentEventBaseAnimalWrapped = componentEventBaseBirdWrapped;
      componentEvent1AnimalWrapped = componentEvent1BirdWrapped;
   }
}
