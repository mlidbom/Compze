using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess;
using Compze.Threading.InternalSpecifications.TestInfrastructure;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;
// ReSharper disable InconsistentNaming

namespace Compze.Threading.InternalSpecifications.Interprocess;

[Collection(nameof(NonParallelCollection))]
public class InterprocessChangeCounter_specification : UniversalTestBase
{
   public class Construction : InterprocessChangeCounter_specification
   {
      [XF] public void throws_ArgumentException_when_name_is_empty() =>
         Invoking(() => new InterprocessChangeCounter("")).Must().Throw<ArgumentException>();

      [XF] public void throws_ArgumentException_when_name_is_whitespace() =>
         Invoking(() => new InterprocessChangeCounter("   ")).Must().Throw<ArgumentException>();

      [XF] public void succeeds_with_a_simple_name()
      {
         using var counter = new InterprocessChangeCounter("InterprocessChangeCounter_specification.Construction.simple_name");
         counter.Must().NotBeNull();
      }

      [XF] public void Name_matches_the_provided_name()
      {
         using var counter = new InterprocessChangeCounter("InterprocessChangeCounter_specification.Construction.Name");
         counter.Name.Must().Be("InterprocessChangeCounter_specification.Construction.Name");
      }
   }

   public class Count : InterprocessChangeCounter_specification
   {
      [XF] public void increments_by_one_when_Increment_is_called()
      {
         using var counter = new InterprocessChangeCounter("InterprocessChangeCounter_specification.Count.increments_by_one");
         var countBefore = counter.Count;
         counter.Increment();
         counter.Count.Must().Be(countBefore + 1);
      }

      [XF] public void increments_correctly_after_multiple_calls()
      {
         using var counter = new InterprocessChangeCounter("InterprocessChangeCounter_specification.Count.multiple_calls");
         var countBefore = counter.Count;
         counter.Increment();
         counter.Increment();
         counter.Increment();
         counter.Count.Must().Be(countBefore + 3);
      }
   }

   public class Two_instances_with_the_same_name : InterprocessChangeCounter_specification
   {
      [XF] public void see_each_others_increments()
      {
         const string name = "InterprocessChangeCounter_specification.SameName.see_increments";
         using var counter1 = new InterprocessChangeCounter(name);
         using var counter2 = new InterprocessChangeCounter(name);

         var countBefore = counter2.Count;
         counter1.Increment();
         counter2.Count.Must().Be(countBefore + 1);
      }

      [XF] public void both_can_increment_and_both_see_the_accumulated_count()
      {
         const string name = "InterprocessChangeCounter_specification.SameName.both_increment";
         using var counter1 = new InterprocessChangeCounter(name);
         using var counter2 = new InterprocessChangeCounter(name);

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
         const string name = "InterprocessChangeCounter_specification.Concurrent.no_lost_increments";
         const int threadsCount = 10;
         const int incrementsPerThread = 100;

         using var counter = new InterprocessChangeCounter(name);

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
         var counter = new InterprocessChangeCounter("InterprocessChangeCounter_specification.Dispose.no_error");
         counter.Dispose();
      }

      [XF] public void can_be_disposed_twice_without_error()
      {
         var counter = new InterprocessChangeCounter("InterprocessChangeCounter_specification.Dispose.double_dispose");
         counter.Dispose();
         counter.Dispose();
      }

      [XF] public void Increment_after_Dispose_throws_ObjectDisposedException()
      {
         var counter = new InterprocessChangeCounter("InterprocessChangeCounter_specification.Dispose.Increment_after");
         counter.Dispose();
         Invoking(() => counter.Increment()).Must().Throw<ObjectDisposedException>();
      }

      [XF] public void Count_after_Dispose_throws_ObjectDisposedException()
      {
         var counter = new InterprocessChangeCounter("InterprocessChangeCounter_specification.Dispose.Count_after");
         counter.Dispose();
         Invoking(() => _ = counter.Count).Must().Throw<ObjectDisposedException>();
      }
   }
}
