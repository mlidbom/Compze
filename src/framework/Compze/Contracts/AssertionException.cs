using System;

namespace Compze.Contracts;

class AssertionException : Exception
{
   public AssertionException(InspectionType inspectionType, string failureMessage) : base($"{inspectionType}: {failureMessage}") {}
}