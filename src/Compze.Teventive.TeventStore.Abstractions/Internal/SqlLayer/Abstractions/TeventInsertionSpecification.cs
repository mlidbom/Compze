namespace Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;

public class TeventInsertionSpecification(TaggregateTeventData tevent, int insertedVersion, int effectiveVersion)
{
   public TeventInsertionSpecification(TaggregateTeventData tevent) : this(tevent, tevent.TaggregateVersion, tevent.TaggregateVersion) {}

   internal TaggregateTeventData Tevent { get; } = tevent;
   public int InsertedVersion { get; } = insertedVersion;
   internal int EffectiveVersion { get; } = effectiveVersion;
}