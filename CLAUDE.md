# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## About

Arcus is a C# library for calculating, parsing, formatting, converting, and comparing IPv4 and IPv6 addresses and subnets. It uses `BigInteger` throughout to handle 128-bit IPv6 math on 32-bit platforms. The key dependency is [Gulliver](https://github.com/sandialabs/gulliver) for low-level byte manipulation.

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

# Lint and format (run from src/)
dotnet format style; dotnet format analyzers; dotnet csharpier format .
```

## Pre-commit Hook

A Husky.Net pre-commit hook automatically runs `dotnet format style`, `dotnet format analyzers`, and `dotnet csharpier format` on staged `.cs` files. Set `HUSKY=0` to skip hooks in CI/CD contexts.

## Project Targets

- **Library** (`src/Arcus/`): `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`
- **Tests** (`src/Arcus.Tests/`): `net48` (covers .NET Standard 2.0), `net8.0`, `net9.0`, `net10.0`

Tests must be run across all targets before a change is considered complete.

## Architecture

The type hierarchy flows: `IIPAddressRange` → `AbstractIPAddressRange` → `Subnet` / `IPAddressRange`.

- **`Subnet`** — the primary type and main reason the library exists. Represents an IPv4/IPv6 subnetwork, constrained to power-of-two size with a valid network address. Constructors accept two `IPAddress` bounds (builds the smallest containing subnet) or an `IPAddress` + routing prefix integer. Also has `Parse`/`TryParse` for strings like `"192.168.1.0/24"`.
- **`IPAddressRange`** — arbitrary inclusive range of same-family IP addresses; not constrained to power-of-two size or valid broadcast boundaries.
- **`AbstractIPAddressRange`** — shared implementation of `IIPAddressRange`; provides `Head`, `Tail`, `Length` (`BigInteger`), set operations (Contains, Overlaps, Touches), and enumeration.
- **`Comparers/`** — `DefaultAddressFamilyComparer`, `DefaultIPAddressComparer`, `DefaultIPAddressRangeComparer`, `DefaultIIPAddressRangeComparer`.
- **`Converters/IPAddressConverters`** — static conversion utilities for `IPAddress`.
- **`Math/IPAddressMath`** — extension methods for `IPAddress`: `Increment`, `Decrement`, and comparison operators (`IsGreaterThan`, `IsLessThan`, etc.). Overflow/underflow throws `InvalidOperationException`.
- **`Utilities/IPAddressUtilities`** — parsing from hex/octal/`BigInteger`, address family detection (`IsIPv4`, `IsIPv6`).
- **`Utilities/SubnetUtilities`** — `FewestConsecutiveSubnetsFor(IPAddress, IPAddress)` returns the minimal set of subnets covering an arbitrary inclusive IP range.
- **`MacAddress`** — 48-bit MAC address (EUI-48/MAC-48). Marked `[Obsolete]` — candidate for removal in a future major version.

## Testing

Tests use xUnit v3 and NSubstitute. `src/Arcus.Tests/XunitSerializers/` contains `SubnetXunitSerializer` and `IPAddressRangeXunitSerializer` needed for theory data serialization.

## Code Style

StyleCop, Roslynator, SonarAnalyzer, and AsyncFixer analyzers all run as part of the build. CSharpier enforces formatting (print width 128, 4-space indentation). `usings` go outside the namespace per StyleCop config.

## Versioning and Publishing

Releases follow Semantic Versioning. Publishing to NuGet is triggered by pushing a tag matching `v*.*.*`. The version is injected via the `VersionFromCI` MSBuild property; without it, the local build version is `0.0.0-build` and the package is not packable.
