namespace Compze.Tests.Infrastructure.Diagnostics;

///<summary>Measures how many bytes of managed memory code allocates, such as to specify how much memory creating an object costs.</summary>
public static class ManagedAllocation
{
   ///<summary>The bytes of managed memory that calling <paramref name="create"/> allocates on the calling thread, exactly,<br/>
   /// including everything the object it returns allocates for itself when it is created.</summary>
   ///<remarks>It measures the second call, so that what only the first call allocates, such as static fields initialized on first use, doesn't count.<br/>
   /// The created object is passed on to a method call before the measurement ends, so the JIT can't allocate it on the stack instead of the heap.</remarks>
   public static long BytesAllocatedBy(Func<object> create)
   {
      GC.KeepAlive(create());

      var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
      var created = create();
      var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

      GC.KeepAlive(created);
      return allocatedAfter - allocatedBefore;
   }
}
