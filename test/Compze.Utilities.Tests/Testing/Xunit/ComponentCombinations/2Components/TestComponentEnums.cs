// ReSharper disable UnusedMember.Global
namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components;

#pragma warning disable CA1724 //I don't care that a namespace somewhere has the same name as one of these types

/// <summary>
/// Serializer component dimension for pluggable components testing.
/// Each enum value corresponds to a component in the TestUsingPluggableComponentCombinations file.
/// </summary>
enum Serializer
{
   Microsoft,
   Newtonsoft
}

/// <summary>
/// SQL layer component dimension for pluggable components testing.
/// Each enum value corresponds to a component in the TestUsingPluggableComponentCombinations file.
/// </summary>
enum SqlLayer
{
   MsSql,
   Postgre,
   MySql
}

enum DIContainer
{
   Microsoft,
   SimpleInjector
}

enum TeventStore
{
   InMemory,
   SqlServer
}

enum TessageBus
{
   InProcess,
   RabbitMQ
}
