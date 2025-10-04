# Compze

**A .NET framework for building maintainable Teventive systems exposing Typermedia APIs**

[📖 Fledgling Documentation](http://compze.net/) | [🔧 Development Setup](DEVELOPMENT.md)

---

## Ushering in Paradigm Shifts

There are two areas where we feel the industry has been stuck in old models for more than a decade. Implementing hypermedia and event-driven architectures as if modern C# language features and .NET runtime abilities did not exist. Compze aims to change that by introducing two paradigms that are improvements on old ones.

### Teventive programming

Leveraging well-established C# features enables an event modeling paradigm which:

- **Gives unprecedented ability to understand domains** in terms of how events relate to each other
- **Eliminates all need for manual event routing and type checking**
- **Dramatically reduces the number of event subscriptions needed**
- **Enables modeling inheritance and composition** of event-based [aggregates](https://compze.net/docs/prerequisite-terms.html#aggregate) with elegant precision
- **[Unifies fine-grained and coarse-grained events](https://compze.net/paradigms/semantic-events/property-updated-events.html)** - no more choosing between property-updated style events and domain events
- **Enables subscribing to precisely the event you need**, while being guaranteed that when new events are added, inheriting the current event, you will receive those too without needing to change anything in your subscriber code


> **💡 Note:** Teventive programming does **not** require event sourcing or asynchronous messaging. All benefits described above are available with **synchronous, in-memory communication**. Aggregates can be stored using traditional ORMs or any persistence mechanism you prefer. Event sourcing and async messaging are optional architectural choices that Compze supports, but they are in no way prerequisites for leveraging Teventive programming.

#### Core Concepts

Tevents, type routed events, use the type system to declare their meaning in detail and unambiguously:

```csharp
public interface IEvent;

public interface IAggregateEvent : IEvent
{
   Guid AggregateId { get; }
}

interface IUserEvent : IAggregateEvent;
interface IUserRegistered : IUserEvent, IAggregateCreatedEvent;
interface IUserImported : IUserRegistered;
```

Events are routed by **type compatibility**, including support for **generic covariance**:

```csharp
registrar
  .ForEvent<IUserEvent>(userEvent => 
      Console.WriteLine($"User: {userEvent.AggregateId} something happened"))
  .ForEvent<IUserRegistered>(userRegistered => 
      Console.WriteLine($"User: {userRegistered.AggregateId} registered"))
  .ForEvent<IUserImported>(userImported => 
      Console.WriteLine($"User: {userImported.AggregateId} imported"));
```

When an `IUserImported` event is published, **all three handlers** are called automatically because `IUserImported` is type-compatible with all registered handlers.

#### Property Updates Without the Pain

Unify property-updated events and semantic domain events:

```csharp
interface IUserEmailPropertyUpdated : IUserEvent
{
   Email Email { get; }
}

interface IUserRegistered : IUserEmailPropertyUpdated, IAggregateCreatedEvent;
interface IUserChangedEmail : IUserEmailPropertyUpdated;
```

Now you can:
- Listen to **all email updates** via `IUserEmailPropertyUpdated` - listeners never need to change
- Listen to **registration events** via `IUserRegistered` - listeners never need to change
- Add new ways to update email without touching existing code

**Learn more:** [Semantic Events Documentation](https://compze.net/paradigms/semantic-events/definition.html)

### 🌐 Typermedia APIs

The most popular API in the world is a Hypermedia API. You're using it right now - it's called the World Wide Web. Can you imagine trying to use it without links and forms? Instead being given an int or a string? Why do we build APIs like that?

Compze extends Hypermedia into Typermedia which is Hypermedia that:

- **Routes messages by .NET types** giving:
  - Zero configuration routing
  - A simple, already well-known programming model
- **Can be fully explored** using a `Navigator`, browsed much like a website, by:
  - `Get`ting `Link`s
  - `Post`ing `Command`s
  - All with full type safety and autocomplete in your IDE
- **Further encapsulates your domain**, exposing less implementation details than traditional services
- **Excellently suited for building a Just-Beneath-The-UI-Rendering-Layer**
  - All domain logic, validation, and available actions can be encapsulated in the .NET types returned by the API
  - The UI only binds these types to UI components without implementing any domain logic
  - Application behavior can be fully tested without any UI framework
  - Changing the UI becomes far less burdensome and does not risk changing domain logic

> **💡 Note:** All of the above benefits can be had with Typermedia APIs implemented **entirely in-memory and synchronously**. Distribution and asynchronous communication is entirely optional, not a requirement.

#### Quick Example

```csharp
//Set up a specification for how we want to navigate the API.
var navigationToUserManagement = 
    NavigationSpecification.Get(MyApi.StartPage) //Get startpage
                           .Get(start => start.UserManagement);//Navigate to user management

//Execute the navigation spec using a browser. Since it is async we may safely assume this is 
//a remote call, but the spec could just as well be executed synchronously in memory:
var userManagementPage =  await navigationToUserManagement.NavigateAsyncUsing(httpBrowser);
                                                  

//use the page to create a command
var registerCommand = userManagementPage.Commands.RegisterUser();
registerCommand.Email = email; //In a real app you would bind it to UI controls, not do this
registerCommand.Password = password;
registerCommand.RepeatedPassword = repeatedPassword;

//Executes client side validation implemented in the command before posting it.
//In a real app any validation errors would be caught and bound to the UI
var user = await httpBrowser.Post(registerCommand); 

var userProfilePage = await httpBrowser.Get(user.ProfilePage);

//Profile page displayed in ui here
```
> **💡 Note:** A developer could write all of that whithout ever leaving their IDE. Autocomplete in the IDE makes the API browsable, not just at runtime, but as part of writing code. The same goes for the all of the functionality of a domain exposed through a Typermedia API, not just the simple example above.

Once you've used APIs like this, how would you feel about an API that gives you an `int` instead of an `ILink<User>`?

**Learn more:** [Typermedia Documentation](https://compze.net/paradigms/hypermedia-apis/introduction.html)

---

## Why Compze?

In spite of these powerful paradigms being available to us today, countless event-driven applications are chock-full of code that leverages none of these possibilities, resulting in what can only be described as maintenance nightmares.

It's essentially the event equivalent of working in C# while completely refusing to use generics and object-oriented programming. Surely we should not even need to argue that this is ill-advised?

Compze provides the tools and patterns to build better systems.

---

## Getting Started

### Prerequisites
- .NET 8.0 or later
- Visual Studio 2022, Rider, or VS Code
- (Optional) SQL Server, PostgreSQL, or MySQL for persistence testing

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/mlidbom/Compze.git
   cd Compze
   ```

2. **Set up for development**
   
   See the [Development Setup Guide](DEVELOPMENT.md) for detailed instructions on configuring your environment, database connections, and running tests.

3. **Explore the samples**
   
   Check out the `Samples/` directory for example projects demonstrating Compze's capabilities.

---

## Features

- **Event Sourcing & CQRS** - Event sourcing with Tevents
- **Multiple Persistence Options** - Support for SQL Server, PostgreSQL and MySQL
- **Dependency Injection** - Integration with Microsoft DI and SimpleInjector
- **Type-Safe Event Routing** - Leverage C# type system for automatic event routing
- **Aggregate Inheritance** - Model complex domains with greater ease

---

## Fledgling Documentation

- **[Project Site](http://compze.net/)**
- **[Semantic Events](https://compze.net/paradigms/semantic-events/definition.html)**
- **[Development Setup](DEVELOPMENT.md)**

---

## Contributing

We welcome contributions! Whether it's:
- 🐛 Bug reports
- 💡 Feature suggestions  
- 📝 Documentation improvements
- 🔧 Code contributions

Please feel free to open issues or submit pull requests.

---

## License

See [LICENSE.txt](LICENSE.txt) for details.

---

**Built with ❤️ for developers who care about maintainability**