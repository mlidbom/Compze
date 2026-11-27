using System.Transactions;
using Compze.Contracts;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.TessageBus;

namespace Compze.Tessaging.Engine.HandlerRegistration._private;

///<summary>What a handler's registration chose about how an exactly-once endpoint's inbox handles its tessages: the<br/>
/// <see cref="HandlingOrder"/>, and how long the handling's transaction may run before it times out and rolls back.</summary>
sealed class InboxHandlingChoices
{
   ///<summary>The choices of a registration that chose nothing: the tessages keep their place in the order, and the handling's<br/>
   /// transaction times out after <see cref="TransactionScopeCe.DefaultTimeout"/>.</summary>
   internal static readonly InboxHandlingChoices Default = new(HandlingOrder.AsAdmitted, TransactionScopeCe.DefaultTimeout);

   InboxHandlingChoices(HandlingOrder order, TimeSpan transactionTimeout)
   {
      Order = order;
      TransactionTimeout = transactionTimeout;
   }

   internal HandlingOrder Order { get; }

   ///<summary>How long the inbox's handling transaction may run before it times out and rolls back.</summary>
   internal TimeSpan TransactionTimeout { get; }

   ///<summary>The choices a registration made: <paramref name="transactionTimeout"/> null keeps<br/>
   /// <see cref="TransactionScopeCe.DefaultTimeout"/>. A timeout that isn't positive, or that<br/>
   /// <see cref="TransactionManager.MaximumTimeout"/> would silently shorten, explodes.</summary>
   internal static InboxHandlingChoices Chosen(HandlingOrder order, TimeSpan? transactionTimeout)
   {
      if(transactionTimeout is not {} timeout) return new InboxHandlingChoices(order, TransactionScopeCe.DefaultTimeout);

      Argument.Assert(timeout > TimeSpan.Zero, () => $"A handler's transaction timeout must be longer than zero, and was {timeout}.");
      Argument.Assert(TheMaximumTimeoutAllows(timeout),
                      () => $"A handler's transaction timeout of {timeout} is longer than {nameof(TransactionManager)}.{nameof(TransactionManager.MaximumTimeout)}, {TransactionManager.MaximumTimeout}, which would silently shorten it. Raise {nameof(TransactionManager)}.{nameof(TransactionManager.MaximumTimeout)}, process-wide, before declaring the handler.");
      return new InboxHandlingChoices(order, timeout);

      //A maximum of zero is no maximum.
      static bool TheMaximumTimeoutAllows(TimeSpan timeout) => TransactionManager.MaximumTimeout == TimeSpan.Zero || timeout <= TransactionManager.MaximumTimeout;
   }

   ///<summary>The choices for a tevent that runs the handlers whose registrations made <paramref name="choicesOfItsHandlers"/>, all in<br/>
   /// one unit of work. It gives up its place in the order only when every handler gives it up; one that runs no handler keeps it,<br/>
   /// since what its observers see follows the order its handlings commit in. Its transaction may run for as long as the<br/>
   /// longest timeout any of them chose.</summary>
   internal static InboxHandlingChoices OfATeventRunning(IReadOnlyCollection<InboxHandlingChoices> choicesOfItsHandlers) =>
      choicesOfItsHandlers.Count == 0
         ? Default
         : new InboxHandlingChoices(choicesOfItsHandlers.All(it => it.Order == HandlingOrder.None) ? HandlingOrder.None : HandlingOrder.AsAdmitted,
                                    choicesOfItsHandlers.Max(it => it.TransactionTimeout));
}
