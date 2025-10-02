namespace Compze.DDD;

/// <summary>Anything that can be uniquely identified using its id over any number of persist/load cycles.</summary>
public interface IHasPersistentIdentity<out TKeyType>
{
   /// <summary>The unique identifier for this instance.</summary>
   TKeyType Id { get; }
}