using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Public;

public interface ITeventModifier
{
   ///<summary>Replaces the inspected tevent with <paramref name="wrappedTevents"/>. Each replacement is a complete wrapped tevent:<br/>
   /// the migration author supplies the <see cref="ITaggregateTevent{TTeventInterface}"/> wrapper, so publisher identity is rewritten as deliberately as the tevent itself.</summary>
   void Replace(params ITaggregateTevent<ITaggregateTevent>[] wrappedTevents);
   ///<summary>Inserts <paramref name="insert"/> before the inspected tevent. Each inserted tevent is a complete wrapped tevent:<br/>
   /// the migration author supplies the <see cref="ITaggregateTevent{TTeventInterface}"/> wrapper, so publisher identity is written as deliberately as the tevent itself.</summary>
   void InsertBefore(params ITaggregateTevent<ITaggregateTevent>[] insert);
}