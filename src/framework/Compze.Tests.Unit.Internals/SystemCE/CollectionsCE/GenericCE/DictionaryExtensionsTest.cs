using System;
using System.Collections.Generic;
using Compze.SystemCE.CollectionsCE.GenericCE;
using Compze.Testing;
using NUnit.Framework;

namespace Compze.Tests.Unit.Internals.SystemCE.CollectionsCE.GenericCE;

public static class DictionaryExtensionsGetOrAddWhenKey
{
   const int ExistingValue = 1;
   const int InsertedValue = 2;
   static readonly Func<int> Constructor = () => InsertedValue;
   const string Key = "key";
   const int ResultOfDefaultConstructor = new();

   [TestFixture]
   public class DictionaryExtensions_GetOrAdd_When_Key_Is_Not_Present : UniversalTestBase
   {
      [Test]
      public void ShouldReturnResulOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         Assert.That(empty.GetOrAdd(Key, Constructor), Is.EqualTo(InsertedValue));
      }

      [Test]
      public void ShouldAddResultOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAdd(Key, Constructor);

         Assert.That(empty[Key], Is.EqualTo(InsertedValue));
      }
   }

   [TestFixture]
   public class DictionaryExtensions_GetOrAdd_When_Key_Is_Present : UniversalTestBase
   {
      [Test]
      public void ShouldReturnExistingValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };

         Assert.That(empty.GetOrAdd(Key, Constructor), Is.EqualTo(ExistingValue));
      }

      [Test]
      public void ShouldLeaveValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };
         empty.GetOrAdd(Key, Constructor);

         Assert.That(empty[Key], Is.EqualTo(ExistingValue));
      }
   }


   [TestFixture]
   public class DictionaryExtensions_GetOrAddDefault_When_Key_Is_Not_Present: UniversalTestBase
   {
      [Test]
      public void ShouldReturnResulOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         Assert.That(empty.GetOrAddDefault(Key), Is.EqualTo(ResultOfDefaultConstructor));
      }

      [Test]
      public void ShouldAddResultOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAddDefault(Key);

         Assert.That(empty[Key], Is.EqualTo(ResultOfDefaultConstructor));
      }
   }

   [TestFixture]
   public class DictionaryExtensions_GetOrAddDefault_When_Key_Is_Present: UniversalTestBase
   {
      [Test]
      public void ShouldReturnExistingValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };

         Assert.That(empty.GetOrAddDefault(Key), Is.EqualTo(ExistingValue));
      }

      [Test]
      public void ShouldLeaveValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };
         empty.GetOrAddDefault(Key);

         Assert.That(empty[Key], Is.EqualTo(ExistingValue));
      }
   }
}