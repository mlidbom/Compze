using Xunit;

namespace Compze.Typermedia.Specifications;

public class Placeholder_specification
{
   [Fact] public void Typermedia_project_compiles() => Assert.True(typeof(IRemoteTypermediaNavigator).Assembly != null);
}
