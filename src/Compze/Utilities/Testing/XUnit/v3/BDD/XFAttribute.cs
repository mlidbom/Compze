using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.v3.BDD;
#pragma warning disable CA1813 //avoid unsealed attributes

/// <summary>
/// eXclusive Fact attribute.
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
[XunitTestCaseDiscoverer(typeof(XFactDiscoverer))]
public class XFactAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1) :
      FactAttribute(sourceFilePath, sourceLineNumber)
{
   static XFactAttribute()
   {
      var expectedTypeName = typeof(XFactDiscoverer).FullName;
      var expectedAssembly = typeof(XFactDiscoverer).Assembly.GetName().Name;
      
      if(expectedTypeName != "Compze.Utilities.Testing.XUnit.v3.BDD.XFactDiscoverer")
         throw new Exception($"""
                              XFactDiscoverer type name validation failed
                              Expected: Compze.Utilities.Testing.XUnit.v3.BDD.XFactDiscoverer
                              Was: {expectedTypeName}
                              """);
      if(expectedAssembly != "Compze.Utilities.Testing.XUnit.v3")
         throw new Exception($"""
                              XFactDiscoverer assembly name validation failed
                              Expected: Compze.Utilities.Testing.XUnit.v3
                              Was: {expectedAssembly}
                              """);
   }
}

/// <summary>
/// Short alias for <see cref="XFactAttribute"/>
/// eXclusive Fact attribute.
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
public sealed class XFAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1) 
   : XFactAttribute(sourceFilePath, sourceLineNumber) {}

#pragma warning restore CA1813 //avoid unsealed attributes

