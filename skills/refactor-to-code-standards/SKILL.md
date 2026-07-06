---
name: refactor-to-code-standards
description: >-
  A skill for refactoring code into alignment with our code standards.
  Use whenever finishing any significant code change, that is not itself a refactoring towards the code standards.
  This skill provides the concrete diffs required to know what the code standards mean in practice instead of in vague theory.
---

# How to do the refactoring

* Read each of the files in [the examples folder](examples) in full
* Consider carefully how they relate to the instructions in our [Code Standards](../../../.claude/rules/01-universal-shared/code-standards)

* Apply the insights you reach to the code you just wrote, or to the code you were instructed to refactor
* Keep iterating until the code is as cleanly split into well defined responsibilities as the code in the diffs

# At the end of a development task

* Stage all your changes in git.
* Instruct a general subagent to run this skill on the code that is staged.
