namespace Compze.Unit;

///<summary>Provides conversions between <see cref="Action"/> and <see cref="Func{TResult}"/> delegate families
/// using <see cref="Unit"/> as the return type, bridging the void / value-type gap in the .NET type system.
///</summary>
public static partial class UnitConvert
{
   ///<summary>Executes the action and returns Unit
   /// <code>
   ///   Unit Method() => Unit.Invoke(() => AVoidMethod())
   /// </code>
   /// </summary>
   public static Unit Invoke(Action action)
   {
      action();
      return unit;
   }

   ///<summary>Awaits the async action and returns Unit
   /// <code>
   ///   Task&lt;Unit&gt; Method() => Unit.InvokeAsync(() => AnAsyncVoidMethod())
   /// </code>
   /// </summary>
   public static async Task<Unit> InvokeAsync(Func<Task> action)
   {
      await action().ConfigureAwait(false);
      return unit;
   }
}
