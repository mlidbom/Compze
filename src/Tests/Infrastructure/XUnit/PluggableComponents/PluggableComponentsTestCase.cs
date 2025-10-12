using System;
using Compze.Utilities.SystemCE;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

public class PluggableComponentsTestCase : XunitTestCase
{
   Infrastructure.PluggableComponents? _combination = null;

   public Infrastructure.PluggableComponents Components => _combination!.Value;

   [Obsolete("Called by deserializer")]
   public PluggableComponentsTestCase() {}

   public PluggableComponentsTestCase(
      IXunitTestMethod testMethod,
      Infrastructure.PluggableComponents combination,
      string testCaseDisplayName,
      string uniqueID,
      bool @explicit,
      int? timeout,
      object?[]? testMethodArguments)
      : base(testMethod,
             testCaseDisplayName,
             uniqueID,
             @explicit,
             skipReason: null,
             skipType: null,
             skipUnless: null,
             skipWhen: null,
             timeout: timeout,
             testMethodArguments: testMethodArguments,
             traits: null) =>
      _combination = combination;

   protected override void Serialize(IXunitSerializationInfo info)
   {
      base.Serialize(info);
      info.AddValue(nameof(_combination), _combination.ToString());
   }

   protected override void Deserialize(IXunitSerializationInfo info)
   {
      base.Deserialize(info);
      _combination = Infrastructure.PluggableComponents.FromString(info.GetValue<string>(nameof(_combination)).NotNull());
   }
}
