using System;
using System.Collections.Generic;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.SystemCE.CollectionsCE.GenericCE;

public static class DictionaryExtensionsGetOrAddWhenKey
{
   const int ExistingValue = 1;
   const int InsertedValue = 2;
   static readonly Func<int> Constructor = () => InsertedValue;
   const string Key = "key";
   const int ResultOfDefaultConstructor = new();

   
   public class DictionaryExtensions_GetOrAdd_When_Key_Is_Not_Present : UniversalTestBase
   {
      [XF]
      public void ShouldReturnResulOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAdd(Key, Constructor).Should().Be(InsertedValue);
      }

      [XF]
      public void ShouldAddResultOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAdd(Key, Constructor);

         empty[Key].Should().Be(InsertedValue);
      }
   }

   
   public class DictionaryExtensions_GetOrAdd_When_Key_Is_Present : UniversalTestBase
   {
      [XF]
      public void ShouldReturnExistingValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };

         empty.GetOrAdd(Key, Constructor).Should().Be(ExistingValue);
      }

      [XF]
      public void ShouldLeaveValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };
         empty.GetOrAdd(Key, Constructor);

         empty[Key].Should().Be(ExistingValue);
      }
   }


   
   public class DictionaryExtensions_GetOrAddDefault_When_Key_Is_Not_Present: UniversalTestBase
   {
      [XF]
      public void ShouldReturnResulOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAddDefault(Key).Should().Be(ResultOfDefaultConstructor);
      }

      [XF]
      public void ShouldAddResultOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAddDefault(Key);

         empty[Key].Should().Be(ResultOfDefaultConstructor);
      }
   }

   
   public class DictionaryExtensions_GetOrAddDefault_When_Key_Is_Present: UniversalTestBase
   {
      [XF]
      public void ShouldReturnExistingValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };

         empty.GetOrAddDefault(Key).Should().Be(ExistingValue);
      }

      [XF]
      public void ShouldLeaveValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };
         empty.GetOrAddDefault(Key);

         empty[Key].Should().Be(ExistingValue);
      }
   }
}