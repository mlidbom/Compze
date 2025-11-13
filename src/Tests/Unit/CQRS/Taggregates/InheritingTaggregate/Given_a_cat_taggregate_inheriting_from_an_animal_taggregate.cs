using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.InheritingTaggregate;

public class Given_a_cat_taggregate_inheriting_from_an_animal_taggregate
{
   [XF] public void cat_birth()
   {
      var cat = CatTaggregate.RegisterBirth();
   }

   [XF] public void dog_birth()
   {
      var dog = DogTaggregate.RegisterBirth();
   }
}
