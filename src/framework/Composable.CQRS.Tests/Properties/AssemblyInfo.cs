﻿using System.Reflection;
using System.Runtime.InteropServices;
using Composable.DependencyInjection;
using Composable.Testing;
using NCrunch.Framework;
using NUnit.Framework;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("Core.Tests")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("Core.Tests")]
[assembly: AssemblyCopyright("Copyright © Microsoft 2009")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("750bf622-43eb-4542-9043-8b99dd374195")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:

[assembly: AssemblyVersion("1.0.0.0")]
//urgent: include at least MySql as testing PersistenceLayerProvider [assembly:NCrunch.Framework.DuplicateByDimensions(nameof(PersistenceLayer.MsSql), nameof(PersistenceLayer.MySql), nameof(PersistenceLayer.InMemory))]
//Urgent: Make sure that only tests that use the duplicated persistence layers accurately are impacted by duplication

[assembly:ConfigurationBasedDuplicateByDimensions]