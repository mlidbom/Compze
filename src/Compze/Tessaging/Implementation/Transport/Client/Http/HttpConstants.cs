namespace Compze.Tessaging.Implementation.Transport.Client.Http;

static class HttpConstants
{
   internal static class Routes
   {
      internal static class Rpc
      {
         internal const string TommandNoResult = "/internal/rpc/tommand-no-result";
         internal const string Tuery = "/internal/rpc/tuery";
         internal const string TommandWithResult = "/internal/rpc/tommand-with-result";
      }

      internal static class Tessaging
      {
         internal const string Event = "/internal/tessaging/event";
         internal const string Tommand = "/internal/tessaging/tommand";
      }
   }

   internal static class Headers
   {
      internal const string TessageId = nameof(TessageId);
      internal const string PayLoadTypeId = nameof(PayLoadTypeId);
   }
}
