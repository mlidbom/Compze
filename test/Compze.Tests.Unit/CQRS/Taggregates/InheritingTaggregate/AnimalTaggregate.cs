using System;
using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

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
   public CatTaggregate(){}
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


public interface IAnimalTevent<out T> : ITaggregateIdentifyingTevent<T> where T : IAnimalTevent {}

public interface IAnimalTevent : ITaggregateTevent
{
#pragma warning disable CA1715 // Nested event interface follows semantic events naming convention (compze.net/paradigms/semantic-events/event-naming.html)
   interface Born : IAnimalTevent, ITaggregateCreatedTevent{}
#pragma warning restore CA1715
}

public class AnimalTevent<T>(T tevent) : TaggregateIdentifyingTevent<T>(tevent), IAnimalTevent<T> where T : IAnimalTevent{}

public class AnimalTevent : TaggregateTevent, IAnimalTevent
{
   protected AnimalTevent(){}
   AnimalTevent(TaggregateId taggregateId):base(taggregateId){ }

    public class Born : AnimalTevent, IAnimalTevent.Born
   {
      public Born(TaggregateId taggregateId) : base(taggregateId){}
   }
}


public interface ICatTevent<out T> : IAnimalTevent<T> where T : IAnimalTevent { }
public interface ICatTevent : IAnimalTevent {}
public class CatTevent<T>(T tevent) : AnimalTevent<T>(tevent) where T : IAnimalTevent {}
public class CatTevent : AnimalTevent, ICatTevent{}


public interface IDogTevent<out T> : IAnimalTevent<T> where T : IAnimalTevent { }
public interface IDogTevent : IAnimalTevent {}
public class DogTevent<T>(T tevent) : AnimalTevent<T>(tevent) where T : IAnimalTevent {}
public class DogTevent : AnimalTevent, IDogTevent{}




