# Playable Demo V1 Design

## Goal

Upgrade the existing cockroach survival prototype into a clearer 3-5 minute playable demo while preserving the current procedural scene approach.

## Scope

This iteration improves four areas together:

- Route readability: make the apartment route easier to understand from spawn to food, hiding, egg laying, and escape.
- Modeling: add recognizable low-poly route and cover props through the existing Blender-to-Unity resource pipeline.
- Interaction UI: reorganize the HUD around survival state, current objective, area hints, and run results.
- Game pacing: make the first playable loop feel like a complete mini-run without replacing the existing AI system.

## Player Loop

The run starts with a short grace period. The player explores the apartment as a cockroach, follows scent and floor clues toward food, avoids humans and pets, hides under furniture when threatened, lays eggs only while hidden, and clears the stage by completing the visible objectives.

The first stage objective sequence is:

1. Eat enough food.
2. Reach or use a hiding area.
3. Lay one egg cluster.
4. Survive and escape after being detected.

## Route Design

The apartment keeps its current kitchen, living room, bedroom, and bathroom layout. New route readability comes from visual markers rather than hard navigation arrows:

- Scent trail pieces point from common spawn positions toward food-rich areas.
- Wall cracks, pipe gaps, and floor stains act as small landmarks.
- Hide spots get clearer glowing floor silhouettes.
- Area names appear in the HUD so the player understands where they are.

## Modeling Design

The implementation continues the existing asset replacement strategy:

- Runtime gameplay collision remains simple boxes and triggers.
- Visual props are loaded from `Assets/Resources/Models/Environment`.
- Blender generation is used for new low-poly props.

New visual prop targets:

- Trash can and crumbs near the kitchen.
- Cardboard box and slipper near living room routes.
- Pipe gap and wall crack near safe travel routes.
- Larger food scrap pile for stronger objective readability.

## UI Design

The HUD should be easier to scan while moving:

- Left panel: survival time, stage, eaten food, eggs, hidden state, noise, suspicion.
- Center objective banner: the next meaningful action in plain Chinese.
- Bottom event text: temporary feedback such as detection, food eaten, or egg blocked.
- Right panel: current area, suggested route, controls, and best local scores.
- Result panel: stage reached, food eaten, eggs laid, survival time, death reason or clear message.

## Code Design

The current prototype is concentrated in one large script. This iteration keeps the file structure stable to avoid risky refactors, but adds small helper types inside the same namespace:

- `DemoObjectiveStep`: tracks the current route objective.
- `RouteLandmark`: describes placed route props and nearby hint text.
- `RunEndState`: stores run result text for the result panel.

New behavior should be isolated in methods with clear names:

- Build route landmarks and visual trail props after apartment geometry.
- Track current apartment area from player position.
- Update objective stage based on food, hidden state, eggs, and detection escape.
- Build and update the revised HUD and result panel.

## Testing And Verification

This Unity project currently has no dedicated edit-mode test assembly. Verification for this iteration will use:

- C# compile check by opening or batch-running Unity.
- Git diff review for unintended file churn.
- Manual Play Mode smoke test when Unity batch mode is available.
- If batch mode cannot run in the environment, verify scripts with static inspection and document the blocker.

## GitHub Update

After implementation and verification, commit the project changes and push `main` to:

`https://github.com/liizyii/IfYouWereCockroach.git`
