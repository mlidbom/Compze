using Compze.Must;

using Compze.xUnitBDD;
using static Compze.Must.MustActions;
using AssertionFailedException = Compze.Contracts.Exceptions.AssertionFailedException;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.PipeAssertTarget;

public class True_method
{
   const bool FalseCondition = 1 > 2;

   public class called_on_a_false_value : True_method
   {
      [XF] public void throws_AssertionFailedException() =>
         Invoking(() => FalseCondition._assert().True()).Must().Throw<AssertionFailedException>();

      [XF] public void exception_message_contains_the_value_expression() =>
         Invoking(() => FalseCondition._assert().True())
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain(nameof(FalseCondition));
   }

   public class called_on_a_true_value : True_method
   {
      [XF] public void returns_true() =>
         (2 > 1)._assert().True().Must().Be(true);
   }
}
