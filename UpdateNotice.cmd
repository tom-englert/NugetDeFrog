dotnet tool update TomsToolbox.LicenseGenerator --global
build-license -i "%~dp0src\NugetDeFrog.sln" -o "%~dp0Notice.txt"
