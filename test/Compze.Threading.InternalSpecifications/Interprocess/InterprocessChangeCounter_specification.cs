using Compze.Must;

using Compze.Tests.Infrastructure;
using Compze.Threading.InternalSpecifications.TestInfrastructure;
using Compze.Underscore;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;
using Compze.Threading.Interprocess._internal;
// ReSharper disable InconsistentNaming

namespace Compze.Threading.InternalSpecifications.Interprocess;

[Collection(nameof(NonParallelCollection))]
public class InterprocessChangeCounter_specification : UniversalTestBase
{
   static readonly DirectoryInfo TestDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "Signals"))._mutate(it => it.Create());

   static FileInfo TestFile(string name) => new(Path.Combine(TestDirectory.FullName, name));

   public class Construction : InterprocessChangeCounter_specification
   {
      [XF] public void succeeds_with_a_valid_file()
      {
         using var counter = new InterprocessChangeCounter(TestFile("Construction.simple"));
         counter.Must().NotBeNull();
      }
   }

   public class Count : InterprocessChangeCounter_specification
   {
      [XF] public void increments_by_one_when_Increment_is_called()
      {
         using var counter = new InterprocessChangeCounter(TestFile("Count.increments_by_one"));
         var countBefore = counter.Count;
         counter.Increment();
         counter.Count.Must().Be(countBefore + 1);
      }

      [XF] public void increments_correctly_after_multiple_calls()
      {
         using var counter = new InterprocessChangeCounter(TestFile("Count.multiple_calls"));
         var countBefore = counter.Count;
         counter.Increment();
         counter.Increment();
         counter.Increment();
         counter.Count.Must().Be(countBefore + 3);
      }
   }

   public class Two_instances_with_the_same_file : InterprocessChangeCounter_specification
   {
      [XF] public void see_each_others_increments()
      {
         var file = TestFile("SameName.see_increments");
         using var counter1 = new InterprocessChangeCounter(file);
         using var counter2 = new InterprocessChangeCounter(file);

         var countBefore = counter2.Count;
         counter1.Increment();
         counter2.Count.Must().Be(countBefore + 1);
      }

      [XF] public void both_can_increment_and_both_see_the_accumulated_count()
      {
         var file = TestFile("SameName.both_increment");
         using var counter1 = new InterprocessChangeCounter(file);
         using var counter2 = new InterprocessChangeCounter(file);

         var initialCount = counter1.Count;
         counter1.Increment();
         counter2.Increment();
         counter1.Count.Must().Be(initialCount + 2);
         counter2.Count.Must().Be(initialCount + 2);
      }
   }

   public class Concurrent_incrementing : InterprocessChangeCounter_specification
   {
      [XF] public void does_not_lose_increments_under_contention()
      {
         const int threadsCount = 10;
         const int incrementsPerThread = 100;

         using var counter = new InterprocessChangeCounter(TestFile("Concurrent.no_lost_increments"));

         var initialCount = counter.Count;

         var threads = Enumerable.Range(0, threadsCount)
                                 .Select(_ => new Thread(() =>
                                 {
                                    for(var i = 0; i < incrementsPerThread; i++)
                                       // ReSharper disable once AccessToDisposedClosure
                                       counter.Increment();
                                 }))
                                 .ToList();

         foreach(var thread in threads)
            thread.Start();

         foreach(var thread in threads)
            thread.Join();

         counter.Count.Must().Be(initialCount + threadsCount * incrementsPerThread);
      }
   }

   public new class Dispose : InterprocessChangeCounter_specification
   {
      [XF] public void can_be_disposed_without_error()
      {
         var counter = new InterprocessChangeCounter(TestFile("Dispose.no_error"));
         counter.Dispose();
      }

      [XF] public void can_be_disposed_twice_without_error()
      {
         var counter = new InterprocessChangeCounter(TestFile("Dispose.double_dispose"));
         counter.Dispose();
         counter.Dispose();
      }

      [XF] public void Increment_after_Dispose_throws_ObjectDisposedException()
      {
         var counter = new InterprocessChangeCounter(TestFile("Dispose.Increment_after"));
         counter.Dispose();
         Invoking(() => counter.Increment()).Must().Throw<ObjectDisposedException>();
      }

      [XF] public void Count_after_Dispose_throws_ObjectDisposedException()
      {
         var counter = new InterprocessChangeCounter(TestFile("Dispose.Count_after"));
         counter.Dispose();
         Invoking(() => _ = counter.Count).Must().Throw<ObjectDisposedException>();
      }
   }
}
