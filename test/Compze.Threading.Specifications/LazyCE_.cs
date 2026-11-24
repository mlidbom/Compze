using Compze.Contracts;
using System.Runtime.CompilerServices;
using Compze.Must;
using Compze.Tests.Infrastructure.Diagnostics;
using Compze.Threading.Testing;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications;

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

      [XF] public void IsInitialized_is_false() => _lazy.IsInitialized.Must().BeFalse();
   }

   public class Creating_an_instance : LazyCE_
   {
      static readonly Func<object> CreateAnObject = () => new object();

      [XF] public void allocates_32_bytes() => ManagedAllocation.BytesAllocatedBy(() => new LazyCE<object>(CreateAnObject)).Must().Be(32L);
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

      [XF] public void IsInitialized_is_true() => _lazy.IsInitialized.Must().BeTrue();
   }

   public class When_the_factory_returns_null_and_the_value_is_read_twice : LazyCE_
   {
      readonly LazyCE<object?> _lazy;
      readonly object? _firstRead;
      readonly object? _secondRead;

      public When_the_factory_returns_null_and_the_value_is_read_twice()
      {
         _lazy = new LazyCE<object?>(() =>
         {
            Interlocked.Increment(ref _factoryRuns);
            return null;
         });
         _firstRead = _lazy.Value;
         _secondRead = _lazy.Value;
      }

      [XF] public void both_reads_return_null() => new { _firstRead, _secondRead }.Must().Satisfy(it => it._firstRead is null && it._secondRead is null);

      [XF] public void the_factory_runs_once() => _factoryRuns.Must().Be(1);

      [XF] public void IsInitialized_is_true() => _lazy.IsInitialized.Must().BeTrue();
   }

   public class Once_the_value_is_created : LazyCE_
   {
      readonly LazyCE<string> _lazy;
      readonly WeakReference _whatTheFactoryCaptured;

      public Once_the_value_is_created()
      {
         _lazy = CreateAnInstanceWhoseFactoryCapturesAnObject(out _whatTheFactoryCaptured);
         _ = _lazy.Value;
         GC.Collect();
         GC.WaitForPendingFinalizers();
         GC.Collect();
      }

      ///<summary>Not inlined, so that no local of the constructor's can keep the captured object alive.</summary>
      [MethodImpl(MethodImplOptions.NoInlining)]
      static LazyCE<string> CreateAnInstanceWhoseFactoryCapturesAnObject(out WeakReference whatTheFactoryCaptures)
      {
         var captured = new object();
         whatTheFactoryCaptures = new WeakReference(captured);
         return new LazyCE<string>(() => captured.GetType().Name);
      }

      [XF] public void the_instance_has_let_go_of_its_factory_so_what_the_factory_captured_has_been_collected() =>
         _whatTheFactoryCaptured.IsAlive.Must().BeFalse();

      [XF] public void the_instance_still_returns_its_value() => _lazy.Value.Must().Be(nameof(Object));
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

      [XF] public void IsInitialized_is_false() => _lazy.IsInitialized.Must().BeFalse();

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
            taskRunner.Start(() => valueTheFirstThreadGot = lazy.Value);
            insideTheFactory.AwaitQueueLengthEqualTo(1);

            taskRunner.Start(() => valueTheSecondThreadGot = lazy.Value);
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
