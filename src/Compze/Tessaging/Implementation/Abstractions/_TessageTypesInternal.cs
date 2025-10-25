// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

using System.Collections.Generic;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Newtonsoft.Json;

namespace Compze.Tessaging.Implementation.Abstractions;

public static class TessageTypesInternal
{
   internal interface ITessage;

   internal class EndpointInformationTuery : TessageTypesInternal.ITessage, IRemotableTuery<EndpointInformation>;

   internal class EndpointInformation
   {
#pragma warning disable IDE0051 // Remove unused private members
      [JsonConstructor] EndpointInformation(string name, EndpointId id, HashSet<TypeId> handledTessageTypes)
#pragma warning restore IDE0051 // Remove unused private members
      {
         Name = name;
         Id = id;
         HandledTessageTypes = handledTessageTypes;
      }

      public EndpointInformation(IEnumerable<TypeId> handledRemoteTessageTypeIds, EndpointConfiguration configuration)
      {
         Id = configuration.Id;
         Name = configuration.Name;
         HandledTessageTypes = [..handledRemoteTessageTypeIds];
      }

      public string Name { get; private set; }
      public EndpointId Id { get; private set; }
      public HashSet<TypeId> HandledTessageTypes { get; private set; }
   }

   public static void RegisterHandlers(TessageHandlerRegistrarWithDependencyInjectionSupport registrar)
      => registrar.ForTuery((EndpointInformationTuery _, TypeMapper _, ITessageHandlerRegistry registry, EndpointConfiguration configuration) =>
                               new EndpointInformation(registry.HandledRemoteTessageTypeIds(), configuration));
}
