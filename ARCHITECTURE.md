# Virtual ZPL Printer — Architecture

## Overview

Virtual ZPL Printer is a Windows desktop application that emulates an Ethernet-connected Zebra label printer. It listens for incoming TCP connections carrying ZPL (Zebra Programming Language) data, forwards that data to the third-party [Labelary REST API](http://labelary.com/service.html) to render PNG label images, and displays the resulting images in a WPF UI. It is intended for testing applications that produce barcode labels without requiring physical Zebra hardware.

---

## Technology Stack

| Layer | Technology |
|---|---|
| UI framework | WPF (.NET 8) |
| Application host | `Microsoft.Extensions.Hosting` (Generic Host) |
| Dependency injection | `Microsoft.Extensions.DependencyInjection` + Diamond.Core extension helpers |
| MVVM | Prism (`Prism.Events`, `IEventAggregator`) |
| Database | SQLite via Entity Framework Core (`Microsoft.Data.Sqlite`) |
| Logging | Serilog (file sink, rolling by minute) |
| HTTP client | `System.Net.Http.HttpClient` |
| Unit conversion | UnitsNet |
| Serialization | Newtonsoft.Json |
| Installer | WiX / MSI (`VirtualPrinter-Setup`) |

---

## Solution Layout

The single Visual Studio solution (`ZPL Printer Solution.sln`) lives under `Src/Virtual Printer Solution/` and contains 20+ projects grouped by concern.

```
Src/Virtual Printer Solution/
├── VirtualPrinter/                        # WPF host application (entry point)
│
├── — Hosted Services —
│   ├── VirtualPrinter.HostedService.TcpSystem/    # TCP listener
│   └── VirtualPrinter.HostedService.PrintSystem/  # Physical printer bridge
│
├── — Request Handlers —
│   ├── VirtualPrinter.Handler.Abstractions/  # Base classes & interfaces
│   ├── VirtualPrinter.Handler.Zpl/           # Handles ZPL payloads
│   ├── VirtualPrinter.Handler.HostStatus/    # Responds to host-status queries
│   └── VirtualPrinter.Handler.Nop/           # No-op / fallback handler
│
├── — Labelary Integration —
│   ├── Labelary.Abstractions/   # ILabelService, ILabelConfiguration interfaces
│   ├── Labelary.Service/        # HTTP client wrapper for Labelary REST API
│   └── Labelary Example/        # Standalone usage example
│
├── — Image Cache —
│   ├── ImageCache.Abstractions/   # IImageCacheRepository interface
│   └── ImageCache.Repository/    # PNG file persistence
│
├── — Database —
│   ├── VirtualPrinter.Db.Abstractions/  # IPrinterConfiguration, IApplicationVersion
│   └── VirtualPrinter.Db.Ef/           # EF Core context + migrations (SQLite)
│
├── — Supporting Services —
│   ├── VirtualPrinter.PublishSubscribe/         # Prism event definitions
│   ├── VirtualPrinter.ApplicationSettings/      # Global settings repository
│   ├── VirtualPrinter.Repository.HostAddresses/ # Network interface enumeration
│   ├── VirtualPrinter.Repository.LabelParameters/ # Label config repository
│   ├── VirtualPrinter.FontService/              # Custom TrueType font management
│   └── VirtualPrinter.TemplateManager/         # ZPL test-label templates
│
├── VirtualPrinter.TcpClient/     # Developer test client
└── VirtualPrinter-Setup/         # WiX MSI installer project
```

---

## Key Architectural Patterns

### 1. Generic Host + Hosted Services

`App.xaml.cs` extends `HostedApplication` (from `Diamond.Core.Wpf`) which wraps a standard `Microsoft.Extensions.Hosting` Generic Host. All long-running background work runs as `IHostedService` implementations:

- **`TcpListenerService`** — runs a `System.Net.Sockets.TcpListener` loop and accepts client connections.
- **`PrintServer`** — subscribes to the `LabelCreatedEvent` and forwards successfully rendered labels to a configured physical printer via `System.Drawing.Printing.PrintDocument`.

### 2. Event-Driven Communication (Prism `IEventAggregator`)

Components are decoupled via publish/subscribe events defined in `VirtualPrinter.PublishSubscribe`:

| Event | Purpose |
|---|---|
| `StartEvent` | UI requests the TCP listener to start |
| `StopEvent` | UI requests the TCP listener to stop |
| `RunningStateChangedEvent` | Listener reports its running/error state back to the UI |
| `LabelCreatedEvent` | A label image has been rendered and saved; triggers UI refresh and optional physical printing |
| `PrintRequestEvent` | Carries the raw ZPL payload for processing |
| `ChangeEvent` | Generic change notification |
| `TimerEvent` | Periodic tick for UI updates |
| `ViewMetaDataEvent` / `WindowHiddenEvent` | Window lifecycle coordination |

### 3. Chain-of-Responsibility Request Handlers

Incoming TCP data is dispatched through a prioritised chain of `IRequestHandler` implementations. Each handler:

1. Implements `CanHandleRequestAsync(string requestData)` to indicate whether it can process the request.
2. If accepted, executes `HandleRequest(...)` and returns a `(bool closeConnection, string responseData)` tuple.

Concrete handlers:

| Handler | Trigger |
|---|---|
| `ZplRequestHandler` | Accepts all ZPL payloads (catch-all, lowest priority) |
| `HostStatusRequestHandler` | Responds to Zebra host-status query commands |
| `NopRequestHandler` | No-op fallback |

The abstract base `TemplateRequestHandler` (in `VirtualPrinter.Handler.Abstractions`) holds shared dependencies (`ILabelService`, `IImageCacheRepository`, `IEventAggregator`).

### 4. MVVM (WPF)

All views follow the MVVM pattern. The `Views/` folder contains XAML files; corresponding ViewModels in `ViewModels/` (split into `Primary/` and `Secondary/` sub-folders) contain all logic. Key views:

| View | Purpose |
|---|---|
| `MainWindow` | Primary shell, printer list, label preview grid |
| `ConfigurationView` | Manage printer configurations |
| `EditPrinterView` | Add / edit a single printer configuration |
| `EditFiltersView` | Manage ZPL find-replace filters (regex supported) |
| `GlobalSettingsView` | Labelary URL, HTTP method, linting toggle |
| `FontManagerView` | Install / manage custom TrueType fonts |
| `SendTestView` | Send raw ZPL for testing |
| `TestLabelaryView` | Direct Labelary connectivity test |
| `ZplView` | Inline ZPL editor |
| `AboutView` / `SplashView` | Metadata and splash screen |

### 5. Repository Pattern

All persistence uses interfaces defined in `*.Abstractions` projects and implemented in concrete `*.Repository` or `*.Db.Ef` projects:

- **`IPrinterConfigurationRepository`** → `PrinterConfigurationRepository` (EF Core / SQLite).
- **`IImageCacheRepository`** → `ImageCache.Repository` (file-system PNG store).
- **`IApplicationSettingsRepository`** → `VirtualPrinter.ApplicationSettings` (app config file).
- **`IHostAddressRepository`** → `VirtualPrinter.Repository.HostAddresses` (network interfaces).
- **`ILabelParametersRepository`** → `VirtualPrinter.Repository.LabelParameters`.

---

## Data Flow

```
External App (ZPL sender)
        │  TCP (default port 9100)
        ▼
TcpListenerService          ← started/stopped via StartEvent / StopEvent
        │  accepts TcpClient
        ▼
TcpListenerClientHandler    (scoped per connection)
        │  reads raw bytes → string
        ▼
IRequestHandler chain
  ├── HostStatusRequestHandler  (if host-status query)
  ├── ZplRequestHandler         (ZPL payload)
  │       │
  │       │  calls LabelService.GetLabelsAsync()
  │       ▼
  │   Labelary REST API  (HTTP POST or GET)
  │       │  returns PNG bytes
  │       ▼
  │   ImageCacheRepository.StoreLabelImagesAsync()
  │       │  writes PNG files to disk
  │       ▼
  │   Publishes LabelCreatedEvent
  │       │
  │       ├──▶ MainViewModel  (updates UI image list)
  │       └──▶ PrintServer    (optional physical print)
  └── NopRequestHandler     (fallback / ignored data)
```

---

## Database

The application uses a local **SQLite** database (`db.sqlite` in the application folder) managed by Entity Framework Core via `VirtualPrinterContext`. Two tables are defined:

- **`PrinterConfiguration`** — stores named printer profiles (IP, port, label dimensions, resolution, rotation, filters, physical printer JSON, image output path). Five default profiles are seeded on first run.
- **`ApplicationVersion`** — single-row version marker used for schema migration checks.

Schema upgrades are handled manually in `CheckUpgradeAsync()` using raw SQL `ALTER TABLE` statements rather than EF migrations.

---

## Configuration & Logging

- `appsettings.json` in the application folder holds the SQLite connection string and Serilog configuration.
- Serilog writes rolling log files to `%USERPROFILE%\Documents\Virtual ZPL Printer\Logs\` (1 GB per file, up to 10 retained files).
- Global application settings (Labelary base URL, HTTP method, linting on/off) are persisted separately via `IApplicationSettingsRepository`.

---

## Font Management

`VirtualPrinter.FontService` allows users to install custom TrueType fonts so that ZPL `^A` font commands can reference them. Before sending ZPL to Labelary, `LabelService` calls `IFontService.GetReferencedFontsAsync()` and `ApplyReferencedFontsAsync()` to embed font data directly into the ZPL payload.

---

## ZPL Filters

Each printer configuration supports a JSON-encoded list of find/replace rules (regular expressions supported). Filters are applied to incoming ZPL by `LabelService` before the payload is forwarded to Labelary, enabling on-the-fly ZPL transformation without modifying the source application.

---

## Test Infrastructure

- **`VirtualPrinter.TcpClient`** — a lightweight test client that connects to the virtual printer over TCP and sends ZPL payloads. Useful for automated or manual testing.
- **`Labelary Example`** — a standalone console project demonstrating direct use of `ILabelService`.
- **`SendTestView` / `TestLabelaryView`** — built-in GUI tools for sending ad-hoc ZPL and verifying Labelary connectivity.

---

## Localisation

String resources live in `Properties/Strings.resx` (and locale-specific variants such as `Strings.es.resx`, `Strings.uk-UA.resx`) in the following projects:

- `VirtualPrinter`
- `ImageCache.Repository`
- `VirtualPrinter.Db.Ef`
- `VirtualPrinter.Repository.LabelParameters`

The active culture is set at startup in `App.xaml.cs` and applied to all WPF framework elements and the resource manager.

---

## Installer

`VirtualPrinter-Setup/` (WiX) produces a standard Windows MSI. A companion `Setup.exe` bootstrapper ensures the .NET 8 runtime is present before installation.

---

## Flash Memory Simulation (NFRC / Labelary Extensions)

A set of additions was made to support Zebra flash-memory workflows used by the NFRC label templates. These are not part of the original open-source project.

### What Was Added

| Project | Purpose |
|---|---|
| `VirtualPrinter.GrfStorageService` | Captures `~DG` (Download Graphics) blobs from incoming ZPL and persists them as files under `Documents\Virtual ZPL Printer\Graphics\`. Before forwarding ZPL to Labelary, injects stored blobs for any `^XG` (recall graphic) references so Labelary can render them. |
| `VirtualPrinter.ZplFormatService` | Captures `^DF` (Download Format) commands and stores the template body under `Documents\Virtual ZPL Printer\Formats\`. On `^XF` (recall format) jobs, loads the template, substitutes `^FN` field-number placeholders with the `^FD` field-data values from the print job, and expands the result before passing it to Labelary. |
| `VirtualPrinter.HostedService.HttpSystem` | Runs a raw `TcpListener` on port 9200 with protocol detection. HTTP requests (`GET`/`POST`) are served as a Zebra-compatible web UI; raw ZPL connections are saved through the flash-storage pipeline. Endpoints: `/printer` (browser index), `/printer/dir` (ZebraLabelUpdate-compatible format listing), `/printer/zpl` (format content), `/printer/grf` (Labelary-rendered GRF preview), `/printer/upload` (file upload), `/printer/delete` (file delete). |

Both `GrfStorageService` and `ZplFormatService` are wired into `ZplRequestHandler` before the Labelary call, so any ZPL job containing `~DG` or `^DF` automatically populates flash storage as a side-effect of normal printing.

### Testing

**Send a GRF template to flash:**
```powershell
# Replace <file> with a .zpl containing ~DG lines
$zpl = Get-Content <file> -Raw
$tcp = [System.Net.Sockets.TcpClient]::new('127.0.0.1', 9100)
$stream = $tcp.GetStream()
$bytes = [System.Text.Encoding]::UTF8.GetBytes($zpl)
$stream.Write($bytes, 0, $bytes.Length)
$tcp.Close()
```

**Verify storage and preview via browser:**
Navigate to `http://localhost:9200/printer`. The page lists stored Formats (ZPL) and Graphics (GRF). Each GRF has a **View** link that renders the image live via Labelary. Files can be uploaded directly or deleted from this page.

**ZebraLabelUpdate compatibility:**
- Set the printer name in ZebraLabelUpdate to `localhost:9200`.
- The **Process** button converts `^FD<%N%>` field markers to `^FN` placeholders correctly.
- The **Save** button sends ZPL back via TCP; the format file is updated in flash storage and the updated content is visible on the next reload from ZebraLabelUpdate.
  - **Known issue:** As of this writing, ZebraLabelUpdate Save does not update the stored file. Root cause is under investigation — Delphi's `TClientSocket` may be connecting to port 9100 (hardcoded) rather than 9200, so the raw ZPL reaches the standard TCP listener but the `^DF` job is treated as a storage-only job and skips Labelary without updating the HTTP-served copy. Workaround: use the browser upload at `http://localhost:9200/printer` to replace a format file manually.

**Regex for multi-GRF ZPL files** (`GrfStorageService`):
```
~DG(?<device>[A-Z]):(?<filename>[\w]+\.GRF),(?:(?!~DG)[^\^])+
```
The negative lookahead `(?!~DG)` ensures each `~DG` block is captured separately even when multiple graphics appear in a single ZPL file.
