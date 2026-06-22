using Compze.Abstractions.Hosting.Public;

namespace Compze.DocumentDb.Wiring;

public static class EndpointBuilderDocumentDbExtensions
{
   public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IEndpointBuilder @this)
      => @this.Registrar.DocumentDb();
}
