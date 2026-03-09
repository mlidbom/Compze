using Xunit;

namespace Compze.Threading.InternalSpecifications.TestInfrastructure;

[CollectionDefinition(nameof(NonParallelCollection), DisableParallelization = true)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class NonParallelCollection;
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
