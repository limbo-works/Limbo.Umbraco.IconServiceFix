# Limbo.Umbraco.IconServiceFix

`Limbo.Umbraco.IconServiceFix` is small package created to fix one [specific issue](https://github.com/umbraco/Umbraco-CMS/pull/12833) in Umbraco's default implementation of the `IIconService` interface.

It's possible for packages to provide their own icons for the Umbraco backoffice. The `IconService` class is responsible for picking up those icons, and making them available for Umbraco's icon picker. While the class correctly picks up icons from disk inside the web project, it fails to pick up icons from virtual directories (like from Razor Class Libraries).

https://github.com/umbraco/Umbraco-CMS/pull/12833 is my PR with the same fix (+/-) that is provided my this package. While my PR has been merged, it's scheduled to be released in Umbraco 10.3, and we'll need the fix before that. As a result, this package will replace the `IconService` with a custom implementation of `IIconService`, but only for Umbraco 10.0, 10.1 and 10.2, but not for 10.3 and above.

The package is not relevant to other versions for Umbraco.


<br /><br />

## Installation

The package is only available via [NuGet](https://www.nuget.org/packages/Limbo.Umbraco.IconServiceFix/1.0.0). To install the package, you can use either .NET CLI:

```
dotnet add package Limbo.Umbraco.IconServiceFix --version 1.0.0
```

or the older NuGet Package Manager:

```
Install-Package Limbo.Umbraco.IconServiceFix -Version 1.0.0
```
