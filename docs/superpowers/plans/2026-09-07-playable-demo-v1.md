# Playable Demo V1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a clearer 3-5 minute cockroach survival demo with route hints, improved HUD, result panel, and additional low-poly route props.

**Architecture:** Keep the existing runtime-generated prototype and collision model. Add small pure helper types for objective and route text, then integrate them into `CockroachGameManager` without restructuring the whole prototype file.

**Tech Stack:** Unity 2022.3.62f3c1, C#, UGUI, Blender Python, Git/GitHub.

---

## File Map

- Modify: `Assets/Scripts/CockroachPrototype/CockroachPrototype.cs` for objective state, route landmarks, HUD, and result panel.
- Create: `Assets/Tests/EditMode/PlayableDemoObjectiveTests.cs` for pure objective and route helper tests.
- Modify: `Assets/Tools/Blender/create_environment_models.py` to generate extra route props.
- Create/Update: `Assets/Resources/Models/Environment/RouteProps_LowPoly.fbx` from Blender.
- Create: `docs/superpowers/specs/2026-09-07-playable-demo-v1-design.md` and this implementation plan.

### Task 1: Objective Helper Tests

**Files:**
- Create: `Assets/Tests/EditMode/PlayableDemoObjectiveTests.cs`
- Modify: `Assets/Scripts/CockroachPrototype/CockroachPrototype.cs`

- [ ] Step 1: Add tests for objective step selection: food first, hidden next, egg next, escape last, clear after escape.
- [ ] Step 2: Run Unity edit-mode tests and confirm the helper is missing or failing.
- [ ] Step 3: Add `DemoObjectiveStep` and `DemoObjectivePlanner` to the prototype namespace.
- [ ] Step 4: Run Unity edit-mode tests again and confirm they pass.

### Task 2: Route And Area UI

**Files:**
- Modify: `Assets/Scripts/CockroachPrototype/CockroachPrototype.cs`

- [ ] Step 1: Track current area from player position using the existing apartment coordinates.
- [ ] Step 2: Add objective banner and route panel text to `BuildUi`.
- [ ] Step 3: Update `UpdateUi` to show current area, suggested route, and controls.
- [ ] Step 4: Verify C# compile through Unity batch mode.

### Task 3: Result Panel

**Files:**
- Modify: `Assets/Scripts/CockroachPrototype/CockroachPrototype.cs`

- [ ] Step 1: Store the latest run end message when the player dies or clears a challenge.
- [ ] Step 2: Add a centered result panel that appears when the run is no longer alive.
- [ ] Step 3: Include survival time, food eaten, egg count, stage, and restart prompt.
- [ ] Step 4: Verify C# compile through Unity batch mode.

### Task 4: Route Landmarks And Models

**Files:**
- Modify: `Assets/Tools/Blender/create_environment_models.py`
- Modify: `Assets/Scripts/CockroachPrototype/CockroachPrototype.cs`
- Create/Update: `Assets/Resources/Models/Environment/RouteProps_LowPoly.fbx`

- [ ] Step 1: Add Blender generation for trash can, cardboard box, slipper, pipe gap, wall crack, scent flecks, and scrap pile in one FBX.
- [ ] Step 2: Run Blender in background to export the FBX into the existing Resources environment folder.
- [ ] Step 3: Load route props at runtime when available and fall back to primitives if the FBX is missing.
- [ ] Step 4: Verify the generated asset exists and the C# compile still passes.

### Task 5: GitHub Update

**Files:**
- All changed files

- [ ] Step 1: Review `git status` and `git diff --stat` for unexpected files.
- [ ] Step 2: Run final Unity compile/test verification.
- [ ] Step 3: Commit changes with message `feat: improve playable demo route and hud`.
- [ ] Step 4: Push `main` to `origin`.
- [ ] Step 5: Report the GitHub repository link and pushed commit hash.

## Self Review

The plan covers every design requirement: route readability, modeling, interaction UI, pacing, verification, and GitHub update. It avoids a broad refactor of the large prototype file and keeps the demo scoped to one playable apartment loop. Unity batch verification is the main automated check because the project has no existing test assembly yet; Task 1 adds focused edit-mode tests for the new pure objective helper.
