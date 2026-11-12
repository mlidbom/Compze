using Compze.Contracts;
using Compze.Threading;
using Compze.Threading.Testing;
using static Compze.Must.MustActions;

namespace Compze.Internals.SystemCE.Tests;

public class LazyCE_
{
   int _factoryRuns;

   object CreateAndCountTheRun()
   {
      Interlocked.Increment(ref _factoryRuns);
      return new object();
   }

   public class Before_the_value_is_read : LazyCE_
   {
      readonly LazyCE<object> _lazy;

      public Before_the_value_is_read() => _lazy = new LazyCE<object>(CreateAndCountTheRun);

      [XF] public void ValueIfInitialized_returns_null() => _lazy.ValueIfInitialized().Must().BeNull();

      [XF] public void the_factory_has_not_run() => _factoryRuns.Must().Be(0);
   }

   public class When_the_value_is_read_twice : LazyCE_
   {
      readonly LazyCE<object> _lazy;
      readonly object _firstRead;
      readonly object _secondRead;

      public When_the_value_is_read_twice()
      {
         _lazy = new LazyCE<object>(CreateAndCountTheRun);
         _firstRead = _lazy.Value;
         _secondRead = _lazy.Value;
      }

      [XF] public void both_reads_return_the_same_instance() => _secondRead.Must().ReferenceEqual(_firstRead);

      [XF] public void the_factory_runs_once() => _factoryRuns.Must().Be(1);

      [XF] public void ValueIfInitialized_returns_that_instance() => _lazy.ValueIfInitialized().Must().NotBeNull().ReferenceEqual(_firstRead);
   }

   public class When_the_factory_throws_on_the_first_read : LazyCE_
   {
      readonly LazyCE<object> _lazy;
      readonly InvalidOperationException _exceptionFromTheFirstRead;

      public When_the_factory_throws_on_the_first_read()
      {
         _lazy = new LazyCE<object>(() => Interlocked.Increment(ref _factoryRuns) == 1
                                             ? throw new InvalidOperationException("The first run fails.")
                                             : new object());
         _exceptionFromTheFirstRead = Invoking(() => _lazy.Value).Must().Throw<InvalidOperationException>().Which;
      }

      [XF] public void the_read_throws_the_factorys_exception() => _exceptionFromTheFirstRead.Message.Must().Be("The first run fails.");

      [XF] public void ValueIfInitialized_returns_null() => _lazy.ValueIfInitialized().Must().BeNull();

      public class and_the_value_is_read_again : When_the_factory_throws_on_the_first_read
      {
         readonly object _valueFromTheSecondRead;

         public and_the_value_is_read_again() => _valueFromTheSecondRead = _lazy.Value;

         [XF] public void the_factory_runs_again() => _factoryRuns.Must().Be(2);

         [XF] public void ValueIfInitialized_returns_the_value_the_second_run_created() =>
            _lazy.ValueIfInitialized().Must().NotBeNull().ReferenceEqual(_valueFromTheSecondRead);
      }
   }

   public class When_a_second_thread_reads_the_value_while_the_factory_is_running_on_the_first : LazyCE_
   {
      readonly bool _theSecondThreadStartedTheFactoryToo;
      readonly object _valueTheFirstThreadGot;
      readonly object _valueTheSecondThreadGot;

      public When_a_second_thread_reads_the_value_while_the_factory_is_running_on_the_first()
      {
         var insideTheFactory = IThreadGate.NewClosed(WaitTimeout.Seconds(10));
         var lazy = new LazyCE<object>(() =>
         {
            insideTheFactory.AwaitPassThrough();
            return CreateAndCountTheRun();
         });

         object? valueTheFirstThreadGot = null;
         object? valueTheSecondThreadGot = null;
         using(var taskRunner = TestingTaskRunner.WithTimeout(TimeSpan.FromSeconds(10)))
         {
            taskRunner.Run(() => valueTheFirstThreadGot = lazy.Value);
            insideTheFactory.AwaitQueueLengthEqualTo(1);

            taskRunner.Run(() => valueTheSecondThreadGot = lazy.Value);
            _theSecondThreadStartedTheFactoryToo = insideTheFactory.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(100));

            insideTheFactory.Open();
         }

         _valueTheFirstThreadGot = valueTheFirstThreadGot._assert().NotNull();
         _valueTheSecondThreadGot = valueTheSecondThreadGot._assert().NotNull();
      }

      [XF] public void the_second_thread_does_not_start_the_factory_too() => _theSecondThreadStartedTheFactoryToo.Must().BeFalse();

      [XF] public void the_factory_runs_once() => _factoryRuns.Must().Be(1);

      [XF] public void both_threads_get_the_same_instance() => _valueTheSecondThreadGot.Must().ReferenceEqual(_valueTheFirstThreadGot);
   }
}
