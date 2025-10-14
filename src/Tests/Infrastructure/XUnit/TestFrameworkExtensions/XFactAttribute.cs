using System;
using System.Collections.Generic;
using Compze.Utilities.SystemCE.ReflectionCE;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;

//XUnit.v3 version ready to go once v3 is stable in NCrunch is at git commit: deb6be8d66ec03db2a55f84ff28feab220ae50b1
/// <summary>
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
[XunitTestCaseDiscoverer(XFactDiscovererFullTypeName, XFactDiscovererAssembly)]
public sealed class XFactAttribute : FactAttribute
{
   const string XFactDiscovererFullTypeName = "Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions.XFactDiscoverer";
   const string XFactDiscovererAssembly = "Compze.Tests.Infrastructure.XUnit";

   static XFactAttribute()
   {
      Invariant.Is(XFactDiscovererFullTypeName == typeof(XFactDiscoverer).GetFullNameCompilable());
      Invariant.Is(XFactDiscovererAssembly == typeof(XFactDiscoverer).Assembly.GetName().Name);
   }
}

[UsedImplicitly] class XFactDiscoverer(IMessageSink diagnosticMessageSink) : IXunitTestCaseDiscoverer
{
   readonly IMessageSink _diagnosticMessageSink = diagnosticMessageSink;

   public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
   {
      var declaringType = testMethod.Method.ToRuntimeMethod().DeclaringType;
      var currentType = testMethod.TestClass.Class.ToRuntimeType();

      if(declaringType != currentType) // Skip tests declared in base classes
         return [];

      return [new XFactTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod)];
   }
}
