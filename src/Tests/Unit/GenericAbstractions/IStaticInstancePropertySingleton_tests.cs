using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using FluentAssertions;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.GenericAbstractions;

public class IStaticInstancePropertySingleton_tests : XUnitTestBase
{
   [UsedImplicitly] class ImplicitImplementation : IStaticInstancePropertySingleton<ImplicitImplementation>
   {
      public static ImplicitImplementation Instance { get; } = new ImplicitImplementation();
      ImplicitImplementation() { }
   }

   [UsedImplicitly] class ExplicitImplementation : IStaticInstancePropertySingleton<ExplicitImplementation>
   {
      static ExplicitImplementation IStaticInstancePropertySingleton<ExplicitImplementation>.Instance { get; } = new ExplicitImplementation();
      ExplicitImplementation() { }
   }

   [XF]
   public void Constructor_should_work_with_implicit_interface_implementation()
   {
      var instance1 = Constructor.For<ImplicitImplementation>.DefaultConstructor.Instance();
      var instance2 = Constructor.For<ImplicitImplementation>.DefaultConstructor.Instance();

      instance1.Should().NotBeNull();
      instance1.Should().BeSameAs(instance2);
      instance1.Should().BeSameAs(ImplicitImplementation.Instance);
   }

   [XF]
   public void Constructor_should_work_with_explicit_interface_implementation()
   {
      var instance1 = Constructor.For<ExplicitImplementation>.DefaultConstructor.Instance();
      var instance2 = Constructor.For<ExplicitImplementation>.DefaultConstructor.Instance();

      instance1.Should().NotBeNull();
      instance1.Should().BeSameAs(instance2);
   }
}
