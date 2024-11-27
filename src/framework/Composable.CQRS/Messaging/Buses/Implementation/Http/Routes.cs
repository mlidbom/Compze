namespace Composable.Messaging.Buses.Implementation.Http;

static class Routes
{
   internal static class Rpc
   {
      internal const string CommandNoResult = "/internal/rpc/command-no-result";
      internal const string Query = "/internal/rpc/query";
      internal const string CommandWithResult = "/internal/rpc/command-with-result";
   }

   internal static class Messaging
   {
      internal const string Event = "/internal/messaging/event";
      internal const string Command = "/internal/messaging/command";
   }
}