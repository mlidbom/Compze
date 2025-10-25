using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TEventStore.Refactoring.Migrations.Public;

public interface ITeventModifier
{
   void Replace(params TaggregateTevent[] tevents);
   void InsertBefore(params TaggregateTevent[] insert);
}