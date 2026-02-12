namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;

public static class HttpConstants
{
   public static class Routes
   {
      public static class Typermedia
      {
         public const string TommandNoResult = "internal/rpc/tommand-no-result";
         public const string Tuery = "internal/rpc/tuery";
         public const string TommandWithResult = "internal/rpc/tommand-with-result";
      }

      public static class Tessaging
      {
         public const string Tevent = "internal/tessaging/tevent";
         public const string Tommand = "internal/tessaging/tommand";
      }
   }

   public static class Headers
   {
      public const string TessageId = nameof(TessageId);
      public const string PayLoadTypeId = nameof(PayLoadTypeId);
   }
}
