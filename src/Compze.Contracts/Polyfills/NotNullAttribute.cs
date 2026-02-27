// ReSharper disable once CheckNamespace — Must match the real attribute's namespace for the compiler to use it
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
sealed class NotNullAttribute : Attribute;
