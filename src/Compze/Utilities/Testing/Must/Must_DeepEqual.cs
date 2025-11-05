using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Testing.Must.Serialization;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
namespace Compze.Utilities.Testing.Must;

public class EquivalencyConfig<TValue>
{
   internal HashSet<MemberInfo> ExcludedMembers { get; } = new();
   internal bool TypesIgnored { get; private set; }

   public EquivalencyConfig<TValue> ExcludeTypeMember<TMember>(Expression<Func<TValue, TMember>> memberExpression) =>
      ExcludedMembers.Add(memberExpression.ExtractFinalMemberInfo())
                     .then(this);

   public EquivalencyConfig<TValue> IgnoreTypes() => this.mutate(it => it.TypesIgnored = true);
}

public static class Must_DeepEqual
{
   public static IAssertionContext<TValue> DeepEqual<TValue>(this IAssertionContext<TValue> context,
                                                TValue expected,
                                                Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null,
                                                [CallerArgumentExpression(nameof(expected))]
                                                string expectedExpression = null!) =>
      DeepEqualPrivate(context, expected, config, expectedExpression);

   public static IAssertionContext<TValue> DeepEqualPrivate<TValue>(this IAssertionContext<TValue> context,
                                                       TValue expected,
                                                       Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null,
                                                       [CallerArgumentExpression(nameof(expected))]
                                                       string expectedExpression = null!) =>
      DeepEqualCore(context, expected, expectedExpression, TestingJsonSettings.AllMembers, config);

   public static IAssertionContext<TValue> DeepEqualInternal<TValue>(this IAssertionContext<TValue> context,
                                                         TValue expected,
                                                         Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null,
                                                         [CallerArgumentExpression(nameof(expected))]
                                                         string expectedExpression = null!)
      => DeepEqualCore(context, expected, expectedExpression, TestingJsonSettings.InternalAndPublicMembers, config);

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
      
      if (equivalencyConfig.TypesIgnored)
      {
         serializerSettings = new JsonSerializerSettings(serializerSettings)
         {
            TypeNameHandling = TypeNameHandling.None
         };
      }

      var actualJson = JsonConvert.SerializeObject(context.Actual, serializerSettings);
      var expectedJson = JsonConvert.SerializeObject(expected, serializerSettings);

      return context.SatisfyInternal(it => actualJson == expectedJson,
                          messageOverride: _ =>
                             $"""
                              {context.FailingAssertionHeading(nameof(DeepEqual), [new(expectedExpression, expected)])}
                              {context.Diff(expectedJson, actualJson)}
                              {context.ArgumentValue(context.Expression, context.Actual)}
                              {context.ArgumentValue(expectedExpression, expected)}
                              """);
   }
}
