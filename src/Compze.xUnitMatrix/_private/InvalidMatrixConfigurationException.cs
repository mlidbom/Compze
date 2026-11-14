namespace Compze.xUnitMatrix._private;

///<summary>
/// Thrown when a matrix's configuration cannot be used: its matrix attribute's dimension enum types, the
/// <see cref="SkipAttribute{TDimension}"/>s on a test method, or the <see cref="MatrixConfigurationFile"/> it names.<br/>
/// <see cref="MatrixTheoryDiscoverer"/> turns it into a failing test whose message is this exception's.
///</summary>
class InvalidMatrixConfigurationException(string message) : Exception(message);
