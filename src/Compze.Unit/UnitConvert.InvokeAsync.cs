namespace Compze.Unit;

public static partial class UnitConvert
{
   ///<summary>Awaits <paramref name="this"/> and returns <see cref="Unit.Value"/>.</summary>
   public static async Task<Unit> InvokeAsync(Func<Task> @this)
   {
      await @this().ConfigureAwait(false);
      return Unit.Value;
   }
}
