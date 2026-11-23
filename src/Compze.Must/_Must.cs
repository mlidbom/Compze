using Compze.Must._private.Serialization.Rules;

namespace Compze.Must;

/// <summary>The non-generic context for a <see cref="MustExtensions.Must{T}(T, string)"/> assertion chain: the captured expression text plus a way to narrow to a typed context.</summary>
public interface IAssertionContext
{
   /// <summary>The source text of the expression under assertion, captured automatically from the call site by the compiler.</summary>
   string Expression { get; }
   /// <summary>Narrows this context to an assertion over a value of type <typeparamref name="T"/>.</summary>
   IAssertionContext<T> Cast<T>();
   /// <summary>The rules this assertion chain serializes values by: its own, configured on <see cref="MustExtensions.Must{T}(T, Func{ISerializationConfig{T}, ISerializationConfig{T}}, string)"/>, before the global ones of <see cref="MustSerialization"/>.</summary>
   internal SerializationRules SerializationRules { get; }
}

/// <summary>
/// The context for a <see cref="MustExtensions.Must{T}(T, string)"/> assertion chain over a value of type <typeparamref name="T"/>. Assertions are extension methods on this interface.<br/>
/// For a check that no Compze.Must assertion makes, use <see cref="SatisfyAssertions.Satisfy{T}(IAssertionContext{T}, Func{T, bool}, Func{T, string}, string)"/>:<br/>
/// it takes any predicate, and its failure shows the predicate and the value.
/// </summary>
public interface IAssertionContext<out T> : IAssertionContext
{
   /// <summary>The value under assertion.</summary>
   T Actual { get; }
}
