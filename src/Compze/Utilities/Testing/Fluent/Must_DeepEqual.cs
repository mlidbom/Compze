using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Testing.Fluent.Serialization;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
namespace Compze.Utilities.Testing.Fluent;

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
   public static IMust<TValue> DeepEqual<TValue>(this IMust<TValue> must,
                                                TValue expected,
                                                Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null,
                                                [CallerArgumentExpression(nameof(expected))]
                                                string expectedExpression = null!) =>
      DeepEqualPrivate(must, expected, config, expectedExpression);

   public static IMust<TValue> DeepEqualPrivate<TValue>(this IMust<TValue> must,
                                                       TValue expected,
                                                       Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null,
                                                       [CallerArgumentExpression(nameof(expected))]
                                                       string expectedExpression = null!) =>
      DeepEqualCore(must, expected, expectedExpression, TestingJsonSettings.AllMembers, config);

   public static IMust<TValue> DeepEqualInternal<TValue>(this IMust<TValue> must,
                                                         TValue expected,
                                                         Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null,
                                                         [CallerArgumentExpression(nameof(expected))]
                                                         string expectedExpression = null!)
      => DeepEqualCore(must, expected, expectedExpression, TestingJsonSettings.InternalAndPublicMembers, config);

   public static IMust<TValue> DeepEqualPublic<TValue>(this IMust<TValue> must,
                                                      TValue expected,
                                                      Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>>? config = null,
                                                      [CallerArgumentExpression(nameof(expected))]
                                                      string expectedExpression = null!)
      => DeepEqualCore(must, expected, expectedExpression, TestingJsonSettings.PublicMembers, config);

   static IMust<TValue> DeepEqualCore<TValue>(IMust<TValue> must,
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

      var actualJson = JsonConvert.SerializeObject(must.Actual, serializerSettings);
      var expectedJson = JsonConvert.SerializeObject(expected, serializerSettings);

      return must.Satisfy(it => actualJson == expectedJson,
                          messageOverride: _ =>
                             $"""
                              {must.Separator}
                              expected:
                              {must.Separator}
                              {must.Expression.Indent()}
                              {must.Separator}
                              to be deeply equal to:
                              {must.Separator}
                              {must.NormalizeExpressionIndentation(expectedExpression).Indent()}
                              {must.Separator}
                              But comparison of the objects serialized as JSON resulted in the Diff:
                              {must.Separator}
                              {DiffGenerator.CreateDiff(expected: expectedJson, actual: actualJson)}
                              {must.Separator}
                              Actual was:
                              {must.Separator}
                              {actualJson}
                              {must.Separator}
                              Expected was:
                              {must.Separator}
                              {expectedJson}
                              {must.Separator}
                              """);
   }
}
