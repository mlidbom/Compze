// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;

namespace Compze.Tessaging.Implementation.Abstractions;

public static class TessageTypesInternal
{
#pragma warning disable CA1040 // Marker interface used for type-routing
   public interface ITessage;
#pragma warning restore CA1040

   public class EndpointInformationTuery : TessageTypesInternal.ITessage, IRemotableTuery<EndpointInformation>;

   public class EndpointInformation
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

   public class NetworkTopologyTuery : TessageTypesInternal.ITessage, IRemotableTuery<NetworkTopology>;

   public class NetworkTopology
   {
      [Obsolete("Called by serializer", error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
      public NetworkTopology() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

      public NetworkTopology(IEnumerable<EndPointAddress> endpointAddresses) => EndpointAddresses = [..endpointAddresses];

      public List<EndPointAddress> EndpointAddresses { get; private set; }
   }

   public static void RegisterHandlers(TessageHandlerRegistrarWithDependencyInjectionSupport registrar)
   {
      registrar.ForTuery((EndpointInformationTuery _, TypeMapper _, ITessageHandlerRegistry registry, EndpointConfiguration configuration) =>
                            new EndpointInformation(registry.HandledRemoteTessageTypeIds(), configuration));

      registrar.ForTuery((NetworkTopologyTuery _, IEndpointRegistry endpointRegistry) =>
                            new NetworkTopology(endpointRegistry.ServerEndpointAddresses));
   }
}
