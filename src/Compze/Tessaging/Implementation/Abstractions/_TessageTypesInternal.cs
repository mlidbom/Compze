// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

using System;
using System.Collections.Generic;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;

namespace Compze.Tessaging.Implementation.Abstractions;

public static class TessageTypesInternal
{
   internal interface ITessage;

   internal class EndpointInformationTuery : TessageTypesInternal.ITessage, IRemotableTuery<EndpointInformation>;

   internal class EndpointInformation
   {
      [Obsolete("Called by serializer", error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
      public EndpointInformation() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

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
