using System.Runtime.CompilerServices;
// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>Entry point for assertions: the <see cref="Must{T}(T, string)"/> extension that begins a chain over any value.</summary>
public static class MustExtensions
{
   /// <summary>Begins an assertion chain over <paramref name="subject"/>, e.g. <c>subject.Must().Be(expected)</c>.</summary>
   public static IAssertionContext<T> Must<T>(this T subject, [CallerArgumentExpression(nameof(subject))] string expression = null!) =>
      new AssertionContext<T>(subject, expression);
}
