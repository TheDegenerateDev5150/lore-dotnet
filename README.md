# Lore C# SDK

## About

This repository contains the C# SDK for integrating with Lore.

Lore is an open source version control system that is designed for unprecedented scalability of both data and teams. It is optimized for projects that combine code with large binary assets, including games and entertainment, and caters to the needs of developers and artists alike.

For full Lore documentation, architecture details, and contribution guidelines, visit the [main Lore repository](https://github.com/EpicGames/lore).

## Install

### Stable Release

```bash
dotnet add package LoreVcs --project /<path-to>/project.csproj
```

## Minimal example

The top-level `LoreVcs` namespace exposes the high-level fluent API. A low-level, C-like wrapper around the underlying FFI is also available under `LoreVcs.Interop` for advanced use cases.

```csharp
using LoreVcs;
using LoreVcs.Types;
using LoreVcs.Types.Args;
using LoreVcs.Types.Enums;
using LoreVcs.Types.Events;

var globalArgs = new LoreGlobalArgs { RepositoryPath = "/path/to/local/repository" };
var statusArgs = new LoreRepositoryStatusArgs { Staged = true, Scan = true };

Lore.LogConfigure(new LoreLogConfig { File = true, FilePath = "/path/to/log/directory", Level = LoreLogLevel.DEBUG });

Lore.RepositoryStatus(globalArgs, statusArgs)
    .Callback((evt, ctx) =>
    {
        if (evt.Tag == LoreEventTag.REPOSITORY_STATUS_FILE)
        {
            var statusEvent = evt.GetData<LoreRepositoryStatusFileEventDataFFI>();
            Console.WriteLine(statusEvent.Path);
        }
    })
    .Wait();
```

### Build and run with a runtime identifier (RID)

`LoreVcs` carries no native library itself. The native `lorelib` is delivered by a separate `LoreVcs.runtime.<rid>` package that NuGet pulls in only during a RID-qualified restore. You therefore have to tell .NET which platform you are targeting. There are two ways to do it:

* (Recommended) Set it once in your project file:

```xml
<PropertyGroup>
  <RuntimeIdentifier>osx-arm64</RuntimeIdentifier>
</PropertyGroup>
```

Then run your project with no extra flags:

```bash
dotnet run --project /<path-to>/project.csproj
```

* Pass the RID on each command:

```bash
dotnet run --project /<path-to>/project.csproj -r <runtime-identifier>
```


Supported runtime identifiers for the `LoreVcs` package are

- osx-arm64
- win-x64
- linux-x64

See [.NET RID Catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog) for more information on runtime identifiers

For comprehensive examples, see the [examples directory](examples/). Both [examples/example/Program.cs](examples/example/Program.cs) (fluent API) and [examples/example-native/Program.cs](examples/example-native/Program.cs) (low-level native API) run offline by default; pass a remote URL (e.g. `lore://localhost`) as the first argument to exercise the online push/clone flow. See [examples/README.md](examples/README.md) for details, including how to run a local Lore server.

## Contributing

### Set up your dev environment

1. Clone the Lore C# SDK repository

```bash
git clone https://github.com/EpicGames/lore-dotnet
```

2. Download .NET from https://dotnet.microsoft.com/en-us/download
3. Install CSharpier

```bash
dotnet new tool-manifest
dotnet tool install csharpier
```

4. Create a virtual environment and activate it:

```bash
uv venv .venv
source .venv/bin/activate
```

5. Install the dev tooling:

```bash
uv pip install --group dev
```

### Get the Lore library

The SDK binds against the Lore C library. Pick one of the two options below depending on whether you're also modifying the Lore core.

#### Option A — build the library from Lore source

Use this when you're changing the Lore C/Rust core alongside the C# SDK.

1. Clone [Lore's repository](https://github.com/EpicGames/lore) and build it:

```bash
cargo build --release
```

2. Set the environment variable `LORE_BUILD_PATH` to point to the release build path:

```bash
export LORE_BUILD_PATH="/<path-to>/lore/target/release"
```

#### Option B — fetch a pre-built Lore library

Use this when you only need to develop the C# SDK against an existing Lore version.

1. Download the header and binaries from the [Lore repository](https://github.com/EpicGames/lore) release page and place them under `/<path-to>/lore/`

2. Set the environment variable `LORE_BUILD_PATH` to point to the download path:

```bash
export LORE_BUILD_PATH="/<path-to>/lore/"
```

### Generate the C# bindings

With `LORE_BUILD_PATH` set (see [Get the Lore library](#get-the-lore-library)), generate the C# bindings from it:

```bash
uv run python find_lorelib.py
uv run python generator/generate.py
```

If you change anything under `generator/templates` or pull a new Lore pre-built binary, re-run the commands above to regenerate the bindings.

### Run the examples

The example projects reference LoreVcs via [ProjectReference](examples/example/example.csproj#L10), so they pick up the freshly built assembly and the host's native library directly from your working tree. No runtime identifier is needed as the native library is copied next to the build output.

```bash
dotnet run --project examples/example/example.csproj
```

Replace `example` with `example-native` to run the low-level native example. See [examples/README.md](examples/README.md) for offline/online run modes and local server setup.

### Run the test suite

Run the [Get the Lore library](#get-the-lore-library) and [Generate the C# bindings](#generate-the-c-bindings) steps first (`find_lorelib.py` then `generator/generate.py`): the tests consume `LoreVcs` via `ProjectReference` and need the regenerated bindings and the host's native library present.

```bash
dotnet test LoreVcs.Tests --logger "console;verbosity=detailed"
```

### Format

Format the code with CSharpier (installed during [Set up your dev environment](#set-up-your-dev-environment)). Run it before opening a PR so the source stays consistently formatted for users.

```bash
dotnet csharpier .
```
