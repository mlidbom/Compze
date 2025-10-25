namespace Compze.Abstractions.Tessaging.Teventive.Public;

/// <summary>
/// Marks an tevent as meaning that the aggregate was created.
/// <para>Can be used by clients to perform logic that should happen whenever an aggregate is created. </para>
/// <para>Is used in several places in the infrastructure and the infrastructure will fail in various ways if this tevents is not inherited correctly. For example:</para>
/// <para>Aggregate: Id is only set when such an tevent is raised. It is only ever possibly to raise 1 such tevent. More than one will cause an exception</para>
/// <para>SingleAggregateQueryModelUpdater: Creates the initial tuery model when it receives such an tevent</para>
/// </summary>
public interface IAggregateCreatedTevent : IAggregateTevent;