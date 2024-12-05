using System;
using Compze.Contracts.Deprecated;

#pragma warning disable CA1815 // Override equals and operator equals on value types

namespace Compze.Contracts;

static class Assert
{
   ///<summary>Assert conditions about current state of "this". Failures would mean that someone made a call that is illegal given state of "this".</summary>
   public static BaseAssertion State { get; } = BaseAssertion.StateInstance;

   ///<summary>Assert something that must always be true for "this".</summary>
   public static BaseAssertion Invariant { get; } = BaseAssertion.InvariantInstance;

   ///<summary>Assert conditions on arguments to current method.</summary>
   public static BaseAssertion Argument { get; } = BaseAssertion.ArgumentsInstance;

   ///<summary>Assert conditions on the result of making a method call.</summary>
   public static BaseAssertion Result { get; } = BaseAssertion.ResultInstance;

   public class AssertionException : Exception
   {
      public AssertionException(InspectionType inspectionType, int index) : base($"{inspectionType}: {index}") { }
   }
}