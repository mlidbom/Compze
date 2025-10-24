namespace Compze.Tessaging.Implementation.Transport.Client.Http;

static class HttpConstants
{
   internal static class Routes
   {
      internal static class Rpc
      {
         internal const string CommandNoResult = "/internal/rpc/command-no-result";
         internal const string Query = "/internal/rpc/query";
         internal const string CommandWithResult = "/internal/rpc/command-with-result";
      }

      internal static class Tessaging
      {
         internal const string Event = "/internal/tessaging/event";
         internal const string Command = "/internal/tessaging/command";
      }
   }

   internal static class Headers
   {
      internal const string MessageId = nameof(MessageId);
      internal const string PayLoadTypeId = nameof(PayLoadTypeId);
   }
}
