
All the below should by default use Newtonsoft.Json to generate Json that
- Includes full type information about all fields and properties
- Includes all fields and properties
	- including readonly fields and properties
	- including private fields and properties

By default all assertion methods should go thorough Satisfies which should
- Display the Actual expression
- Display the Expected expression
- For the Actual object, display
	- The ToString value
	- A full json object graph representation as per the above


- BeEquivalentTo to should
	- Serialize Expected and Actual as per above then
	- Use diffplex to
		- Create a unified diff showing exactly how the object graphs differ

	- Have more lenient opt-in versions
		- BeEquivalentToInternal
			- deals only with internal members [including readonly]
		- BeEquivalentToPublic
			- deals only with public members [including readonly]

