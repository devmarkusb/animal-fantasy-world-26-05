# Claude Code — Animal Fantasy World

Read and follow `AGENTS.md` at the repository root. It is the canonical source of truth for project structure, commands, and conventions.

## Additional Claude-specific notes

- Unity YAML files (`.unity`, `.prefab`, `.asset`, `.meta`) are machine-serialized. Do not edit them directly — describe the intended change and let the user apply it in the Unity Editor.
- The project has no CLI build or test pipeline. Compilation checks require the Unity Editor.
- When creating new C# scripts, always create a matching `.meta` file note: **do not** generate `.meta` files yourself. Unity auto-generates them on the next Editor import.
