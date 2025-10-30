using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Utilities.SystemCE.ReflectionCE;

// ReSharper disable ClassNeverInstantiated.Global

namespace Compze.Tests.Common.CQRS.TeventRefactoring.Migrations
{
   public interface IRootTevent : ITaggregateTevent;

   public abstract class RootTevent : TaggregateTevent, IRootTevent;

   namespace Tevents
   {
      public abstract class EcAbstract : RootTevent, ITaggregateCreatedTevent;

      // ReSharper disable ClassNeverInstantiated.Global
      public class Ec1 : EcAbstract;
      public class Ec2 : EcAbstract;
      public class Ec3 : EcAbstract;
      public class E1 : RootTevent;
      public class E2 : RootTevent;
      public class E3 : RootTevent;
      public class E4 : RootTevent;
      public class E5 : RootTevent;
      public class E6 : RootTevent;
      public class E7 : RootTevent;
      public class E8 : RootTevent;
      public class E9 : RootTevent;

      public class Ef : RootTevent;
      // ReSharper restore ClassNeverInstantiated.Global
   }

   public class TestTaggregate : Taggregate<TestTaggregate, IRootTevent, RootTevent>
   {
      public void Publish(params RootTevent[] tevents)
      {
#pragma warning disable CS0618 // Type or member is obsolete
         // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
         if(GetIdBypassContractValidation() is not null && GetIdBypassContractValidation().IsEmpty && tevents.First().TaggregateId.IsEmpty)
         {
            Id = new TaggregateId();
            tevents.Cast<IMutableTaggregateTevent>().First().SetTaggregateIdInternal(Id);
         }
#pragma warning restore CS0618 // Type or member is obsolete

         foreach(var tevent in tevents)
         {
            base.Publish(tevent);
         }
      }

      public TestTaggregate() => SetupAppliers();

      void SetupAppliers()
      {
         RegisterTeventAppliers()
           .For<IRootTevent>(e => _history.Add(e));
      }

      public TestTaggregate(params RootTevent[] tevents) : this()
      {
         if(tevents.First() is not ITaggregateCreatedTevent) throw new Exception($"First tevent must be {nameof(ITaggregateCreatedTevent)}");

         Publish(tevents);
      }

      public static TestTaggregate FromTevents(TaggregateId? id, IEnumerable<Type> tevents)
      {
         var rootTevents = tevents.ToTevents();
#pragma warning disable CS0618 // Type or member is obsolete
         rootTevents.Cast<IMutableTaggregateTevent>().First().SetTaggregateIdInternal(id ?? new TaggregateId());
#pragma warning restore CS0618 // Type or member is obsolete
         return new TestTaggregate(rootTevents);
      }

      readonly List<IRootTevent> _history = [];
      public IReadOnlyList<ITaggregateTevent> History => _history;
   }

   static class TeventSequenceGenerator
   {
      public static RootTevent[] ToTevents(this IEnumerable<Type> types) => types.Select(Constructor.CreateInstance).Cast<RootTevent>().ToArray();
   }
}
