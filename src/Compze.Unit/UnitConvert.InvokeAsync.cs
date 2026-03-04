namespace Compze.Unit;

public static partial class UnitConvert
{
   public static async Task<Unit> InvokeAsync(this Func<Task> @this)
   {
      await @this().ConfigureAwait(false);
      return Unit.Value;
   }
}
