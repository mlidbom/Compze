using System;
using System.Collections.Generic;
using Compze.SystemCE.ReflectionCE;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static Compze.Contracts.Assert;

namespace Compze.Testing.TestFrameworkExtensions.XUnit;

///<summary>
/// This attribute will run the test eXclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
///This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
[XunitTestCaseDiscoverer(CompzeTestingTestframeworkExtensionsXUnitXFactDiscovererFullTypeName, XFactAttributeAssembly)]
sealed class XFactAttribute : FactAttribute
{
   const string CompzeTestingTestframeworkExtensionsXUnitXFactDiscovererFullTypeName = "Compze.Testing.TestFrameworkExtensions.XUnit.XFactDiscoverer";
   const string XFactAttributeAssembly = "Compze.Testing";

   static XFactAttribute()
   {
      Invariant.Is(CompzeTestingTestframeworkExtensionsXUnitXFactDiscovererFullTypeName == typeof(XFactDiscoverer).GetFullNameCompilable());
      Invariant.Is(XFactAttributeAssembly == typeof(XFactDiscoverer).Assembly.GetName().Name);
   }
}

[UsedImplicitly] public class XFactDiscoverer(IMessageSink diagnosticMessageSink) : IXunitTestCaseDiscoverer
{
   readonly IMessageSink _diagnosticMessageSink = diagnosticMessageSink;

   public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
   {
      var declaringType = testMethod.Method.ToRuntimeMethod().DeclaringType;
      var currentType = testMethod.TestClass.Class.ToRuntimeType();

      if(declaringType != currentType) // Skip tests declared in base classes
         return Array.Empty<IXunitTestCase>();

      return [new XunitTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod)];
   }
}

//Below here is the working implementation of this for XUnit.v3:
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Xunit;
// using Xunit.Sdk;
// using Xunit.v3;
//
// namespace Compze.Testing.TestFrameworkExtensions.XUnit;
//
// ///<summary>
// /// This attribute will run the test eXclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
// ///This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
// /// </summary>
// [XunitTestCaseDiscoverer(typeof(XFactAttributeTestCaseDiscoverer))]
// sealed class XFactAttribute : FactAttribute {}
//
// class XFactAttributeTestCaseDiscoverer : IXunitTestCaseDiscoverer
// {
//    public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, IFactAttribute factAttribute)
//    {
//       var declaringType = testMethod.Method.DeclaringType;
//       var currentType = testMethod.TestClass.Class;
//
//       if(declaringType != currentType)
//          return ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(Array.Empty<IXunitTestCase>());
//
//       return ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([
//                                                                           new XunitTestCase(
//                                                                              testMethod: testMethod,
//                                                                              testCaseDisplayName: testMethod.Method.Name,
//                                                                              uniqueID: $"{testMethod.UniqueID}.{testMethod.Method.Name}",
//                                                                              @explicit: factAttribute.Explicit)
//                                                                        ]);
//    }
// }
