namespace Compze.xUnitBDD.Tests;

public class AfterSomethingHappened
{
   [XF] public void ThisIsTrue() => Console.WriteLine(nameof(ThisIsTrue));

   public class AndThenThisHappened : AfterSomethingHappened
   {
      [XF] public void ThisIsAlsoTrue() => Console.WriteLine(nameof(ThisIsAlsoTrue));

      public class AndThenSomethingElseHappened : AndThenThisHappened
      {
         [XF] public void NumberOfRoadsToWalkDownIs42() => Console.WriteLine(nameof(NumberOfRoadsToWalkDownIs42));
      }
   }
}
