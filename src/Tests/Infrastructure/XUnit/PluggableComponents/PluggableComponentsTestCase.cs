using System;
using Compze.Utilities.SystemCE;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

public class PluggableComponentsTestCase : XunitTestCase
{
   Tessaging.Hosting.Testing.PluggableComponents? _combination = null;

   public Tessaging.Hosting.Testing.PluggableComponents Components => _combination!.Value;

   [Obsolete("Called by deserializer")]
   public PluggableComponentsTestCase() {}

   public PluggableComponentsTestCase(
      IXunitTestMethod testMethod,
      Tessaging.Hosting.Testing.PluggableComponents combination,
      string testCaseDisplayName,
      string uniqueId,
      bool @explicit,
      string? skipReason,
      Type? skipType,
      string? skipUnless,
      string? skipWhen,
      int? timeout,
      object?[]? testMethodArguments)
      : base(testMethod,
             testCaseDisplayName,
             uniqueId,
             @explicit,
             skipReason: skipReason,
             skipType: skipType,
             skipUnless: skipUnless,
             skipWhen: skipWhen,
             timeout: timeout,
             testMethodArguments: testMethodArguments,
             traits: new System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>>()) =>
      _combination = combination;

   protected override void Serialize(IXunitSerializationInfo info)
   {
      base.Serialize(info);
      info.AddValue(nameof(_combination), _combination.ToString());
   }

   protected override void Deserialize(IXunitSerializationInfo info)
   {
      base.Deserialize(info);
      _combination = Tessaging.Hosting.Testing.PluggableComponents.FromString(info.GetValue<string>(nameof(_combination)).NotNull());
   }
}
