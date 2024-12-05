using System;

namespace Compze.Contracts.Deprecated;

class AssertionException : Exception
{
   public AssertionException(InspectionType inspectionType, string failureMessage) : base($"{inspectionType}: {failureMessage}") { }
}