# Renaming is the most important refactoring there is.
A name is the interface to a concept — read at every callsite, far more than the body — so a name that lies or
blurs miscalibrates every reader continuously, and the rename that fixes it is the highest-leverage change
there is. The moment a truer name appears, rename; never defer it as cosmetic, and never keep a poor name
because it is "everywhere" — that ubiquity is the cost, not a reason to keep it.

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

# Default to naming the "flowing" lambda variable in fluent code "it"

It means exactly the right thing in english, "the thing we are already talking about".
Use it in code and fluent code becomes shorter and more readable. "it" == the thing we are talking about.