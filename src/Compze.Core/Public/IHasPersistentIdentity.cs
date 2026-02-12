using System;

namespace Compze.Core.Public;

/// <summary>Anything that can be uniquely identified using its id over any number of persist/load cycles.</summary>
public interface IEntity<TKeyType> where TKeyType : IEquatable<TKeyType>
{
   /// <summary>The unique identifier for this instance.</summary>
   EntityId<TKeyType> Id { get; }
}