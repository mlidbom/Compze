using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Typermedia;

namespace Compze.DocumentDb.Wiring;

public static class EndpointBuilderDocumentDbExtensions
{
   public static EndpointDocumentDbRegistrationBuilder RegisterDocumentDb(this ExactlyOnceEndpointBuilder @this)
   {
      @this.Registrar.DocumentDb();
      return new EndpointDocumentDbRegistrationBuilder(register => @this.RegisterTypermediaHandlers(register));
   }
}

public class EndpointDocumentDbRegistrationBuilder
{
   readonly Action<Action<TypermediaHandlerRegistrar>> _registerTypermediaHandlers;
   internal EndpointDocumentDbRegistrationBuilder(Action<Action<TypermediaHandlerRegistrar>> registerTypermediaHandlers) => _registerTypermediaHandlers = registerTypermediaHandlers;

   ///<summary>Declares the document db's typermedia handlers for <typeparamref name="TDocument"/> — save, delete, get-for-update,<br/>
   /// and the read tueries — into the endpoint's one engine: a store integration is a handler contributor like any other,<br/>
   /// declaring through the same surface as the application's own handlers.</summary>
   public EndpointDocumentDbRegistrationBuilder HandleDocumentType<TDocument>() where TDocument : class
   {
      _registerTypermediaHandlers(DocumentDbApi.HandleDocumentType<TDocument>);
      return this;
   }
}
