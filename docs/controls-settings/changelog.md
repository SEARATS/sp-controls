# Controls & Settings change notes

## 0.3.0 unreleased

- Uses Controls, Settings, and UI as the project and namespace names.
- Builds Controls.dll and packages the source as SP.Controls.UI 0.2.0 and
  SP.Controls.Settings 0.3.0.
- Updates the examples and consuming mod references.
- Verification passed for both Release solutions, the source packages, and a
  consumer restored from the packages.

## 0.2.2 — unreleased

- Moves SharedUi, SharedSettings, and the Controls DLL project to
  `SEARATS/sp-controls`, retaining their source history and existing assembly names.
- Names the Workshop item Controls & Settings.
- Versions the UI source package at 0.1.2 and settings source package and runtime
  at 0.2.2. Runtime consumers declare Controls & Settings 0.2.2 or newer.
- Builds the runtime and typed provider examples from `SpControls.slnx`.
  Source consumers update their import paths to this checkout.
- Keeps Workshop artwork with the package source so standalone builds stage
  the thumbnail and previews.
- Verification passed for the standalone Release solution, both source packages,
  a consumer restored from those packages, and the Crimean Tom Release solution
  using the extracted projects. This extraction has build verification only.

## 0.2.1 — unreleased

- Combines Controls and the settings system in one Anchor Chain mod, retaining
  the `SeaPower.Controls.dll` assembly name and existing native-control namespaces.
- Adds automatic registration for declarative and scripted settings-provider
  interfaces on the loader's existing mod instance. No duplicate plugin instances
  are constructed. Consumers declare the library dependency and start after it.
- Public providers use concrete settings models and typed editor selectors.
  Annotated bool, numeric and enum properties generate fields without exposing
  string-keyed value stores to the example mods.
- Exposes settings values, persistence, draft validation, boolean/number/choice
  editors, and scripted page refresh through the public DLL.
- Queues dynamic page rebuilds through the Noesis dispatcher so the control
  handling the current native input event is not removed inside that event.
- Ignores control initialization events during rebuilds and unchanged mode/value
  notifications, preventing refresh loops and unrelated pages being marked dirty.
- Checks every page's validation before saving any page. Existing settings files
  stay in their original location.
- Stages Workshop metadata, documentation, thumbnail and available preview images
  at build time. The standalone Settings listing is retired.

Deck Tweaks, Amphibs, UNREP and Observer move to version 0.1.1 and reference this
shared runtime instead of compiling private copies of its controls and settings.
Players need Controls & Settings enabled alongside those updated mods.

The whole solution and the two public-API examples are build-checked. Runtime
verification and screenshots are recorded separately with the staged artifacts.
