# AGENTS.md

Compact guidance for agents working in this repo.

## Commands

```shell
# Restore tools (run from repo root, not src/)
dotnet tool restore

# Build and test (repo root or src/)
dotnet build ./src
dotnet test ./src

# Run a single TFM or test class
dotnet test ./src --framework net10.0
dotnet test ./src --filter "FullyQualifiedName~SubnetTests"

# Lint & format — run from src/, order matters
dotnet format style && dotnet format analyzers && dotnet csharpier format .

# Benchmarks
./src/run-benchmarks.sh [-- --filter *Pattern*]

# Smoke tests — validates packed NuGet, not project reference
./smoketests/run-smoke-tests.sh
```

`dotnet test` runs all 4 TFMs — tests must pass all targets before merge.

## Frameworks

| Project | Targets |
|---|---|
| Library (`src/Arcus/`) | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| Tests (`src/Arcus.Tests/`) | `net48`, `net8.0`, `net9.0`, `net10.0` |

SDK: `10.0.100` (`global.json`). net48 target must run on Windows — validates netstandard2.0 asset.

## Architecture

`IIPAddressRange` → `AbstractIPAddressRange` → `Subnet` / `IPAddressRange`

Every type is split into partials by interface/grouping (`Factory`, `IComparable`, `IEquatable`, `ISerializable`, `Operators`). `.csproj` nests them via `<DependentUpon>`. Test files mirror this structure.

`BigEndianBitWrapper` is `internal`; `InternalsVisibleTo` grants access to Tests and Benchmarks.

Enumeration cap: `MaxEnumerationExponent = 12` (4096 addresses) — use `ToIPAddresses()` for large ranges.

## Testing quirks

- xUnit v3 + NSubstitute
- Custom serializers in `src/Arcus.Tests/XunitSerializers/` needed for theory data with `Subnet`/`IPAddressRange`
- `packages.lock.json` committed per project. Update after changing deps: `dotnet restore --force-evaluate`

## Code style

- `using` directives go **outside** the namespace (StyleCop)
- CSharpier: printWidth 128, 4-space indent (`.csharpierrc`)
- Husky pre-commit hook auto-formats staged `.cs` files. Set `HUSKY=0` to skip.
- Use ASCII hyphens (`-`) in source code, not typographic dashes (en-dash `–`, em-dash `—`).
  Documentation files may use proper typographic dashes.

## Versioning & publishing

- Version injected via MSBuild property `VersionFromCI` — without it: `0.0.0-build`, not packable.
- Push tag `v*.*.*` to publish NuGet (GitHub Packages + nuget.org).
- Solution uses `.slnx` format.
