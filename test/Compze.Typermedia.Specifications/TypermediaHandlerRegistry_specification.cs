using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Must;
using Compze.TypeIdentifiers;
using Compze.Typermedia.HandlerRegistration;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1812 // Never-instantiated test message types exist only to be looked up

namespace Compze.Typermedia.Specifications;

///<summary>Every lookup on the <see cref="TypermediaHandlerRegistry"/> speaks one language for a missing handler:<br/>
/// <see cref="NoHandlerException"/> naming the tessage type — never a raw dictionary failure. The two Type-keyed lookups the<br/>
/// remote executor calls regressed to raw indexers once (KeyNotFoundException, retried server-side); these specifications pin<br/>
/// every lookup path.</summary>
public class TypermediaHandlerRegistry_specification
{
   readonly ITypermediaHandlerRegistry _registry = new TypermediaHandlerRegistry(new TypeMapper());

   [XF] public void requesting_the_tuery_handler_for_a_type_no_handler_is_registered_for_throws_NoHandlerException_naming_the_type() =>
      Invoking(() => _registry.GetTueryHandler(typeof(UnhandledTuery)))
         .Must().Throw<NoHandlerException>()
         .Which.Message.Must().Contain(typeof(UnhandledTuery).FullName!);

   [XF] public void requesting_the_tommand_handler_with_result_for_a_type_no_handler_is_registered_for_throws_NoHandlerException_naming_the_type() =>
      Invoking(() => _registry.GetTommandHandlerWithReturnValue(typeof(UnhandledTommand)))
         .Must().Throw<NoHandlerException>()
         .Which.Message.Must().Contain(typeof(UnhandledTommand).FullName!);

   [XF] public void requesting_the_void_tommand_handler_for_a_tommand_no_handler_is_registered_for_throws_NoHandlerException_naming_the_type() =>
      Invoking(() => _registry.GetVoidTommandHandler(UnhandledVoidTommand.Create()))
         .Must().Throw<NoHandlerException>()
         .Which.Message.Must().Contain(typeof(UnhandledVoidTommand).FullName!);

   class UnhandledAnswer;
   class UnhandledTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<UnhandledAnswer>;
   class UnhandledTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<UnhandledAnswer>;

   class UnhandledVoidTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand
   {
      UnhandledVoidTommand() {}
      public static UnhandledVoidTommand Create() => new() { Id = new TessageId() };
   }
}
