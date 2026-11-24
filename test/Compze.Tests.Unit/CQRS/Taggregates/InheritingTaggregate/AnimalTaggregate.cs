using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning  disable CA1812 // Avoid uninstantiated internal classes # used via reflection

namespace Compze.Tests.Unit.CQRS.Taggregates.InheritingTaggregate;

class AnimalTaggregate : Taggregate<AnimalTaggregate, IAnimalTevent, AnimalTevent, IAnimalTevent<IAnimalTevent>, AnimalTevent<AnimalTevent>>
{
   protected static readonly TeventRouting<AnimalTaggregate, IAnimalTevent> AnimalAppliers = DeclareAppliers(it => it
     .For<IAnimalTevent.Born>(static (animal, tevent) => animal.TimeOfBirth = tevent.UtcTimeStamp));

   protected override TeventRouting TeventAppliers => AnimalAppliers;

   protected override Type WrapperTeventImplementation => typeof(AnimalTevent<AnimalTevent>);

   protected AnimalTaggregate() {}

   DateTime TimeOfBirth { get; set; }

   protected void RegisterBirthProtected() => Publish(new AnimalTevent.Born(new TaggregateId()));

   protected AnimalSnapshot TakeAnimalSnapshot() => new(Id, TimeOfBirth);

   protected void RestoreAnimalFrom(AnimalSnapshot snapshot)
   {
      RestoreIdentity(snapshot.Id);
      TimeOfBirth = snapshot.TimeOfBirth;
   }
}

class CatTaggregate : AnimalTaggregate, ISnapshotable<CatTaggregate, AnimalSnapshot>
{
   static readonly TeventRouting<CatTaggregate, IAnimalTevent> CatAppliers = AnimalAppliers.Extend<CatTaggregate>(_ => {});

   protected override TeventRouting TeventAppliers => CatAppliers;

   CatTaggregate(){}
   protected override Type WrapperTeventImplementation => typeof(CatTevent<CatTevent>);

   public static CatTaggregate RegisterBirth()
   {
      var cat = new CatTaggregate();
      cat.RegisterBirthProtected();
      return cat;
   }

   ///<summary>A new, empty instance, to restore from a snapshot.</summary>
   internal static CatTaggregate Empty() => new();

   public AnimalSnapshot TakeSnapshot() => TakeAnimalSnapshot();
   public void RestoreFrom(AnimalSnapshot snapshot) => RestoreAnimalFrom(snapshot);
}

class DogTaggregate : AnimalTaggregate
{
   static readonly TeventRouting<DogTaggregate, IAnimalTevent> DogAppliers = AnimalAppliers.Extend<DogTaggregate>(_ => {});

   protected override TeventRouting TeventAppliers => DogAppliers;

   DogTaggregate(){}
   protected override Type WrapperTeventImplementation => typeof(DogTevent<DogTevent>);

   public static DogTaggregate RegisterBirth()
   {
      var dog = new DogTaggregate();
      dog.RegisterBirthProtected();
      return dog;
   }
}


interface IAnimalTevent<out T> : ITaggregateTevent<T> where T : IAnimalTevent;

interface IAnimalTevent : ITaggregateTevent
{
#pragma warning disable CA1715 // Nested event interface follows semantic events naming convention (compze.net/Compze/Teventive/Taggregates/_docs/tevent-naming.html)
   interface Born : IAnimalTevent, ITaggregateCreatedTevent;
#pragma warning restore CA1715
}

class AnimalTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IAnimalTevent<T> where T : IAnimalTevent;

class AnimalTevent : TaggregateTevent, IAnimalTevent
{
   protected AnimalTevent(){}
   AnimalTevent(TaggregateId taggregateId):base(taggregateId){ }

    internal class Born(TaggregateId taggregateId) : AnimalTevent(taggregateId), IAnimalTevent.Born;
}

interface ICatTevent : IAnimalTevent;
interface ICatTevent<out T> : IAnimalTevent<T> where T : IAnimalTevent;
class CatTevent<T>(T tevent) : AnimalTevent<T>(tevent), ICatTevent<T> where T : IAnimalTevent;
class CatTevent : AnimalTevent, ICatTevent;
interface IDogTevent : IAnimalTevent;
interface IDogTevent<out T> : IAnimalTevent<T> where T : IAnimalTevent;
class DogTevent<T>(T tevent) : AnimalTevent<T>(tevent), IDogTevent<T> where T : IAnimalTevent;
class DogTevent : AnimalTevent, IDogTevent;




