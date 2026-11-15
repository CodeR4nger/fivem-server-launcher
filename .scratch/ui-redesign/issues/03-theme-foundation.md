# 03: Theme foundation

**What to build:** the new visual language applied to the existing screen, with the layout
unchanged. Inter is bundled (license text in the repo, README mention) and set at the window
root with Segoe UI as fallback. The theme dictionary gains the token set (ground, panel,
surfaces, borders, three text levels, accent, game tag and status colours, radii, spacing,
type ramp) and the reusable control styles: primary and secondary pill buttons, icon button,
filter chip (optional dot and count), segmented control, toggle switch (a styled
`ToggleButton`), filled game tag, server mark (icon with initials fallback and a deterministic
colour from the name), card, text input, dropdown, section header, settings row, modal frame,
popup menu, empty state. The Interface animations setting exists end to end in the model
(`Default`/`Minimal`, persisted, Windows' animation setting honoured) together with the shared
motion resources and the single switch that turns them all off; its UI arrives in ticket 09.

See `../spec.md` (Theme, Reusable controls/styles, Interface animations).

**Blocked by:** 01.

**Status:** done

- [x] Inter renders in the running app (Debug and published single-file); license file present.
- [x] Every token has one definition; text/background pairs used by the styles reach 4.5:1 (listed in this ticket).
- [x] Game tags use exactly `#F5A623` / `#4DA3FF` / `#E05252` with near-black bold text, filled, small radius.
- [x] Server mark initials/colour logic is tested; every XAML-instantiated type passes the parameterless-ctor reflection test.
- [x] Interface animations: setting persisted, defaults to Default, effective motion = Default AND Windows animations on; tested at the view-model seam; with motion off, no storyboard runs.
- [x] The current screen uses the new styles without layout changes; screenshots recorded; existing layout test still passes; zero warnings.

## Comments

**Delivered (2026-11-15).** Five commits on `claude/ui-03-theme-foundation-efae27`: the
Interface animations setting and motion flag, the server mark helper, the bundled Inter font,
the new CFX status dot colours, and the theme with its styles and motion applied to the
current screen.

### Tokens and contrast

Every token is defined once, in `Views/Theme.xaml`. Three data-to-colour mappings keep their
colours in code, as before: `GameClientToBrushConverter` for the game tags (`#F5A623` /
`#4DA3FF` / `#E05252`, unchanged), `CfxStatusToBrushConverter` for the status dots, and
`ServerMark.Palette` for the mark colours.

| Token | Value | Token | Value |
|---|---|---|---|
| GroundBrush | `#161616` | TextPrimaryBrush | `#F2F2F2` |
| TitleBarBrush | `#111111` | TextSecondaryBrush | `#B0B0B0` |
| PanelBrush (radius 22) | `#222222` | TextTertiaryBrush | `#A8A8A8` |
| SurfaceRaisedBrush | `#2B2B2B` | FooterTextBrush | `#8A8A8A` |
| SurfaceHoverBrush (hover / selected row) | `#333333` | DangerTextBrush | `#FF8A80` |
| SettingsGroupBrush (radius 16) | `#2A2A2A` | AccentBrush (primary pill only) | `#F5A623` |
| InputBrush (radius 12) | `#1B1B1B` | OnAccentBrush | `#1A1A1A` |
| FooterBrush | `#1A1A1A` | OnSelectedBrush (on a `#F2F2F2` fill) | `#1A1A1A` |
| BorderSubtleBrush | `#2E2E2E` | GameTagTextBrush | `#101010` |
| BorderBrush | `#353535` | ScrimBrush | `#99000000` |
| BorderStrongBrush | `#3A3A3A` | Status dots (converter) | `#3FB950` / `#D29922` / `#E05252` / `#7A7A7A` |

Radii: pill (999, clamped by `RoundedBorder`), panel 22, modal 22, group 16, card 14, menu 14,
input 12, item 8, tag 3. Spacing: 4 / 8 / 12 / 16 / 20 / 24. Type ramp: page title 22 bold,
section header 11 bold, row title 14 semibold, body 13, caption 12, tag 10 bold.

Contrast is the WCAG ratio for normal text (minimum 4.5:1). `ThemeTokenTests` checks every
pair below from the theme itself:

| Text | On | Ratio |
|---|---|---|
| `#F2F2F2` primary | ground / title bar / panel / raised / hover / group / input | 16.16 / 16.87 / 14.21 / 12.65 / 11.29 / 12.82 / 15.39 |
| `#B0B0B0` secondary | ground / panel / raised / hover / group / input | 8.34 / 7.34 / 6.53 / 5.83 / 6.62 / 7.94 |
| `#A8A8A8` tertiary | panel / raised / hover / group / input | 6.69 / 5.95 / 5.31 / 6.04 / 7.24 |
| `#FF8A80` danger | ground / panel / raised / input | 7.93 / 6.97 / 6.20 / 7.55 |
| `#8A8A8A` footer | `#1A1A1A` | 5.04 (the old `#666666` was 3.0) |
| `#1A1A1A` on accent | `#F5A623` | 8.59 |
| `#1A1A1A` on selected | `#F2F2F2` | 15.55 |
| `#101010` tag text | `#F5A623` / `#4DA3FF` / `#E05252` | 9.39 / 7.25 / 4.98 |
| `#F2F2F2` initials | 8 mark colours | 4.77 to 5.30 (`ServerMarkTests`) |

No token value needed adjusting. Tertiary text is never used on the footer, and `#8A8A8A`
appears only on the footer.

### Reusable styles (all in `Theme.xaml`)

- **Buttons:** `PrimaryButtonStyle`, `SecondaryButtonStyle`, `IconButtonStyle` and
  `MenuRowButtonStyle`, all on one `PillButtonTemplate` with a hover/press layer and a
  keyboard-only `PillFocusVisual`.
- **Controls:** `FilterChipStyle` (with `ChipDotStyle` and `ChipCountStyle`),
  `SegmentedControlStyle` (a horizontal `ListBox`, so keyboard selection works) and
  `ToggleSwitchStyle` (a `ToggleButton` with a 50×28 track and a 20 px knob that slides from
  4 to 26).
- **Data:** `GameTagStyle` with `GameTagTextStyle`, and `ServerMarkStyle` (Content = icon
  bytes, Tag = name; the icon if there is one, otherwise the initials on the name's colour).
- **Surfaces:** `ContentPanelStyle`, `CardStyle`, `ListWellStyle`, `SettingsGroupStyle` and
  `ModalFrameStyle`.
- **Inputs:** `TextInputStyle` and `PillTextInputStyle`, the implicit dropdown (`ComboBox`)
  and `CheckBox` styles, and `ListRowStyle`.
- **Settings and text:** `SettingsRowStyle` (a `HeaderedContentControl`: Header = title,
  Tag = description, Content = control), `SectionHeaderStyle`, `PageTitleStyle`,
  `RowTitleStyle`, `BodyTextStyle`, `CaptionTextStyle`, `DangerTextStyle`,
  `EmptyStateTextStyle` and `PlaceholderTextStyle`.
- **Popups:** `PopupMenuStyle` and `PopupMenuItemStyle`, plus a dark ToolTip.
- **Motion:** `FadeSurfaceStyle` and `SlideSurfaceStyle`.

The server mark logic is `Views/ServerMark`, which is tested. Initials are the first letter
or digit of each of the first two words. Apostrophes stay inside a word, a character outside
the basic plane is never split, and a name with no letters or digits gives "?". The colour
is FNV-1a over the trimmed, upper-cased UTF-8 name, modulo 8, so it is stable across runs;
the test pins its expected values from an independent computation. Two converters,
`ServerMarkInitialsConverter` and `ServerMarkBrushConverter`, expose it to XAML.

Two attached-property holders back the templates. `Views/Shape.CornerRadius` lets one
template serve several radii. `Views/RoundedBorder` is a `Border` that clamps its radius to
half its arranged short side; a plain `Border` draws `CornerRadius="999"` as an ellipse, as
the first after-screenshots showed. Every new XAML-instantiated type passes the
parameterless-constructor reflection test.

### Inter

- **Source:** the official rsms/inter GitHub release **v4.1** (published 2024-11-16), asset
  `Inter-4.1.zip` (33,707,794 B, SHA-256 `9883fdd4…6b11e`).
- **Files:** from its `extras/ttf/` folder, in `src/FiveMServerLauncher/Assets/Fonts/`:

  | File | Size | SHA-256 (prefix) |
  |---|---|---|
  | `Inter-Regular.ttf` (400) | 411,640 B | `40d692fc` |
  | `Inter-Medium.ttf` (500) | 417,300 B | `97ad806f` |
  | `Inter-SemiBold.ttf` (600) | 419,744 B | `78a843fa` |
  | `Inter-Bold.ttf` (700) | 420,428 B | `28831609` |
  | `Inter-ExtraBold.ttf` (800) | 421,796 B | `e6756ad5` |
  | `OFL.txt` (the release's `LICENSE.txt`, SIL OFL 1.1) | 4,380 B | `262481e8` |

- **Static or variable:** I chose the static weights after checking both. WPF does not
  resolve `InterVariable.ttf` at all: there is no glyph typeface under either family name,
  and the text falls back to another font. Each static file resolves to its own real weight
  with no bold simulation.
- **Wiring:** the fonts are WPF `Resource` items (csproj wildcard). The theme's
  `UiFontFamily` is `pack://application:,,,/CFXLauncher;component/Assets/Fonts/#Inter, Segoe UI`,
  set once on the MainWindow root grid. Popups that don't inherit it (ContextMenu, ToolTip)
  set it in their style. Glyphs Inter lacks (⚙, ↻) fall back to Segoe UI. The README has a
  "Third-party font" section.
- **Tests:** `ThemeTokenTests` checks that weights 400 to 800 each resolve a real `Inter-*`
  face with no simulation.
- **Seen running:** both the Debug build and the published single-file exe were run and
  captured: `screenshots/03-theme-foundation/after/runtime-*.png`.
- **Exe size:** **65,632,195 B before, 66,652,425 B after** (+1,020,230 B, +1.6%; compression
  shrinks the fonts' 2.09 MB).

### Interface animations and the motion switch

- **Setting:** `LauncherSettings.InterfaceAnimations` (`Core/Enums/InterfaceAnimations`:
  `Default` or `Minimal`, default `Default`). It persists through `FileSettingsStorage`, which
  was tested with a round trip and with a missing field, which reads as Default.
- **View model:** `MainViewModel.InterfaceAnimations` persists like the other settings
  through the generalised `SetPersisted<T>`, which was `SetPersistedFlag`. A failed save
  reverts it; it was added to the shared revert-on-failure theory.
- **Effective motion:** `IsMotionEnabled` is Default AND `ISystemAnimationSetting.IsEnabled`.
  The seam is an optional constructor parameter; left unwired it counts as on.
- **System seam:** the real `Service/SystemAnimationSetting` reads
  `SystemParameters.ClientAreaAnimation`. It raises `Changed` from
  `SystemParameters.StaticPropertyChanged`, so a change to the Windows setting while the app
  is running is followed; the view model re-raises `IsMotionEnabled`. The composition root
  wires it.
- **Settings UI:** none here; that is ticket 09.
- **One switch:** `views:Motion.IsEnabled` is inherited and **off by default**. MainWindow
  binds it once to `IsMotionEnabled`. It defaults to off because the page controls are
  parsed before the window has a DataContext. With a default of on, the theme triggers began
  storyboards during parsing that were only removed a tick later. With off, a Minimal user
  never starts a single storyboard.
- **Motion resources:** `MotionFast` 150 ms, `MotionMedium` 250 ms, `MotionSlow` 350 ms,
  `MotionEase` (cubic ease-out), and the storyboards `MotionFadeIn`, `MotionFadeOut`,
  `MotionSlideIn`, `MotionPopIn` and `MotionDropIn`. Nothing gates input on them.
- **Where the motion lives:** surface fade, side-panel slide, modal pop, menu drop,
  dropdown drop, switch-knob glide, and button hover/press easing are all theme triggers
  conditioned on the switch. The code-behind motion (browser slide, client-list unroll)
  checks `Motion.GetIsEnabled` and snaps when it is off.
- **Two WPF facts, now in a theme comment and pinned by tests:**
  - An in/out pair must sit on ONE trigger (Enter plus Exit actions). Storyboards begun by
    different triggers compose in layers by declaration order, so a separate later "out"
    trigger held the surfaces at opacity 0 forever. That was the first after-run, with an
    empty left panel; `FadeSurface_WhenMotionIsOnBeforeItIsShown_…` pins it.
  - `RemoveStoryboard` takes effect on the next clock tick of an element that is rendering,
    so the tests for switching motion off host the element in an offscreen window and pump.
- **With motion off:** the surfaces snap through setters, and `ThemeStyleTests` asserts
  `HasAnimatedProperties == false` for the switch, the fade surface and the pill press layer.
  Re-rendering all 12 states with Minimal gave pixel-identical final frames (0 differing
  pixels).

### Applied to the current screen (layout unchanged)

The existing controls now use the tokens and styles:

- **Panels and dialogs:** the two content panels are rounded `#222222` panels. The settings
  dropdown is a settings group. The dialogs are modal frames over a scrim, and they pop in.
- **Rows and inputs:** rows use `ListRowStyle` with a server mark, a 14 px semibold title and
  10 px game tags. Inputs, dropdowns and checkboxes are restyled, and the search boxes are
  pills.
- **Buttons and banners:** buttons are pills. ENTER SERVER, SAVE, CONFIRM, UPDATE, LAUNCH and
  the browser CONNECT are primary; the rest are secondary or icon buttons. The title bar and
  footer use their tokens, and the footer text is `#8A8A8A`.
- **Accent:** no longer used for hover fills, combo highlights, check marks or the drop line.

Every container keeps its position and size. Small deliberate differences inside them:

- **Settings panel:** bottom padding went from 14 to 18. With Inter's metrics, the CFX STATUS
  header it overlays sits about 3 px lower and showed a sliver under the panel. This is the
  one layout fix for the font.
- **OPEN split button:** it is now two secondary pills with a 6 px gap. Spec: Connect stays
  the only amber action on the servers side, and the open-client button is secondary. Its
  client list is a rounded card 6 px below them, with 4 px padding. The code-behind unroll
  now adds the padding to its target height.
- **NEW SERVER:** now secondary.
- **Server rows:** rows without an icon now show initials where there used to be empty
  space.
- **Section headers:** bold instead of semibold. WPF `TextBlock` has no letter-spacing, so
  the mockup's tracking is not reproduced; the strings are already uppercase.

`SettingsPanel_ShouldShowEveryCheckboxUnclipped` passes in all 11 languages with Inter.

### Screenshots

I added a throwaway probe test, deleted before the final commit, that rendered the real
`MainWindow` offscreen per state after the animations settled. It ran with 4 saved servers
(presence and games applied) and English, capturing 12 states: main, settings, Dev Mode, OPEN
list open, add dialog, loopback dialog, confirm, update available, update failed, browser,
empty list, and selected row. It ran on the baseline commit (before) and on this branch
(after), and again on the branch with motion off. A representative set is in
`.scratch/ui-redesign/screenshots/03-theme-foundation/{before,after}/`. Side by side, every
panel, list, field and button sits in the same box. The update-available state still covers
the connect field, as it did before (the banner becomes a title-bar pill in ticket 04).

### Verification

- The suite passes: **1348/1348**, green on 4 consecutive full runs after the final commit
  (plus the runs in between).
- `dotnet build FiveMServerLauncher.slnx --no-incremental`: 0 warnings, 0 errors.
- The Release self-contained build and single-file publish (the `AGENTS.md` commands)
  succeeded. The exe ran and closed cleanly with exit code 0 against scratch data files.

### Not verified by hand

There was no desktop-control tool, so nothing was clicked by mouse:

- the hover/press easing seen live;
- the dropdown and ContextMenu drop-in, which are popups in their own windows and so are
  not in the offscreen captures;
- the switch to Minimal through the real Windows "Show animations" toggle while the app is
  running.

These code paths are covered by the style tests and the `SystemParameters` wiring. The new
styles that the current screen doesn't use yet (chip, segmented control, switch, settings
row, card, page title) are covered only by the style-application and motion tests; tickets
04 to 09 put them on screen.

