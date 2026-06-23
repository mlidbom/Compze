# Renaming is the most important refactoring there is.
The full principle lives in
[012-renaming-is-the-most-important-refactoring](012-renaming-is-the-most-important-refactoring.md);
this file is the naming how-to.

# Avoid abbreviations
Abbreviations assume the reader knows them and that they are obvious in context.
Unless both are certain to be true

# Methods: However long required
* You should know what it does from the name only.
If you need to read the method to understand what a line calling it does, renaming or other refactoring is in order.

**Do not try to keep names short at the expense of clarity.**

# Classes and interfaces

The name must denote a clearly-defined abstraction and tell the truth about what the thing is. A name that
implies something untrue is not polish — it is a fatal design error: a `User` must be a user; a value object
holding a user's registration details is `UserRegistrationData`, never `User`. A documentation comment briefly
explains what the abstraction is.

The name you choose becomes the concept's word in the project's [ubiquitous language](007-ubiquitous-language.md) (DDD):
write that *same* word everywhere the concept appears — comments, tests, docs, user-facing text — never a synonym.

# Default to naming the "flowing" lambda variable in fluent code "it"

It means exactly the right thing in english, "the thing we are already talking about".
Use it in code and fluent code becomes shorter and more readable. "it" == the thing we are talking about.