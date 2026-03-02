// ReSharper disable once CheckNamespace — Must match the real attribute's namespace for the compiler to use it
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies that the method will not return if the associated Boolean parameter is passed the specified value.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
sealed class DoesNotReturnIfAttribute(bool parameterValue) : Attribute
{
   public bool ParameterValue { get; } = parameterValue;
}
