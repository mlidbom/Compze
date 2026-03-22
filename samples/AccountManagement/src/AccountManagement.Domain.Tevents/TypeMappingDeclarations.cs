using Compze.TypeIdentifiers;

[assembly: TypeMappings(typeof(AccountManagement.Domain.Tevents.TypeMappingDeclarations))]

namespace AccountManagement.Domain.Tevents;

class TypeMappingDeclarations : ITypeMappingDeclaration
{
   public void DeclareMappings(ITypeMappingRegistrar map)
   {
      map.Map<IAccountTevent.Created>("ae1684ff-a150-4840-ac08-1b9d21806da6")
         .Map<AccountTevent.LoggedIn>("37079b83-103e-4832-a718-3ad4c71700a7")
         .Map<AccountTevent.LoginFailed>("57fbdbc8-e0c8-4161-a67d-b086d69b1b16")
         .Map<AccountTevent>("1299958f-bb1c-423b-9e44-be1ca8638cf9")
         .Map<AccountTevent.UserChangedEmail>("61556a08-356c-4fa7-a63c-91e667ea65c9")
         .Map<AccountTevent.UserChangedPassword>("19998285-432f-4296-93e8-7a8d0f6814bd")
         .Map<AccountTevent.UserRegistered>("80d90851-4ae5-4818-9d62-dc255fb5bcd3")
         .Map<IAccountTevent.LoggedIn>("a43f6c96-a985-4060-8b22-f6f9bcac0755")
         .Map<IAccountTevent.LoginAttempted>("665023a5-515d-4edf-9cb6-5288baf29db2")
         .Map<IAccountTevent.LoginFailed>("5dbee37a-a4f9-43da-8229-baf5ecc93cf1")
         .Map<IAccountTevent.PropertyUpdated.Email>("33933b47-b98e-4c8a-98ae-2ecf88d962f5")
         .Map<IAccountTevent.PropertyUpdated.Password>("a533f2ff-155f-4d53-a755-eb2ac81c61e8")
         .Map<IAccountTevent>("8aea46ad-968d-4e38-8c2d-3736de0c70b3")
         .Map<IAccountTevent.UserChangedEmail>("aeaf8613-14af-4c5b-acad-0d07570b1677")
         .Map<IAccountTevent.UserChangedPassword>("648b86c0-49d5-4987-b7a8-6581f9aedc4d")
         .Map<IAccountTevent.UserRegistered>("ca41c0b1-56f7-4a21-914c-af0729de29ba");
   }
}
