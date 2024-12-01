// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

using System.Collections.Generic;
using Compze.Messaging.Buses;
using Compze.Messaging.Buses.Implementation;
using Compze.Refactoring.Naming;
using Newtonsoft.Json;

namespace Compze.Messaging;

public static partial class MessageTypes
{
   internal static class Internal
   {
      internal interface IMessage;

      internal class EndpointInformationQuery : Internal.IMessage, IRemotableQuery<EndpointInformation>;

      internal class EndpointInformation
      {
#pragma warning disable IDE0051 // Remove unused private members
         [JsonConstructor]EndpointInformation(string name, EndpointId id, HashSet<TypeId> handledMessageTypes)
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

      public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
         (EndpointInformationQuery _, TypeMapper _, IMessageHandlerRegistry registry, EndpointConfiguration configuration) =>
            new EndpointInformation(registry.HandledRemoteMessageTypeIds(), configuration));
   }
}