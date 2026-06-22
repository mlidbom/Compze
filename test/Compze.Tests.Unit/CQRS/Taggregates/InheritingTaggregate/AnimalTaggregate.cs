using Compze.Abstractions.Public;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents.Public;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning  disable CA1812 // Avoid uninstantiated internal classes # used via reflection

namespace Compze.Tests.Unit.CQRS.Taggregates.InheritingTaggregate;

class AnimalTaggregate : Taggregate<AnimalTaggregate, IAnimalTevent, AnimalTevent, IAnimalTevent<IAnimalTevent>, AnimalTevent<AnimalTevent>>
{
   protected override Type WrapperTEventImplementation => typeof(AnimalTevent<AnimalTevent>);

   protected AnimalTaggregate()
   {
      RegisterTeventAppliers()
        .For<IAnimalTevent.Born>(tevent => TimeOfBirth = tevent.UtcTimeStamp);
    }

   public DateTime TimeOfBirth { get; private set; }

   protected void RegisterBirthProtected() => Publish(new AnimalTevent.Born(new TaggregateId()));
}

class CatTaggregate : AnimalTaggregate
{
   CatTaggregate(){}
   protected override Type WrapperTEventImplementation => typeof(CatTevent<CatTevent>);

   public static CatTaggregate RegisterBirth()
   {
      var cat = new CatTaggregate();
      cat.RegisterBirthProtected();
      return cat;
   }
}

class DogTaggregate : AnimalTaggregate
{
   DogTaggregate(){}
   protected override Type WrapperTEventImplementation => typeof(DogTevent<DogTevent>);

   public static DogTaggregate RegisterBirth()
   {
      var dog = new DogTaggregate();
      dog.RegisterBirthProtected();
      return dog;
   }
}


interface IAnimalTevent<out T> : ITaggregateIdentifyingTevent<T> where T : IAnimalTevent;

interface IAnimalTevent : ITaggregateTevent
{
#pragma warning disable CA1715 // Nested event interface follows semantic events naming convention (compze.net/paradigms/semantic-events/event-naming.html)
   interface Born : IAnimalTevent, ITaggregateCreatedTevent;
#pragma warning restore CA1715
}

class AnimalTevent<T>(T tevent) : TaggregateIdentifyingTevent<T>(tevent), IAnimalTevent<T> where T : IAnimalTevent;

class AnimalTevent : TaggregateTevent, IAnimalTevent
{
   protected AnimalTevent(){}
   AnimalTevent(TaggregateId taggregateId):base(taggregateId){ }

    internal class Born(TaggregateId taggregateId) : AnimalTevent(taggregateId), IAnimalTevent.Born;
}

interface ICatTevent : IAnimalTevent;
class CatTevent<T>(T tevent) : AnimalTevent<T>(tevent) where T : IAnimalTevent;
class CatTevent : AnimalTevent, ICatTevent;
interface IDogTevent : IAnimalTevent;
class DogTevent<T>(T tevent) : AnimalTevent<T>(tevent) where T : IAnimalTevent;
class DogTevent : AnimalTevent, IDogTevent;




