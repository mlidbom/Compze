﻿using System.Collections.Generic;
using Compze.Refactoring.Naming;
using Compze.Serialization;
using Compze.Testing;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;
// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Compze.Tests.Unit.Internals.Serialization
{
   namespace OriginalTypes
   {
      class BaseTypeA;

      class TypeA : BaseTypeA
      {
         internal static TypeA Create() => new() { TypeAName = typeof(TypeA).FullName };

         public string? TypeAName { get; set; }

         public class TypeAA : TypeA
         {
            public new static TypeA Create() => new TypeAA { TypeAAName = typeof(TypeAA).FullName };
            public string? TypeAAName { get; set; }
         }
      }

      class TypeB : BaseTypeA
      {
         internal static TypeB Create() => new() { TypeBName = typeof(TypeB).FullName };
         public string? TypeBName { get; set; }

         public class TypeBB : TypeB
         {
            public new static TypeBB Create() => new() { TypeBBName = typeof(TypeBB).FullName };
            public string? TypeBBName { get; set; }
         }
      }

      class Root
      {
         internal static Root Create() => new()
                                          {
                                             TypeA = OriginalTypes.TypeA.Create(),
                                             TypeB = OriginalTypes.TypeB.Create(),
                                             ListOfTypeA = [OriginalTypes.TypeA.Create(), OriginalTypes.TypeB.Create(), OriginalTypes.TypeA.TypeAA.Create(), OriginalTypes.TypeB.TypeBB.Create()]
                                          };

         public BaseTypeA? TypeA { get; set; }
         public BaseTypeA? TypeB { get; set; }

         public List<BaseTypeA>? ListOfTypeA { get; set; }
      }
   }

   namespace RenamedTypes
   {
      class BaseTypeA;

      class TypeA : BaseTypeA
      {
         public string? TypeAName { get; set; }

         [UsedImplicitly] public class TypeAA : TypeA
         {
            public string? TypeAAName { get; set; }
         }
      }

      class TypeB : BaseTypeA
      {
         public string? TypeBName { get; set; }

         [UsedImplicitly] public class TypeBB : TypeB
         {
            public string? TypeBBName { get; set; }
         }
      }

      class Root
      {
         public BaseTypeA? TypeA { get; set; }
         public BaseTypeA? TypeB { get; set; }

         public List<BaseTypeA>? ListOfTypeA { get; set; }
      }
   }

   [TestFixture] public class TypeRenamingSerializationTests : UniversalTestBase
   {
      ITypeMapper _originaltypesMap;
      ITypeMapper _renamedTypesMap;
      RenamingSupportingJsonSerializer _originalTypesSerializer;
      RenamingSupportingJsonSerializer _renamedTypesSerializer;

      static class Ids
      {
         internal const string TypeA = "5A4DACAF-FAE1-4D8A-87AA-99E84CE4819B";
         internal const string TypeAA = "d774e63b-c796-4219-8570-882cceb072a3";
         internal const string TypeB = "AADA2B9D-62BC-4C81-ADF1-E8075F41D2BA";
         internal const string TypeBB = "243C4874-529F-44B6-91BE-1353DB87AAEE";
      }

      [OneTimeSetUp] public void SetupTask()
      {
         _originaltypesMap = new TypeMapper();
         _renamedTypesMap = new TypeMapper();

         ((ITypeMappingRegistar)_originaltypesMap)
           .Map<OriginalTypes.TypeA>(Ids.TypeA)
           .Map<OriginalTypes.TypeB>(Ids.TypeB)
           .Map<OriginalTypes.TypeA.TypeAA>(Ids.TypeAA)
           .Map<OriginalTypes.TypeB.TypeBB>(Ids.TypeBB);

         ((ITypeMappingRegistar)_renamedTypesMap)
           .Map<RenamedTypes.TypeA>(Ids.TypeA)
           .Map<RenamedTypes.TypeB>(Ids.TypeB)
           .Map<RenamedTypes.TypeA.TypeAA>(Ids.TypeAA)
           .Map<RenamedTypes.TypeB.TypeBB>(Ids.TypeBB);

         _originalTypesSerializer = new RenamingSupportingJsonSerializer(JsonSettings.JsonSerializerSettings, _originaltypesMap);
         _renamedTypesSerializer = new RenamingSupportingJsonSerializer(JsonSettings.JsonSerializerSettings, _renamedTypesMap);
      }

      [Test] public void Roundtrips_polymorphic_types()
      {
         var originalRoot = OriginalTypes.Root.Create();
         var originalJson = _originalTypesSerializer.Serialize(originalRoot);
         var deserializedRoot = (OriginalTypes.Root)_originalTypesSerializer.Deserialize(typeof(OriginalTypes.Root), originalJson);

         deserializedRoot.Should().BeEquivalentTo(originalRoot, options => options.RespectingRuntimeTypes());
         originalRoot.Should().BeEquivalentTo(deserializedRoot, options => options.RespectingRuntimeTypes());
      }

      [Test] public void Handles_renaming_of_types()
      {
         var originalRoot = OriginalTypes.Root.Create();
         var originalJson = _originalTypesSerializer.Serialize(originalRoot);

         var deserializedRenamedRoot = (RenamedTypes.Root)_renamedTypesSerializer.Deserialize(typeof(RenamedTypes.Root), originalJson);

         deserializedRenamedRoot.Should().BeEquivalentTo(originalRoot, options => options.RespectingRuntimeTypes());
         originalRoot.Should().BeEquivalentTo(deserializedRenamedRoot, options => options.RespectingRuntimeTypes());
      }
   }
}
