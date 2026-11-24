// ReSharper disable All
#pragma warning disable // Documentation example code: deliberately illustrative, not production code.

#region type_mapper
using Compze.TypeIdentifiers;
using Compze.Tessaging.Documentation.DeclaringEndpoints;
using Compze.Tessaging.Documentation.OnOneMachine;

[assembly: AssemblyTypeMapper(typeof(Compze.Tessaging.Documentation.TessageTypeMappings))]

namespace Compze.Tessaging.Documentation;

class TessageTypeMappings : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map) => map
      .Map<RegisterAccount>("632b961b-1b64-4747-bb5e-a76a9b46b58f")
      .Map<IAccountRegistered>("cb849aa3-e951-4334-9118-9dd55003378c")
      .Map<AccountRegistered>("eefb3f2d-256f-4a00-a280-07a62239959d")
      .Map<RecordWelcomeEmailSent>("d95c7d8d-bc07-41f1-91cd-bccb24704497")
      .Map<IIndexingProgressed>("599d64e1-18e7-4384-90be-d70609045d4d")
      .Map<IndexingProgressed>("1579d6ff-cc6d-4112-891f-51240ea20bba");
}
#endregion
