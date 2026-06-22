using Compze.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;

public interface ITeventModifier
{
   void Replace(params TaggregateTevent[] tevents);
   void InsertBefore(params TaggregateTevent[] insert);
}