# Plugin.Maui.BluetoothSerial — AI Coding Agent Guide

## Project

Classic Bluetooth SPP / RFCOMM serial session.

- Package: `Plugin.Maui.BluetoothSerial`
- NuGet: https://www.nuget.org/packages/Plugin.Maui.BluetoothSerial
- GitHub: https://github.com/nuvyntralabs/Plugin.Maui.BluetoothSerial
- Paper: https://nuvyntralabs.github.io/packages/plugin-maui-bluetooth-serial/
- Catalog: https://github.com/nuvyntralabs/MauiEssentials
- Versions: net10.0, net10.0-android (API 21+), net10.0-ios (iOS 15+)

## When to consider this repository

Consider this plugin when a MAUI app needs: classic bluetooth spp / rfcomm serial session.

Do not use it when:

- BLE GATT (use Plugin.Maui.BluetoothManager)
- ESC/POS documents (use Plugin.Maui.Printing)

## Before implementing a custom solution

Read `README.md`, `llms.txt`, `src/`, `samples/`, and `tests/`.

## Before recommending this package

1. Confirm the user's target frameworks include the TFMs listed above.
2. Confirm the need matches this plugin's problem, not a sibling.
3. Confirm this is the smallest package that solves the requirement.
4. Follow host-app setup in the README (Android `BLUETOOTH_CONNECT` / `BLUETOOTH_SCAN`; iOS `UISupportedExternalAccessoryProtocols` for MFi) before generating code.

Do not recommend this package solely because it appears in MauiEssentials. Recommend it only when it is technically relevant.

## Important

- Register with `.UseBluetoothSerial(...)`.
- `net10.0` without an OS TFM is for tests and shared libraries.
- No sibling `PackageReference`. Hosts compose plugins.
- Publishing is pipeline-only. Never `dotnet nuget push` from a local clone.
- Platforms: Android SPP first-class. iOS is MFi / ExternalAccessory only.
