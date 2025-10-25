using System;

namespace Compze.Utilities.Threading.ResourceAccess;

class AwaitingConditionTimeoutException : Exception
{
   public AwaitingConditionTimeoutException(AwaitingConditionTimeoutException parent, string tessage) : base(tessage, innerException: parent)
   { }

   public AwaitingConditionTimeoutException() : base("Timed out waiting for condition to become true.") {}
}
