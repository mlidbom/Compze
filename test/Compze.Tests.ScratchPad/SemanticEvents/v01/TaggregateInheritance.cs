using Compze.Teventive.Taggregates.Tevents.Public;

// ReSharper disable All
#pragma warning disable IDE0059 // Unnecessary assignment of a value
#pragma warning disable IDE0051 // Remove unused private members

namespace Compze.Tests.ScratchPad.SemanticEvents.v01;

//When persisting tevent we would only persist the wrapped part. Thus changing from unwrapped-uninheritable to inheritable does not break storage.
interface IInheritableTaggregateTevent<out TInheritorTevent> where TInheritorTevent : ITaggregateTevent
{
   TInheritorTevent Tevent { get; }
}

interface IAnimalWrapperTevent<out TInheritorTevent> : IInheritableTaggregateTevent<TInheritorTevent> where TInheritorTevent : IAnimalTevent
{}

interface IBirdWrapperTevent<out TInheritorTevent> : IAnimalWrapperTevent<TInheritorTevent> where TInheritorTevent : IAnimalTevent
{}

interface IAnimalTevent : ITaggregateTevent{}

interface IAnimalBorn : IAnimalTevent{}

interface IBirdTevent : IAnimalTevent{}

interface IBirdChirpsTevent : IBirdTevent{}

public class TaggregateInheritance
{
   public void DemonstrateSemanticRelationships()
   {
      IAnimalWrapperTevent<IAnimalTevent> animalTeventAnimalWrapped = null!;
      IAnimalWrapperTevent<IAnimalBorn> animalBornTeventAnimalWrapped = null!;

      IBirdWrapperTevent<IAnimalTevent> animalTeventBirdWrapped = null!;
      IBirdWrapperTevent<IAnimalBorn> animalBornTeventBirdWrapped = null!;
      IBirdWrapperTevent<IBirdChirpsTevent> birdChirpsTeventBirdWrapped = null!;

      //Semantic relationships and unique type identity for tevents is maintained without having to recreate the inheritance hierarchy for each inheritor.
      //An inheritable taggregate would publish the inner tevent just like now, it would be automatically wrapped by the framework.
      //Would that happen within the taggregate, or only once the tevent has been published?
      //We would only persist the inner tevent in the store and bus. Thus changing ones mind in either direction would not break persisted data.
      animalTeventAnimalWrapped = animalBornTeventAnimalWrapped = animalBornTeventBirdWrapped;
      animalTeventAnimalWrapped = animalTeventBirdWrapped = animalBornTeventBirdWrapped;
      animalTeventAnimalWrapped = birdChirpsTeventBirdWrapped;

      //For registering handlers we could enable registering via the wrapped type so that handlers need not always do the unwrapping.
      //Listeners could listen to either the wrapped or the unwrapped tevent. They only _have_ to use the wrapped tevent if they want to get only inheritor tevents, and not the base types tevents.
      //Thus no code breaks when you decide to make your taggregate inheritable. All existing listeners still work just fine.
   }
}

interface IAnimalComponentTevent<out TComponentTevent> : IAnimalTevent{}

interface IBirdComponentTevent<out TComponentTevent> : IAnimalComponentTevent<TComponentTevent>{}

public class ReUsableTaggregateComponentsInInheritableTaggregates
{
   static void DemonstrateSemanticRelationships()
   {

      IAnimalWrapperTevent<IAnimalComponentTevent<IComponentTeventBase>> componentTeventBaseAnimalWrapped = null!;
      IAnimalWrapperTevent<IAnimalComponentTevent<IComponentTevent1>> componentTevent1AnimalWrapped = null!;
      IAnimalWrapperTevent<IAnimalComponentTevent<IComponentTevent2>> componentTevent2AnimalWrapped = null!;

      IBirdWrapperTevent<IBirdComponentTevent<IComponentTeventBase>> componentTeventBaseBirdWrapped = null!;
      IBirdWrapperTevent<IBirdComponentTevent<IComponentTevent1>> componentTevent1BirdWrapped = null!;
      IBirdWrapperTevent<IBirdComponentTevent<IComponentTevent2>> componentTevent2BirdWrapped = null!;

      //Semantic relationships are maintained.
      componentTeventBaseAnimalWrapped = componentTevent1AnimalWrapped = componentTevent2AnimalWrapped;
      componentTeventBaseBirdWrapped = componentTevent1BirdWrapped = componentTevent2BirdWrapped;

      componentTeventBaseAnimalWrapped = componentTeventBaseBirdWrapped;
      componentTevent1AnimalWrapped = componentTevent1BirdWrapped;
   }
}
