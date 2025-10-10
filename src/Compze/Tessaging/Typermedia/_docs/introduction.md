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