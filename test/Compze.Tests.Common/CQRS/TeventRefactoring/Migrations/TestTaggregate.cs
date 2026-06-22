using Compze.Abstractions.Public;
using Compze.Tessaging.Teventive.Public;
using Compze.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ReflectionCE;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning  disable CA1812 // Avoid uninstantiated internal classes # used via reflection

namespace Compze.Tests.Common.CQRS.TeventRefactoring.Migrations
{
   public interface ITestTaggregateTevent<out T> : ITaggregateIdentifyingTevent<T> where T : ITestTaggregateTevent;
   public interface ITestTaggregateTevent : ITaggregateTevent;

   public class TestTaggregateTevent<T>(T tevent) : TaggregateIdentifyingTevent<T>(tevent), ITestTaggregateTevent<T> where T : ITestTaggregateTevent;

   public abstract class TestTaggregateTevent : TaggregateTevent, ITestTaggregateTevent;

   namespace Tevents
   {
      public abstract class EcAbstract : TestTaggregateTevent, ITaggregateCreatedTevent;

      // ReSharper disable ClassNeverInstantiated.Global
      public class Ec1 : EcAbstract;
      class Ec2 : EcAbstract;
      class Ec3 : EcAbstract;
      public class E1 : TestTaggregateTevent;
      public class E2 : TestTaggregateTevent;
      public class E3 : TestTaggregateTevent;
      public class E4 : TestTaggregateTevent;
      public class E5 : TestTaggregateTevent;
      public class E6 : TestTaggregateTevent;
      public class E7 : TestTaggregateTevent;
      public class E8 : TestTaggregateTevent;
      public class E9 : TestTaggregateTevent;

      public class Ef : TestTaggregateTevent;
      // ReSharper restore ClassNeverInstantiated.Global
   }

   public class TestTaggregate : Taggregate<TestTaggregate, ITestTaggregateTevent, TestTaggregateTevent, ITestTaggregateTevent<ITestTaggregateTevent>, TestTaggregateTevent<TestTaggregateTevent>>
   {
      public void Publish(params TestTaggregateTevent[] tevents) => tevents.ForEach(base.Publish);

      TestTaggregate() => SetupAppliers();

      void SetupAppliers()
      {
         RegisterTeventAppliers()
           .For<ITestTaggregateTevent>(e => _history.Add(e));
      }

      TestTaggregate(params TestTaggregateTevent[] tevents) : this()
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

      readonly List<ITestTaggregateTevent> _history = [];
      public IReadOnlyList<ITaggregateTevent> History => _history;
   }

   static class TeventSequenceGenerator
   {
      public static TestTaggregateTevent[] ToTevents(this IEnumerable<Type> types) => types.Select(Constructor.CreateInstance).Cast<TestTaggregateTevent>().ToArray();
   }
}
