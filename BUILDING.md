# Building

The solution targets **.NET 10** (`net10.0`). Use the .NET 10 SDK.

## Windows

Use Visual Studio

## Linux

Use VSCode or...:

```bash
dotnet restore SonicOrca.GameTemplate.sln
dotnet build SonicOrca.GameTemplate.sln -c Debug -p:Platform=x64
dotnet publish SonicOrca.GameTemplate.csproj -c Release -r linux-x64 --self-contained true -p:Platform=x64
```