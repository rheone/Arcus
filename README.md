# ![Arcus](src/Arcus/icon.png) Arcus

![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/sandialabs/Arcus/build.yml?branch=main)
[![nuget Version](https://img.shields.io/nuget/v/Arcus)](https://www.nuget.org/packages/Arcus)
[![GitHub Release](https://img.shields.io/github/v/release/sandialabs/Arcus)](https://github.com/sandialabs/Arcus/releases)
[![GitHub Tag](https://img.shields.io/github/v/tag/sandialabs/Arcus)](https://github.com/sandialabs/Arcus/tags)
![Targets](https://img.shields.io/badge/.NET%20Standard%202.0%20|%20.NET%208.0%20|%20.NET%209.0|%20.NET%2010.0-blue)
[![Apache 2.0 License](https://img.shields.io/github/license/sandialabs/Arcus?logo=apache)](https://github.com/sandialabs/Arcus/blob/main/LICENSE)

## About the Project

Arcus is a C# manipulation library for calculating, parsing, formatting, converting, and comparing both IPv4 and IPv6 addresses and subnets. It accounts for 128-bit numbers on 32-bit platforms.

## ❗Breaking Changes in v4.0.0

### Removed types and members

The following previously `[Obsolete]` types and members have been removed.

| Removed | Namespace / Location | Migration |
| --- | --- | --- |
| `MacAddress` (entire type) | `Arcus` | No direct replacement in this library |
| `DefaultIPAddressRangeComparer` (entire type) | `Arcus.Comparers` | Use `DefaultIIPAddressRangeComparer` |

---

### `SubnetUtilities` static fields are now `readonly`

`SubnetUtilities.PrivateIPAddressRangesList` and `SubnetUtilities.LinkLocalIPAddressRangesList` are now declared `readonly`. Previously the field *reference* could be replaced by external code (e.g., `SubnetUtilities.PrivateIPAddressRangesList = myList`). That pattern will no longer compile. The `IReadOnlyList<Subnet>` type already prevented mutation of the list *contents*; `readonly` now also prevents replacement of the list itself.

**Migration:** If you were replacing these fields to customize private-address detection, extract that logic into a separate variable and pass it explicitly to your own helper methods.

---

### Behavior corrections (non-breaking for correct usage)

The following bugs have been fixed. If your code was intentionally relying on the incorrect behavior, you will need to update it.

| Type / Member | Previous (incorrect) | Fixed |
| --- | --- | --- |
| `AbstractIPAddressRange.Overlaps(IIPAddressRange)` | `B.Overlaps(A)` returned `false` when B was wholly inside A | Both directions now return `true` (symmetric) |
| `AbstractIPAddressRange.ContainsAnyPrivateAddresses()` | Checked only endpoints; missed ranges that span a private block with public endpoints | Uses range-overlap detection across `PrivateIPAddressRangesList` |
| `AbstractIPAddressRange.ContainsAllPublicAddresses()` | Checked only endpoints; returned `true` for ranges whose endpoints are public but whose interior spans a private block | Uses range-overlap detection |
| `IPAddressRange.TryExcludeAll` | Threw `InvalidOperationException` when an exclusion ended at the family maximum address | Returns `(true, leading segment)` or `(true, [])` as appropriate |

---

### IP Address Parsing across .NET Targets

In .NET versions up to and including .NET 4.8 (which corresponds to .NET Standard 2.0), stricter parsing rules are enforced for `IPAddress` according to the IPv6 specification. Specifically, the presence of a terminal '%' character without a valid zone index is considered invalid in these versions. As a result, the input `abcd::%` fails to parse, leading to a null or failed address parsing depending on `Parse`/`TryParse`.

In newer versions of .NET, including .NET 8, .NET 9, and .NET 10, the parsing rules have been relaxed. The trailing '%' character is now ignored during parsing, allowing for inputs that would have previously failed.

It is important to note that this scenario appears to be an extreme edge case. If in doubt, sanitize IP address user input to meet your development needs.

## Getting Started

The latest stable release of Arcus is [available on NuGet](https://www.nuget.org/packages/Arcus/).

The latest [Arcus documentation](https://arcus.readthedocs.io/en/latest/) may be found on [ReadTheDocs](https://arcus.readthedocs.io/en/latest/).

### Usage

Arcus provides types and utilities for working with IP addresses and subnets beyond what the .NET BCL offers. The library is organized around a few core concepts.

#### Quick Start

```csharp
using Arcus;
using Arcus.Utilities;

// Parse a subnet
var subnet = Subnet.Parse("192.168.1.0/24");
Console.WriteLine($"Network: {subnet.Head}, Broadcast: {subnet.Tail}");
Console.WriteLine($"Usable addresses: {subnet.UsableHostAddressCount}");

// Check containment
var testAddress = IPAddress.Parse("192.168.1.50");
Console.WriteLine(subnet.Contains(testAddress)); // True

// IP math
var nextAddress = testAddress.Increment(); // 192.168.1.51
var prevAddress = testAddress.Increment(-2); // 192.168.1.48

// Comparison
testAddress.IsBetween(subnet.Head, subnet.Tail);      // True
testAddress.IsGreaterThan(IPAddress.Parse("10.0.0.1")); // True

// Address family detection
testAddress.IsIPv4(); // True
testAddress.IsIPv6(); // False

// Work with ranges
var range = new IPAddressRange(
    IPAddress.Parse("10.0.0.0"),
    IPAddress.Parse("10.0.0.255"));
Console.WriteLine($"Range length: {range.Length}");

// Fewest subnets covering a range
var covering = SubnetUtilities.FewestConsecutiveSubnetsFor(
    IPAddress.Parse("128.64.20.3"),
    IPAddress.Parse("128.64.20.12"));
```

#### `Subnet`

An IPv4 or IPv6 subnetwork representation conforming to CIDR rules (power-of-two length, aligned to a prefix boundary). Implements `IIPAddressRange`, `IEquatable<Subnet>`, `IComparable<Subnet>`, `IFormattable`, `IEnumerable<IPAddress>`, and `ISerializable`.

**Construction:**

```csharp
// From two addresses (smallest subnet containing both)
var subnet = new Subnet(
    IPAddress.Parse("192.168.1.0"),
    IPAddress.Parse("192.168.1.255"));

// From address and routing prefix
var subnet = new Subnet(IPAddress.Parse("192.168.1.1"), 24); // autocorrects to 192.168.1.0/24

// From a CIDR string
var subnet = Subnet.Parse("192.168.1.0/24");
Subnet.TryParse("10.0.0.0/8", out var result);

// From netmask
var subnet = Subnet.FromNetMask(
    IPAddress.Parse("192.168.1.0"),
    IPAddress.Parse("255.255.255.0"));

// Single-address subnet
var single = new Subnet(IPAddress.Parse("192.168.1.1")); // 192.168.1.1/32
```

**Properties:** `Head`, `Tail`, `RoutingPrefix`, `Netmask` (IPv4 only), `BroadcastAddress`, `NetworkPrefixAddress`, `UsableHostAddressCount`, `Length`.

**Set operations:** `Contains(Subnet)`, `Overlaps(Subnet)`, `Touches(IIPAddressRange)`.

#### `IPAddressRange`

An inclusive range of IP addresses of the same address family. Unlike `Subnet`, not restricted to CIDR boundaries — any `head` through `tail` inclusive.

```csharp
var range = new IPAddressRange(
    IPAddress.Parse("10.0.0.5"),
    IPAddress.Parse("10.0.0.42"));

// Static utilities
IPAddressRange.TryCollapseAll(ranges, out var collapsed);
IPAddressRange.TryExcludeAll(initial, exclusions, out var remaining);
IPAddressRange.TryMerge(left, right, out var merged);
```

#### Comparers

| Comparer | Description |
|---|---|
| `DefaultAddressFamilyComparer` | Compares `AddressFamily` values (`InterNetwork` < `InterNetworkV6`) |
| `DefaultIPAddressComparer` | Compares `IPAddress` by family then unsigned big-endian value |
| `DefaultIIPAddressRangeComparer` | Compares ranges by head address then by length |

#### `IPAddressMath` — Arithmetic and Comparison

```csharp
// Increment and decrement
var next = address.Increment();       // +1
var prev = address.Increment(-5);     // -5
address.TryIncrement(out var result, delta: 10);

// Comparison
address.IsEqualTo(other);
address.IsGreaterThan(other);
address.IsLessThan(other);
address.IsBetween(low, high);

// Bounds
address.IsAtMin();    // true for 0.0.0.0 or ::
address.IsAtMax();    // true for 255.255.255.255 or ffff:...:ffff
IPAddressMath.Min(a, b);
IPAddressMath.Max(a, b);
```

#### `IPAddressConverters`

```csharp
// CIDR conversion (IPv4 only)
netmask.NetmaskToCidrRoutePrefix(); // IPAddress → int

// String formatting
address.ToDottedQuadString();       // IPv6 → mixed IPv4/IPv6 notation
address.ToHexString();              // Big-endian hex
address.ToNumericString();          // Unsigned integer string
address.ToUncompressedString();     // Fully expanded
address.ToBase85String();           // RFC 1924 (IPv6 only)
```

#### `IPAddressUtilities`

```csharp
// Address family
address.IsIPv4();
address.IsIPv6();

// Detection
address.IsIPv4MappedIPv6();         // RFC 4291
netmask.IsValidNetMask();           // IPv4 netmask validation

// Special values
AddressFamily.InterNetwork.MaxIPAddress();
AddressFamily.InterNetworkV6.MinIPAddress();

// Parsing
IPAddressUtilities.ParseFromHexString("C0A80101", AddressFamily.InterNetwork);
IPAddressUtilities.TryParseFromHexString(input, family, out address);
IPAddressUtilities.ParseIgnoreOctalInIPv4("010.000.001.001");
IPAddressUtilities.TryParse(bigInteger, family, out address);
```

#### `SubnetUtilities`

```csharp
// Fewest consecutive subnets covering an address range
var subnets = SubnetUtilities.FewestConsecutiveSubnetsFor(
    IPAddress.Parse("128.64.20.3"),
    IPAddress.Parse("128.64.20.12"));

// Largest / smallest subnet in a collection
SubnetUtilities.LargestSubnet(subnets);
SubnetUtilities.SmallestSubnet(subnets);

// Private/public range detection (constants)
SubnetUtilities.PrivateIPAddressRangesList;
SubnetUtilities.LinkLocalIPAddressRangesList;
```

### Developer Notes

## Built With

This project was built with the aid of:

- [CSharpier](https://csharpier.com/)
- [dotnet-outdated](https://github.com/dotnet-outdated/dotnet-outdated)
- [Husky.Net](https://alirezanet.github.io/Husky.Net/)
- [Roslynator](https://josefpihrt.github.io/docs/roslynator/)
- [SonarAnalyzer](https://www.sonarsource.com/products/sonarlint/features/visual-studio/)
- [StyleCop.Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
- [xUnit.net](https://xunit.net/)

### Versioning

This project uses [Semantic Versioning](https://semver.org/)

### Targeting

The project targets [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0), [.NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8), [.NET 9](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview), and [.NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview). The test project similarly targets .NET 8, .NET 9, .NET 10, but targets [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) for the .NET Standard 2.0 tests.

### Commit Hook

The project itself has a configured pre-commit git hook, via [Husky.Net](https://alirezanet.github.io/Husky.Net/) that automatically lints and formats code via [dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format) and [csharpier](https://csharpier.com/).

#### Disable husky in CI/CD pipelines

Per the [Husky.Net instructions](https://alirezanet.github.io/Husky.Net/guide/automate.html#disable-husky-in-ci-cd-pipelines)

> You can set the `HUSKY` environment variable to `0` in order to disable husky in CI/CD pipelines.

#### Manual Linting and Formatting

On occasion a manual run is desired it may be done so via the `src` directory and with the command

```shell
dotnet format style; dotnet format analyzers; dotnet csharpier format .
```

These commands may be called independently, but order may matter.

#### Testing

After making changes tests should be run that include all targets

## Acknowledgments

This project was built by the Production Tools Team at Sandia National Laboratories. Special thanks to all contributors and reviewers who helped shape and improve this library.

Including, but not limited to:

- **Robert H. Engelhardt** - *Primary Developer, Source of Ideas Good and Bad* - [rheone](https://github.com/rheone)
- **Andrew Steele** - *Review and Suggestions* - [ahsteele](https://github.com/ahsteele)
- **Nick Bachicha** - *Git Wrangler and DevOps Extraordinaire* - [nicksterx](https://github.com/nicksterx)
- **Drew Antonich** - *Review and Suggestions* [drewantonich](https://github.com/drewantonich)

## Copyright

> Copyright 2025 National Technology & Engineering Solutions of Sandia, LLC (NTESS). Under the terms of Contract DE-NA0003525 with NTESS, the U.S. Government retains certain rights in this software.

## License

> Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
>
> <http://www.apache.org/licenses/LICENSE-2.0>
>
> Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
