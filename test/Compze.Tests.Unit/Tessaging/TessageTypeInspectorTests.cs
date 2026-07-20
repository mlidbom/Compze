using Compze.Tessaging;
using Compze.Tessaging.Validation;
using Compze.Tessaging.Validation.Exceptions;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Tessaging;

interface INonGenericPublisherTevent : IPublisherTevent<ITevent>;
interface INonCovariantParameterPublisherTevent : IPublisherTevent<ITevent>;

public class TessageTypeInspector_throws_TessageTypeDesignViolationException_if_ : UniversalTestBase
{
   static void AssertInvalidForSending<TTessage>() => Invoking(TessageInspector.AssertValid<TTessage>).Must().Throw<TessageTypeDesignViolationException>();
   static void AssertInvalidForSubscription<TTessage>() => Invoking(TessageInspector.AssertValidForSubscription<TTessage>).Must().Throw<TessageTypeDesignViolationException>();

   public class Inspecting_type_for_subscription_ : TessageTypeInspector_throws_TessageTypeDesignViolationException_if_
   {
      public class Type_implements_Wrapper_tevent_interface_but_ : Inspecting_type_for_subscription_
      {
         [XF] public void Is_not_generic() => AssertInvalidForSubscription<INonGenericPublisherTevent>();

         [XF] public void Does_not_have_a_covariant_type_parameter() => AssertInvalidForSubscription<INonCovariantParameterPublisherTevent>();
      }
   }

   public class Inspecting_type_for_sending_and_ : TessageTypeInspector_throws_TessageTypeDesignViolationException_if_
   {
      public class Type_implements_Wrapper_tevent_interface_but_ : Inspecting_type_for_sending_and_
      {
         [XF] public void Is_not_generic() => AssertInvalidForSubscription<INonGenericPublisherTevent>();

         [XF] public void Does_not_have_a_covariant_type_parameter() => AssertInvalidForSubscription<INonCovariantParameterPublisherTevent>();
      }

      interface INotTessage;
      [XF] public void Is_not_ITessage() => AssertInvalidForSending<INotTessage>();

      interface ITommandAndTevent : ITevent, ITommand;
      [XF] public void Is_Both_tommand_and_tevent() => AssertInvalidForSending<ITommandAndTevent>();

      interface ITommandAndTuery : ITevent, ITuery<object>;
      [XF] public void Is_Both_tommand_and_tuery() => AssertInvalidForSending<ITommandAndTuery>();

      interface IStrictlyLocalAndRemotable : IRemotableTessage, IStrictlyLocalTessage;
      [XF] public void Is_Both_strictly_local_and_remotable() => AssertInvalidForSending<IStrictlyLocalAndRemotable>();

      interface IForbidAndRequireTransactionalSender : IMustBeSentTransactionally, ICannotBeSentRemotelyFromWithinTransaction;
      [XF] public void Forbids_and_requires_transactional_sender() => AssertInvalidForSending<IForbidAndRequireTransactionalSender>();
   }
}
