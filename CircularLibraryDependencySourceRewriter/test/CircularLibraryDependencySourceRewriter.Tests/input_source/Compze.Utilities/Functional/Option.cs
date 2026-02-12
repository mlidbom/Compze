using System.Diagnostics.CodeAnalysis;
using Compze.Utilities.Contracts;

namespace Compze.Utilities.Functional;

public static class Option
{
   public static Option<T> Some<T>(T value) => new Some<T>(value);
   public static  Option<T> None<T>() => Functional.None<T>.Instance;
}

public abstract class Option<T> : DiscriminatedUnion<Option<T>, Some<T>, None<T>>
{}

public sealed class Some<T> : Option<T>
{
   public Some(T value)
   {
      Assert.Argument.NotNull(value);
      Value = value;
   }

   [NotNull]public T Value { get; }
}

public sealed class None<T> : Option<T>
{
   None(){}
   public static readonly None<T> Instance = new();
}