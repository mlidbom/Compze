# When to disregard the rules in our coding standards

## If following the rules, you cannot reach a design that read well and is easy to maintain.

When that happens:

* Surface it to the user and ask for guidance

If the user approves it:
* Document it clearly in the code under a comment starting with  //rationale: 
* Add this exception to the code standards to a document under dev_docs/code_standard_exceptions