using System;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Tests.Unit.CQRS.Taggregates.InheritingTaggregate;

class AnimalTaggregate : Taggregate<AnimalTaggregate, IAnimalTevent, AnimalTevent, IAnimalTevent<IAnimalTevent>, AnimalTevent<AnimalTevent>>
{
   protected override Type WrapperTEventImplementation => typeof(AnimalTevent<AnimalTevent>);
}

class CatTaggregate : AnimalTaggregate
{
   protected override Type WrapperTEventImplementation => typeof(CatTevent<CatTevent>);
}


public interface IAnimalTevent<out T> : ITaggregateIdentifyingTevent<T> where T : IAnimalTevent {}
public interface IAnimalTevent : ITaggregateTevent {}
public class AnimalTevent<T>(T tevent) : TaggregateIdentifyingTevent<T>(tevent), IAnimalTevent<T> where T : IAnimalTevent {}
public class AnimalTevent : TaggregateTevent, IAnimalTevent {}


public interface ICatTevent<out T> : IAnimalTevent<T> where T : IAnimalTevent { }
public interface ICatTevent : IAnimalTevent
{
}

public class CatTevent<T>(T tevent) : AnimalTevent<T>(tevent) where T : IAnimalTevent {}
public class CatTevent : AnimalTevent, ICatTevent{}




