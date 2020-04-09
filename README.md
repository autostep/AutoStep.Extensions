# AutoStep.Extensions

This library provides the functionality for loading extensions into AutoStep. 

> [Contribution Guide](https://github.com/autostep/.github/blob/master/CONTRIBUTING.md) and [Code of Conduct](https://github.com/autostep/.github/blob/master/CODE_OF_CONDUCT.md)


---

**Status**

AutoStep is currently under development (in alpha). You can grab the CI package
from our feedz.io package feed: https://f.feedz.io/autostep/ci/nuget/index.json.
Get the 'develop' tagged pre-release package for latest develop. 

---

Extensions to AutoStep can:

- Define their own steps
- Define their own interaction methods
- Hook into events during test execution to add any behaviour they might need.

To write an AutoStep Extension:

- Create a .NET class library that targets netcoreapp3.1 or netstandard2.1.
- Add a reference to the the AutoStep.Extensions.Abstractions nuget package.
- Implement an Extension entry point that implements ``IExtensionEntryPoint``.
- Build the extension.
- Publish the extension to NuGet.
- Add the package name to the AutoStep Project Configuration file.
