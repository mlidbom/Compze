namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public class TeventInsertionSpecification(TaggregateTeventData tevent, int insertedVersion, int effectiveVersion)
{
   public TeventInsertionSpecification(TaggregateTeventData tevent) : this(tevent, tevent.TaggregateVersion, tevent.TaggregateVersion) {}

   public TaggregateTeventData Tevent { get; } = tevent;
   public int InsertedVersion { get; } = insertedVersion;
   public int EffectiveVersion { get; } = effectiveVersion;
}