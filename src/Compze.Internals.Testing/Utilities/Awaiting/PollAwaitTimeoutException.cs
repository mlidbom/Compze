using Compze.Threading;

namespace Compze.Internals.Testing.Utilities.Awaiting;

///<summary>Thrown by <see cref="PollAwaitExtensions.PollAwait{TThis}"/> when the condition it waited for never became true.</summary>
///<remarks>The message quotes the condition's own source text — captured by the compiler, not written by the caller — so a failing
/// specification names what it was waiting for without anyone having had to describe it twice.</remarks>
public class PollAwaitTimeoutException(object? polled, string? conditionExpression, WaitTimeout timeout)
   : Exception($"Waited {timeout} for this condition to become true, polling until the timeout expired:{Environment.NewLine}"
             + $"   {conditionExpression}{Environment.NewLine}"
             + $"The polled {polled?.GetType().Name ?? "object"} was: {polled}");
