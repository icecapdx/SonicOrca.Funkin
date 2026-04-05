# Building

The solution targets **.NET 10** (`net10.0`). Use the .NET 10 SDK.

## Windows

Use Visual Studio

## Linux

Use VSCode or...:

```bash
dotnet restore SonicOrca.Funkin.sln
dotnet build SonicOrca.Funkin.sln -c Debug -p:Platform=x64
dotnet publish SonicOrca.Funkin.csproj -c Release -r linux-x64 --self-contained true -p:Platform=x64
```