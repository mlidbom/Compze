using Compze.Abstractions;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.Teventive.Taggregates.Tevents;
using static System.Console;

#pragma warning disable // Documentation example code: a small example domain that is compiled, never run, and shown in regions.
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

// ReSharper disable once CheckNamespace
namespace Website.Teventive.Taggregates.SharedTomponentsAndTentities;

#region IPriceTevent
interface IPriceTevent : ITevent
{
   interface IChanged : IPriceTevent
   {
      decimal Amount { get; }
   }
}
#endregion

#region PriceTevent
abstract class PriceTevent : IPriceTevent
{
   internal class Changed(decimal amount) : PriceTevent, IPriceTevent.IChanged
   {
      public decimal Amount { get; } = amount;
   }
}
#endregion

#region Price
class Price : SharedTomponent<IPriceTevent>
{
   static readonly TeventRouting<Price, IPriceTevent> Appliers = DeclareAppliers<Price>(it => it
     .For<IPriceTevent.IChanged>(static (price, changed) => price.Amount = changed.Amount));

   protected override TeventRouting TeventAppliers => Appliers;

   public Price(SharedTomponentSlotInAnOwner<IPriceTevent> slot) : base(slot) {}

   public decimal Amount { get; private set; }

   public void ChangeTo(decimal amount) => Publish(new PriceTevent.Changed(amount));
}
#endregion

#region IProductTevent
interface IProductTevent : ITaggregateTevent
{
   interface ICreated : IProductTevent, ITaggregateCreatedTevent
   {
      string Name { get; }
   }

   interface IRepriced : IProductTevent;
}

interface IProductTevent<out T> : ITaggregateTevent<T> where T : IProductTevent;
#endregion

#region ProductTevent
abstract class ProductTevent : TaggregateTevent, IProductTevent
{
   protected ProductTevent() {}
   ProductTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId taggregateId, string name) : ProductTevent(taggregateId), IProductTevent.ICreated
   {
      public string Name { get; } = name;
   }
}

class ProductTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IProductTevent<T> where T : IProductTevent;
#endregion

#region IProductPriceTevents
interface IProductListPriceTevent<out T> : IProductTevent.IRepriced, IPublisherTevent<T> where T : IPriceTevent;
interface IProductSalePriceTevent<out T> : IProductTevent.IRepriced, IPublisherTevent<T> where T : IPriceTevent;
#endregion

#region ProductPriceTevents
class ProductListPriceTevent<T>(T tevent) : ProductTevent, IProductListPriceTevent<T> where T : IPriceTevent
{
   public T Tevent { get; } = tevent;
}

class ProductSalePriceTevent<T>(T tevent) : ProductTevent, IProductSalePriceTevent<T> where T : IPriceTevent
{
   public T Tevent { get; } = tevent;
}
#endregion

class Product : Taggregate<Product, IProductTevent, ProductTevent, IProductTevent<IProductTevent>, ProductTevent<ProductTevent>>
{
   #region ProductSlots
   static class Slots
   {
      internal static readonly SharedTomponentSlot<Product, IProductTevent, ProductTevent, IPriceTevent, IProductListPriceTevent<IPriceTevent>> ListPrice =
         new(typeof(ProductListPriceTevent<>), static product => product.ListPrice);

      internal static readonly SharedTomponentSlot<Product, IProductTevent, ProductTevent, IPriceTevent, IProductSalePriceTevent<IPriceTevent>> SalePrice =
         new(typeof(ProductSalePriceTevent<>), static product => product.SalePrice);
   }
   #endregion

   #region ProductRouting
   static readonly TeventRouting<Product, IProductTevent> Appliers = DeclareAppliers(it => it
     .For<IProductTevent.ICreated>(static (product, created) => product.Name = created.Name)
     .SharedTomponent(Slots.ListPrice)
     .SharedTomponent(Slots.SalePrice));

   protected override TeventRouting TeventAppliers => Appliers;
   #endregion

   #region ProductConstructor
   Product()
   {
      ListPrice = new Price(Slots.ListPrice.In(this));
      SalePrice = new Price(Slots.SalePrice.In(this));
   }

   public string Name { get; private set; } = "";
   public Price ListPrice { get; }
   public Price SalePrice { get; }
   #endregion

   public static Product Create(string name)
   {
      var product = new Product();
      product.Publish(new ProductTevent.Created(TaggregateId.New(), name));
      return product;
   }
}

class Examples
{
   public void ChangeTheListPrice()
   {
      #region ChangeTheListPrice
      var product = Product.Create("Teapot");
      product.ListPrice.ChangeTo(25m);
      #endregion
   }

   public void Subscribe()
   {
      TessageBusHandlerRegistrar registrar = null!;

      #region SubscribeToTheListPrice
      registrar.ForTevent<IProductTevent<IProductListPriceTevent<IPriceTevent.IChanged>>>(
         changed => WriteLine($"Product {changed.Tevent.TaggregateId}: the list price is now {changed.Tevent.Tevent.Amount}"));
      #endregion

      #region SubscribeToRepricing
      registrar.ForTevent<IProductTevent.IRepriced>(repriced => WriteLine($"Product {repriced.TaggregateId} was repriced"));
      #endregion

      #region SubscribeToEveryPrice
      registrar.ForTevent<IPublisherTevent<IPublisherTevent<IPriceTevent.IChanged>>>(
         changed => WriteLine($"A price is now {changed.Tevent.Tevent.Amount}"));
      #endregion
   }
}

readonly struct ReviewId : IEquatable<ReviewId>
{
   readonly Guid _value;

   ReviewId(Guid value) => _value = value;

   public static ReviewId New() => new(Guid.NewGuid());

   public bool Equals(ReviewId other) => _value == other._value;
   public override bool Equals(object? obj) => obj is ReviewId other && Equals(other);
   public override int GetHashCode() => _value.GetHashCode();
}

#region IReviewTevent
interface IReviewTevent : ISharedTentityTevent<ReviewId>
{
   interface IWritten : IReviewTevent
   {
      int Stars { get; }
   }

   interface IAnswered : IReviewTevent
   {
      string Answer { get; }
   }
}
#endregion

#region ReviewTevent
abstract class ReviewTevent(ReviewId entityId) : IReviewTevent
{
   public ReviewId EntityId { get; } = entityId;

   internal class Written(ReviewId entityId, int stars) : ReviewTevent(entityId), IReviewTevent.IWritten
   {
      public int Stars { get; } = stars;
   }

   internal class Answered(ReviewId entityId, string answer) : ReviewTevent(entityId), IReviewTevent.IAnswered
   {
      public string Answer { get; } = answer;
   }
}
#endregion

#region Review
class Review : SharedTentity<Review, ReviewId, IReviewTevent, IReviewTevent.IWritten>
{
   static readonly TeventRouting<Review, IReviewTevent> Appliers = DeclareAppliers(it => it
     .For<IReviewTevent.IWritten>(static (review, written) => review.Stars = written.Stars)
     .For<IReviewTevent.IAnswered>(static (review, answered) => review.Answer = answered.Answer));

   protected override TeventRouting TeventAppliers => Appliers;

   public Review(SharedTentityCollection<Review, ReviewId, IReviewTevent, IReviewTevent.IWritten> reviews) : base(reviews) {}

   public int Stars { get; private set; }
   public string? Answer { get; private set; }

   public void AnswerWith(string answer) => Publish(new ReviewTevent.Answered(Id, answer));
}
#endregion

#region Bundle
interface IBundleTevent : ITaggregateTevent
{
   interface ICreated : IBundleTevent, ITaggregateCreatedTevent
   {
      string Name { get; }
   }
}

interface IBundleTevent<out T> : ITaggregateTevent<T> where T : IBundleTevent;

interface IBundlePriceTevent<out T> : IBundleTevent, IPublisherTevent<T> where T : IPriceTevent;
interface IBundleReviewsTevent<out T> : IBundleTevent, IPublisherTevent<T> where T : IReviewTevent;

abstract class BundleTevent : TaggregateTevent, IBundleTevent
{
   protected BundleTevent() {}
   BundleTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId taggregateId, string name) : BundleTevent(taggregateId), IBundleTevent.ICreated
   {
      public string Name { get; } = name;
   }
}

class BundleTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IBundleTevent<T> where T : IBundleTevent;

class BundlePriceTevent<T>(T tevent) : BundleTevent, IBundlePriceTevent<T> where T : IPriceTevent
{
   public T Tevent { get; } = tevent;
}

class BundleReviewsTevent<T>(T tevent) : BundleTevent, IBundleReviewsTevent<T> where T : IReviewTevent
{
   public T Tevent { get; } = tevent;
}

class Bundle : Taggregate<Bundle, IBundleTevent, BundleTevent, IBundleTevent<IBundleTevent>, BundleTevent<BundleTevent>>
{
   static class Slots
   {
      internal static readonly SharedTomponentSlot<Bundle, IBundleTevent, BundleTevent, IPriceTevent, IBundlePriceTevent<IPriceTevent>> Price =
         new(typeof(BundlePriceTevent<>), static bundle => bundle.Price);

      internal static readonly SharedTomponentSlot<Bundle, IBundleTevent, BundleTevent, IReviewTevent, IBundleReviewsTevent<IReviewTevent>> Reviews =
         new(typeof(BundleReviewsTevent<>), static bundle => bundle.Reviews);
   }

   static readonly TeventRouting<Bundle, IBundleTevent> Appliers = DeclareAppliers(it => it
     .For<IBundleTevent.ICreated>(static (bundle, created) => bundle.Name = created.Name)
     .SharedTomponent(Slots.Price)
     .SharedTomponent(Slots.Reviews));

   protected override TeventRouting TeventAppliers => Appliers;

   Bundle()
   {
      Price = new Price(Slots.Price.In(this));
      Reviews = new SharedTentityCollection<Review, ReviewId, IReviewTevent, IReviewTevent.IWritten>(Slots.Reviews.In(this));
   }

   public string Name { get; private set; } = "";
   public Price Price { get; }
   public SharedTentityCollection<Review, ReviewId, IReviewTevent, IReviewTevent.IWritten> Reviews { get; }

   public static Bundle Create(string name)
   {
      var bundle = new Bundle();
      bundle.Publish(new BundleTevent.Created(TaggregateId.New(), name));
      return bundle;
   }

   public Review AddReview(int stars) => Reviews.AddByPublishing(new ReviewTevent.Written(ReviewId.New(), stars));
}
#endregion
