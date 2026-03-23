using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(AccountManagement.API.AssemblyTypeMapper))]

namespace AccountManagement.API;

class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<AccountResource>("84c1bfcd-a5dd-41e2-ade0-e25bbe0337c3")
         .Map<AccountResource.Tommand.ChangeEmail>("337af6fe-e645-49c7-9da1-b00dbc19cfa6")
         .Map<AccountResource.Tommand.ChangePassword>("80f95aa8-e751-4446-86cf-b94b603c95db")
         .Map<AccountResource.Tommand.LogIn>("6603f035-4f35-462d-8bca-152391cadd2e")
         .Map<AccountResource.Tommand.LogIn.LoginAttemptResult>("3822959a-a558-41e6-bdc7-2be7c42e44d9")
         .Map<AccountResource.Tommand.Register>("099173a3-0d5e-4c26-8566-b33dbf6a5a5a")
         .Map<AccountResource.Tommand.Register.RegistrationAttemptResult>("ec7a836e-62c8-4946-8433-6f46106f84b0")
         .Map<StartResource>("bf9b7eb2-98cb-473f-9b58-e07743a3eeb3")
         .Map<AccountManagement.Domain.AccountId>("fa6daaec-f8b9-4050-8e7f-a2d3a55cb148");
   }
}
