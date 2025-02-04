﻿using System;
using Compze.DependencyInjection;
using Compze.Messaging.Buses;
using Compze.Testing.Persistence;

namespace Compze.Testing.Messaging.Buses;

public class TestingEndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory) : TestingEndpointHostBase(mode, containerFactory)
{
   public static ITestingEndpointHost Create(Func<IRunMode, IDependencyInjectionContainer> containerFactory)
      => new TestingEndpointHost(new RunMode(isTesting: true), containerFactory);

   internal override void ExtraEndpointConfiguration(IEndpointBuilder builder) => builder.RegisterCurrentTestsConfiguredPersistenceLayer();
}