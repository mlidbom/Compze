# DDD: Ubiquitous language — the same word for a concept everywhere a human reads it

We apply 'Eric Evans' **ubiquitous language**, universally.

Every concept in the system should have exactly one name, and that exact name should be used **in every artifact a
human reads**: type and member names, comments, commit messages, documentation, test and specification names,
user-facing text (labels, dialogs, errors). Even throwaway lab and research code. No excuses accepted.

The inverse is NOT true. As long as the context makes things clear, multiple classes having members with the same name that mean different things, is perfectly fine.
Identical type names are more iffy though. If there is any risk of them being confused for each other, it is better to add something to the name to make them unique. 

## Why:

* Every time different words are used the reader must do mental mapping and translation making comprehension far harder.

## How

- **Renames apply everywhere.** 
- **Audit user-facing text too.** 
- **Two words for one concept is a modeling question, not a styling one.** 
- **A distinct word must mean a distinct concept.**

See also: [the human mind](005-software-design-and-the-human-mind.md)


