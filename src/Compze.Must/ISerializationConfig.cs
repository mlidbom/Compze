using System.Linq.Expressions;

namespace Compze.Must;

/// <summary>
/// Changes how an assertion chain serializes values: what its failure messages show, and what
/// <see cref="DeepEqualAssertions"/>' assertions compare. Configure it on the <see cref="MustExtensions.Must{T}(T, Func{ISerializationConfig{T}, ISerializationConfig{T}}, string)"/> call:
/// <c>actual.Must(serialization: it => it.ExcludeMember(actual => actual.Id)).DeepEqual(expected)</c>.
/// </summary>
/// <remarks>
/// Must serializes values with Newtonsoft.Json. When it cannot serialize one, the failure message says what failed and
/// which of these rules leaves it out. To set a rule for every assertion, use <see cref="MustSerialization"/>; the rules
/// configured here take precedence over those.
/// </remarks>
public interface ISerializationConfig<TValue>
{
   /// <summary>Leaves the member selected by <paramref name="member"/> out, wherever its declaring type, or a type derived from it, appears in the object graph.</summary>
   ISerializationConfig<TValue> ExcludeMember<TMember>(Expression<Func<TValue, TMember>> member);

   /// <summary>
   /// Leaves the member named <paramref name="memberName"/> on <paramref name="declaringType"/> out, wherever that type,
   /// or a type derived from it, appears in the object graph.<br/>
   /// For members a test cannot select in an expression: private ones, and those of types the test cannot name.
   /// </summary>
   ISerializationConfig<TValue> ExcludeMember(Type declaringType, string memberName);

   /// <summary>Writes every value of <typeparamref name="TExcluded"/>, or of a type derived from it, as a short marker naming its type, instead of serializing it.</summary>
   ISerializationConfig<TValue> ExcludeType<TExcluded>();

   /// <summary>
   /// Writes every value of <paramref name="excluded"/>, or of a type derived from it, as a short marker naming its type,
   /// instead of serializing it. An open generic type, such as <c>typeof(List&lt;&gt;)</c>, excludes every type constructed from it.
   /// </summary>
   ISerializationConfig<TValue> ExcludeType(Type excluded);

   /// <summary>Serializes, in place of every value of <typeparamref name="TReplaced"/>, the value <paramref name="replacement"/> returns for it.</summary>
   ISerializationConfig<TValue> SerializeAs<TReplaced>(Func<TReplaced, object?> replacement);

   /// <summary>Writes no <c>$type</c> annotations, so that <see cref="DeepEqualAssertions"/>' assertions compare values of different types by their members alone.</summary>
   ISerializationConfig<TValue> WithoutTypeNames();
}
