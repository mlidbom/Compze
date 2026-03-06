using Xunit;

namespace Compze.Typermedia.Client.Specifications;

public class Placeholder_specification
{
   [Fact] public void Typermedia_Client_project_compiles() => Assert.True(typeof(TypermediaClientPlaceholder).Assembly != null);
}
