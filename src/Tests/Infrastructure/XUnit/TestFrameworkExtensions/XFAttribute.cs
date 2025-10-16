using Compze.Utilities.SystemCE.ReflectionCE;
using Xunit;
using Xunit.Sdk;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
#pragma warning disable CA1813 //avoid unsealed attributes

//XUnit.v3 version ready to go once v3 is stable in NCrunch is at git commit: deb6be8d66ec03db2a55f84ff28feab220ae50b1
/// <summary>
/// eXclusive Fact attribute.
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
[XunitTestCaseDiscoverer(XFactDiscovererFullTypeName, XFactDiscovererAssembly)]
public class XFactAttribute : FactAttribute
{
   const string XFactDiscovererFullTypeName = "Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions.XFactDiscoverer";
   const string XFactDiscovererAssembly = "Compze.Tests.Infrastructure";

   static XFactAttribute()
   {
      Invariant.Is(XFactDiscovererFullTypeName == typeof(XFactDiscoverer).GetFullNameCompilable());
      Invariant.Is(XFactDiscovererAssembly == typeof(XFactDiscoverer).Assembly.GetName().Name);
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