namespace Compze.Internals.SystemCE.ThreadingCE;

public static class ThreadCE
{
   extension(Thread @this)
   {
      ///<summary>Delegates to <see cref="Thread.Join(TimeSpan)"/> but throws an exception if the thread does not join within the timeout.</summary>
      public void JoinCE(TimeSpan timeout)
      {
         if(!@this.Join(timeout))
         {
            throw new Exception($"Thread {@this} failed to join within the timeout: {timeout}");
         }
      }
   }
}
