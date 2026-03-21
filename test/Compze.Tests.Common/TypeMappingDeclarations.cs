using Compze.Abstractions.Refactoring.Naming;

[assembly: TypeMappings(typeof(Compze.Tests.Common.TypeMappingDeclarations))]

namespace Compze.Tests.Common;

class TypeMappingDeclarations : ITypeMappingDeclaration
{
   public void DeclareMappings(ITypeMappingRegistrar map)
   {
      map.Map<CQRS.TeventRefactoring.Migrations.ITestTaggregateTevent>("11e69911-38c6-4dd9-9798-0ac8014e52fe")
         .Map<CQRS.TeventRefactoring.Migrations.TestTaggregate>("858998a5-20d9-474a-9ec9-595d60cf2a3f")
         .Map<CQRS.TeventRefactoring.Migrations.TestTaggregateTevent>("e97aa97c-3761-404f-a8b4-6689e22c9aa3")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.E1>("e5e9e630-3b2c-4323-9bf2-a4d2270157aa")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.E2>("32eb31d6-e61a-41ae-8f4c-4ca275c056a6")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.E3>("fd649708-a138-4d22-abb2-15c3c38e1af4")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.E4>("bf452ac4-a6ec-417b-adb7-b6760aca1f91")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.E5>("d95c69d5-ca1a-47ba-920c-fbdc3ee673bf")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.E6>("862f902c-5e2c-4fa7-bba3-1b5853e6dbf9")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.E7>("f6ea065a-8ea5-4ef4-8d27-cf8b427d5be7")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.E8>("4df3c2de-a77f-4e3e-9c33-7bc213213bf4")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.E9>("5467fe0f-0c6f-4ca6-b0f7-3e5a154704d4")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.Ec1>("2d1f06a3-2a0b-42c3-8105-226bfd8f1bd8")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.Ec2>("a87101d3-38a0-4c2d-b50d-e28adf9f4ddd")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.Ec3>("361c1132-caa7-4261-909c-d16a02dfb153")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.EcAbstract>("41ac6559-69b3-4868-abf3-64b2c2f14151")
         .Map<CQRS.TeventRefactoring.Migrations.Tevents.Ef>("437c3500-ece7-46bf-bc58-3b3fcd8cac1b")
         .Map<Sql.DocumentDb.Dog>("9cdd62b2-e7e6-4443-a5ce-900a610ef8e3")
         .Map<Sql.DocumentDb.Email>("b0c1d2e3-f4a5-6789-0abc-def012345678")
         .Map<Sql.DocumentDb.Person>("95204c89-46f6-4dac-af3e-957fb547cc3a")
         .Map<Sql.DocumentDb.User>("d40010d2-912a-4496-b7c8-2b1cec7297d2")
         .Map<Sql.DocumentDb.UserSet>("A4F4E620-1889-4C0C-90F1-772B16C65075")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.IMyExactlyOnceTevent>("91db1fa3-d379-4485-9904-0e8a7a21566f")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.IMyTaggregateTevent>("919c31dd-596e-4b80-a5fd-13af2319ee16")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.IMyTaggregateTevent.Created>("7b4dbf40-39c6-4403-b622-482e98c73601")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.IMyTaggregateTevent.Updated>("2a04991f-0cd9-4bc8-b9a4-30ed5e8f66e4")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.MyAtMostOnceTypermediaTommandWithResult>("ff9beeb1-9ede-43a5-9f7e-64ac0dccaf16")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.MyCreateTaggregateTommand>("cb7ce885-5300-4e63-8b72-c86ab6b61655")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.MyExactlyOnceTevent>("4f37723d-f981-4aff-9a8c-e0c3ee53b568")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.MyExactlyOnceTommand>("c0233a55-955f-4ec9-b0dc-376f1c8b1f9a")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.MyTaggregate>("9a19bd04-7d1e-4846-a4a3-909b889a3d44")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.MyTaggregateTevent>("39c8432a-4481-4552-9d91-d28810e64155")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.MyTaggregateTevent.Created>("69068b48-4efd-4e3f-9f84-17c3f7881ef0")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.MyTaggregateTevent.Updated>("139c9a83-92ce-4f52-9a09-02f579a9e61b")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.MyTuery>("c4f84bc9-82e2-4ba5-8840-585ede693d69")
         .Map<Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler.MyUpdateTaggregateTommand>("5f82364d-820a-4bba-9dca-470c75e73ea4");
   }
}
