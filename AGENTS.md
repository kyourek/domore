# Repository guidance

This file applies to the entire repository. Read the relevant project README and
nearby implementation and tests before making changes.

## Solution layout

- `domore.slnx` contains the library, test, sample, and shared projects.
- `source/Domore.*` contains the libraries, grouped by package. Some packages
  have a `README.md` with package-specific documentation; the root `README.md`
  describes the package family.
- `tests/Domore.*.Tests/` contains the package test projects. Tests are grouped
  by the area or type they cover.
- `samples/` contains sample projects and a helper project used by tests.
- `shared/Domore.Sharing/` contains source shared by multiple projects.
- `Directory.Build.props` and `Directory.Packages.props` hold shared build
  settings and centrally managed package versions. The `source/`, `tests/`,
  `samples/`, and `shared/` directories add group-specific build settings.
  `global.json` selects the .NET SDK, and `.github/workflows/workflow.yml`
  defines CI.

Inspect `git status` and existing diffs before editing. Preserve existing work;
do not overwrite or revert unrelated changes. Keep unrelated changes out of
fixes and commits, and stage only the changes belonging to the requested task.

## Build and test

Identify the solution, affected projects, and target frameworks before building.
Use the SDK selected by `global.json`, and the operating system, targeting packs,
and runtimes required by the affected projects. Run validation on Windows to
match CI and to support the WPF and .NET Framework targets. From the repository
root, use:

```powershell
dotnet restore domore.slnx
dotnet build domore.slnx --no-restore --configuration Release
dotnet test domore.slnx --no-restore --configuration Release --no-build
dotnet pack domore.slnx --no-restore --configuration Release --no-build
```

For focused iteration, select the affected test project and target framework.
Use test filters to run the relevant fixture or test when supported by the
repository's test runner.

For code changes, run relevant tests and build all affected target frameworks.
Use full solution validation when changing shared build settings, dependencies,
or behavior spanning projects. Documentation-only changes need a content and
diff review. Report the checks performed and any validation that could not run.

For efficient testing, run the affected test first, then its fixture or project,
before broader validation required by the change. Confirm that a regression test
fails because of the reported bug, rather than a build or environment problem.
Use `--no-build` only when the binaries include the latest code changes.

## Code and project conventions

- Follow `.editorconfig`: CRLF line endings; four spaces for C#; two spaces for
  project XML, XAML, JSON, and Markdown. C# and project/XAML files use UTF-8 with
  BOM; Markdown uses UTF-8 without BOM. Keep C# and XAML lines within the
  configured 120-column guideline.
- Use file-scoped namespaces and opening braces on the same line. Follow the
  configured C# style; prefer `var` over explicit type names for local variables.
  Use PascalCase for types and members and an `I` prefix for interfaces.
- Never prefix type names with their namespace. Always use `using` statements
  to import namespaced types.
- Method parameters of type `CancellationToken` must always come last in the
  parameter list. Pass `CancellationToken` arguments last when calling methods.
- Use the configured C# language version. APIs must be available on every
  framework targeted by the affected project. Preserve framework-specific
  conditions and compatibility shims; a newer language version does not supply
  newer runtime APIs.
- Check shared build settings and global usings before adding project-specific
  settings or imports. Preserve existing build-property import structures.
- When central package management is configured, manage dependency versions in
  `Directory.Packages.props` and omit versions from project package references.
  Keep dependency changes focused on the task.
- Do not add to the public API unless explicitly instructed to do so. Keep new
  types and members internal or private when the requested change permits it.
- Declare classes either `sealed` or `abstract`. Avoid concrete classes that
  can be extended.
- Put each top-level type in a separate file named after the type. Keep closely
  related helper types nested with their owning type when that matches the
  existing design.
- Update public API documentation, relevant READMEs, and usage examples when
  changing the documented contract. Do not add XML documentation comments to
  private or internal types or members.
- Keep changes scoped. Avoid unrelated formatting, framework, package-version,
  or release-workflow changes. Do not commit generated build/test output,
  packages, or IDE state such as `out/`, `bin/`, `obj/`, and `.vs/`.

Within a C# type, use this member order, omitting items that do not exist:

1. Static constructor.
2. Private constants.
3. Private fields.
4. Private events.
5. Private properties.
6. Private instance constructors.
7. Private methods.
8. Internal constants.
9. Internal events.
10. Internal properties.
11. Internal instance constructors.
12. Internal methods.
13. Protected constants.
14. Protected events.
15. Protected properties.
16. Protected instance constructors.
17. Protected methods.
18. Public constants.
19. Public fields, only where interop layout or framework conventions require them.
20. Public events.
21. Public properties.
22. Public instance constructors.
23. Public methods.
24. Explicit interface implementations, after everything else.

Avoid fields in ordinary classes; expose state as properties instead. Use
fields when required by layout or framework conventions. Use the `field` keyword
in properties when a separate backing field is not otherwise necessary.
For ordinary comments, prefer `/* ... */` block comments, with `*` at the start
of each interior line. Use `//` comments for a single-line clarification. Public API XML
documentation is an exception: use `///` documentation comments and follow the
nearby XML documentation style. Short XML elements may stay on one line; use
separate lines for the opening tag, value, and closing tag when wrapping longer
content.


## Test conventions

Test projects use NUnit. Test fixtures generally append `Test` to the tested
type's name. Follow the existing assertions, fixtures, and helper patterns when
writing tests.

Use the repository's existing test framework and assertion style. Add regression
coverage for behavior changes. Prefer explicit task signals with bounded waits
over timing-dependent sleeps. Reuse existing test helpers where applicable, and
keep tests and shared test shims compatible with all applicable target frameworks.

## Code reviews

When asked to review code, inspect the entire requested scope thoroughly and
identify all bugs and issues you can find. Write the findings to `Review.md` in
the repository root. Number each issue so it can be referenced later. For every
issue, describe the problem and a way to resolve it. Cite specific file names
and line numbers where applicable. Review existing findings in `Review.md` before
editing it so relevant issues are retained or updated without duplication.
Keep review issue numbers stable: never renumber existing issues or reuse their
numbers, including those marked resolved. Assign each new issue a number greater
than the highest issue number already used in `Review.md`.

When asked to fix issues from `Review.md`, work only on the issue numbers
specified in the request. Prefer red/green testing: first add a test that fails
because of each issue, then fix the code, then rerun the previously failing test
to confirm it passes. Once an issue is resolved, keep it in `Review.md` and mark
it as resolved in that issue's heading; do not remove it.

If worktrees are explicitly requested for fixing multiple issues, create a
separate Git worktree for each requested issue number. Place each worktree
directory directly in the parent directory of the original repository root,
as a sibling of that repository. Name it
`agent-{original-directory-name}-{issue-number}`, where
`original-directory-name` is the name of the original repository's root directory.
Name each worktree branch
`agent/{issue-number}/{short-description}`. Fix, test, and update `Review.md`
for that issue in its own worktree. Commit each issue's changes on
its worktree branch. The first line of the commit message must start with
`Fix #{issue-number}.`, followed by a blank line and a description of what was
broken and how it was fixed. Once the commit is ready, notify the requester
that the worktree branch is ready to be merged.

Before reporting completion, review the final diff for unintended changes,
whitespace problems, and accidental public API additions. Check new and untracked
files as well as tracked changes, and confirm that the changes match the request.

Report completion consistently: summarize what changed, list resolved issue
numbers when applicable, and state which tests or checks ran and their results.
Identify any checks that could not run. For worktree fixes, include the branch
name and commit hash that are ready to be merged.
