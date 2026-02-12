using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;

public interface ITeventModifier
{
   void Replace(params TaggregateTevent[] tevents);
   void InsertBefore(params TaggregateTevent[] insert);
}