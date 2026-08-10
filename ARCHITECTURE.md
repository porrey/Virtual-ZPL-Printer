# Virtual ZPL Printer — Architecture

## Overview

Virtual ZPL Printer is a Windows desktop application that emulates an Ethernet-connected Zebra label printer. It accepts incoming TCP connections carrying ZPL (Zebra Programming Language) data, forwards that data to the third-party [Labelary REST API](http://labelary.com/service.html) to render PNG label images, and displays the resulting images in a WPF UI. It also provides an embedded HTTP web interface for managing stored ZPL formats and GRF graphics, and implements Zebra flash-memory simulation so that labels using `^DF`/`^XF` format templates and `~DG`/`^XG` graphic recalls render correctly without physical hardware.

The application is intended for developers testing applications that produce barcode labels without requiring a physical Zebra printer.

---

## Technology Stack

| Layer | Technology |
|---|---|
| UI framework | WPF (.NET 10) |
| Application host | `Microsoft.Extensions.Hosting` (Generic Host) |
| Dependency injection | `Microsoft.Extensions.DependencyInjection` + Diamond.Core extension helpers |
| MVVM | Prism (`Prism.Events`, `IEventAggregator`) |
| Database | SQLite via Entity Framework Core (`Microsoft.Data.Sqlite`) |
| Logging | Serilog (file sink, rolling by minute) |
| HTTP client | `System.Net.Http.HttpClient` |
| HTTP server | `System.Net.HttpListener` |
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
│   ├── VirtualPrinter.HostedService.TcpSystem/    # TCP listener (ZPL ingestion)
│   ├── VirtualPrinter.HostedService.HttpSystem/   # HTTP web interface
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
├── — Flash Memory Simulation —
│   ├── VirtualPrinter.GrfStorageService/   # ~DG / ^XG graphic storage
│   └── VirtualPrinter.ZplFormatService/    # ^DF / ^XF format template storage
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

- **`TcpListenerService`** — runs a `System.Net.Sockets.TcpListener` loop, accepts client connections, and dispatches ZPL payloads through the request handler chain.
- **`HttpListenerService`** — runs a `System.Net.HttpListener` on a configurable port (default 9200) serving the browser-based web interface. Started and stopped alongside the TCP listener via `StartEvent`/`StopEvent`. Can be disabled entirely via Global Settings.
- **`PrintServer`** — subscribes to the `LabelCreatedEvent` and forwards successfully rendered labels to a configured physical printer via `System.Drawing.Printing.PrintDocument`.

### 2. Event-Driven Communication (Prism `IEventAggregator`)

Components are decoupled via publish/subscribe events defined in `VirtualPrinter.PublishSubscribe`:

| Event | Purpose |
|---|---|
| `StartEvent` | UI requests the TCP and HTTP listeners to start |
| `StopEvent` | UI requests the listeners to stop |
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
| `MainWindow` | Primary shell, printer list, label preview grid, web interface link |
| `ConfigurationView` | Manage printer configurations |
| `EditPrinterView` | Add / edit a single printer configuration |
| `EditFiltersView` | Manage ZPL find-replace filters (regex supported) |
| `GlobalSettingsView` | Labelary API settings, TCP socket settings, HTTP interface settings |
| `FontManagerView` | Install / manage custom TrueType fonts |
| `SendTestView` | Send raw ZPL for testing |
| `TestLabelaryView` | Direct Labelary connectivity test |
| `ZplView` | Inline ZPL editor / warning viewer |
| `AboutView` / `SplashView` | Metadata and splash screen |

### 5. Repository Pattern

All persistence uses interfaces defined in `*.Abstractions` projects and implemented in concrete `*.Repository` or `*.Db.Ef` projects:

- **`IPrinterConfigurationRepository`** → `PrinterConfigurationRepository` (EF Core / SQLite).
- **`IImageCacheRepository`** → `ImageCache.Repository` (file-system PNG store).
- **`ISettings`** → `VirtualPrinter.ApplicationSettings` (user-scoped application settings).
- **`IHostAddressRepository`** → `VirtualPrinter.Repository.HostAddresses` (network interfaces).
- **`ILabelParametersRepository`** → `VirtualPrinter.Repository.LabelParameters`.

---

## Data Flow

### TCP Label Ingestion

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
  │       ├── GrfStorageService    (captures ~DG blobs → disk; injects ^XG recalls)
  │       ├── ZplFormatService     (captures ^DF templates; expands ^XF + ^FN fields)
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

### HTTP Web Interface

```
Browser / ZebraLabelUpdate
        │  HTTP (default port 9200)
        ▼
HttpListenerService
        │
        ├── GET  /printer          → HTML index page (format + GRF listings)
        ├── GET  /printer/dir      → JSON format directory (ZebraLabelUpdate compatible)
        ├── GET  /printer/zpl      → ZPL format file content
        ├── POST /printer/zpl      → Save / update a ZPL format file
        ├── POST /printer/zpl/meta → Save field metadata for a format
        ├── POST /printer/zpl/new  → Create a new ZPL format file
        ├── GET  /printer/grf      → GRF graphic rendered to PNG via Labelary
        ├── POST /printer/upload   → Upload a ZPL or GRF file to flash storage
        ├── POST /printer/delete   → Delete a file from flash storage
        ├── POST /printer/preview  → Render arbitrary ZPL to PNG via Labelary
        └── POST /printer/image/upload → Upload a PNG/image file
```

The HTTP interface is enabled by default and its port (default 9200) is configurable from **Options → Global Settings → HTTP Settings**. The interface can also be disabled entirely from that dialog.

---

## Flash Memory Simulation

Zebra printers have on-board flash storage that holds reusable format templates (ZPL) and graphics (GRF). Virtual ZPL Printer simulates this storage so that ZPL jobs using these features render correctly.

### GRF Graphics (`VirtualPrinter.GrfStorageService`)

- **Download** (`~DG`): When a ZPL job contains `~DG<device>:<filename>,<data>`, the binary GRF image is extracted and saved to `Documents\Virtual ZPL Printer\Graphics\`.
- **Recall** (`^XG`): Before forwarding ZPL to Labelary, any `^XG<device>:<filename>` references are resolved by injecting the corresponding `~DG` blob inline, so Labelary receives a self-contained ZPL payload.
- **Multi-graphic regex**: The capture pattern uses a negative lookahead (`(?!~DG)`) to correctly split multiple `~DG` blocks within a single ZPL file.

### ZPL Format Templates (`VirtualPrinter.ZplFormatService`)

- **Download** (`^DF`): When a ZPL job contains `^DF<device>:<filename>`, the template body (between `^DF` and `^XZ`) is saved to `Documents\Virtual ZPL Printer\Formats\`.
- **Recall** (`^XF`): When a print job contains `^XF<device>:<filename>`, the stored template is loaded, `^FN` field-number placeholders are substituted with `^FD` field-data values from the current job, and the expanded ZPL is passed to Labelary.

Both services are wired into `ZplRequestHandler` before the Labelary call. Any ZPL job containing `~DG` or `^DF` automatically populates flash storage as a side-effect of normal printing.

---

## HTTP Web Interface

`VirtualPrinter.HostedService.HttpSystem` provides a browser-accessible front-end for managing the virtual printer's flash storage. It is a first-class feature of the application, not just a debugging aid.

### Capabilities

| Capability | Description |
|---|---|
| Format library | Browse, view, upload, delete, and edit stored ZPL format files |
| GRF graphic library | Browse, view (rendered to PNG via Labelary), upload, and delete GRF graphics |
| Live ZPL preview | POST arbitrary ZPL to `/printer/preview` and receive a rendered PNG |
| ZebraLabelUpdate compatibility | `/printer/dir` returns a JSON listing in the format expected by ZebraLabelUpdate's printer browse dialog |
| Image upload | Upload PNG or image files directly for use in labels |

### Configuration

- **Enable/disable**: Global Settings → HTTP Settings → "Enable HTTP Interface" checkbox.
- **Port**: Global Settings → HTTP Settings → "HTTP Port" field (default 9200).
- Changes take effect on the next printer start (Stop then Start).
- The main window displays a clickable URL (`http://localhost:<port>/printer`) when the printer is running and HTTP is enabled.

### ZebraLabelUpdate Notes

- Set the printer address in ZebraLabelUpdate to `localhost:9200` (or the configured port).
- The **Process** button converts `^FD<%N%>` field markers to `^FN` placeholders correctly.
- The **Save** button sends updated ZPL back to the printer.
- **Known issue**: ZebraLabelUpdate's Save operation may connect to port 9100 (hardcoded TCP) rather than the HTTP port, so the raw ZPL reaches the TCP listener but the stored format file is not updated via HTTP. Workaround: use the browser upload at `/printer` to replace a format file manually.

---

## Database

The application uses a local **SQLite** database (`db.sqlite` in the application folder) managed by Entity Framework Core via `VirtualPrinterContext`. Two tables are defined:

- **`PrinterConfiguration`** — stores named printer profiles (IP, port, label dimensions, resolution, rotation, filters, physical printer JSON, image output path). Five default profiles are seeded on first run.
- **`ApplicationVersion`** — single-row version marker used for schema migration checks.

Schema upgrades are handled in `CheckUpgradeAsync()` using raw SQL `ALTER TABLE` statements rather than EF migrations.

---

## Configuration & Settings

### `appsettings.json`

Holds the SQLite connection string, Serilog configuration, and the `HttpSystem:Port` fallback value. The HTTP port in `appsettings.json` is only used if the user setting has never been saved; the user-scoped setting takes precedence once Global Settings has been opened and saved.

### `ISettings` (User-Scoped Application Settings)

Persisted via `System.Configuration.ApplicationSettingsBase`. Managed through **Options → Global Settings**:

**TCP Settings**
- `ReceiveTimeout` / `SendTimeout` — socket timeouts in milliseconds
- `ReceiveBufferSize` / `SendBufferSize` — socket buffer sizes (-1 = OS default)
- `NoDelay` — disables Nagle algorithm
- `Linger` / `LingerTime` — socket linger behaviour
- `ReceivedDataEncoding` — encoding for incoming ZPL bytes
- `MaximumWaitTime` — max ms to wait for data before closing the connection

**HTTP Settings**
- `HttpEnabled` — enables/disables the HTTP web interface (default: true)
- `HttpPort` — HTTP listener port (default: 9200)

**Labelary Settings**
- `ApiUrl` — Labelary REST endpoint
- `ApiMethod` — HTTP method (`POST` or `GET`)
- `ApiLinting` — enables ZPL linting in Labelary responses

### Logging

Serilog writes rolling log files to `%USERPROFILE%\Documents\Virtual ZPL Printer\Logs\` (1 GB per file, up to 10 retained files).

---

## Font Management

`VirtualPrinter.FontService` allows users to install custom TrueType fonts so that ZPL `^A` font commands can reference them. Before sending ZPL to Labelary, `LabelService` calls `IFontService.GetReferencedFontsAsync()` and `ApplyReferencedFontsAsync()` to embed font data directly into the ZPL payload.

---

## ZPL Filters

Each printer configuration supports a JSON-encoded list of find/replace rules (regular expressions supported). Filters are applied to incoming ZPL by `LabelService` before the payload is forwarded to Labelary, enabling on-the-fly ZPL transformation without modifying the source application.

---

## Test Infrastructure

- **`VirtualPrinter.TcpClient`** — a lightweight test client that connects to the virtual printer over TCP and sends ZPL payloads.
- **`Labelary Example`** — a standalone console project demonstrating direct use of `ILabelService`.
- **`SendTestView` / `TestLabelaryView`** — built-in GUI tools for sending ad-hoc ZPL and verifying Labelary connectivity.

---

## Localisation

String resources live in `Properties/Strings.resx` (and locale-specific variants such as `Strings.es.resx`, `Strings.uk.resx`) in the following projects:

- `VirtualPrinter`
- `ImageCache.Repository`
- `VirtualPrinter.Db.Ef`
- `VirtualPrinter.Repository.LabelParameters`

The active culture is set at startup in `App.xaml.cs` and applied to all WPF framework elements and the resource manager.

---

## Installer

`VirtualPrinter-Setup/` (WiX) produces a standard Windows MSI. A companion `Setup.exe` bootstrapper ensures the .NET runtime is present before installation.
