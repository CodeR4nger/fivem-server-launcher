# 01: GetExecutablePath null cuando no instalado + remove RedM mapping

**What to build:** fix spec: real `ClientInstallLocator.GetExecutablePathAsync` debe devolver null si no instalado (no debería devolver la ruta esperada si el archivo no existe). Remove scope creep: `GameClient.RedM` mapping borrado (no lo pide la spec). TestsREAL contra el real — seam inyectable `Func<string,bool>` ctor — sin novedades del filesystem real.

**Blocked by:** None

**Status:** resolved

- [x] `ClientInstallLocator` con ctor inyectable `Func<string,bool>` (default: `File.Exists`).
- [x] `GetExecutablePathAsync` devuelve null cuando no instalado.
- [x] `IsInstalledAsync` usa `File.Exists` (no `Path.Exists`).
- [x] RedM mapping borrado (scope creep).
- [x] Suite verde (117).

## Comments