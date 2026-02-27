// ReSharper disable once CheckNamespace — Must match the real attribute's namespace for the compiler to use it
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter)]
sealed class DoesNotReturnIfAttribute(bool parameterValue) : Attribute
{
   public bool ParameterValue { get; } = parameterValue;
}
