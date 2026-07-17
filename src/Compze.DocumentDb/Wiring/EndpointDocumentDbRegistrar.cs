using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Hosting;

namespace Compze.DocumentDb.Wiring;

public static class EndpointBuilderDocumentDbExtensions
{
   public static EndpointDocumentDbRegistrationBuilder RegisterDocumentDb(this IEndpointBuilder @this)
   {
      @this.Registrar.DocumentDb();
      return new EndpointDocumentDbRegistrationBuilder(@this);
   }
}

public class EndpointDocumentDbRegistrationBuilder
{
   readonly IEndpointBuilder _endpointBuilder;
   internal EndpointDocumentDbRegistrationBuilder(IEndpointBuilder endpointBuilder) => _endpointBuilder = endpointBuilder;

   ///<summary>Declares the document db's typermedia handlers for <typeparamref name="TDocument"/> — save, delete, get-for-update,<br/>
   /// and the read tueries — into the endpoint's one engine: a store integration is a handler contributor like any other,<br/>
   /// declaring through the same surface as the application's own handlers.</summary>
   public EndpointDocumentDbRegistrationBuilder HandleDocumentType<TDocument>() where TDocument : class
   {
      _endpointBuilder.RegisterTessageHandlers(DocumentDbApi.HandleDocumentType<TDocument>);
      return this;
   }
}
