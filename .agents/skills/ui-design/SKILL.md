---
name: ui-design
description: UI/UX design rules for the FiveM Server Launcher main window. Apply when touching MainView, MainWindow, styles, colors, layout, or when proposing UI changes. Encodes DOC.md's UX philosophy, the color tokens, and WPF conventions for this repo.
---

# UI Design Rules

## Philosophy (from DOC.md #UX)

> Don't bother if everything is ready. If something needs intervention, show an explanatory
> progress state.

- Default: no extra screens. One calm main surface; actions are one click.
- Only when something needs the user (waiting, preparing, intervention) do we show a status/progress
  line. No splash, no confirmation spam.
- The main window is minimalist: connect flow primary, OPEN split button secondary, settings/dev
  tucked away behind toggles.

## Theme

- **Dark theme is the app's identity** — it stays. The DOC.md tokens document the *components* of
  the scheme (accent, error, success); the current dark palette (greys `#101010`/`#181818`/`#202020`,
  amber accent `#F5A623`, deep borders `#080808`) is the active one.
- The accent (`#F5A623` today) is reserved for the primary CTA and hover/pressed feedback.
- Secondary elements use borders and panel fills, never competing fills.
- Error keeps like `#E05252` / success `#55C271` (or DOC.md's `#D32F2F`/`#2E7D32` if migrating —
  both naming schemes map to the same intent; migrate the hex values gradually, not the structure).

## Layout conventions

- Generous white space; large, comfortable hit targets (min height ~44-50 for primary buttons).
- Everything lives in `Window.Root` Grid rows inside `Views/MainView.xaml`; panels switch via
  visibility + converters (no code-behind).
- Typography: default label 11px SemiBold uppercase for section headers (Secondary), values 12-13px
  (Text); status messages Secondary, 11px.

## WPF / MVVM conventions

- All UI strings in the XAML or VM are English.
- Zero business logic in code-behind; the only acceptable handlers are pure visual toggles
  (show/hide dropdown). Everything else is commands on the VM.
- Style resources use the existing pattern: `StaticResource MainButtonStyle`,
  `SecondaryButtonStyle`, `DropdownButtonStyle`, and the color brushes by the token names
  (`BackgroundBrush` = `#101010`, `PanelBrush` = `#181818`, `PanelLightBrush` = `#202020`,
  `AccentBrush` = `#F5A623`, `AccentDarkBrush` = `#D88E18`, `TextBrush` = `#FFFFFF`,
  `SecondaryTextBrush` = `#999999`, `BorderBrush` = `#080808`, `GreenBrush` = `#55C271`,
  `RedBrush` = `#E05252`, etc.). New brushes must be added under the matching DOC.md token name.
- Bindings over code-behind; raise PropertyChanged for every UI-visible change.
- Any XAML's Content strings are user-visible; keep them in the VM unless they are static labels.

## When applying

1. Restyle existing controls: prefers editing `MainView.xaml` resources + the panel bodies; no new
   files except shared value converters.
2. Keep TDD: VM-side changes (e.g. new bindable state) still follow RED/GREEN.
3. After visual changes, report to the user for manual visual verification before committing.
