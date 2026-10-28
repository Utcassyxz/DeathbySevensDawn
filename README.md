# Death by Seventh's Dawn

## Open the current playable apartment

The active Unity project is **`Weekend/`**. Add that folder in Unity Hub using **Unity 6000.6.7f1**, then open `Assets/Scenes/Apartment/LivingRoom.unity` and press Play. Click the Game view and hold WASD or the arrow keys to move; walking into a doorway changes rooms.

The five room scenes are grouped under `Weekend/Assets/Scenes/Apartment/`: LivingRoom, Bedroom, Bathroom, Balcony, and Corridor. The root-level `Assets/Scripts/` contains the earlier foundation; continue gameplay work in `Weekend/`.

See [development log](docs/DEVLOG.md) for the current state and apartment polish milestone.

**Death by Seventh's Dawn** is a narrative-driven cosmic-horror time-loop game built in Unity.

The world is struck by the passing influence of an incomprehensible cosmic entity. Its radiation causes humanity to mutate over seven days. The protagonist dies on Day 7, but Yog-Sothoth takes an interest in him and allows his consciousness to return to Day 1.

The player cannot keep weapons, health, items, or physical progress between loops. What survives is **knowledge**.

> You do not become stronger because your character remembers.  
> You become stronger because *you know what will happen next*.

## 最新可玩流程（2026-10-28）

首次旅程以童年蚁穴梦境开场，惊醒后进入卧室。J打开日记，靠近床/电视/窗户/望远镜按E互动。第二夜望远镜可发现古神轮廓。测试界面默认隐藏：~输入1997，本次游戏只需一次，之后~开关。

脚本编译通过，新增玩法尚未完成Play Mode验收。请查看[最新开发存档](docs/开发存档-2026-10-28.md)和[剧情修改说明](docs/日记梦境与望远镜说明.md)。下文为早期设计背景，当前实现以最新存档为准。

## Visual Direction

The game intentionally uses three visual layers:

- **Exploration:** low-resolution 2D pixel art inspired by story-heavy indie RPGs such as *Undertale*, while developing its own visual identity.
- **Boss encounters:** 3D spaces and 3D entities so cosmic beings feel like they exist on a different level of reality.
- **Endings:** high-resolution illustrations / CGs used as emotional payoff and final-state summaries.

The UI, symbols, typography, recurring motifs, and color language should stay consistent across all three styles.

## Core Gameplay Loop

```text
DAY 1
  ↓
Explore / Talk / Investigate
  ↓
Spend time
  ↓
Events change as the week progresses
  ↓
Gain knowledge
  ↓
DAY 7
  ↓
Death
  ↓
The Void / Yog-Sothoth
  ↓
Return to DAY 1
  ↓
Knowledge remains
```

## Design Pillars

### 1. Knowledge is progression

Most state resets after death:

- HP
- inventory
- location
- mutation level
- NPC physical state
- current day and time

Persistent state includes:

- discovered knowledge
- major truths
- loop count
- unlocked dialogue options
- ending records

Example:

```text
First loop:
Doctor: "There is no basement level."

Later loop:
[Knowledge] "Then what is room B2-17?"
```

### 2. Seven-day structure

Each day is divided into four periods:

```text
Morning
Afternoon
Evening
Night
```

Important actions advance time. The player cannot see every event in one loop, which makes replaying the week meaningful rather than repetitive.

### 3. Mutation is not purely negative

Mutations can grant abilities while changing how the world reacts to the player.

Examples:

- Eye mutation: perceive hidden spatial fractures.
- Arm mutation: stronger melee attacks, reduced ability to use ordinary equipment.
- Brain mutation: perceive fragments of other timelines at the cost of stability.

### 4. Cosmic beings should remain incomprehensible

Yog-Sothoth is not a conventional quest-giver. The player should never be fully certain whether the time loop is a gift, experiment, accident, or something that has already happened infinitely many times.

## Prototype Scope

The first playable prototype should contain only:

- one apartment / hallway area
- one playable character
- two NPCs
- one investigation object
- one persistent Knowledge flag
- time advancement
- forced death / reset
- one Void scene
- one changed dialogue option after looping
- one placeholder 3D boss encounter

The goal is to validate this loop:

```text
2D exploration → discovery → death → Void → reset → changed 2D dialogue
```

## Project Structure

```text
Assets/
├── Art/
│   ├── Characters/
│   ├── Environment/
│   ├── Bosses/
│   ├── UI/
│   └── EndingCG/
├── Audio/
│   ├── BGM/
│   └── SFX/
├── Materials/
├── Prefabs/
│   ├── Characters/
│   ├── NPCs/
│   ├── Enemies/
│   └── UI/
├── Scenes/
│   ├── 2D/
│   ├── Boss/
│   └── System/
├── Scripts/
│   ├── Core/
│   ├── Player/
│   ├── Dialogue/
│   ├── Time/
│   ├── Knowledge/
│   ├── Events/
│   ├── Mutation/
│   ├── Boss/
│   └── Ending/
└── ScriptableObjects/
    ├── Dialogue/
    ├── Knowledge/
    └── Endings/
```

## Current Systems

This repository starts with a small, compile-friendly foundation:

- `GameManager` — persistent global state entry point.
- `TimeManager` — tracks Day 1–7 and time periods.
- `LoopManager` — resets temporary state and increments the loop count.
- `KnowledgeManager` — stores knowledge that survives loops.
- `PlayerController2D` — basic top-down movement prototype.
- `DialogueData` — ScriptableObject-ready dialogue lines with knowledge gates.

These are intentionally small. The first milestone is a complete playable loop, not a large framework.

## First Milestone

### Milestone 0.1 — The First Loop

- [ ] Create `Apartment` scene
- [ ] Add top-down player movement
- [ ] Add one NPC
- [ ] Add dialogue box
- [ ] Add one investigable object
- [ ] Unlock one Knowledge ID
- [ ] Advance time through a debug interaction
- [ ] Trigger death at the end of the loop
- [ ] Load a temporary Void scene
- [ ] Reset to Day 1
- [ ] Show a new dialogue choice based on retained Knowledge

## Planned Later Systems

- branching dialogue system
- event scheduler
- mutation system
- inventory
- save/load
- 3D boss battle framework
- ending-condition evaluator
- high-resolution ending CG viewer
- adaptive audio / ambience

## Boss Direction

Boss encounters should use 3D as a deliberate break from normal reality rather than simply changing genres.

Recommended prototype approach:

- 3D boss and arena
- 2D battle UI layered over the 3D scene
- simple dodge / bullet-pattern mechanics
- shared dialogue and knowledge systems
- scene transition back into the 2D world

The first boss can be a human NPC seen earlier in pixel form. When the encounter begins, the game reveals what that person actually looks like in three dimensions.

## Development Rule

**Do not build the whole seven-day story before the loop works.**

The immediate goal is to prove one complete loop is fun, readable, and technically stable before expanding the world.