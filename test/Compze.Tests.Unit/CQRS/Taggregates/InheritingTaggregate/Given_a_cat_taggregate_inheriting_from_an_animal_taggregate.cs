using Compze.xUnitBDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.InheritingTaggregate;

//todo: is this supposed to actually test anything? The names are meaningless
public class Given_a_cat_taggregate_inheriting_from_an_animal_taggregate
{
   [XF] public void cat_birth() => CatTaggregate.RegisterBirth();

   [XF] public void dog_birth() => DogTaggregate.RegisterBirth();
}
