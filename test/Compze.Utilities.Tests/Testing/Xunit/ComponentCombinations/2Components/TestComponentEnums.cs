// ReSharper disable UnusedMember.Global
namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components;

#pragma warning disable CA1724 //I don't care that a namespace somewhere has the same name as one of these types

/// <summary>
/// Serializer component dimension for pluggable components testing.
/// Each enum value corresponds to a component in the TestUsingPluggableComponentCombinations file.
/// </summary>
internal enum Serializer
{
   Microsoft,
   Newtonsoft
}

/// <summary>
/// SQL layer component dimension for pluggable components testing.
/// Each enum value corresponds to a component in the TestUsingPluggableComponentCombinations file.
/// </summary>
internal enum SqlLayer
{
   MsSql,
   Postgre,
   MySql
}

internal enum DIContainer
{
   Microsoft,
   SimpleInjector
}

internal enum TeventStore
{
   InMemory,
   SqlServer
}

internal enum TessageBus
{
   InProcess,
   RabbitMQ
}
