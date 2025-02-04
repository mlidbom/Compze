using System;
using System.Collections.Generic;

using System.Linq;
using Compze.Contracts;
using Compze.GenericAbstractions.Wrappers;
using Compze.SystemCE.LinqCE;
using static Compze.Contracts.Assert;

namespace Compze.GenericAbstractions.Hierarchies;

/// <summary>
/// Represents a hierarchy in which the instances in the hierarchy do not themselves
/// implement <see cref="IHierarchy{T}"/>.
/// 
/// <example>
/// For instance you could use <see cref="HierarchyExtensions.AsHierarchy{T}"/> like this:
/// <code>
///     directoryName.AsHierarchy&lt;
/// </code>
/// </example>
/// 
/// </summary>
interface IAutoHierarchy<T> : IHierarchy<IAutoHierarchy<T>>, IWrapper<T>;

/// <summary>
/// Provides extension methods for working with hierarchical data.
/// </summary>
static class HierarchyExtensions
{
   class Hierarchy<T> : IAutoHierarchy<T>
   {
      readonly Func<T, IEnumerable<T>> _childGetter;

      public IEnumerable<IAutoHierarchy<T>> Children => _childGetter(Wrapped).Select(child => child.AsHierarchy(_childGetter));

      public T Wrapped { get; private set; }

      internal Hierarchy(T nodeValue, Func<T, IEnumerable<T>> childGetter)
      {
         Argument.NotNull(childGetter);
         Wrapped = nodeValue;
         _childGetter = childGetter;
      }
   }

   /// <summary>
   /// Returns an <see cref="IAutoHierarchy{T}"/> where <see cref="IWrapper{T}.Wrapped"/> is <paramref name="me"/> and
   /// <see cref="IHierarchy{T}.Children"/> is implemented via delegation to <paramref name="childGetter"/>
   /// </summary>
   public static IAutoHierarchy<T> AsHierarchy<T>(this T me, Func<T, IEnumerable<T>> childGetter)
   {
      Argument.NotNull(me).NotNull(childGetter);
      return Result.ReturnNotNull(new Hierarchy<T>(me, childGetter));
   }

   /// <summary>
   /// Returns <paramref name="root"/> and all the objects in the hierarchy
   /// below <paramref name="root"/> flattened into a sequence
   /// </summary>
   public static IEnumerable<T> Flatten<T>(this T root) where T : IHierarchy<T>
   {
      Argument.NotNull(root);
      return EnumerableCE.Create(root).FlattenHierarchy(me => me.Children);
   }
}