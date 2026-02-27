using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Compze.Contracts.Exceptions;

namespace Compze.Contracts;

/// <summary>Extension methods on <see cref="PipeAssertTarget{T}"/>.</summary>
public static class TAssertions
{
   ///<summary>Throws <see cref="AssertionFailedException"/> if the value is null. Returns the value as non-nullable on success.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   [return: NotNull] public static T NotNull<T>(this PipeAssertTarget<T> @this)
   {
      if(@this.Value is null) @this.ThrowAssertionFailed();
      return @this.Value;
   }

   ///<summary>Throws <see cref="AssertionFailedException"/> if the value equals <c>default(T)</c>. Returns the value on success.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static T NotDefault<T>(this PipeAssertTarget<T> @this) where T : struct
   {
      if(EqualityComparer<T>.Default.Equals(@this.Value, default)) @this.ThrowAssertionFailed();
      return @this.Value;
   }
}
