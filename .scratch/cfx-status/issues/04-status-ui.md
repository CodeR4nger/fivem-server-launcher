# 04: Status UI (XAML + converter)

**Type:** task

**What to build:** replace the hardcoded `STATUS FIVEM` block (`MainView.xaml`) with a
three-row status display (colored dot + game name + status label) bound to
`FiveMStatus`/`FiveMEnhancedStatus`/`RedMStatus`. New data-only `CfxStatusToBrushConverter`
(`Operational`→green, `Degraded`→amber, `PartialOutage`/`MajorOutage`→red,
`Maintenance`/`Unknown`/default→grey) reusing existing brush resources. Visibility rules
unchanged (hidden in Dev Mode via `InverseBoolToVisibility`). Business logic stays out of
XAML code-behind; label text comes from the VM.

**Blocked by:** 03.

**Status:** resolved

- [x] `CfxStatusToBrushConverter` maps every enum member + unknown int → expected brush
      (prior art: `GameClientToBrushConverter` tests).
- [x] `STATUS FIVEM` block replaced by three bound rows, same visual language/spacing.
- [x] Dev Mode isolation preserved (rows hidden while `IsDevMode`).
- [x] Composition root wires `CfxStatusService(httpClient)` sharing the app `HttpClient`.