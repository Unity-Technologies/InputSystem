# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.0.14-preview] - 2018-12-11

### Changed

- `Pointer.delta` no longer has `SensitivityProcessor` on it. The processor was causing many issues with mouse deltas. It is still available for adding it manually to action bindings but the processor likely needs additional work.

### Fixed

Core:
- Invalid memory accesses when using .NET 4 runtime
- Mouse.button not being identical to Mouse.leftButton
- DualShock not being recognized when connected via Bluetooth

Actions:
- Parameters disappearing on processors and interactions in UI when edited
- Parameters on processors and interactions having wrong value type in UI (e.g. int instead of float)
- RebindingOperation calling OnComplete() after being cancelled

Misc:
- Documentation no longer picked up as assets in user project

## [0.0.13-preview] - 2018-12-5

First release from stable branch.
