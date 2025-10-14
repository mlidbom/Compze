using System;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Common;
using Compze.Tests.Infrastructure;
using JetBrains.Annotations;
using Xunit;
using Compze.Tests.Infrastructure.XUnit;
using FluentAssertions;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Unit.Internals.XUnit.Tessaging;

interface INonGenericWrapperEvent : IWrapperEvent<IEvent>;
interface INonCovariantTypeParameterWrapperEvent : IWrapperEvent<IEvent>;

 public class MessageTypeInspector_throws_MessageTypeDesignViolationException_if_ : XUnitTestBase
{
   static void AssertInvalidForSending<TMessage>() => Invoking(MessageInspector.AssertValid<TMessage>).Should().Throw<MessageTypeInspector.MessageTypeDesignViolationException>();
   static void AssertInvalidForSubscription<TMessage>() => Invoking(MessageInspector.AssertValidForSubscription<TMessage>).Should().Throw<MessageTypeInspector.MessageTypeDesignViolationException>();

    public class Inspecting_type_for_subscription_ : XUnitTestBase
   {
      public class Type_implements_Wrapper_event_interface_but_ : XUnitTestBase
      {
         [Fact] public void Is_not_generic() => AssertInvalidForSubscription<INonGenericWrapperEvent>();

         [Fact] public void Does_not_have_a_covariant_type_parameter() => AssertInvalidForSubscription<INonCovariantTypeParameterWrapperEvent>();
      }
   }

    public class Inspecting_type_for_sending_and_ : XUnitTestBase
   {
      public class Type_implements_Wrapper_event_interface_but_ : XUnitTestBase
      {
         [Fact] public void Is_not_generic() => AssertInvalidForSubscription<INonGenericWrapperEvent>();

         [Fact] public void Does_not_have_a_covariant_type_parameter() => AssertInvalidForSubscription<INonCovariantTypeParameterWrapperEvent>();
      }

      interface INotMessage;
      [Fact] public void Is_not_IMessage() => AssertInvalidForSending<INotMessage>();

      interface ICommandAndEvent : IEvent, ICommand;
      [Fact] public void Is_Both_command_and_event() => AssertInvalidForSending<ICommandAndEvent>();

      interface ICommandAndQuery : IEvent, IQuery<object>;
      [Fact] public void Is_Both_command_and_query() => AssertInvalidForSending<ICommandAndQuery>();

      interface IStrictlyLocalAndRemotable : IRemotableMessage, IStrictlyLocalMessage;
      [Fact] public void Is_Both_strictly_local_and_remotable() => AssertInvalidForSending<IStrictlyLocalAndRemotable>();

      interface IForbidAndRequireTransactionalSender : IMustBeSentTransactionally, ICannotBeSentRemotelyFromWithinTransaction;
      [Fact] public void Forbids_and_requires_transactional_sender() => AssertInvalidForSending<IForbidAndRequireTransactionalSender>();

      [UsedImplicitly] internal class AtMostOnceCommandSettingMessageIdInDefaultConstructor : IAtMostOnceHypermediaCommand
      {
         public Guid MessageId { get; } = Guid.NewGuid();
      }

      [Fact] public void Is_at_most_once_command_and_sets_MessageId_in_defaultConstructor() => AssertInvalidForSending<AtMostOnceCommandSettingMessageIdInDefaultConstructor>();
   }
}
