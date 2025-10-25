using System;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Unit.Internals.Tessaging;

interface INonGenericWrapperTevent : IWrapperTevent<ITevent>;
interface INonCovariantTypeParameterWrapperTevent : IWrapperTevent<ITevent>;

public class TessageTypeInspector_throws_TessageTypeDesignViolationException_if_ : UniversalTestBase
{
   static void AssertInvalidForSending<TTessage>() => Invoking(TessageInspector.AssertValid<TTessage>).Should().Throw<TessageTypeInspector.TessageTypeDesignViolationException>();
   static void AssertInvalidForSubscription<TTessage>() => Invoking(TessageInspector.AssertValidForSubscription<TTessage>).Should().Throw<TessageTypeInspector.TessageTypeDesignViolationException>();

   public class Inspecting_type_for_subscription_ : UniversalTestBase
   {
      public class Type_implements_Wrapper_event_interface_but_ : UniversalTestBase
      {
         [XF] public void Is_not_generic() => AssertInvalidForSubscription<INonGenericWrapperTevent>();

         [XF] public void Does_not_have_a_covariant_type_parameter() => AssertInvalidForSubscription<INonCovariantTypeParameterWrapperTevent>();
      }
   }

   public class Inspecting_type_for_sending_and_ : UniversalTestBase
   {
      public class Type_implements_Wrapper_event_interface_but_ : UniversalTestBase
      {
         [XF] public void Is_not_generic() => AssertInvalidForSubscription<INonGenericWrapperTevent>();

         [XF] public void Does_not_have_a_covariant_type_parameter() => AssertInvalidForSubscription<INonCovariantTypeParameterWrapperTevent>();
      }

      interface INotTessage;
      [XF] public void Is_not_ITessage() => AssertInvalidForSending<INotTessage>();

      interface ITommandAndTevent : ITevent, ITommand;
      [XF] public void Is_Both_tommand_and_event() => AssertInvalidForSending<ITommandAndTevent>();

      interface ITommandAndTuery : ITevent, ITuery<object>;
      [XF] public void Is_Both_tommand_and_tuery() => AssertInvalidForSending<ITommandAndTuery>();

      interface IStrictlyLocalAndRemotable : IRemotableTessage, IStrictlyLocalTessage;
      [XF] public void Is_Both_strictly_local_and_remotable() => AssertInvalidForSending<IStrictlyLocalAndRemotable>();

      interface IForbidAndRequireTransactionalSender : IMustBeSentTransactionally, ICannotBeSentRemotelyFromWithinTransaction;
      [XF] public void Forbids_and_requires_transactional_sender() => AssertInvalidForSending<IForbidAndRequireTransactionalSender>();

#pragma warning disable CA1812 //uninstantiated class 
      internal class AtMostOnceTommandSettingTessageIdInDefaultConstructor : IAtMostOnceHypermediaTommand
      {
         public Guid TessageId { get; } = Guid.NewGuid();
      }
#pragma warning restore CA1812 //uninstantiated class 

      [XF] public void Is_at_most_once_tommand_and_sets_TessageId_in_defaultConstructor() => AssertInvalidForSending<AtMostOnceTommandSettingTessageIdInDefaultConstructor>();
   }
}
