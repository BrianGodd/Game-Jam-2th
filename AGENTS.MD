# Unity Notes

- Do not add null guards, fallback lookups, `FindObjectOfType`/`FindFirstObjectByType`, instance fallbacks, or silent skips for required references. If a required reference is missing, let the error surface so the problem is easy to locate.

- Do not add unnecessary features, state, duplicated data, synchronization, or defensive code "just in case." Keep logic simple, predictable, and close to the original design. Refactor only for a clear problem or explicit requirement.

- If requirements are unclear or an architecture choice is risky, ask the user first or list the decisions that need confirmation. Do not make hidden assumptions.

# Project Notes

- This is a Unity 2022.3.62f3 3D horror game prototype in `Game-Jam-2th`.
- Important installed packages: URP 14.0.12, Input System 1.14.0, Cinemachine 2.10.3, TextMeshPro 3.0.7, Unity Test Framework 1.1.33.
- Current base: Unity Starter Assets First Person Controller and scenes under `Assets/Scenes`.
- Unity MCP server does not exist in this setup; use file edits plus `dotnet build` and manual Unity checks.
- Existing gameplay code is minimal; prefer adding project scripts under `Assets/Scripts` instead of editing Starter Assets.
- Do not touch UI or art/filter work unless explicitly requested. Current priority is playable horror mechanics.
- Target game direction: first-person exploratory horror in a laboratory, with a night guard/admin role, monster containment, patrol/survival pressure, doors, lights, cage/trap interactions, and a 4-day escalation loop.
- Keep the first playable slice small: player exploration, monster patrol/chase, door/cage interaction, survival timer. Add monitoring, broadcasts, potions, story reversal, and polish only after that loop works.
- Reuse existing packages and Unity components before adding dependencies. Do not add new packages for simple mechanics.
- Avoid speculative architecture. One clear MonoBehaviour is better than a framework until the behavior needs to split.
- Preserve unrelated user changes. This worktree may already be dirty.
- For non-trivial gameplay logic, leave one small runnable check when practical, or verify in Unity/`dotnet build` and report what was run.
