namespace Compze.Tessaging.Internals.Transport;

#pragma warning disable CA1724 // Nested type names intentionally match namespace concepts
static class HttpConstants
{
   public static class Routes
   {
      public static class Typermedia
      {
         public const string TommandNoResult = "internal/rpc/tommand-no-result";
         public const string Tuery = "internal/rpc/tuery";
         public const string TommandWithResult = "internal/rpc/tommand-with-result";
      }

      public static class EndpointDiscovery
      {
         public const string Query = "internal/endpoint-discovery/query";
      }

      public static class Tessaging
      {
         public const string Tevent = "internal/tessaging/tevent";
         public const string Tommand = "internal/tessaging/tommand";
         public const string BestEffortTevent = "internal/tessaging/best-effort-tevent";
      }
   }

   public static class Headers
   {
      public const string TessageId = nameof(TessageId);
      public const string PayLoadTypeId = nameof(PayLoadTypeId);
   }
}
