using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using Compze.TypeIdentifiers;

namespace AccountManagement;

public static class AccountManagementTypeMappings
{
   /// <summary>Registers the AccountManagement domain type mappings (domain, tevents, and API resources).</summary>
   public static void RegisterAccountManagementTypeMappings(this ITypeMapper mapper)
   {
      mapper.MapTypesFromAssemblyContaining<Account>();
      mapper.MapTypesFromAssemblyContaining<IAccountTevent>();
      mapper.MapTypesFromAssemblyContaining<AccountResource>();
   }
}
