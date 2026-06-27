# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## About

Arcus is a C# library for calculating, parsing, formatting, converting, and comparing IPv4 and IPv6 addresses and subnets. It uses `BigInteger` throughout to handle 128-bit IPv6 math on 32-bit platforms. Low-level byte manipulation is handled by the internal `BigEndianBitWrapper` struct; there are no external runtime dependencies beyond the BCL.

## Commands

All dotnet commands should be run from the `src/` directory unless noted otherwise.

```shell
# Restore tools (run from repo root)
dotnet tool restore

# Restore, build, and test
dotnet restore
dotnet build ./src
dotnet test ./src

# Run tests for a specific framework target
dotnet test ./src --framework net10.0

# Run a single test class
dotnet test ./src --filter "FullyQualifiedName~SubnetTests"

# Lint and format (run from src/ — order matters)
dotnet format style && dotnet format analyzers && dotnet csharpier format .
```

## Benchmarks

`src/Arcus.Benchmarks/` uses BenchmarkDotNet and targets all four TFMs.

```shell
# Run from repo root
./src/run-benchmarks.sh                          # all benchmarks
./src/run-benchmarks.sh -- --filter *Subnet*     # subset by pattern
```

Results land in `src/results/<timestamp>/` as JSON and Markdown. Commit that directory to record a progression snapshot.

## Smoke Tests

`smoketests/` proves that the **packed NuGet package** works for every target framework.
Unlike the xUnit suite (which uses a project reference), smoke tests consume the real `.nupkg`
so NuGet asset-selection and package metadata are validated end-to-end.

```shell
# Linux / macOS — covers net8.0, net9.0, net10.0
./smoketests/run-smoke-tests.sh

# Windows (PowerShell) — covers net48 + net8.0, net9.0, net10.0
.\smoketests\run-smoke-tests.ps1
```

net48 must run on Windows; it validates the `netstandard2.0` asset that .NET Framework consumers receive.

The `smoke-test` CI job in `.github/workflows/build.yml` runs automatically after the `build` job,
using a matrix: ubuntu-latest for net8/9/10 and windows-latest for net48.

## Pre-commit Hook

A Husky.Net pre-commit hook automatically runs `dotnet format style`, `dotnet format analyzers`, and `dotnet csharpier format` on staged `.cs` files. Set `HUSKY=0` to skip hooks in CI/CD contexts.

## Project Targets

- **Library** (`src/Arcus/`): `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`
- **Tests** (`src/Arcus.Tests/`): `net48` (covers .NET Standard 2.0), `net8.0`, `net9.0`, `net10.0`

Tests must be run across all targets before a change is considered complete.

Use C# preprocessor directives `#if`, `#else`, `#endif` to target `NETSTANDARD2_0`, `NET8_0`, `NET9_0`, `NET10_0` to make code compatible to all targets when features may be unavailable.

## Code Intelligence

Prefer LSP over Grep for code navigation - it's faster, precise, and avoids reading entire files:

- `workspaceSymbol` to find where something is defined
- `findReferences` to see all usages across the codebase
- `goToDefinition` / `goToImplementation` to jump to source
- `hover` for type info without reading the file

Use Grep only when LSP isn't available or for text/pattern searches (comments, strings, config).

After writing or editing code, check LSP diagnostics and fix errors before proceeding.

## Architecture

The type hierarchy flows: `IIPAddressRange` → `AbstractIPAddressRange` → `Subnet` / `IPAddressRange`. `IIPAddressRange` extends `IFormattable` and `IEnumerable<IPAddress>`.

- **`Subnet`** — the primary type and main reason the library exists. Represents an IPv4/IPv6 subnetwork, constrained to power-of-two size with a valid network address. Constructors accept two `IPAddress` bounds (builds the smallest containing subnet) or an `IPAddress` + routing prefix integer. `Subnet.Factory.cs` adds static factory methods: `Parse`/`TryParse` (CIDR strings like `"192.168.1.0/24"`), `FromNetMask`, and `FromBytes`.
- **`IPAddressRange`** — arbitrary inclusive range of same-family IP addresses; not constrained to power-of-two size or valid broadcast boundaries. `IPAddressRange.Factory.cs` adds set-theoretic statics: `TryCollapseAll`, `TryExcludeAll`, `TryMerge`.
- **`AbstractIPAddressRange`** — shared implementation of `IIPAddressRange`; provides `Head`, `Tail`, `Length` (`BigInteger`), set operations (Contains, Overlaps, Touches), and enumeration.
- **`BigEndianBitWrapper`** — `internal readonly partial struct`; low-level unsigned big-endian integer backing store for binary operations. Invisible to consumers; uses `UInt128` on net8+ and a `ulong` hi/lo pair on netstandard2.0.
- **`Comparers/`** — `DefaultAddressFamilyComparer`, `DefaultIPAddressComparer`, `DefaultIIPAddressRangeComparer`.
- **`Converters/IPAddressConverters`** — static conversion utilities for `IPAddress`.
- **`Math/IPAddressMath`** — extension methods for `IPAddress`: `Increment`, `Decrement`, and comparison operators (`IsGreaterThan`, `IsLessThan`, etc.). Overflow/underflow throws `InvalidOperationException`.
- **`Utilities/IPAddressUtilities`** — parsing from hex/octal/`BigInteger`, address family detection (`IsIPv4`, `IsIPv6`).
- **`Utilities/SubnetUtilities`** — `FewestConsecutiveSubnetsFor(IPAddress, IPAddress)` returns the minimal set of subnets covering an arbitrary inclusive IP range.

### Partial file convention

Each type is split into partial files by interface and functional grouping — `TypeName.cs` (core), `TypeName.Factory.cs` (static factories), `TypeName.IComparable.cs`, `TypeName.IEquatable.cs`, `TypeName.ISerializable.cs`, `TypeName.Operators.cs`. The `.csproj` nests partials under their primary file via `<DependentUpon>`. Test files mirror this structure exactly: `SubnetTests.cs`, `SubnetTests.IComparable.cs`, etc.

## Testing

Tests use xUnit v3 and NSubstitute. `src/Arcus.Tests/XunitSerializers/` contains `SubnetXunitSerializer` and `IPAddressRangeXunitSerializer` needed for theory data serialization.

The library sets `InternalsVisibleTo` for both `Arcus.Tests` and `Arcus.Benchmarks`, so test and benchmark code can access internal types (e.g. `BigEndianBitWrapper`) directly.

Each project uses `RestorePackagesWithLockFile=true` with a committed `packages.lock.json`. Update the lock file after adding or changing packages (`dotnet restore --force-evaluate`).

## Code Style

StyleCop, Roslynator, SonarAnalyzer, AsyncFixer, and VS.Threading analyzers all run as part of the build. CSharpier enforces formatting (print width 128, 4-space indentation). `usings` go outside the namespace per StyleCop config.

## Versioning and Publishing

Releases follow Semantic Versioning. Publishing to NuGet is triggered by pushing a tag matching `v*.*.*`. The version is injected via the `VersionFromCI` MSBuild property; without it, the local build version is `0.0.0-build` and the package is not packable.
