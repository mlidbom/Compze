﻿namespace Composable.GenericAbstractions.Wrappers;

/// <summary>
/// Represents the generic concept of a type that extends another type by containing a value of the other type.
/// </summary>
interface IWrapper<out T>
{
   ///<summary>The wrapped value.</summary>
   T Wrapped { get; }
}