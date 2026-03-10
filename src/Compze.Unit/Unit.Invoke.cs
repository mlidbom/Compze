namespace Compze.SystemCE;

/// <summary>Extensions for invoking Action and returning unit.</summary>
public static class UnitInvokeExtensions
{
   extension(Unit)
   {
      ///<summary>Executes the action and returns Unit
      /// <code>
      ///   Unit Method() => Unit.Invoke(() => AVoidMethod())
      /// </code>
      /// </summary>
      public static Unit Invoke(Action action) => UnitConvert.Invoke(action);

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
}
