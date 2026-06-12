# Everything in its proper place

Every interface/class/method/property etc should represent a clear abstraction that stays at a consistent level of abstraction even in its implementation.
Anything that does not fit this criteria should be moved somewhere else.
If there is nowhere to move it. Figure out what the missing abstraction is and create it.

## Abstractions refer to logical parts of domains relevant to our application

These are NOT examples of a valid abstraction
* All methods: doing dll imports, making database calls, making network calls.
No class is cohesive if it contains logic belonging to many different abstractions that only share such technical details.

## Interfaces and classes
Each member, regardless of accessibility modifier, should clearly be part of the responsibility of this abstraction.

## Methods
Methods should read like how you would describe what they do in a few words to someone.
Any line that needs an explanatory comment is a line that should be refactored somehow,
likely by extracting another method, quite possibly to another class that might need to be created first.

If this new method makes no sense outside of the current method, make it a local method.

## Boolean conditions
Most boolean conditions testing more than a single value should be either properties or methods.
If this makes no sense outside of the current method, make it a local method.

## Local methods
place local methods last in the method declaring them.

## Reuse is not the goal
The goal is maintainable easy to read code, not DRY. Split as soon as that goal is furthered by splitting.
