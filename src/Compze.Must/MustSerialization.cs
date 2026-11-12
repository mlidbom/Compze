using System.Linq.Expressions;
using Compze.Must._private.Serialization.Converters;
using Compze.Must._private.Serialization.Rules;
using Compze.Threading;

namespace Compze.Must;

/// <summary>
/// The rules every assertion serializes values by, in its failure messages and in <see cref="DeepEqualAssertions"/>'
/// comparisons: for types that should never be serialized, or should be serialized differently, wherever they appear.
/// </summary>
/// <remarks>
/// Set them once, where the test framework runs code as the test assembly starts. A rule changed while tests run
/// applies to the assertions that start after the change, including those of tests already running in parallel.
/// An assertion chain can add rules of its own through <see cref="ISerializationConfig{TValue}"/>; those take precedence.
/// </remarks>
/// <remarks>
/// The rules cost nothing per value: Newtonsoft applies them once per type, when it first serializes a value of the type.
/// </remarks>
public static class MustSerialization
{
   static readonly IMonitor Monitor = IMonitor.New();
   static volatile SerializationRules _rules = SerializationRules.None;

   internal static SerializationRules Rules => _rules;

   /// <summary>
   /// The longest text, in characters, a failure message shows in full: a value's JSON, its <c>ToString()</c>, or a diff.<br/>
   /// Longer text is written to a file under the test assembly's output folder, and the message shows its start and the file's path.
   /// </summary>
   public static int LongestTextShownInFull { get; set; } = 100_000;

   /// <summary>Writes every value of <typeparamref name="TExcluded"/>, or of a type derived from it, as a short marker naming its type, instead of serializing it.</summary>
   public static void ExcludeType<TExcluded>() => ExcludeType(typeof(TExcluded));

   /// <summary>
   /// Writes every value of <paramref name="excluded"/>, or of a type derived from it, as a short marker naming its type,
   /// instead of serializing it. An open generic type, such as <c>typeof(List&lt;&gt;)</c>, excludes every type constructed from it.
   /// </summary>
   public static void ExcludeType(Type excluded) => Add(rules => rules.WithTypeRule(new ExcludedTypeJsonConverter(excluded)));

   /// <summary>Leaves the member selected by <paramref name="member"/> out, wherever its declaring type, or a type derived from it, appears in an object graph.</summary>
   public static void ExcludeMember<TDeclaring, TMember>(Expression<Func<TDeclaring, TMember>> member) =>
      Add(rules => rules.WithExcludedMember(ExcludedMember.Of(member.ExtractFinalMemberInfo())));

   /// <summary>
   /// Leaves the member named <paramref name="memberName"/> on <paramref name="declaringType"/> out, wherever that type,
   /// or a type derived from it, appears in an object graph.<br/>
   /// For members a test cannot select in an expression: private ones, and those of types the test cannot name.
   /// </summary>
   public static void ExcludeMember(Type declaringType, string memberName) =>
      Add(rules => rules.WithExcludedMember(new ExcludedMember(declaringType, memberName)));

   /// <summary>Serializes, in place of every value of <typeparamref name="TReplaced"/>, the value <paramref name="replacement"/> returns for it.</summary>
   public static void SerializeAs<TReplaced>(Func<TReplaced, object?> replacement) =>
      Add(rules => rules.WithTypeRule(new SerializeAsJsonConverter<TReplaced>(replacement)));

   static void Add(Func<SerializationRules, SerializationRules> addRule) => Monitor.Locked(() => _rules = addRule(_rules));
}
