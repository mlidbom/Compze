
namespace Compze.Hosting.Testing;

///<summary>One combination of the pluggable components a test run exercises: which SQL backend, DI container, serializer and transport the framework is wired with. The test matrix runs the suite once per configured combination.</summary>
public readonly record struct PluggableComponents(SqlLayer SqlLayer, DIContainer DiContainer, Serializer Serializer, Transport Transport)
{
   public override string ToString() => $"{SqlLayer}:{DiContainer}:{Serializer}:{Transport}";
}
