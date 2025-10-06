// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

using System.Collections.Generic;
using Compze.Abstractions.Internal;
using Compze.Abstractions.Internal.Refactoring;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Common.Refactoring.Naming;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Newtonsoft.Json;

namespace Compze.Tessaging;

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

    public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery((EndpointInformationQuery _, TypeMapper _, IMessageHandlerRegistry registry, EndpointConfiguration configuration) =>
                                                                                                                                   new EndpointInformation(registry.HandledRemoteMessageTypeIds(), configuration));

    internal static void MapTypes(ITypeMappingRegistrar typeMapper)
    {
        typeMapper
           .MapTypeAndStandardCollectionTypes<MessageTypesInternal.EndpointInformationQuery>("D94259E4-7479-442C-99AE-D49C12CF8713")
           .MapTypeAndStandardCollectionTypes<MessageTypesInternal.EndpointInformation>("2B598C6D-4893-4CB9-B4CE-7B705AD92DF9");
    }
}
