namespace Compze.Utilities.SystemCE;

public static class EnumCE
{
   public static bool IsValid<TEnum>(this TEnum value) where TEnum : struct, Enum
      => Values<TEnum>().Contains(value);

   static IReadOnlySet<TEnum> Values<TEnum>() where TEnum : struct, Enum
      => TypeCache<TEnum>.ValidValues;

   static class TypeCache<T> where T : struct, Enum
   {
      public static readonly IReadOnlySet<T> ValidValues = Enum.GetValues<T>().ToHashSet();
   }
}
