// ReSharper disable once CheckNamespace — Must match the real attribute's namespace for the compiler to use it
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Parameter)]
sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
{
   public string ParameterName { get; } = parameterName;
}
