using Compze.Utilities.Testing.XUnit.v3.BDD;

namespace Compze.Utilities.Testing.XUnit.v3.Tests.BDD;

public class AfterSomethingHappened
{
   [XF] public void ThisIsTrue() => Console.WriteLine(nameof(ThisIsTrue));

   public class AndThenThisHappened : AfterSomethingHappened
   {
      [XF] public void ThisIsAlsoTrue() => Console.WriteLine(nameof(ThisIsAlsoTrue));

      public class AndThenSomethingElseHappened : AndThenThisHappened
      {
         [XF] public void EvenThisIsTrue() => Console.WriteLine(nameof(EvenThisIsTrue));
      }
   }
}
