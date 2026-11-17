using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning  disable CA1812 // Avoid uninstantiated internal classes # used via reflection

namespace Compze.Tests.Unit.CQRS.Taggregates.InheritingTaggregate;

class AnimalTaggregate : Taggregate<AnimalTaggregate, IAnimalTevent, AnimalTevent, IAnimalTevent<IAnimalTevent>, AnimalTevent<AnimalTevent>>, ITeventRouted
{
   protected static readonly TeventRouting<AnimalTaggregate, IAnimalTevent> AnimalAppliers = DeclareAppliers(it => it
     .For<IAnimalTevent.Born>(static (animal, tevent) => animal.TimeOfBirth = tevent.UtcTimeStamp));

   TeventRouting ITeventRouted.TeventAppliers => AnimalAppliers;

   protected override Type WrapperTeventImplementation => typeof(AnimalTevent<AnimalTevent>);

   protected AnimalTaggregate() {}

   public DateTime TimeOfBirth { get; private set; }

   protected void RegisterBirthProtected() => Publish(new AnimalTevent.Born(new TaggregateId()));
}

class CatTaggregate : AnimalTaggregate, ITeventRouted
{
   static readonly TeventRouting<CatTaggregate, IAnimalTevent> CatAppliers = AnimalAppliers.Extend<CatTaggregate>(_ => {});

   TeventRouting ITeventRouted.TeventAppliers => CatAppliers;

   CatTaggregate(){}
   protected override Type WrapperTeventImplementation => typeof(CatTevent<CatTevent>);

   public static CatTaggregate RegisterBirth()
   {
      var cat = new CatTaggregate();
      cat.RegisterBirthProtected();
      return cat;
   }
}

class DogTaggregate : AnimalTaggregate, ITeventRouted
{
   static readonly TeventRouting<DogTaggregate, IAnimalTevent> DogAppliers = AnimalAppliers.Extend<DogTaggregate>(_ => {});

   TeventRouting ITeventRouted.TeventAppliers => DogAppliers;

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
#pragma warning disable CA1715 // Nested event interface follows semantic events naming convention (compze.net/paradigms/semantic-events/event-naming.html)
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




