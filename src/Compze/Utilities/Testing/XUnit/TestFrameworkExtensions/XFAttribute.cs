using System;
using Xunit;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
#pragma warning disable CA1813 //avoid unsealed attributes

/// <summary>
/// eXclusive Fact attribute.
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
[XunitTestCaseDiscoverer(XFactDiscovererFullTypeName, XFactDiscovererAssembly)]
public class XFactAttribute : FactAttribute
{
   const string XFactDiscovererFullTypeName = "Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions.XFactDiscoverer";
   const string XFactDiscovererAssembly = "Compze.Utilities.Testing.XUnit";

   static XFactAttribute()
   {
      if(XFactDiscovererFullTypeName != typeof(XFactDiscoverer).FullName)
         throw new Exception($"""
                              {nameof(XFactDiscovererFullTypeName)} does not indicate the correct type
                              Should be: {typeof(XFactDiscoverer).FullName}
                              Was   : {XFactDiscovererFullTypeName}
                              """);
      if(XFactDiscovererAssembly != typeof(XFactDiscoverer).Assembly.GetName().Name)
         throw new Exception($"""
                              {nameof(XFactDiscovererAssembly)} does not indicate the correct assembly
                              Should be: {typeof(XFactDiscoverer).Assembly.GetName().Name}
                              Was   : {XFactDiscovererAssembly}
                              """);
   }
}

/// <summary>
/// Short alias for <see cref="XFactAttribute"/>
/// eXclusive Fact attribute.
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
public sealed class XFAttribute : XFactAttribute {}

#pragma warning restore CA1813 //avoid unsealed attributes
