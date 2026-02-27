// ReSharper disable once CheckNamespace — Must match the real attribute's namespace for the compiler to use it
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method)]
sealed class DoesNotReturnAttribute : Attribute;
