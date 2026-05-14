# Animal Fantasy World

## Project overview

Educational 3D game built in **Unity 6000.4** (URP) targeting children. Players explore procedurally-generated biomes, orbit the camera, click animals, and read fun facts. Data-driven via `ScriptableObject` assets (`BiomeDefinition`, `AnimalDefinition`).

## Stack

| Layer | Technology |
|---|---|
| Engine | Unity 6000.4.6f1 |
| Language | C# 9.0 / .NET Standard 2.1 |
| Render pipeline | Universal Render Pipeline 17.4.0 |
| Input | Unity Input System 1.19.0 |
| AI / Navigation | Unity AI Navigation 2.0.12 |
| IDE support | VS Code (vstuc), JetBrains Rider |
| VCS | Git + LFS (binary assets tracked via `.gitattributes`) |

## Build commands

All builds run through the Unity Editor — there is no CLI build script yet.

| Action | How |
|---|---|
| Open project | Unity Hub > Open > select repo root |
| Generate biome scene | Unity menu: **Tools > Generate Biome Scene**, pick a `BiomeDefinition` asset |
| Build player | File > Build Settings > Build (platform-specific) |

> **Unverified**: No headless / CI build pipeline exists. A `BuildScript.cs` would be needed for `unity -batchmode -executeMethod` workflows.

## Test commands

Unity Test Framework 1.6.0 is included but **no tests exist yet**.

| Action | How |
|---|---|
| Run tests (Editor) | Window > General > Test Runner > EditMode / PlayMode > Run All |
| Run tests (CLI) | `unity -batchmode -runTests -testResults results.xml` (unverified — no CI) |

## Formatting and linting

No `.editorconfig`, Roslyn analyzer rules, or formatter configuration is present. Unity's default source generators are the only analyzers.

**Recommendation**: add an `.editorconfig` with C# conventions when the team grows.

## Architecture and important directories

```
Assets/
├── Game/
│   ├── Scripts/          # Runtime MonoBehaviours & ScriptableObjects
│   ├── Editor/           # Editor-only tooling (BiomeSceneGenerator)
│   ├── Biomes/           # BiomeDefinition .asset files
│   ├── Materials/
│   ├── Prefabs/
│   └── Scenes/
├── Art/
│   ├── Animals/          # Third-party animal models (licensed)
│   └── Nature/           # Third-party nature models (licensed)
├── ThirdParty/           # Reserved for additional third-party assets
├── Prefabs/              # Legacy/root-level prefabs (Wolf, BirchTree, Rock)
├── Scenes/               # Legacy/root-level scene (SampleScene)
└── Settings/             # URP pipeline & volume profile assets
```

Key runtime classes:
- `BiomeDefinition` — ScriptableObject describing terrain, vegetation, animals, sky/fog
- `AnimalDefinition` — ScriptableObject describing an animal species
- `BiomeSceneGenerator` — Editor tool that builds a complete scene from a `BiomeDefinition`
- `AnimalWander` — Random walk behaviour injected per-animal
- `ClickableAnimal` — Click handler showing fun-fact text and playing audio
- `SimpleOrbitCamera` — Drag-to-rotate, scroll-to-zoom orbit camera

## Coding conventions

- Place runtime scripts in `Assets/Game/Scripts/`, editor scripts in `Assets/Game/Editor/`.
- One `MonoBehaviour` or `ScriptableObject` per file; filename matches class name.
- Use `[Header]`, `[Tooltip]`, `[Min]`, `[Range]` attributes to make the Inspector self-documenting.
- Use `[HideInInspector]` for fields set by code, not by designers.
- Prefix private fields with `_` (e.g., `_audioSource`).
- `Validate()` methods on ScriptableObjects for editor-time safety checks.
- Avoid `Awake/Start` side-effects that break scene generation — keep editor-safe.
- Prefer `FindObjectsByType` over the deprecated `FindObjectsOfType`.

## Testing expectations

- Editor tests for ScriptableObject validation logic.
- Play-mode tests for animal spawning and click interaction.
- No tests exist yet — add them under `Assets/Game/Tests/` with appropriate `.asmdef` files.

## Files and directories agents must not edit without explicit approval

| Path | Reason |
|---|---|
| `ProjectSettings/` | Unity project-wide settings; incorrect edits break the project |
| `Packages/manifest.json` | Adding/removing packages affects the whole project |
| `Packages/packages-lock.json` | Auto-generated lockfile |
| `Assets/Art/` | Licensed third-party assets |
| `Assets/ThirdParty/` | Licensed third-party assets |
| `*.meta` files | Unity GUIDs — editing breaks asset references |
| `*.unity` scene files | Large serialized YAML — prefer editor tooling |
| `*.prefab` files | Serialized YAML — prefer editor tooling |
| `.gitattributes` | LFS tracking rules |

## Security and privacy constraints

- This is a children's game. No network calls, no analytics, no user data collection.
- Third-party art assets under `Assets/Art/` have their own licenses (see `License.txt` files). Do not redistribute or modify license terms.

## Review checklist before final response

1. Does the change compile without errors in Unity? (Check console or `dotnet build` on the `.csproj`)
2. Are new scripts placed in the correct `Assets/Game/Scripts/` or `Assets/Game/Editor/` directory?
3. Does any new `MonoBehaviour` follow the existing pattern (attributes, `_`-prefixed privates, `Validate()` where applicable)?
4. Are `.meta` files left untouched?
5. Is the change child-safe (no violent/scary content, no external network calls)?
6. For ScriptableObjects: are inspector tooltips provided?
