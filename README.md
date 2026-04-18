# sbclient

Blazor-based web client for `sbmud`, intended to connect to an LDMud-powered MUD while keeping the telnet socket on the server side.

## Current architecture

The first implementation slice uses a **Blazor Web App**:

- the browser renders the terminal UI, command input, and future media area
- the ASP.NET Core server owns the TCP/telnet connection to the MUD
- telnet negotiation and subnegotiation frames are separated from plain text before rendering
- ANSI color sequences are rendered into styled terminal spans
- a side-channel decoder boundary is in place for future maps, images, and other non-text payloads

The app now also includes a small **Interactive WebAssembly** learning page at `/wasm-learning`:

- `/` remains the reference **Interactive Server** page and talks directly to `MudClientSession`
- `/wasm-learning` runs its component code in the browser and fetches a small browser-safe snapshot from `/api/learning/browser-boundary`
- the MUD TCP/telnet connection still stays on the server
- the terminal bundles a compressed FiraCode Nerd Font fallback from `SbClient.Web/wwwroot/fonts/` so glyphs still render when no Nerd Font is installed locally

## Local development

```bash
dotnet restore sbclient.slnx
dotnet build sbclient.slnx
dotnet run --project SbClient.Web
```

Open the app in the browser, enter the MUD host and port, and connect from the UI.

## Tests

Run the full suite:

```bash
dotnet test sbclient.slnx
```

Run a single test by name:

```bash
dotnet test sbclient.slnx --filter "FullyQualifiedName~AnsiTranscriptParserTests"
```
