using System;
using System.Collections.Generic;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.SystemCE.CollectionsCE.GenericCE;

public static class DictionaryExtensionsGetOrAddWhenKey
{
   const int ExistingValue = 1;
   const int InsertedValue = 2;
   static readonly Func<int> Constructor = () => InsertedValue;
   const string Key = "key";
   const int ResultOfDefaultConstructor = new();

   [TestFixture]
   public class DictionaryExtensions_GetOrAdd_When_Key_Is_Not_Present : NUnitTestBase
   {
      [Test]
      public void ShouldReturnResulOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAdd(Key, Constructor).Should().Be(InsertedValue);
      }

      [Test]
      public void ShouldAddResultOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAdd(Key, Constructor);

         empty[Key].Should().Be(InsertedValue);
      }
   }

   [TestFixture]
   public class DictionaryExtensions_GetOrAdd_When_Key_Is_Present : NUnitTestBase
   {
      [Test]
      public void ShouldReturnExistingValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };

         empty.GetOrAdd(Key, Constructor).Should().Be(ExistingValue);
      }

      [Test]
      public void ShouldLeaveValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };
         empty.GetOrAdd(Key, Constructor);

         empty[Key].Should().Be(ExistingValue);
      }
   }


   [TestFixture]
   public class DictionaryExtensions_GetOrAddDefault_When_Key_Is_Not_Present: NUnitTestBase
   {
      [Test]
      public void ShouldReturnResulOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAddDefault(Key).Should().Be(ResultOfDefaultConstructor);
      }

      [Test]
      public void ShouldAddResultOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAddDefault(Key);

         empty[Key].Should().Be(ResultOfDefaultConstructor);
      }
   }

   [TestFixture]
   public class DictionaryExtensions_GetOrAddDefault_When_Key_Is_Present: NUnitTestBase
   {
      [Test]
      public void ShouldReturnExistingValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };

         empty.GetOrAddDefault(Key).Should().Be(ExistingValue);
      }

      [Test]
      public void ShouldLeaveValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };
         empty.GetOrAddDefault(Key);

         empty[Key].Should().Be(ExistingValue);
      }
   }
}