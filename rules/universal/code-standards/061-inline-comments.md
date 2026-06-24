# Inline `//` comments:

* Most lines with inline comments are lines that should be refactored somehow rather than adding a comment. Try that first
* Put inline comments at the end of the line they explain, or on the line preceding the line/section they are talking about. Never on a member or type.
* The same reader calibration as in [documentation comments](060-documentation-comments.md) §0 applies here too: explain the specialized substrate in plain terms — don't assume the reader only needs the advanced details.
* Speak the project's [ubiquitous language](007-ubiquitous-language.md) (DDD): use the same word the code uses for a concept, never a synonym. A comment beside a `preview` that calls it a "tile" is the exact defect 007 forbids.
* Never put // comments on members. Use doc comments for members so that they can use linking tags and appear on hover of references in the IDE
