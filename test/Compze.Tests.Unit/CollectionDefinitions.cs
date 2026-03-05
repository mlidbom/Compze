using Xunit;

namespace Compze.Tests.Unit;

[CollectionDefinition(nameof(NonParallelCollection), DisableParallelization = true)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class NonParallelCollection
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
}
