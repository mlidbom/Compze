using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Must;
using Compze.Tessaging.Typermedia;
using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Retry_policies_AtMostOnceTommand_when_tommand_handler_throws : EndpointHostTestBase
{
   protected override async Task InitializeAsyncInternal()
   {
      await base.InitializeAsyncInternal();
      MyCreateTaggregateTommandHandlerThreadGate.ThrowPostPassThrough(new Exception("82369B6E-80D4-4E64-92B6-A564A7195CC5"));

      Invoking(() => Navigator.Post(MyCreateTaggregateTommand.Create()))
                                    .Must().Throw<TessageDispatchingFailedException>();
   }

   [PCT] public void Tommand_handler_is_tried_5_times() => MyCreateTaggregateTommandHandlerThreadGate.Passed.Must().Be(expected: 5);
}

public class Retry_policies_AtMostOnceTommand_when_tommand_transaction_fails_to_commit_after_its_handler_returned : EndpointHostTestBase
{
   protected override async Task InitializeAsyncInternal()
   {
      await base.InitializeAsyncInternal();
      MyCreateTaggregateTommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception("D1C2E7A0-5B4F-4C0E-9A57-6E2B1F3C8D94"));

      Invoking(() => Navigator.Post(MyCreateTaggregateTommand.Create()))
                                    .Must().Throw<TessageDispatchingFailedException>();
   }

   [PCT] public void Tommand_handler_is_tried_once_since_a_failed_commit_may_have_committed() => MyCreateTaggregateTommandHandlerThreadGate.Passed.Must().Be(expected: 1);

   [PCT] public void ExactlyOnce_Tevent_raised_in_handler_reaches_local_handler_once() => MyLocalTaggregateTeventHandlerThreadGate.Passed.Must().Be(expected: 1);

   [PCT] public async Task ExactlyOnce_Tevent_raised_in_handler_does_not_reach_remote_handler()
   {
      (await MyRemoteTaggregateTeventHandlerThreadGate.TryAwaitPassedThroughCountEqualToAsync(count: 1, WaitTimeout.Seconds(1)))
                                                    .Must()
                                                    .Be(expected: false);
   }
}
