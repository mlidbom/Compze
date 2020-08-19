﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Composable.DDD;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
using Composable.SystemCE;
using Composable.SystemCE.ReflectionCE.EmitCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Newtonsoft.Json;

// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Composable.Messaging
{
    public static partial class MessageTypes
    {
        public static class StrictlyLocal
        {
            public static class Queries
            {
#pragma warning disable CA1724 //Class name conflicts with namespace name.
                public abstract class StrictlyLocalQuery<TQuery, TResult> : IStrictlyLocalQuery<TQuery, TResult> where TQuery : StrictlyLocalQuery<TQuery, TResult> {}
#pragma warning restore CA1724 //

                public sealed class EntityLink<TResult> : StrictlyLocal.Queries.StrictlyLocalQuery<EntityLink<TResult>, TResult> where TResult : IHasPersistentIdentity<Guid>
                {
                    [JsonConstructor] public EntityLink(Guid entityId) => EntityId = entityId;
                    public Guid EntityId { get; private set; }
                }
            }

            public static class Commands
            {
                public abstract class StrictlyLocalCommand : IStrictlyLocalCommand {}

                public abstract class StrictlyLocalCommand<TResult> : IStrictlyLocalCommand<TResult> {}
            }
        }

        public static class Remotable
        {
            public static class AtMostOnce
            {
                //Todo: How can we prevent UI's from just defaulting to using a constructor that creates a new guid?
                public class AtMostOnceHypermediaCommand : IAtMostOnceHypermediaCommand
                {
                    ///<summary>How and when a <see cref="AtMostOnceHypermediaCommand.MessageId"/> is generated is vital to correctly maintain the At most once delivery guarantee. When creating the command in the backend service we must generate a new <see cref="AtMostOnceHypermediaCommand.MessageId"/>. When binding the command in a UI we must reuse the <see cref="AtMostOnceHypermediaCommand.MessageId"/> generated by the backend. This enum helps make that important distinction explicit in your code.</summary>
                    protected enum DeduplicationIdHandling
                    {
                        ///<summary>When creating the command within the owning handler endpoint a new <see cref="AtMostOnceHypermediaCommand.MessageId"/> must be generated. </summary>
                        Create,
                        ///<summary>When binding the command in a UI or when deserializing it is very important to NOT create a new <see cref="AtMostOnceHypermediaCommand.MessageId"/> as this would break the At most once delivery guarantee.</summary>
                        Reuse
                    }

                    ///<summary>How and when a <see cref="MessageId"/> is generated is vital to correctly maintain the At most once delivery guarantee. When creating the command in the backend service we must generate a new <see cref="MessageId"/>. When binding the command in a UI we must reuse the <see cref="MessageId"/> generated by the backend.</summary>
                    protected AtMostOnceHypermediaCommand(DeduplicationIdHandling scenario) => _deduplicationId = scenario == DeduplicationIdHandling.Create ? Guid.NewGuid() : Guid.Empty;

                    Guid _deduplicationId;

                    public Guid MessageId
                    {
                        get => _deduplicationId;
                        set
                        {
                            if(_deduplicationId != Guid.Empty)
                            {
                                throw new InvalidOperationException($"Once {nameof(MessageId)} has been set it cannot be changed. It is only settable at all because many UI technologies require it to be.");
                            }

                            _deduplicationId = value;
                        }
                    }

                    ///<summary>Allows for replacing the <see cref="MessageId"/> in the rare cases when that is actually helpful. One example is when a command failed to execute due to backend business logic rules and you wish to reuse the entered values in the UI that the command is bound to. If you do not change the <see cref="MessageId"/> when doing that the bus will keep returning the response from the first time you sent the command.</summary>
                    public void ReplaceDeduplicationId() => _deduplicationId = Guid.NewGuid();
                }

                public class AtMostOnceCommand<TResult> : AtMostOnceHypermediaCommand, IAtMostOnceCommand<TResult>
                {
                    ///<summary>It is important not to set a default value if we are binding values in a UI. That would make it very easy to accidentally break the At most once guarantee. That is why you must pass the enum value here so that we can know what is happening.</summary>
                    protected AtMostOnceCommand(DeduplicationIdHandling scenario) : base(scenario) {}
                }
            }

            public static class NonTransactional
            {
                public static class Queries
                {
#pragma warning disable CA1724 //Class name conflicts with namespace name.
                    public abstract class Query<TResult> : IRemotableQuery<TResult> {}
#pragma warning restore CA1724 //Class name conflicts with namespace name.

                    public class EntityLink<TResult> : Remotable.NonTransactional.Queries.Query<TResult> where TResult : IHasPersistentIdentity<Guid>
                    {
                        public EntityLink() {}
                        public EntityLink(Guid entityId) => EntityId = entityId;
                        public EntityLink<TResult> WithId(Guid id) => new EntityLink<TResult>(id);
                        public Guid EntityId { get; private set; }
                    }

                    ///<summary>Implement <see cref="IRemotableCreateMyOwnResultQuery{TResult}"/> by passing a func to this base class.</summary>
                    public abstract class FuncResultQuery<TResult> : Query<TResult>, IRemotableCreateMyOwnResultQuery<TResult>
                    {
                        readonly Func<TResult> _factory;
                        protected FuncResultQuery(Func<TResult> factory) => _factory = factory;
                        public TResult CreateResult() => _factory();
                    }

                    /// <summary>Implements <see cref="IRemotableCreateMyOwnResultQuery{TResult}"/> by calling the default constructor on <typeparamref name="TResult"/></summary>
                    public class NewableResultLink<TResult> : Query<TResult>, IRemotableCreateMyOwnResultQuery<TResult>
                    {
                        static readonly Func<TResult> Constructor = SystemCE.ReflectionCE.Constructor.For<TResult>.DefaultConstructor.Instance;
                        public TResult CreateResult() => Constructor();
                    }
                }
            }

            public static class ExactlyOnce
            {
                public class Command : ValueObject<Command>, IExactlyOnceCommand
                {
                    public Guid MessageId { get; private set; }

                    protected Command()
                        : this(Guid.NewGuid()) {}

                    Command(Guid id) => MessageId = id;
                }
            }
        }

        internal static void MapTypes(ITypeMappingRegistar typeMapper)
        {
            typeMapper
               .MapTypeAndStandardCollectionTypes<IRemotableEvent>("1E0DB1B4-71A6-4D2E-901F-E238ABA30B63")
               .MapTypeAndStandardCollectionTypes<MessageTypes.Internal.EndpointInformationQuery>("D94259E4-7479-442C-99AE-D49C12CF8713")
               .MapTypeAndStandardCollectionTypes<MessageTypes.Internal.EndpointInformation>("2B598C6D-4893-4CB9-B4CE-7B705AD92DF9");
        }
    }
}
