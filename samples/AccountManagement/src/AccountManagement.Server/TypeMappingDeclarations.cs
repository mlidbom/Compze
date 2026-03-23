using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(AccountManagement.AssemblyTypeMapper))]

namespace AccountManagement;

class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<Domain.Account>("57fc716d-d8ca-4224-9f78-4d3b5a7f9ebd")
         .Map<UI.QueryModels.AccountQueryModel>("aee890be-6bb0-4301-90d5-492b0b42a4a8")
         .Map<UI.QueryModels.AccountStatistics.SingletonStatisticsQueryModel>("fd494342-f588-4337-b9de-4222da24169e");
   }
}
