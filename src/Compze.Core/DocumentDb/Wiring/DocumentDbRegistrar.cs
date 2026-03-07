using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.DocumentDb.Wiring;

namespace Compze.Core.DocumentDb.Wiring;

public static class EndpointBuilderDocumentDbExtensions
{
   public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IEndpointBuilder @this)
      => @this.Container.Register().DocumentDb();
}
