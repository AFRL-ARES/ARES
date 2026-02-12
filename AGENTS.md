# AGENTS.md

This solution-level file points to project-specific agent guidance.

- AresScript guidance: `ARES/AresScript/AGENTS.md`
- Namespace style guidance: prefer namespace imports (`using` / `@using`) and concise type names across all files. Avoid fully qualified type names unless needed to resolve unavoidable namespace/type conflicts.
- Do not add extra empty lines at the end of a file after modifying it
- These commands will need network access and inside your sandbox you always have to run those commands
with `with_escalated_permissions: true` on the `shell` tool call and include a one-sentence justification
(e.g., "Need network access for npm install/build").
Ensure to include the `with_escalated_permissions` for all builds, restores, migrations, installs, tests, etc
where network access is required otherwise the command will hang.