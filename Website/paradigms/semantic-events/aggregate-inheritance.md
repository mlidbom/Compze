>[!NOTE]
> Here we are entering less well charted waters. We have few doubts about the design we present here. However, full, convenient support for routing events like this, and for building aggregates in this way, is still a work in progress.

# Aggregate Inheritance
So far we have adressed the inheritance hierarchy of events within a single aggregate. Now let's look at what happens when we inherit one aggregate from another. Let's go with a trivial and classic example to make the point with as little distraction as possible. Animals.

[!code-csharp[](aggregate-inheritance.cs#noises1)]

#### The problem
Now imagine you're a dog person, you only care about when dogs are born. How would you listen to just the dog born events?

Uh oh! You can't. Much of the point of inheritance is to reuse functionality. So we can't very well require every inheriting class to reimplement birth using a different interface. That would defeat the point. 

>[!WARNING]
>If you're thinking of using some sort of factory method pattern for generating the subclass events and adding inheriting interfaces for dogs. Don't. We tried it. And it turns out that you will have to mirror and duplicate the entire semantic hierarchy of the base class event interfaces for each inheriting class. It turns into an absolutely horrifying mess of duplication where the slightest misstep breaks things. We gave up entirely on inheriting aggregates when we had found no better way than that. Please, don't even try it.

#### The solution
Thankfully we eventually realized that there is an elegant simple solution built right into C#. This should be familiar: 
[!code-csharp[](aggregate-inheritance.cs#enumerable-type-compatibility)]

Do you see it? Generic covariance! `IEnumarable<string>` is assignable to `IEnumerable<object>` and assignability is how we route events with Semantic Events. Eureka! Well it turns out it works. (But requires a lot of major refactorings within Compze which are still ongoing.)

Rather than try to twist english into a language capable of expressing it, which I'm finding unmanageable, I'll use C#.

[!code-csharp[](aggregate-inheritance.cs#noises1wrapped)]

Now, what an animal actually publishes is never just `IAnimalEvent`, it is always `IAnimalEvent<T>`, correspondingly a Dog publishes `IDogEvent<T>` and the problem has been solved. Now you can listen to just the events from dogs by doing this:

[!code-csharp[](aggregate-inheritance.cs#doglistener)]

Do this if you only care about cats:
[!code-csharp[](aggregate-inheritance.cs#catlistener)]

This if you don't care what type of animal it was:

[!code-csharp[](aggregate-inheritance.cs#animallistener)]

And this if you care about all animals, but need handle them differently somehow:

[!code-csharp[](aggregate-inheritance.cs#wrappedanimallistener)]

