using Xunit;

namespace Compze.Typermedia.Hosting.Specifications;

public class Placeholder_specification
{
   [Fact] public void Typermedia_Hosting_project_compiles() => Assert.True(typeof(TypermediaHostingPlaceholder).Assembly != null);
}
