using System.Runtime.CompilerServices;
using Compze.Must._private;
using Compze.Must._private.Serialization.Rules;
// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>Entry point for assertions: the <see cref="Must{T}(T, string)"/> extension that begins a chain over any value.</summary>
public static class MustExtensions
{
   /// <summary>Begins an assertion chain over <paramref name="subject"/>, e.g. <c>subject.Must().Be(expected)</c>.</summary>
   public static IAssertionContext<T> Must<T>(this T subject, [CallerArgumentExpression(nameof(subject))] string expression = null!) =>
      new AssertionContext<T>(subject, expression, MustSerialization.Rules);

   /// <summary>
   /// Begins an assertion chain over <paramref name="subject"/> that serializes values by rules of its own, for its failure messages
   /// and for <see cref="DeepEqualAssertions"/>' comparisons, e.g.
   /// <c>subject.Must(serialization: it => it.ExcludeMember(subject => subject.Id)).DeepEqual(expected)</c>.<br/>
   /// These rules take precedence over the global ones of <see cref="MustSerialization"/>.
   /// </summary>
   /// <param name="subject">The value to assert on.</param>
   /// <param name="serialization">Configures the chain's own serialization rules.</param>
   /// <param name="expression">The source text of <paramref name="subject"/>, captured by the compiler.</param>
   public static IAssertionContext<T> Must<T>(this T subject,
                                              Func<ISerializationConfig<T>, ISerializationConfig<T>> serialization,
                                              [CallerArgumentExpression(nameof(subject))] string expression = null!) =>
      new AssertionContext<T>(subject, expression, SerializationConfig<T>.RulesFrom(serialization).TakingPrecedenceOver(MustSerialization.Rules));
}
