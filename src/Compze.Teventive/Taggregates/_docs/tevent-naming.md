# Tevent naming

Name tevents by nesting their interfaces: each level is a tevent interface nested in the one above it, and inheriting it.

[!code-csharp[](tevent-naming.cs#nested-tevents2)]

A subscriber then names exactly the tevent it means, however deep it is:

[!code-csharp[](tevent-naming.cs#helpful2)]

The name says where the tevent belongs: `IUserTevent.IProfile.IPropertyUpdated.IName` is the name of a user's profile, updated. A flat name such as `IUserProfileNamePropertyUpdatedTevent` doesn't say it: it could as well be a tevent of a `UserProfile` taggregate. Nor do namespaces: wherever the IDE shortens a type to its own name, a tevent in `User.Tevents.Profile.PropertyUpdated` reads as just `IName`:

[!code-csharp[](tevent-naming.cs#Unhelpful)]

Only the root interface ends in `Tevent`; the levels below it don't repeat it. They may drop the `I` as well: `IUserTevent.Profile.PropertyUpdated.Name`.
