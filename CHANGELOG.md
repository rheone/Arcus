# Changelog

All notable changes to Arcus are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [5.0.0] - 2026-06-27

### Added
- `IIPAddressRange.ToIPAddresses()` — safe enumeration method capped at `MaxEnumerationExponent`
- `MaxEnumerationExponent` property on all range types (default 12 = 4096 addresses)
- `maxEnumerationExponent` optional parameter on all range constructors and factory methods
- .NET 10 (`net10.0`) target framework
- `BigEndianBitWrapper` internal type replacing the Gulliver dependency
- `SubnetUtilities.FewestConsecutiveSubnetsFor` overload with `maxEnumerationExponent` parameter
- Regex source generators for improved parsing performance
- Partial class split for maintainability (`Subnet`, `IPAddressRange`, `AbstractIPAddressRange`, `BigEndianBitWrapper`)
- `InternalsVisibleTo` for test and benchmark assemblies

### Changed
- `SubnetUtilities.PrivateIPAddressRangesList` and `LinkLocalIPAddressRangesList` are now `readonly`
- `AbstractIPAddressRange.Overlaps` — symmetry restored (both directions now return `true`)
- `ContainsAnyPrivateAddresses`/`ContainsAllPublicAddresses` — now use range-overlap detection
- `IPAddressRange.TryExcludeAll` — boundary guards at family min/max (no longer throws)
- `Subnet` parsing and `IPAddressUtilities` parsing use `[GeneratedRegex]` where available
- Package dependencies updated (SonarAnalyzer, Roslynator, etc.)
- `AllowUnsafeBlocks` enabled in project configuration
- Language version set to C# 14

### Removed
- `MacAddress` type (was `[Obsolete]` in v3.x)
- `DefaultIPAddressRangeComparer` (use `DefaultIIPAddressRangeComparer`)
- Gulliver NuGet dependency (replaced by internal `BigEndianBitWrapper`)
- `GeneratePackageOnBuild` property
- `Arcus.DocExamples` project

### Deprecated (will be removed in v6.0.0)
- `GetEnumerator()` on any range type — use `ToIPAddresses()` instead
- `IIPAddressRange` implementing `IEnumerable<IPAddress>` — use `ToIPAddresses()` explicitly
- `Subnet.TryIPv6FromPartial(string, out IEnumerable<Subnet>)` — to be replaced by more explicit methods

### Fixed
- `AbstractIPAddressRange.Overlaps` asymmetry (inner.Overlaps(outer) returned `false`)
- `ContainsAnyPrivateAddresses`/`ContainsAllPublicAddresses` endpoint heuristic producing incorrect results for ranges spanning private subnets
- `IPAddressRange.TryExcludeAll` throwing `InvalidOperationException` at family max address
- IComparable consistency across range types

## [4.0.0] - 2026-01-15

### Added
- (placeholder — see `docs/design/RELEASE-v4.0.0.md` for the v4.0.0 design document)

## [3.0.0] - 2025-06-15

### Added
- Initial public release of Arcus
