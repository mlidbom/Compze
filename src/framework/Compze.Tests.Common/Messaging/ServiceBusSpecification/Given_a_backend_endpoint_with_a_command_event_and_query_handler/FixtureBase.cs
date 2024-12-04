﻿using System;
using System.Collections.Generic;
using Compze.Messaging.Buses;
using Compze.Messaging.Hypermedia;
using Compze.Testing;
using Compze.Testing.Threading;

namespace Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public abstract class FixtureBase(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   internal ITestingEndpointHost Host;
   internal IThreadGate CommandHandlerThreadGate;
   internal IThreadGate CommandHandlerWithResultThreadGate;
   internal IThreadGate MyCreateAggregateCommandHandlerThreadGate;
   internal IThreadGate MyUpdateAggregateCommandHandlerThreadGate;
   internal IThreadGate MyRemoteAggregateEventHandlerThreadGate;
   internal IThreadGate MyLocalAggregateEventHandlerThreadGate;
   internal IThreadGate EventHandlerThreadGate;
   internal IThreadGate QueryHandlerThreadGate;
   internal IReadOnlyList<IThreadGate> AllGates = [];
   protected static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
   protected IEndpoint ClientEndpoint { get; set; }
   protected IEndpoint RemoteEndpoint { get; set; }
   protected IRemoteHypermediaNavigator RemoteNavigator => ClientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();
}
