using System;

namespace Compze.Contracts;

public class AssertionException : Exception
{
   public AssertionException(InspectionType inspectionType, string failureMessage) : base($"{inspectionType}: {failureMessage}") {}
}