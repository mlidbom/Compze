# Avoid passing primitives

The types we pass around should have meaning encoded directly in the type. 
Pass:
* User not userId.
* Point, not x and y

If a method takes more than one int, bool, string etc a new type should probably be created.