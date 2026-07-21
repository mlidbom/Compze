using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.xUnitBDD;
using Xunit;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Testing.Specifications;

[Collection(nameof(NonParallelCollection))]
public class Given_a_closed_gated_code_section : UniversalTestBase
{
   readonly IGatedCodeSection _codeSection = IGatedCodeSection.NewClosed(WaitTimeout.Seconds(10), "testCodeSection");

   [XF] public void ExecuteWithExclusiveLock_with_an_action_executes_the_action_receiving_the_section_itself()
   {
      IGatedCodeSection? received = null;
      _codeSection.ExecuteWithExclusiveLock(section => received = section);
      received.Must().NotBeNull().ReferenceEqual(_codeSection);
   }
}
