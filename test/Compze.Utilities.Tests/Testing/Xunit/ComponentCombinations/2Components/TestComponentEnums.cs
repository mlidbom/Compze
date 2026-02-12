// ReSharper disable UnusedMember.Global
namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components;

#pragma warning disable CA1724 //I don't care that a namespace somewhere has the same name as one of these types

/// <summary>
/// Serializer component dimension for pluggable components testing.
/// Each enum value corresponds to a component in the TestUsingPluggableComponentCombinations file.
/// </summary>
public enum Serializer
{
   Microsoft,
   Newtonsoft
}

/// <summary>
/// SQL layer component dimension for pluggable components testing.
/// Each enum value corresponds to a component in the TestUsingPluggableComponentCombinations file.
/// </summary>
public enum SqlLayer
{
   MsSql,
   Postgre,
   MySql
}

public enum DIContainer
{
   Microsoft,
   SimpleInjector
}

public enum TeventStore
{
   InMemory,
   SqlServer
}

public enum TessageBus
{
   InProcess,
   RabbitMQ
}
