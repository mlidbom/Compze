namespace Compze.Unit;

public readonly partial struct Unit
{
   ///<summary>Executes the action and returns Unit
   /// <code>
   ///   Unit Method() => Unit.Invoke(() => AVoidMethod())
   /// </code>
   /// </summary>
   public static Unit Invoke(Action action)
   {
      action();
      return Value;
   }

   ///<summary>Awaits the async action and returns Unit
   /// <code>
   ///   Task&lt;Unit&gt; Method() => Unit.InvokeAsync(() => AnAsyncVoidMethod())
   /// </code>
   /// </summary>
   public static async Task<Unit> InvokeAsync(Func<Task> action)
   {
         await action().ConfigureAwait(false);
         return Value;
   }
}
