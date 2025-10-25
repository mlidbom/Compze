namespace Compze.Sql.Common.TeventStore.Abstractions;

public class TeventInsertionSpecification(AggregateTeventData @tevent, int insertedVersion, int effectiveVersion)
{
   public TeventInsertionSpecification(AggregateTeventData @tevent) : this(@tevent, @tevent.AggregateVersion, @tevent.AggregateVersion) {}

   internal AggregateTeventData Tevent { get; } = @tevent;
   internal int InsertedVersion { get; } = insertedVersion;
   internal int EffectiveVersion { get; } = effectiveVersion;
}