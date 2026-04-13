# Copilot Instructions

## Build, test, and lint commands

Build and restore from the solution root:

```bash
dotnet restore sbclient.slnx
dotnet build sbclient.slnx
```

Run the full test suite:

```bash
dotnet test sbclient.slnx
```

Run a single test by name:

```bash
dotnet test sbclient.slnx --filter "FullyQualifiedName~AnsiTranscriptParserTests"
```

There is no separate lint command checked in yet.

## High-level architecture

`sbclient` is a **Blazor Web App** where the ASP.NET Core host also acts as the telnet gateway to the MUD.

1. `SbClient.Web` renders the browser UI for connection controls, terminal output, command input, and the future media pane.
2. `MudClientSession` owns the server-side telnet session and keeps raw socket traffic out of the browser.
3. `TelnetFrameParser` separates telnet negotiation and subnegotiation frames from text output.
4. `AnsiTranscriptParser` turns ANSI-formatted transcript text into styled spans for the terminal renderer.
5. `IMudSideChannelDecoder` is the extension point for future maps, images, or other out-of-band payloads carried alongside telnet text.

`SbClient.Web.Tests` currently covers the protocol parsing layer first, so stream and ANSI behavior should usually be tested there before adding more UI complexity.

## Key conventions

The repository is pinned to the .NET 10 SDK through `global.json`, and the Copilot setup workflow installs the same SDK line.

The browser UI should not connect directly to the MUD. Keep network protocol code on the server side and send only browser-safe UI state across the Blazor circuit.

Rich content is intentionally modeled as a separate side-channel concern. Extend `IMudSideChannelDecoder` and `MudMediaItem` for maps or images instead of mixing binary or structured payload handling into the terminal text renderer.

Nerd Font glyph support is expected at the rendering layer through the terminal font stack in `SbClient.Web/wwwroot/app.css`; text-processing code should preserve Unicode glyphs rather than normalizing or filtering them.
