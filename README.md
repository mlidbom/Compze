## Compze: Expressive domains through Teventive programming and Typermedia APIs

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/mlidbom)

### Teventive programming

Leveraging .NET type compatibility enables an event modeling paradigm which:

- **Gives unprecedented ability to express and understand domains**
  - The event definitions declaritively describe your domain in great detail. Spending 30 minutes reading the event interfaces of a teventive domain may well tell you more about that domain than spending a week reading through the implementation code of a classically implemented domain, where such design aspects can only be found in the implementation details. 
- **Eliminates all need for manual event routing and type checking**
- **Dramatically reduces the number of event subscriptions needed**
- **Enables modeling inheritance and composition** of event-based [aggregates](https://compze.net/docs/prerequisite-terms.html#aggregate) with elegant precision
- **[Unifies fine-grained and coarse-grained events](https://compze.net/paradigms/semantic-events/property-updated-events.html)** - no more choosing between property-updated style events and domain events
- **Enables subscribing to precisely the event you need**, while being guaranteed that when new events are added, inheriting the current event, you will receive those too without needing to change anything in your subscriber code


> **💡 Note:** Teventive programming require neither event sourcing nor asynchronous messaging. All benefits described above are available with synchronous, in-memory communication, and aggregates can be stored using any persistence mechanism you prefer, or not at all.

> **💡 Note:** In memory performance overhead is entirely negligible in the great majority of systems. Event dispatching comes down to looking up subscribers in a dictionary using a Type instance as the key.

> **💡 Note:** Unlike what one might expect, initial subscriber discovery is trivial and not error prone. It just comes down to Type.IsAssignableFrom.

#### Core Concepts

Tevents, type routed events, use the type system to declare their meaning in detail and unambiguously:

```csharp
//ITaggregateTevent and ITaggregateCreatedTevent are framework provided
interface IUserTevent : ITaggregateTevent;
interface IUserRegistered : IUserTevent, ITaggregateCreatedTevent;
interface IUserImported : IUserRegistered;
```

Tevents are routed by **type compatibility**

```csharp
registrar
  .ForTevent<IUserTevent>(userTevent => 
      Console.WriteLine($"User: {userTevent.TaggregateId} something happened"))
  .ForTevent<IUserRegistered>(userRegistered => 
      Console.WriteLine($"User: {userRegistered.TaggregateId} registered"))
  .ForTevent<IUserImported>(userImported => 
      Console.WriteLine($"User: {userImported.TaggregateId} imported"));
```

When an `IUserImported` tevent is published, **all three handlers** are called, in registration order, since `IUserImported` is type-compatible with all registered handlers.

#### Property Updates Without the Pain

Unify property-updated events and semantic domain events:

```csharp
interface IUserEmailPropertyUpdated : IUserTevent
{
   Email Email { get; }
}

interface IUserRegistered : IUserEmailPropertyUpdated, ITaggregateCreatedTevent;
interface IUserChangedEmail : IUserEmailPropertyUpdated;
```

Now you can:
- Listen to **all email updates** via `IUserEmailPropertyUpdated` - listeners never need to change
- Listen to **registration events** via `IUserRegistered` - listeners never need to change
- Add new ways to update email without touching existing code

**Learn more:** [Semantic Events Documentation](https://compze.net/paradigms/semantic-events/definition.html)

### 🌐 Typermedia APIs

The most popular API in the world is a hypermedia API. You're using it right now - it's called the World Wide Web. Can you imagine trying to use it without links and forms? Instead being given an int or a string? Why do we build APIs like that?

Compze extends hypermedia into Typermedia which:

- **Routes messages by .NET types** giving:
  - Zero configuration routing
  - A simple, already well-known programming model
- **Can be fully explored**,  browsed much like a website, by:
  - Getting Links
  - Posting Commands
  - All with full type safety and autocomplete in your IDE
- **Further encapsulates your domain**, exposing less implementation details than traditional APIs
  - All domain logic, validation, and available actions can be encapsulated in the .NET types returned by the API
  - The UI need only bind these types to UI components without implementing any domain logic
  - Application behavior can be fully tested without any UI framework
  - Changing the UI becomes far less burdensome and does not risk changing domain logic

> **💡 Note:** All of the above benefits can be had with Typermedia APIs implemented **entirely in-memory and synchronously**. Distribution and asynchronous communication is entirely optional, not a requirement.

> **💡 Note:** In memory performance overhead is negligible in the great majority of systems. Message dispatching essentially comes down to a single lookup in a dictionary using a Type instance as the key.

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

---


## Fledgling Documentation

- **[Project Site](http://compze.net/)**
- **[Semantic Events](https://compze.net/paradigms/semantic-events/definition.html)**
- **[Development Setup](DEVELOPMENT.md)**

---
