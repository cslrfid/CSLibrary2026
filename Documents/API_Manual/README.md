# CSLibrary2026 API Reference Manual

This folder holds the API Reference Manual of the CSLibrary2026 library. It is
written for the **application developer**: everything that is needed to write an
application with the library is documented here, and nothing about the protocol,
the chipset or the transport that the library uses internally.

## Opening the manual

Open `index.html` in a browser. The search works directly from the file system:
the search index is loaded with a script tag, so no web server is needed.

```
Documents/API_Manual/index.html
Documents/API_Manual/search.html
```

The manual is also published on GitHub Pages; see
`.github/workflows/publish-docs.yml`.

## The content

| Folder | Content |
|---|---|
| `index.html` | The table of contents of the manual. |
| `guides/` | The application guides. They are written by hand. |
| `appendix/` | The reader capability matrix and the migration guide. |
| `api/` | The generated API reference. **Do not edit by hand.** |
| `assets/` | The style sheet, the navigation and the search. |
| `tools/CslApiDump/` | The generator of the API reference and of the search index. |
| `api-layout.json` | Decides which types the manual contains and how they are grouped. |

## Building

```powershell
pwsh -File Documents/API_Manual/build-docs.ps1
```

The script

1. builds the library with the XML documentation file,
2. builds the `CslApiDump` tool,
3. renders the pages of `api/` and refreshes `assets/nav.js`,
4. rebuilds `searchindex.js`.

Add `-Serve 8080` to browse the manual on another machine.

Requirements: the .NET SDK (the .NET 10 target is used to build the library) and
PowerShell. Nothing else.

## Maintaining the manual

### The API reference pages

The pages of `api/` are generated from the compiled library, so the signatures and
the descriptions always match the code. To change them:

1. change the XML documentation comments in `Source/`,
2. run `build-docs.ps1`.

The audit tool reports the members of the public API that are not documented:

```powershell
pwsh -File Documents/API_Manual/tools/audit-xml-doc.ps1
```

### The guides

The guides are plain HTML. They share the layout of the generated pages, and the
navigation is rendered in the browser from `assets/nav.js`, so a guide does not
have to be rewritten when a page is added.

A guide is a single file under `guides/`. Give every heading an `id` so that a
search result can jump to it:

```html
<h2 id="connect">Connect</h2>
```

### The layout

`api-layout.json` decides what the manual contains:

* `pages` lists the pages, the types that each page documents and the member
  groups of a type,
* `obtain` describes how an application obtains an instance of a type,
  for example `reader.rfid`,
* `ignore` lists the members that are public but are not part of the application
  API, such as the per model dispatch helpers.

The generator prints a warning when a member of a documented type is not assigned
to any group, so that a new API member is never silently left out of the manual.

### The search

`searchindex.js` is generated from the HTML of the manual. It holds one entry per
section of every page and an inverted index of the terms. The search matches the
whole word and also the parts of a camel case identifier, so that a search for
`SetPowerLevel` and a search for `power` both find the member.
