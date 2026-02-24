# Repository Guidelines

## Project Structure & Module Organization
- `src/DebugViewCS/`: WPF desktop app (UI, views, converters, app entry).
- `src/DebugViewCS.Core/`: core logic (capture, filters, models, settings, storage, utilities).
- `DebugViewCS.slnx`: solution entry that references both projects.
- Build outputs (`bin/`, `obj/`) are generated artifacts; do not edit or commit them.

## Build, Test, and Development Commands
- `dotnet restore DebugViewCS.slnx`: restore NuGet packages for all projects.
- `dotnet build DebugViewCS.slnx -c Debug`: compile the solution for local development.
- `dotnet run --project src/DebugViewCS/DebugViewCS.csproj`: run the WPF app.
- `dotnet test DebugViewCS.slnx -c Debug`: run tests when test projects are present.

Note: if build fails with file-lock errors, close the running `DebugViewCS.exe` and rebuild.

## Coding Style & Naming Conventions
- Language: C# with nullable enabled (`<Nullable>enable</Nullable>`).
- Indentation: 4 spaces; UTF-8 text encoding.
- Naming: `PascalCase` for types/methods/properties, `_camelCase` for private fields, `camelCase` for locals/parameters.
- Keep UI code in `DebugViewCS`; move reusable/business logic into `DebugViewCS.Core`.
- Avoid silent exception swallowing; handle errors explicitly.

## Testing Guidelines
- Current snapshot has no dedicated test project under `tests/`.
- Add new tests with xUnit in `tests/DebugViewCS.Tests` (or equivalent) and include them in `DebugViewCS.slnx`.
- Test naming pattern: `MethodName_State_ExpectedBehavior`.
- Focus coverage on filter logic, settings persistence, and capture pipeline behavior.

## Commit & Pull Request Guidelines
- Git history is not available in this workspace snapshot (`.git` missing), so follow Conventional Commits:
  - `feat:`, `fix:`, `refactor:`, `test:`, `docs:`.
- Keep commits small and scoped to one concern.
- PRs should include:
  - concise change summary,
  - validation steps/commands run,
  - linked issue (if any),
  - screenshots/GIFs for UI changes.

## Security & Configuration Tips
- Do not commit local runtime artifacts such as `filter_settings.json` from `bin/Debug/...`.
- Validate user-facing filtering/configuration input before persistence.
