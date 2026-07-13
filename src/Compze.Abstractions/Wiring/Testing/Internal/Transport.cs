namespace Compze.Abstractions.Wiring.Testing.Internal;

#pragma warning disable CA1724 //We don't much care that there is a namespace somewhere with the same name.
public enum Transport
{
   // ReSharper disable UnusedMember.Global values are referenced by name when the test-matrix configuration file is parsed
   AspNetCore = 1,
   NamedPipes = 2
   // ReSharper restore UnusedMember.Global
}
#pragma warning restore CA1724 //We don't much care that there is a namespace somewhere with the same name.
