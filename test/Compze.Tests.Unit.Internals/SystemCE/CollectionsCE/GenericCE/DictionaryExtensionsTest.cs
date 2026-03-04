using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Must;
using Compze.xUnit.BDD;
// ReSharper disable PreferConcreteValueOverDefault

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
         empty.GetOrAdd(Key, Constructor).Must().Be(InsertedValue);
      }

      [XF]
      public void ShouldAddResultOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAdd(Key, Constructor);

         empty[Key].Must().Be(InsertedValue);
      }
   }


   public class DictionaryExtensions_GetOrAdd_When_Key_Is_Present : UniversalTestBase
   {
      [XF]
      public void ShouldReturnExistingValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };

         empty.GetOrAdd(Key, Constructor).Must().Be(ExistingValue);
      }

      [XF]
      public void ShouldLeaveValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };
         empty.GetOrAdd(Key, Constructor);

         empty[Key].Must().Be(ExistingValue);
      }
   }



   public class DictionaryExtensions_GetOrAddDefault_When_Key_Is_Not_Present: UniversalTestBase
   {
      [XF]
      public void ShouldReturnResulOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAddDefault(Key).Must().Be(ResultOfDefaultConstructor);
      }

      [XF]
      public void ShouldAddResultOfConstructor()
      {
         var empty = new Dictionary<string, int>();
         empty.GetOrAddDefault(Key);

         empty[Key].Must().Be(ResultOfDefaultConstructor);
      }
   }


   public class DictionaryExtensions_GetOrAddDefault_When_Key_Is_Present: UniversalTestBase
   {
      [XF]
      public void ShouldReturnExistingValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };

         empty.GetOrAddDefault(Key).Must().Be(ExistingValue);
      }

      [XF]
      public void ShouldLeaveValue()
      {
         var empty = new Dictionary<string, int> { { Key, ExistingValue } };
         empty.GetOrAddDefault(Key);

         empty[Key].Must().Be(ExistingValue);
      }
   }
}
