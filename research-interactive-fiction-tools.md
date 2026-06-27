# Interactive Fiction / Gamebook Tools: Market Research

Analysis of five leading tools for creating interactive fiction, branching narratives, and gamebooks. Ordered roughly from simplest to most complex.

---

## 1. Twine (twinery.org)

**Type:** Open-source, web-based + desktop, visual story editor  
**License:** GPL v3 (MIT for story formats)  
**Current Version:** 2.12.0 (April 2026)

### Overview

Twine is the dominant tool for choice-based interactive fiction (IF). It is a **graph-based visual editor** where each "passage" is a node containing text and hyperlinks. Passages are displayed as cards connected by arrows on a canvas. The core value proposition is zero-code entry: you can write a simple branching story using only link syntax (`[[Go left]]`), and publish directly to HTML.

### What It Does Well

- **Visual graph editor is the centerpiece.** Every passage is a card on a canvas. Links between passages are shown as arrows. Authors see the entire structure of their story at a glance — this is Twine's killer feature.
- **Zero-code authoring.** For simple stories, you never write a line of code. `[[Link text]]` and `[[Link text->PassageName]]` are the entire syntax.
- **Publishes directly to HTML.** A Twine story is a single, self-contained HTML file that runs in any browser. No build step, no server, no runtime dependencies. This is the simplest deployment model of any tool.
- **Story formats as pluggable engines.** Twine's story formats (Harlowe, SugarCube, Snowman, Chapbook) act like game engines. Each one has a different philosophy — Harlowe is beginner-friendly with syntactic sugar; SugarCube is extremely powerful and hackable (TiddlyWiki lineage); Snowman is minimalist and exposes raw JS; Chapbook is new and opinionated. Authors can swap formats mid-project.
- **WYSIWYG-ish editing.** Each passage is edited inline with a rich-text-like feel (or raw markup, depending on format). Visual feedback is immediate.
- **Huge community and ecosystem.** Thousands of published works on itch.io and IFDB. Extensive tutorials, cookbooks, Discord, community forum. The Interactive Fiction Technology Foundation backs it organizationally.
- **Free for commercial use.**

### Limitations

- **No native collaboration.** Twine is single-file, single-author. There's no multi-user editing, merging, or version control. The `.twee` format can be stored in git, but the visual editor works only with the proprietary HTML-based archive.
- **Graph can become unmanageable at scale.** For stories with hundreds of passages, the canvas becomes a tangled hairball. Twine provides auto-layout (Arbor, Spring), but there's no zoom overview, minimap, or grouping/folding.
- **Limited logic visualization.** Variables, `if` statements, and scripts are embedded in passage text. They are invisible on the graph — you cannot see which passages are gated by what conditions just by looking at the canvas.
- **No flow analysis.** No dead-end detection, no unreachable passage warnings, no "all paths" testing, no simulation mode. You must playtest manually.
- **Multimedia support is awkward.** While images and audio are possible, Twine's text-first architecture makes asset-heavy projects cumbersome.
- **Story formats fragment the ecosystem.** Each format has its own syntax, macro system, and quirks. A tutorial written for SugarCube is useless for Harlowe users, and vice versa.

### Inspiration for a Web-Based Gamebook Editor

Twine is the closest analog to what you'd want to build. Its graph view is the reference implementation — **every gamebook editor should have a passage/flowchart canvas**. Key features to replicate: the passage card model, in-place editing, HTML export, and the separation between authoring tool and story format. Key improvements to consider: collaborative editing, minimap/navigation aids, inline variable/condition visualization, and flow analysis.

---

## 2. Ink / Inky (inkle Studios)

**Type:** Scripting language + desktop editor  
**License:** MIT (all components)  
**Current Version:** Ink 1.2+ (stable); Inky editor (cross-platform)

### Overview

Ink is a **markup-based scripting language** designed by inkle Studios for writing highly branching narrative in their commercial games (*80 Days*, *Sorcery!*, *Heaven's Vault*). The companion **Inky** desktop app provides a text editor with a live preview pane.

### Language Design

Ink is "markup, not programming." Text comes first; code is inserted *within* the text. Its core concepts:

- **Knots and stitches** — equivalent to chapters and subchapters. `=== knot_name ===` defines a named section.
- **Diverts** (`->`) — immediate, seamless jumps to other knots/stitches. Can happen mid-sentence with glue (`<>`).
- **Choices** (`*`) — player options. Supports one-shot (`*`), sticky/repeatable (`+`), and fallback choices. Choice text can be omitted from output with brackets.
- **Weave** — a powerful indentation-based syntax where choices and **gather** points (`-`) form branching subsections that automatically reconverge. This is ink's primary innovation: you can nest branching arbitrarily deep without needing to name every destination, and the flow always "falls" forward.
- **Variables and logic** — `VAR`, `~ temp`, `{ condition: text | else text }`, `if/else` blocks. Variables can be integers, floats, strings, diverts. Supports `and`/`or`/`not`, read counts on knots, `TURNS_SINCE()`, `CHOICE_COUNT()`.
- **Advanced features** — tunnels (reusable subroutines), threads (parallel flow strands), lists (set-based state tracking with boolean operations), functions (`=== function`), tags (`#tag`), conditional text, sequences/cycles/shuffles/once-only alternates.

### Editor (Inky)

Inky is a desktop editor (Mac/Win/Linux) built on Electron. Its features:
- **Live play pane** — as you edit, the story recompiles and the play pane updates. If you're mid-playthrough and change the script, the play pane resets to show the new content from the beginning.
- **Error highlighting** — as-you-type, with line numbers and error messages.
- **Jump-to-definition** — Alt-click a divert to navigate to its target knot/stitch.
- **Export to JSON** — Ink's compiled intermediate format, for consumption by game engines.
- **Export to web** — generates a standalone HTML page with the story playable in-browser.

### What It Does Well

- **The language is genuinely elegant.** The weave syntax is a masterclass in making branching narrative *declarative*. You write the story flow as it would be read, and the engine handles the rest.
- **Proven at scale.** Millions of words shipped in commercial titles. The language is stable, well-documented, and has years of production testing.
- **Middleware architecture.** Ink is designed to be embedded in game engines. The official Unity integration is mature. There's an Unreal integration (Inkpot). The JSON export is engine-agnostic.
- **Excellent for writers comfortable with text.** The syntax is clean, readable, and close to screenplay format. Writers can learn the basics in an afternoon.
- **Open source with MIT license.** No restrictions on commercial use. Active community.

### Limitations

- **No visual graph view.** Inky is a text editor. There is no passage map, no node visualization, no flow diagrams. You navigate entirely via the text editor, Alt-click jumps, and the "jump to knot" dropdown.
- **No flow analysis tools.** No dead-end detection, no unreachable content warnings, no simulation/testing mode within the editor beyond the live play pane.
- **No built-in collaboration.** Ink files are plain text, so git works. But there's no real-time or multi-user editing support.
- **No web-based editor.** Inky is desktop-only (Electron). There is no browser-based authoring experience.
- **Steeper initial learning curve than Twine.** While simpler than full programming languages, ink requires understanding of its specific concepts (knots, diverts, weave, gather) before you can be productive.

### Inklewriter (the simpler sibling)

Inklewriter is a free web-based tool (no longer being developed but still functional) for writing basic interactive stories with a visual flowchart. It was designed for non-technical writers. However, it lacks variables/conditions/logic and is essentially a "choose your own adventure" flowchart. Ink was built to succeed it with far more power.

### Inspiration for a Web-Based Gamebook Editor

Ink's **language design** is the best reference for a gamebook **syntax**: the weave model, the way choices and gathers form natural branching/rejoining structures, and the separation of content from logic. A web-based editor could offer a **graph view that renders ink-style structures visually** (nodes = knots/stitches, edges = diverts) while keeping the text-first authoring flow. The live preview pane from Inky is essential — test-as-you-write should be standard.

---

## 3. Yarn Spinner

**Type:** Dialogue scripting language + game engine integrations  
**License:** MIT  
**Current Version:** 3.2 (2026); supports Unity, Godot (C# + GDScript), Unreal Engine

### Overview

Yarn Spinner is a **dialogue authoring system for games**, created by Secret Lab. It originated from *Night in the Woods* and has since been used in dozens of shipped titles (DREDGE, A Short Hike, Lost in Random, Lil' Guardsman, Escape Academy, NORCO, etc.). It provides a screenplay-like language, a VS Code extension with graph visualization, and deep integrations with Unity, Godot, and Unreal.

### Language & Branching Model

Yarn files are plain text (`.yarn`) with a structure of **nodes**:

- **Nodes** are the top-level organizational unit (`title: NodeName` / `---` / `===`). They can carry arbitrary metadata headers (`key: value`).
- **Lines** are dialogue lines. Character name prefix is detected automatically: `Mae: Hello.`
- **Options** (`->`) present player choices. Options can be nested, and can have conditions (`<<if $reputation > 10>>`).
- **Commands** (`<<wait 2>>`, `<<stop>>`, custom) send instructions to the game engine.
- **Variables** (`$varname`) support numbers, strings, booleans. Typed and required (no null values in 2.0+).
- **Flow control** — `<<if>>`, `<<elseif>>`, `<<else>>`, `<<endif>>` blocks. `<<jump NodeName>>` to move between nodes.
- **Markup** — inline rich text with attributes: `[wave]hello[/wave]`, self-closing `[shake/]`, overlapping tags, variable-based text replacement (`[select]`, `[plural]`, `[ordinal]` for localization).
- **Functions** — `random()`, `dice()`, `random_range()`, `round()`, etc., plus custom functions defined in the game engine.

Branching in Yarn Spinner is **node-based with jumps**: options either lead to nested indented content or jump to another node via `<<jump>>`. This is less elegant than ink's weave for recombination but more explicit and easier to graph.

### Editor (VS Code Extension)

The official Yarn Spinner VS Code extension provides:
- **Syntax highlighting** and autocomplete for `.yarn` files.
- **Live validation** — errors detected as you type.
- **Graph View** — a visual node graph showing all nodes in the active file, with edges representing `<<jump>>` relationships. Nodes can be rearranged arbitrarily on the canvas. Clicking a node jumps the text editor to its definition.
- **"Add Node" button** — creates new nodes from the graph view.
- **"Jump to Node" dropdown** — quick navigation between nodes.

### What It Does Well

- **Game engine integration is first-class.** Yarn Spinner isn't just an authoring tool — it ships with production-ready Dialogue Runners, Variable Storage, Line Views, and Option Views for Unity and Godot. Localization is built in (string tables, plural rules, ordinal rules).
- **Professional-grade localization.** The `[plural]` and `[ordinal]` markers handle locale-specific grammar automatically. String tables are exported/imported via CSV.
- **VS Code integration.** Writers work in a familiar, powerful editor. The graph view is a bonus, not the primary interface.
- **Clear separation of concerns.** Writers write `.yarn` files. Programmers write commands and functions in C#. The engine handles the rest.
- **Active development and expanding ecosystem.** New engine support (Godot GDScript, Unreal) is recent. The team publishes monthly updates.
- **Open source, MIT licensed.** Large and active community on Discord.

### Limitations

- **Graph view is per-file and basic.** The VS Code graph shows only one `.yarn` file at a time. It doesn't span Yarn Projects (collections of scripts). Nodes show titles and edges, but no logic preview, no conditions on edges, and no minimap.
- **No standalone editor.** You must use VS Code. There's no web-based editor, no dedicated app beyond the extension. The Yarn Editor (a standalone graphical tool from the 1.x era) appears to be deprecated.
- **No flow analysis or simulation.** No dead-end detection, no "play all paths," no interactive test mode within the graph.
- **Language is opinionated for dialogue, not gamebooks.** Yarn is built around the concept of "a conversation" — lines, options, and commands. While it could technically run a gamebook, it assumes a game engine is consuming it. There's no HTML/standalone export.
- **No built-in collaboration features.** Yarn files are text, so git works, but there's no real-time collaboration or merge tooling.

### Inspiration for a Web-Based Gamebook Editor

Yarn Spinner's strongest lesson is the **VS Code graph+text dual view** — a text editor with an auto-synced graph panel. For a web-based gamebook editor, a similar split-pane approach (text on left, graph on right, both synchronized) would serve both writers who prefer text and those who prefer visual editing. The **metadata header system** (`title:` + custom key-values) is also worth adopting for attaching arbitrary data to nodes. The **built-in localization support** is a feature any professional tool should have.

---

## 4. Ren'Py

**Type:** Visual novel engine (Python-based)  
**License:** MIT (open source, free for commercial use)  
**Current Version:** 8.5.3 (May 2026)

### Overview

Ren'Py is the most widely used visual novel engine, powering over 8,000 published games. It is a full **game engine** with its own scripting language (`.rpy` files), Python scripting layer, and a launcher that manages projects, builds, and distribution. It targets desktop (Win/Mac/Linux), mobile (Android/iOS), and web (HTML5 beta).

### Language & Branching Model

Ren'Py scripts use a screenplay-like syntax with Python underneath:

- **Labels** (`label start:`) are the structural building blocks — equivalent to scenes or chapters.
- **Menus** present player choices with `menu:` blocks containing indented options that can `jump` to other labels.
- **Variables** (`default`, `$ variable = value`) track game state with full Python capabilities.
- **Conditionals** (`if/elif/else`) gate content based on player choices.
- **Jumps and calls** — `jump` is a goto; `call` pushes to a stack and can `return`. This allows subroutine-style scene reuse.

### Editing Tools

Ren'Py does **not have a visual graph editor**. The authoring workflow is:

1. **Ren'Py Launcher** — a standalone app that creates projects, launches the chosen text editor (VS Code recommended), runs lint checks, builds distributions, and manages the project lifecycle.
2. **Any text editor** — `.rpy` files are plain text with a script-like syntax. VS Code with the Ren'Py extension provides syntax highlighting and some autocomplete.
3. **Interactive Director** — a WYSIWYG tool *within the running game* for positioning images, setting transitions, and adjusting UI. It generates script code from visual manipulation. This is unique to Ren'Py and useful for asset-heavy VN development.
4. **Lint tool** — static analysis that checks for errors, unused images, missing labels, and other issues. This is Ren'Py's closest equivalent to flow analysis.
5. **Developer mode** — in-game overlay for inspecting variables, jumping to labels, and testing paths.

### What It Does Well

- **Full game engine, not just a narrative tool.** Ren'Py handles image display (sprites, backgrounds, layered images), audio (music, sound, voice), transitions (dissolve, fade, custom), save/load, preferences, and distribution. It's production-ready.
- **Python scripting** gives unlimited power. Anything Python can do, a Ren'Py game can do.
- **Mature and battle-tested.** Used in commercial hits (*Doki Doki Literature Club*, *Slay the Princess*, *VA-11 HALL-A*). Thousands of tutorials, active forums, and a large asset ecosystem.
- **Cross-platform builds** from one-click launcher. Targets Windows, macOS, Linux, Android, iOS, HTML5.
- **Strong accessibility** — self-voicing mode, screen reader support, customizable UI.

### Limitations

- **No visual graph/flowchart editing.** There's no built-in tool to visualize the branching structure of the story. You see labels and jumps as text, period. Third-party tools exist but are community-maintained and fragile.
- **No flow analysis beyond lint.** Lint checks for missing images and basic errors. It doesn't do reachability analysis, dead-end detection, or path coverage testing.
- **Heavyweight for pure-text gamebooks.** Ren'Py is built for visual novels with character art, backgrounds, and transitions. Using it for a text-only or text-primary gamebook would bring substantial overhead.
- **No web-based editor.** Everything runs locally. The launcher and game engine are desktop applications.
- **No built-in collaboration.** `.rpy` files are plain text, so git works. No multi-user editing or merging features.
- **Steep learning curve for non-programmers.** The Ren'Py language is approachable, but Python integration, screen language, and build configuration require technical knowledge.

### Inspiration for a Web-Based Gamebook Editor

Ren'Py's **Interactive Director** concept is worth studying — the idea of having a visual mode that generates script code. For a gamebook editor, this could mean: a graph view where dragging nodes/lines generates the underlying markup, and vice versa. Ren'Py's **lint system** is a good model for static analysis of branching stories (missing destinations, unused passages, potential dead ends). Ren'Py's **developer tools overlay** (in-game variable inspector, label jumping) is a strong pattern for a live testing/simulation mode.

---

## 5. Articy:draft

**Type:** Professional narrative design software (desktop)  
**License:** Commercial (subscription: free tier available, X tier is current)  
**Current Version:** Articy:draft X (v3 end-of-life for active development)

### Overview

Articy:draft is a professional tool used by game studios (Ubisoft, CD Projekt Red, etc.) for narrative design and content management. It's not a game engine — it's a **planning and authoring environment** that exports data to Unity, Unreal, JSON, Excel, and Word. It combines a visual flow editor, a game object database, scripting, and location planning in one application.

### Flow Editor

The **Flow Editor** is articy's centerpiece and the most sophisticated branching narrative visualization in any tool. Key features:

- **Non-linear story flows** — nodes represent story elements (dialogue fragments, choices, scenes). Connections between nodes represent the flow of control.
- **Nested flow** — objects can contain other objects. A "scene" node can expand to reveal its internal dialogue flow. This enables top-down and bottom-up authoring simultaneously.
- **Drag-and-drop visual editing** — create nodes, connect them with edges, rearrange the graph. Full canvas with zoom, pan, and layout.
- **Quick Create** — rapidly add multiple dialogue fragments in one action. The system suggests useful combinations (e.g., speaker + template based on context).
- **Word-like text formatting** within nodes — bold, italic, etc.
- **Flow to Word export** — generates screenplay-format documents from the flow graph.

### Template System

Articy's template system is one of its most powerful features. Every object type (dialogue fragment, character, item, location) is based on editable templates with modular property definitions. Templates can have constraints, and objects inherit from their templates. Changes to a template propagate to all instances. This allows studios to define their own content models — e.g., a "Quest" template with fields for quest giver, objectives, rewards, prerequisites.

### Game Object Database

Beyond the flow, articy maintains a structured database of all game objects:

- **Entities** — characters, enemies, items, weather, abstract concepts. Fully templated.
- **Assets** — images, video, audio referenced within the project.
- **Favorites** — quick-access bookmarks.
- **Attachments and hyperlinks** — any object can link to any other object or external reference.
- Everything is connected and one click away from any view.

### Scripting

Articy has its own expression language for conditions and instructions:

- **Global variables** (boolean, integer, string).
- **Object property access** in scripts — e.g., make a dialogue choice visible only if the player has a specific skill.
- **Conditions** on flow connections — edges can be gated by expressions.
- **Instructions** — set variable values or modify object properties based on player choices.
- **Syntax highlighting and autocompletion**.

### Checkup / Analysis Tools

Articy provides extensive verification tools that no other tool in this survey matches:

- **Simulation Mode** — "play" through the story flow in a PowerPoint-like presentation. Conditions and instructions are evaluated live. You can walk every path and see results instantly without loading a game engine.
- **Conflict Search** — automated detection of invalid property values, invalid references between objects, duplicate technical names, and defective assets.
- **Property Inspector** — watch variable values and object properties change in real-time while simulating.
- **Spellchecker** — built-in.
- **Advanced Search (Query)** — query language for finding objects matching complex criteria (e.g., "all dialogue fragments spoken by Character X where condition Y is true").

### Export & Integration

Articy separates authoring from runtime:
- **Customizable export rulesets** — define exactly what gets exported and how, per object type and template.
- **Export formats:** JSON, XML, Excel, Word, XPS (flow diagrams).
- **Unity importer:** automatic data import, full flow traversal engine, automated script evaluation, localization via Excel.
- **Unreal importer:** Blueprint support, automatic dialogue traversal, custom articy asset picker, localization support.
- **Import:** Excel, FinalDraft.

### What It Does Well

- **The most complete narrative design tool available.** Flow graph + database + scripting + analysis + export makes it a single source of truth for narrative content.
- **Flow visualization is best-in-class.** Nested flows, conditional edges, template-driven node types, and the simulation mode make it far more powerful than any other tool for understanding complex branching.
- **Template system enables studio-scale content management.** Large teams can define consistent content structures and enforce them.
- **Simulation mode + conflict search are killer features.** Catching logic errors before hitting the game engine saves enormous time.
- **Professional integrations.** First-class Unity and Unreal importers with automatic flow traversal.

### Limitations

- **Closed-source and commercial.** Subscription-based pricing. The free version has limitations.
- **Desktop-only.** Windows application. No web-based version. No macOS native version (Windows via VM/Boot Camp).
- **No built-in game runtime.** Articy is purely an authoring/planning tool. You always need a game engine or custom runtime to play the content.
- **Overkill for simple gamebooks.** The template system, entity database, and scripting language are designed for AAA game production. For a simple text-based gamebook, articy is extremely heavyweight.
- **Steep learning curve.** The interface is dense and professional. New users face a significant ramp-up.
- **No real-time collaboration.** Single-user editing. Multi-user workflows require manual coordination (export/import, merge).
- **End of life for v3.** Articy has moved to Articy:draft X. The v3 features described here are legacy; X adds new capabilities but is even more expensive.

### Inspiration for a Web-Based Gamebook Editor

Articy's **flow editor with nested nodes** is the gold standard. For a web-based gamebook editor, the ability to collapse/expand sub-graphs (scenes containing their own branching flows) would be transformative for managing complex stories. The **simulation mode** — walking paths live with variable evaluation — should be a core feature. The **conflict/reference checker** and **property inspector** demonstrate that narrative tools benefit enormously from static analysis. The **export ruleset system** shows how authoring and runtime should be cleanly separated.

---

## Comparative Matrix

| Feature | Twine | Ink/Inky | Yarn Spinner | Ren'Py | Articy:draft |
|---|---|---|---|---|---|
| **Editor type** | Web + desktop app | Desktop app | VS Code extension | Launcher + text editor | Desktop app (Windows) |
| **Graph visualization** | Yes, passage map | No | Yes, per-file in VS Code | No (3rd party only) | Yes, nested flow editor |
| **Flow analysis** | No | No | No | Lint only | Simulation mode + conflict search |
| **Export formats** | HTML | JSON, HTML, engine plugins | Engine plugins (Unity, Godot, Unreal) | Desktop, mobile, web builds | JSON, XML, Excel, Word, Unity, Unreal |
| **Collaboration** | No (single file) | Git (text files) | Git (text files) | Git (text files) | Manual (single user) |
| **Variables & logic** | Via story format macros | Native scripting language | Built-in typed variables | Full Python | Expression language |
| **Learning curve** | Very low | Low-medium | Medium | Medium-high | High |
| **Open source** | Yes (GPL/MIT) | Yes (MIT) | Yes (MIT) | Yes (MIT) | No (commercial) |
| **Web-based editing** | Yes | No | No | No | No |
| **Licensing cost** | Free | Free | Free | Free | Subscription |
| **Best for** | Simple choice-based IF | Complex branching narrative | Game dialogue | Visual novels | AAA narrative design |

---

## Key Takeaways for Building a Web-Based Gamebook Editor

### 1. The Graph View is Essential

Every tool that offers visual editing (Twine, Articy, Yarn Spinner's VS Code extension) has a graph/flowchart view. This is the #1 feature users expect. Twine proves it can be simple and web-based. Articy proves it can be sophisticated with nesting, conditional edges, and inline property display.

### 2. Live Preview / Simulation Should Be Built In

Inky's live play pane and Articy's simulation mode demonstrate that **test-as-you-write** is critical. An author should be able to click on a node in the graph and play through the story from that point, seeing variable values update and condition branches resolve in real time.

### 3. Text-First Authoring with Visual Graph, or Visual-First with Text Backup

- **Twine model**: you edit cards on the graph visually. Text is inside the cards.
- **Ink model**: you write text in a script file. The structure is implied by knots and diverts. (Inky has no graph, but the language *could* map to one.)
- **Yarn Spinner model**: you write text in VS Code. The graph is an auto-generated secondary view.

A web-based editor should ideally support both: a graph canvas where you can drag and edit nodes, and a script/code view that stays synchronized with the graph.

### 4. Ink's Weave Model is the Best Language for Gamebooks

Ink's **choices + gathers** pattern — where branching content naturally reconverges without needing named jump targets — is the most elegant model for choice-based interactive fiction. Any gamebook syntax should support something equivalent. The weave reduces boilerplate and makes the flow readable.

### 5. Static Analysis Adds Professional Value

Articy's conflict search and Ren'Py's lint are the only analysis tools in this space, and both are highly valued by users. A web-based editor could offer:
- **Dead-end detection** — passages with no outgoing links or no path to an ending.
- **Unreachable passage detection** — passages that no path can reach.
- **Condition conflict detection** — contradictory or impossible condition gates.
- **Variable usage analysis** — where is each variable set, read, and checked?

### 6. Collaboration is the Biggest Gap

None of the five tools support real-time collaborative editing. In a web-based editor, this is the single biggest differentiating feature you could offer. Even lightweight collaboration (sharing a URL, seeing other cursors, comment threads on passages) would be unique in this market.

### 7. Export Should Be Clean and Standards-Based

Every tool exports to some form of open format. HTML (Twine), JSON (Ink, Articy), and plain text (Yarn, Ren'Py). A web-based editor should export to a well-documented JSON format that anyone can build a runtime for, plus optionally generate HTML for instant playable output.
