using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit;


public static class ComponentContext
{
   public static ComponentsPermutation? CurrentPermutation => CurrentInternal.Value;
   static readonly AsyncLocal<ComponentsPermutation?> CurrentInternal = new();

   internal static async Task<TReturn> RunTestInContextAsync<TReturn>(
      ComponentsPermutation contextData,
      Func<Task<TReturn>> executeTest)
   {
      CurrentInternal.Value = contextData;
      try
      {
         return await executeTest();
      }
      finally
      {
         CurrentInternal.Value = null;
      }
   }
}
