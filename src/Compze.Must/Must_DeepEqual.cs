using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Must.Serialization;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
namespace Compze.Must;

/// <summary>Configures a deep-equality comparison (<see cref="Must_DeepEqual"/>): which members to exclude and whether to ignore declared types.</summary>
public class EquivalencyConfig<TValue>
{
   internal HashSet<MemberInfo> ExcludedMembers { get; } = [];
   internal bool TypesIgnored { get; private set; }

   /// <summary>Excludes the member selected by <paramref name="memberExpression"/> from the comparison.</summary>
   public EquivalencyConfig<TValue> ExcludeTypeMember<TMember>(Expression<Func<TValue, TMember>> memberExpression) =>
      ExcludedMembers.Add(memberExpression.ExtractFinalMemberInfo())
                     .__(this);

   /// <summary>Ignores declared type information so that values of different types compare equal when their members match.</summary>
   public EquivalencyConfig<TValue> IgnoreTypes() => this._mutate(it => it.TypesIgnored = true);
}

/// <summary>Deep structural-equality assertions that compare two values by serializing them and diffing the results.</summary>
public static class Must_DeepEqual
{
   /// <summary>Asserts deep structural equality with <paramref name="expected"/>, comparing all members (public, internal, and private). Equivalent to <see cref="DeepEqualPrivate{TValue}(IAssertionContext{TValue}, TValue, System.Func{EquivalencyConfig{TValue}, EquivalencyConfig{TValue}}, string)"/>.</summary>
   public static IAssertionContext<TValue> DeepEqual<TValue>(this IAssertionContext<TValue> context,
                                                             TValue expected,
                                                             Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null,
                                                             [CallerArgumentExpression(nameof(expected))]
                                                             string expectedExpression = null!) =>
      DeepEqualPrivate(context, expected, config, expectedExpression);

   /// <summary>Asserts deep structural equality with <paramref name="expected"/>, comparing all members including private ones.</summary>
   public static IAssertionContext<TValue> DeepEqualPrivate<TValue>(this IAssertionContext<TValue> context,
                                                                    TValue expected,
                                                                    Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null,
                                                                    [CallerArgumentExpression(nameof(expected))]
                                                                    string expectedExpression = null!) =>
      DeepEqualCore(context, expected, expectedExpression, TestingJsonSettings.AllMembers, config);

   /// <summary>Asserts deep structural equality with <paramref name="expected"/>, comparing internal and public members.</summary>
   public static IAssertionContext<TValue> DeepEqualInternal<TValue>(this IAssertionContext<TValue> context,
                                                                     TValue expected,
                                                                     Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null,
                                                                     [CallerArgumentExpression(nameof(expected))]
                                                                     string expectedExpression = null!)
      => DeepEqualCore(context, expected, expectedExpression, TestingJsonSettings.InternalAndPublicMembers, config);

   /// <summary>Asserts deep structural equality with <paramref name="expected"/>, comparing only public members.</summary>
   public static IAssertionContext<TValue> DeepEqualPublic<TValue>(this IAssertionContext<TValue> context,
                                                                   TValue expected,
                                                                   Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null,
                                                                   [CallerArgumentExpression(nameof(expected))]
                                                                   string expectedExpression = null!)
      => DeepEqualCore(context, expected, expectedExpression, TestingJsonSettings.PublicMembers, config);

   static IAssertionContext<TValue> DeepEqualCore<TValue>(IAssertionContext<TValue> context,
                                                          TValue expected,
                                                          string expectedExpression,
                                                          JsonSerializerSettings settings,
                                                          Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null)
   {
      var equivalencyConfig = (config ?? (conf => conf))(new EquivalencyConfig<TValue>());
      var serializerSettings = TestingJsonSettings.CreateSettingsWithExclusions(settings, equivalencyConfig.ExcludedMembers);

      if(equivalencyConfig.TypesIgnored)
      {
         serializerSettings = new JsonSerializerSettings(serializerSettings)
                              {
                                 TypeNameHandling = TypeNameHandling.None
                              };
      }

      var actualJson = JsonConvert.SerializeObject(context.Actual, serializerSettings);
      var expectedJson = JsonConvert.SerializeObject(expected, serializerSettings);

      return context.RunAssertion(_ => actualJson == expectedJson,
                                     messageOverride: _ =>
                                        $"""
                                         {context.FailingAssertionHeading(nameof(DeepEqual), [new(expectedExpression, expected)])}
                                         {context.Diff(expectedJson, actualJson)}
                                         {context.ExpressionValue()}
                                         {context.ExpressionValue(expectedExpression, expected)}
                                         """);
   }
}
