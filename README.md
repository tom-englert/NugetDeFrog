# NugetDeFrog
[![Build status](https://ci.appveyor.com/api/projects/status/qbl5og1ivs4iibnm/branch/main?svg=true)](https://ci.appveyor.com/project/tom-englert/nugetdefrog/branch/main)
[![NuGet Status](https://img.shields.io/nuget/v/TomsToolbox.NugetDeFrog.svg)](https://www.nuget.org/packages/TomsToolbox.NugetDeFrog/)

A DotNet command line tool to create a project that references only the runtime packages from an applications *.deps.json file

## Intention of this tool
Package scanners like e.g. JFrog Xray can scan the nuget packages of a project for known vulnerabilities, but they only work on the sources, not on the build output.
This leads to many false positives, as the scanner does not know which packages are actually used in the build output.

This tool creates a project file that references only the runtime packages from the *.deps.json file of the build output.
This project then can then be scanned by the package scanner to get a more accurate result.

## Installation
`dotnet tool install TomsToolbox.NugetDeFrog -g`

## Usage
```
Usage: NugetDeFrog [--output <String>] [--windows] [--help] [--version] file-or-directory

NugetDeFrog

Arguments:
  0: file-or-directory    Path to a dependency file or a directory with files '*.deps.json'. (Default: .)

Options:
  --output <String>    Path to the output project file. (Default: RuntimePackages\RuntimePackages.csproj)
  --windows            Use windows target platform; required if any of the projects require windows platform
  -h, --help           Show help message
  --version            Show version
```
## Example

```
dotnet tool install TomsToolbox.NugetDeFrog -g
NugetDeFrog --output RuntimePackages\RuntimePackages.csproj --windows MyProject\bin\Debug\net8.0\MyProject.deps.json
jf.exe dotnet restore RuntimePackages\RuntimePackages.csproj --build-name="MyBuild" --build-number="MyBuild.1.2.3" --project="MyProject"
```