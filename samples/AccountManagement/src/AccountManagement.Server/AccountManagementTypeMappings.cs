using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using Compze.DependencyInjection.Abstractions;
using Compze.TypeIdentifiers.DependencyInjection;

namespace AccountManagement;

public static class AccountManagementTypeMappings
{
   /// <summary>Requires the type identity the AccountManagement domain needs: its domain types, its tevents, and its API resources.</summary>
   public static IComponentRegistrar RequireAccountManagementTypeMappings(this IComponentRegistrar @this) =>
      @this.RequireMappedTypesFromAssemblyContaining<Account>()
           .RequireMappedTypesFromAssemblyContaining<IAccountTevent>()
           .RequireMappedTypesFromAssemblyContaining<AccountResource>();
}
