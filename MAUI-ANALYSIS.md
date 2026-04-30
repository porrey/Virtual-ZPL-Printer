# .NET MAUI Migration Analysis — Virtual ZPL Printer

## Executive Summary

Virtual ZPL Printer is a Windows-only WPF desktop application. Migrating it to .NET MAUI
is **feasible for a Windows-targeted MAUI app**, but provides limited value because the
application's core use case — listening on a TCP port and driving physical label printers —
is inherently a desktop/server scenario that makes cross-platform mobile targets (iOS, Android)
impractical. A targeted migration to **MAUI on Windows** would modernise the UI framework
without requiring mobile support.

---

## Current Technology Stack (Relevant to MAUI)

| Component | Current | MAUI-compatible? |
|---|---|---|
| UI framework | WPF (`net10.0-windows`, `UseWPF=true`) | No — must be rewritten in MAUI XAML |
| MVVM / events | `Prism.Wpf` (`IEventAggregator`) | Partial — `Prism.Maui` exists but has API differences |
| DI host | `Diamond.Core.Wpf` / Generic Host | Partial — `Diamond.Core.Wpf` is WPF-specific; Generic Host is fine |
| Physical printing | `System.Drawing.Printing` (`PrintDocument`, `PrinterSettings`) | No — Windows-only, not available in MAUI |
| Image generation | `System.Drawing` (`Bitmap`, `Graphics`, `Font`) | Windows only — requires replacement |
| Application settings | `System.Configuration.ApplicationSettingsBase` | No — WinForms/WPF specific; use `Preferences` API in MAUI |
| Database | EF Core + SQLite | Yes — fully cross-platform |
| HTTP client | `System.Net.Http.HttpClient` | Yes — fully cross-platform |
| TCP networking | `System.Net.Sockets.TcpListener` | Windows/macOS/Linux only — not supported on iOS/Android |
| Logging | Serilog | Yes — fully cross-platform |
| Serialization | Newtonsoft.Json | Yes — fully cross-platform |
| Units | UnitsNet | Yes — fully cross-platform |
| Installer | WiX / MSI (`VirtualPrinter-Setup`) | No — platform-specific packaging replaces this |

---

## Platform Scope

.NET MAUI targets Android, iOS, macOS (via Mac Catalyst), and Windows (via Windows App SDK /
WinUI 3). The Virtual ZPL Printer use cases map to platforms as follows:

| Feature | Android | iOS | macOS | Windows |
|---|---|---|---|---|
| TCP server on port 9100 | ❌ OS prohibits inbound server sockets | ❌ OS prohibits inbound server sockets | ✅ | ✅ |
| Physical label printer (GDI+) | ❌ | ❌ | ❌ | ✅ |
| File-system image cache | ⚠️ Sandboxed | ⚠️ Sandboxed | ✅ | ✅ |
| Full desktop UI with multiple windows/views | ⚠️ Limited | ⚠️ Limited | ✅ | ✅ |
| SQLite / EF Core | ✅ | ✅ | ✅ | ✅ |
| Labelary HTTP calls | ✅ | ✅ | ✅ | ✅ |

**Conclusion:** only the **Windows** and **macOS** MAUI targets are viable. Android and iOS
should be excluded from any migration effort.

---

## Areas Requiring Changes

### 1. UI Layer (High effort)

All 11 WPF XAML views and their code-behind files must be recreated in MAUI XAML. WPF and MAUI
XAML share similar syntax but are not compatible:

- `Window` → `ContentPage` (or `Shell` with tabbed navigation)
- WPF `UserControl` → MAUI `ContentView`
- WPF `DataGrid` → `CollectionView` (MAUI has no built-in DataGrid; a third-party control or
  custom layout is needed)
- WPF value converters (`IValueConverter`) → same interface name but different namespace
  (`Microsoft.Maui.Controls`)
- WPF `Style`/`ControlTemplate` → equivalent exists in MAUI but syntax differs
- WPF `Visibility` → MAUI uses `IsVisible` (bool)
- WPF `Image.Source` as `BitmapImage` → MAUI `ImageSource` / `StreamImageSource`

**Views to migrate** (11 WPF views):
`MainWindow`, `ConfigurationView`, `EditPrinterView`, `EditFiltersView`, `GlobalSettingsView`,
`FontManagerView`, `SendTestView`, `TestLabelaryView`, `ZplView`, `AboutView`, `SplashView`

### 2. MVVM / Event Bus (Medium effort)

`Prism.Wpf` must be replaced by `Prism.Maui` (available as a NuGet package). The
`IEventAggregator` API is the same, so event publish/subscribe code can be reused. However:

- `Prism.Wpf` registers views and view-models differently from `Prism.Maui`.
- The `Diamond.Core.Wpf` host bootstrapper (`HostedApplication` base class) is WPF-specific and
  must be replaced with a MAUI-compatible host startup.

### 3. Physical Printer Support (High effort — Windows only)

`PrintServer.cs` uses `System.Drawing.Printing.PrintDocument` and `System.Drawing.Bitmap`/
`Graphics` — both are Windows GDI+ APIs not available in MAUI on other platforms.

**Options:**
- **Windows-only guard:** wrap the physical printing feature in a `#if WINDOWS` / partial class
  so it compiles only for the Windows MAUI target, preserving the feature on Windows while
  gracefully disabling it on macOS.
- **Replacement API:** on Windows, `Microsoft.Maui.Devices` does not expose a printing API;
  `Windows.Graphics.Printing` (WinRT) can be P/Invoked via `[SupportedOSPlatform("windows")]`
  annotated code inside the `Platforms/Windows/` folder.

### 4. Error Image Generation (Medium effort)

`ErrorImage.cs` (in `Labelary.Service`) uses `System.Drawing.Bitmap` and `System.Drawing.Graphics`
to render a PNG error image. Replacement options:

- Use `SkiaSharp` (cross-platform 2D graphics, well-supported in MAUI) to redraw the error image.
- Use `Microsoft.Maui.Graphics` (built into MAUI) as a lighter-weight alternative.

### 5. Application Settings (Low effort)

`VirtualPrinter.ApplicationSettings` uses `System.Configuration.ApplicationSettingsBase`
(WinForms/WPF). In MAUI, use:

- `Microsoft.Maui.Storage.Preferences` for simple key-value settings (window size, etc.).
- Alternatively, keep a JSON file in `FileSystem.AppDataDirectory` and deserialise it manually
  (consistent with the existing JSON service-registration approach).

### 6. Host Bootstrapper (Medium effort)

`App.xaml.cs` extends `HostedApplication` from `Diamond.Core.Wpf`, which bootstraps the
`Microsoft.Extensions.Hosting` Generic Host inside a WPF application lifecycle. MAUI has its own
host (`MauiAppBuilder`) that integrates with the Generic Host pattern. The bootstrapper needs to
be rewritten to use `MauiProgram.cs` / `MauiAppBuilder`, but all registered services (hosted
services, repositories, handlers) remain the same.

### 7. Installer (Low effort)

The WiX MSI installer is Windows-only by design. For a MAUI migration:
- **Windows:** MAUI supports MSIX packaging natively via the Windows Application Packaging project.
- **macOS:** MAUI produces a `.app` bundle; a `.pkg` installer or DMG can be created with standard
  macOS tools.

---

## What Can Be Reused Without Changes

The following projects are already platform-neutral and require no changes for MAUI:

- `VirtualPrinter.HostedService.TcpSystem` (TCP listener — works on Windows and macOS)
- `VirtualPrinter.Handler.*` (ZPL, HostStatus, Nop request handlers)
- `Labelary.Abstractions` / `Labelary.Service` (HTTP client — except `ErrorImage.cs`)
- `ImageCache.Abstractions` / `ImageCache.Repository` (PNG file storage)
- `VirtualPrinter.Db.Abstractions` / `VirtualPrinter.Db.Ef` (EF Core + SQLite)
- `VirtualPrinter.PublishSubscribe` (Prism event definitions)
- `VirtualPrinter.Repository.*` (host addresses, label parameters)
- `VirtualPrinter.TemplateManager` (ZPL template management)
- `VirtualPrinter.FontService` (custom font management)
- `VirtualPrinter.Tests` (unit tests — no UI dependency)
- `VirtualPrinter.TcpClient` (test TCP client)

---

## Recommended Migration Approach

Given the Windows-centric nature of the application, the recommended approach is a
**Windows-targeted MAUI application** with optional macOS support:

### Phase 1 — Replace platform-specific service code (low risk)

1. Replace `ErrorImage.cs` `System.Drawing` usage with `SkiaSharp` or `Microsoft.Maui.Graphics`.
2. Replace `System.Configuration.ApplicationSettingsBase` in `VirtualPrinter.ApplicationSettings`
   with JSON-based settings stored in the app data directory.

### Phase 2 — Migrate the host bootstrapper

3. Replace `Diamond.Core.Wpf` / `HostedApplication` with a standard `MauiAppBuilder` that
   registers the same `IHostedService` implementations and Diamond.Core JSON service files.
4. Replace `Prism.Wpf` with `Prism.Maui`.

### Phase 3 — Recreate the UI

5. Create MAUI XAML equivalents for all 11 views, reusing the existing ViewModel classes
   (which have no WPF dependencies beyond `System.Windows.Input.ICommand` — available
   cross-platform via `CommunityToolkit.Mvvm` or Prism).
6. Replace WPF value converters with MAUI converters.
7. Replace `DataGrid` usages with `CollectionView` or a third-party grid control
   (`Syncfusion.Maui.DataGrid`, `DevExpress MAUI`, etc.).

### Phase 4 — Platform-specific features

8. Wrap `System.Drawing.Printing` (physical label printer) in `Platforms/Windows/` using
   `[SupportedOSPlatform("windows")]` to keep it functional on Windows MAUI and silently
   disabled on macOS.
9. Update the installer: switch to MSIX for Windows, and generate a macOS `.pkg` if macOS
   is targeted.

---

## Effort Summary

| Area | Effort | Risk |
|---|---|---|
| UI views (11 XAML views) | High | Medium — MAUI XAML is similar but different |
| Host bootstrapper | Medium | Low |
| MVVM (Prism.Wpf → Prism.Maui) | Medium | Low — same `IEventAggregator` API |
| Error image (`System.Drawing` → SkiaSharp) | Medium | Low |
| Application settings | Low | Low |
| Physical printer (Windows guard) | Low–Medium | Low |
| Reusable business logic (no change) | None | None |
| Installer (MSIX) | Low | Low |

**Overall:** The migration is well within reach for a single developer over several weeks. The
largest single task is recreating 11 views in MAUI XAML. All business logic, data access, TCP
networking, and Labelary integration can be carried forward unchanged.

---

## Alternatives to Consider

| Alternative | Pros | Cons |
|---|---|---|
| Stay on WPF | Zero migration cost; WPF is fully supported on .NET 10 | Windows-only forever; no path to macOS |
| MAUI (Windows + macOS) | Modernised framework; macOS support | Full UI rewrite required |
| Avalonia UI | Cross-platform WPF-like XAML; closer to WPF dialect | Third-party framework; smaller ecosystem |
| Blazor Hybrid (MAUI) | Web-based UI reuse | No existing web UI to reuse; more complex hosting |

For a developer tool that is predominantly used on Windows, **staying on WPF** remains a
perfectly valid choice. The primary driver for a MAUI migration would be macOS support
(where developers also test Zebra-label-printing applications).
