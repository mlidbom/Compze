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

   public class When_the_factory_reads_the_value_it_is_creating : LazyCE_
   {
      readonly LazyCE<object> _lazy;
      readonly Exception _exceptionFromTheRead;

      public When_the_factory_reads_the_value_it_is_creating()
      {
         _lazy = new LazyCE<object>(() =>
         {
            Interlocked.Increment(ref _factoryRuns);
            return _lazy._assert().NotNull().Value;
         });
         _exceptionFromTheRead = Invoking(() => _lazy.Value).Must().Throw<Exception>().Which;
      }

      [XF] public void the_read_throws_an_InvalidOperationException() => _exceptionFromTheRead.Must().BeAssignableTo<InvalidOperationException>();

      [XF] public void the_factory_runs_once() => _factoryRuns.Must().Be(1);

      [XF] public void ValueIfInitialized_returns_null() => _lazy.ValueIfInitialized().Must().BeNull();
   }

   public class When_the_factories_of_two_instances_read_each_others_value : LazyCE_
   {
      readonly LazyCE<object> _first;
      readonly LazyCE<object> _second;
      readonly Exception _exceptionFromTheRead;

      public When_the_factories_of_two_instances_read_each_others_value()
      {
         _first = new LazyCE<object>(() => _second._assert().NotNull().Value);
         _second = new LazyCE<object>(() => _first.Value);
         _exceptionFromTheRead = Invoking(() => _first.Value).Must().Throw<Exception>().Which;
      }

      [XF] public void the_read_throws_an_InvalidOperationException() => _exceptionFromTheRead.Must().BeAssignableTo<InvalidOperationException>();

      [XF] public void the_first_instance_has_no_value() => _first.ValueIfInitialized().Must().BeNull();

      [XF] public void the_second_instance_has_no_value() => _second.ValueIfInitialized().Must().BeNull();
   }

   public class When_code_that_locked_on_the_instance_from_outside_reads_the_value_for_the_first_time : LazyCE_
   {
      readonly Exception _exceptionFromTheRead;

      public When_code_that_locked_on_the_instance_from_outside_reads_the_value_for_the_first_time()
      {
         var lazy = new LazyCE<object>(CreateAndCountTheRun);
         _exceptionFromTheRead = Invoking(() => ReadWhileLockingOn(lazy)).Must().Throw<Exception>().Which;
      }

      static object ReadWhileLockingOn(LazyCE<object> lazy)
      {
         //rationale: A raw lock, against the locking-primitives rule: it is the outside code locking on a LazyCE that this specifies.
         lock(lazy) return lazy.Value;
      }

      [XF] public void the_read_throws_an_InvalidOperationException() => _exceptionFromTheRead.Must().BeAssignableTo<InvalidOperationException>();

      [XF] public void the_factory_does_not_run() => _factoryRuns.Must().Be(0);
   }
}
