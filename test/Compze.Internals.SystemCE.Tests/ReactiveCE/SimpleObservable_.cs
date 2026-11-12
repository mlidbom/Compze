using Compze.Contracts;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Tests.Infrastructure.Diagnostics;
using Compze.Threading;
using Compze.Threading.Testing;

namespace Compze.Internals.SystemCE.Tests.ReactiveCE;

public class SimpleObservable_
{
   readonly SimpleObservable<string> _observable = new();

   public class Creating_an_observable : SimpleObservable_
   {
      [XF] public void allocates_24_bytes() =>
         ManagedAllocation.BytesAllocatedBy(() => new SimpleObservable<string>()).Must().Be(24L);
   }

   public class When_a_subscriber_disposes_its_own_subscription_during_a_notification : SimpleObservable_
   {
      readonly List<string> _receivedByTheDisposingSubscriber = [];
      readonly List<string> _receivedByTheOtherSubscriber = [];
      readonly IDisposable? _theDisposingSubscribersSubscription;

      public When_a_subscriber_disposes_its_own_subscription_during_a_notification()
      {
         _theDisposingSubscribersSubscription = _observable.Subscribe(it =>
         {
            _receivedByTheDisposingSubscriber.Add(it);
            _theDisposingSubscribersSubscription._assert().NotNull().Dispose();
         });
         _observable.Subscribe(_receivedByTheOtherSubscriber.Add);

         _observable.OnNext("first");
         _observable.OnNext("second");
      }

      [XF] public void the_disposing_subscriber_receives_only_the_notification_it_disposed_its_subscription_during() =>
         _receivedByTheDisposingSubscriber.Must().SequenceEqual(["first"]);

      [XF] public void the_other_subscriber_receives_both_notifications() =>
         _receivedByTheOtherSubscriber.Must().SequenceEqual(["first", "second"]);
   }

   public class When_a_subscriber_subscribes_a_new_subscriber_during_a_notification : SimpleObservable_
   {
      readonly List<string> _receivedByTheSubscribingSubscriber = [];
      readonly List<string> _receivedByTheNewSubscriber = [];

      public When_a_subscriber_subscribes_a_new_subscriber_during_a_notification()
      {
         _observable.Subscribe(it =>
         {
            _receivedByTheSubscribingSubscriber.Add(it);
            if(it == "first") _observable.Subscribe(_receivedByTheNewSubscriber.Add);
         });

         _observable.OnNext("first");
         _observable.OnNext("second");
      }

      [XF] public void the_new_subscriber_receives_only_the_notification_after_the_one_it_was_subscribed_during() =>
         _receivedByTheNewSubscriber.Must().SequenceEqual(["second"]);

      [XF] public void the_subscribing_subscriber_receives_both_notifications() =>
         _receivedByTheSubscribingSubscriber.Must().SequenceEqual(["first", "second"]);
   }

   public class While_a_subscriber_is_blocked_inside_a_notification_on_another_thread : SimpleObservable_
   {
      readonly bool _theSubscriberWasStillBlockedWhenSubscribingAndDisposingCompleted;

      public While_a_subscriber_is_blocked_inside_a_notification_on_another_thread()
      {
         var subscriberGate = IThreadGate.NewClosed(WaitTimeout.Seconds(10));
         _observable.Subscribe(_ => subscriberGate.AwaitPassThrough());

         using var taskRunner = TestingTaskRunner.WithTimeout(TimeSpan.FromSeconds(10));
         taskRunner.Run(() => _observable.OnNext("notification"));
         subscriberGate.AwaitQueueLengthEqualTo(1);

         _observable.Subscribe(_ => {}).Dispose();
         _theSubscriberWasStillBlockedWhenSubscribingAndDisposingCompleted = subscriberGate.Queued == 1;

         subscriberGate.Open();
      }

      [XF] public void subscribing_and_disposing_a_subscription_completes_while_the_subscriber_is_still_blocked() =>
         _theSubscriberWasStillBlockedWhenSubscribingAndDisposingCompleted.Must().BeTrue();
   }

   public class When_the_same_observer_is_subscribed_twice : SimpleObservable_
   {
      readonly RecordingObserver _observer = new();
      readonly IDisposable _firstSubscription;

      public When_the_same_observer_is_subscribed_twice()
      {
         _firstSubscription = _observable.Subscribe(_observer);
         _observable.Subscribe(_observer);
         _observable.OnNext("first");
      }

      [XF] public void the_observer_receives_each_notification_once_per_subscription() =>
         _observer.Received.Must().SequenceEqual(["first", "first"]);

      public class and_the_first_subscription_is_disposed : When_the_same_observer_is_subscribed_twice
      {
         public and_the_first_subscription_is_disposed()
         {
            _firstSubscription.Dispose();
            _observable.OnNext("second");
         }

         [XF] public void the_observer_still_receives_notifications_through_the_second_subscription() =>
            _observer.Received.Must().SequenceEqual(["first", "first", "second"]);
      }
   }

   public class When_a_subscriber_subscribes_after_a_subscription_between_two_others_was_disposed : SimpleObservable_
   {
      readonly List<string> _notifiedSubscribers = [];

      public When_a_subscriber_subscribes_after_a_subscription_between_two_others_was_disposed()
      {
         _observable.Subscribe(_ => _notifiedSubscribers.Add("first subscriber"));
         var secondSubscription = _observable.Subscribe(_ => _notifiedSubscribers.Add("second subscriber"));
         _observable.Subscribe(_ => _notifiedSubscribers.Add("third subscriber"));
         secondSubscription.Dispose();
         _observable.Subscribe(_ => _notifiedSubscribers.Add("fourth subscriber"));

         _observable.OnNext("notification");
      }

      [XF] public void the_remaining_subscribers_are_notified_in_the_order_they_subscribed() =>
         _notifiedSubscribers.Must().SequenceEqual(["first subscriber", "third subscriber", "fourth subscriber"]);
   }

   class RecordingObserver : IObserver<string>
   {
      readonly List<string> _received = [];
      public IReadOnlyList<string> Received => _received;

      public void OnNext(string value) => _received.Add(value);
      public void OnError(Exception error) => throw new NotSupportedException();
      public void OnCompleted() => throw new NotSupportedException();
   }
}
