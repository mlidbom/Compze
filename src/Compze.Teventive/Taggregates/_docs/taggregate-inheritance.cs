using Compze.Abstractions;
using Compze.Tessaging.TessageBus;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using static System.Console;

#pragma warning disable // Documentation example code: deliberately illustrative fragments (empty marker interfaces, never-instantiated examples), not production code.
// ReSharper disable NotAccessedVariable
// ReSharper disable RedundantAssignment

// ReSharper disable once CheckNamespace
namespace Website.Teventive.Taggregates.InheritingTaggregates;

#region noises1
interface IAnimalTevent : ITaggregateTevent
{
   interface IBorn : IAnimalTevent, ITaggregateCreatedTevent;
}
#endregion
#region noises1wrapped
interface IAnimalTevent<out T> : ITaggregateTevent<T> where T : IAnimalTevent;
interface ICatTevent<out T> : IAnimalTevent<T> where T : IAnimalTevent;
interface IDogTevent<out T> : IAnimalTevent<T> where T : IAnimalTevent;
#endregion

#region tevent-classes
abstract class AnimalTevent : TaggregateTevent, IAnimalTevent
{
   protected AnimalTevent() {}
   AnimalTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Born(TaggregateId taggregateId) : AnimalTevent(taggregateId), IAnimalTevent.IBorn;
}

class AnimalTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IAnimalTevent<T> where T : IAnimalTevent;
class CatTevent<T>(T tevent) : AnimalTevent<T>(tevent), ICatTevent<T> where T : IAnimalTevent;
class DogTevent<T>(T tevent) : AnimalTevent<T>(tevent), IDogTevent<T> where T : IAnimalTevent;
#endregion

#region animal
abstract class Animal :
   Taggregate<Animal, IAnimalTevent, AnimalTevent, IAnimalTevent<IAnimalTevent>, AnimalTevent<AnimalTevent>>
{
   protected static readonly TeventRouting<Animal, IAnimalTevent> AnimalAppliers = DeclareAppliers(it => it
     .For<IAnimalTevent.IBorn>(static (animal, born) => animal.TimeOfBirth = born.UtcTimeStamp));

   public DateTime TimeOfBirth { get; private set; }

   protected void PublishBirth() => Publish(new AnimalTevent.Born(TaggregateId.New()));
}
#endregion

#region cat-and-dog
class Cat : Animal
{
   static readonly TeventRouting<Cat, IAnimalTevent> CatAppliers = AnimalAppliers.Extend<Cat>(_ => {});
   protected override TeventRouting TeventAppliers => CatAppliers;

   protected override Type WrapperTeventImplementation => typeof(CatTevent<>);

   Cat() {}

   public static Cat RegisterBirth()
   {
      var cat = new Cat();
      cat.PublishBirth();
      return cat;
   }
}

class Dog : Animal
{
   static readonly TeventRouting<Dog, IAnimalTevent> DogAppliers = AnimalAppliers.Extend<Dog>(_ => {});
   protected override TeventRouting TeventAppliers => DogAppliers;

   protected override Type WrapperTeventImplementation => typeof(DogTevent<>);

   Dog() {}

   public static Dog RegisterBirth()
   {
      var dog = new Dog();
      dog.PublishBirth();
      return dog;
   }
}
#endregion

class Examples
{
   public void Enumerables()
   {
      #region enumerable-type-compatibility
      IEnumerable<object> objects = [new(), new()];
      IEnumerable<string> strings = ["1", "2"];
      objects = strings;
      #endregion
   }

   public void Listeners()
   {
      TessageBusHandlerRegistrar registrar = null!;

      #region doglistener
      registrar
        .ForTevent<IDogTevent<IAnimalTevent.IBorn>>(born => WriteLine($"Dog Id:{born.Tevent.TaggregateId} was born!"));
      #endregion
      #region catlistener
      registrar
        .ForTevent<ICatTevent<IAnimalTevent.IBorn>>(born => WriteLine($"Cat Id:{born.Tevent.TaggregateId} was born!"));
      #endregion
      #region animallistener
      registrar
        .ForTevent<IAnimalTevent.IBorn>(born => WriteLine($"Animal Id:{born.TaggregateId} was born!"));
      #endregion

      #region wrappedanimallistener
      registrar
        .ForTevent<IAnimalTevent<IAnimalTevent.IBorn>>(born => WriteLine(born switch
         {
            ICatTevent<IAnimalTevent.IBorn> => $"A cat was born: {born.Tevent.TaggregateId}",
            IDogTevent<IAnimalTevent.IBorn> => $"A dog was born: {born.Tevent.TaggregateId}",
            _                               => $"Another animal was born: {born.Tevent.TaggregateId}"
         }));
      #endregion
   }
}
