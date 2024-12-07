﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Testing.TestFrameworkExtensions.XUnit;

///<summary>
/// This attribute will run the test eXclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
///This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
[XunitTestCaseDiscoverer("Compze.Testing.TestFrameworkExtensions.XUnit", "Compze.Testing")]
public class XFactAttribute : FactAttribute {}

[UsedImplicitly] public class XFactDiscoverer(IMessageSink diagnosticMessageSink) : IXunitTestCaseDiscoverer
{
   readonly IMessageSink _diagnosticMessageSink = diagnosticMessageSink;

   public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
   {
      var declaringType = testMethod.Method.ToRuntimeMethod().DeclaringType;
      var currentType = testMethod.TestClass.Class.ToRuntimeType();

      if(declaringType != currentType) // Skip tests declared in base classes
         return Array.Empty<IXunitTestCase>();

      return [new XunitTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod)];
   }
}
