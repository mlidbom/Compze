// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Hosting.MessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Abstractions.Time;
using Compze.Common.Refactoring.Naming;
using Compze.Tessaging.Implementation.MessageHandling.Abstractions;
using Newtonsoft.Json;

namespace Compze.Tessaging.Implementation.Abstractions;

public static class MessageTypesInternal
{
   internal interface IMessage;

   internal class EndpointInformationQuery : MessageTypesInternal.IMessage, IRemotableQuery<EndpointInformation>;

   internal class EndpointInformation
   {
#pragma warning disable IDE0051 // Remove unused private members
      [JsonConstructor] EndpointInformation(string name, EndpointId id, HashSet<TypeId> handledMessageTypes)
#pragma warning restore IDE0051 // Remove unused private members
      {
         Name = name;
         Id = id;
         HandledMessageTypes = handledMessageTypes;
      }

      public EndpointInformation(IEnumerable<TypeId> handledRemoteMessageTypeIds, EndpointConfiguration configuration)
      {
         Id = configuration.Id;
         Name = configuration.Name;
         HandledMessageTypes = [..handledRemoteMessageTypeIds];
      }

      public string Name { get; private set; }
      public EndpointId Id { get; private set; }
      public HashSet<TypeId> HandledMessageTypes { get; private set; }
   }

   public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
      => registrar.ForQuery((EndpointInformationQuery _, TypeMapper _, IMessageHandlerRegistry registry, EndpointConfiguration configuration) =>
                               new EndpointInformation(registry.HandledRemoteMessageTypeIds(), configuration));
}
