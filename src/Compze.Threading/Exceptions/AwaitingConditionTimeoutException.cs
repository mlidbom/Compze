namespace Compze.Threading.Exceptions;

///<summary>Thrown when a condition-wait operation times out before the condition becomes true.</summary>
public class AwaitingConditionTimeoutException : Exception
{
   ///<summary>Wraps a prior <see cref="AwaitingConditionTimeoutException"/> with additional context in <paramref name="message"/>.</summary>
   public AwaitingConditionTimeoutException(AwaitingConditionTimeoutException parent, string message) : base(message, innerException: parent)
   { }

   internal AwaitingConditionTimeoutException() : base("Timed out waiting for condition to become true.") {}
}
