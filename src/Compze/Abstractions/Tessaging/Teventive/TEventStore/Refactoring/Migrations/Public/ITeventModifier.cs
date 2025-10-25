using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;

public interface ITeventModifier
{
   void Replace(params TaggregateTevent[] tevents);
   void InsertBefore(params TaggregateTevent[] insert);
}