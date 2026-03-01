using System;

namespace Compze.Threading.ResourceAccess.Exceptions;

public class AwaitingConditionTimeoutException : Exception
{
   public AwaitingConditionTimeoutException(AwaitingConditionTimeoutException parent, string message) : base(message, innerException: parent)
   { }

   public AwaitingConditionTimeoutException() : base("Timed out waiting for condition to become true.") {}
}
