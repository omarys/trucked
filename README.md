# Trucked

> **The game built on the [Fornach](https://github.com/opensec-git/Fornach) engine: an infinite tower, a grief-soaked story, and a Spectre.Console terminal UI.**

Trucked owns everything above the combat engine — the tower run and its floors, the shrine and loot
economy, the Ink-driven story and its Aspects, the tactical AI presses, and both the interactive and
headless clients. Fornach resolves fights; Trucked decides what fights mean.

---

## Project Structure

```
Dev/trucked/
├── Trucked.slnx                 # .NET 10 Solution file
├── Directory.Build.props        # FornachVersion: the engine version this game pins
├── nuget.config                 # Resolves the engine from ../fornach/packages
├── TODO.md                      # Open work only — the live ledger
├── docs/
│   ├── agents/domain.md         # What was built (§1) and the numbered ledger (§5)
│   └── STORY_AND_TOWER.md       # Story, tower and Aspect design
├── openspec/                    # Specs and recorded decisions
├── src/
│   ├── Trucked.Story/           # Ink story runtime, Aspects, bosses, story policies
│   │   ├── StoryTypes.fs        # Story state, choices, encounters
│   │   ├── Bosses.fs            # Grief manifestation bosses and their scaling
│   │   ├── Policies.fs          # The six Aspect presses, as engine policy ids
│   │   ├── StoryRunner.fs       # Ink driver and the story→combat bridge
│   │   └── Scripts/             # Ink sources (prologue.ink.json)
│   └── Trucked.Cli/             # Spectre.Console terminal UI and balance workbench
│       ├── Types.fs             # Simulation types and CLI models
│       ├── Archetypes.fs        # Preset tiers (Novice, Veteran, Master, GrandMaster)
│       ├── Theme.fs             # Palette and markup
│       ├── EnvironmentScenes.fs # Floor dressing
│       ├── Display.fs           # ANSI HUD, resource bars, roll breakdown, event log
│       ├── DeathAnimation.fs    # The collapse and execution animation
│       ├── Simulation.fs        # Monte-Carlo runner and dashboard tables
│       ├── TowerDisplay.fs      # Tower map, shrine and loot screens
│       └── Program.fs           # Entry point, CLI args, interactive menu loop
└── tests/
    └── Trucked.Tests/           # Story, tower, UI and game-balance suite (xUnit)
```

---

## Getting Started

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- A sibling Fornach checkout at `../fornach` — the engine is consumed as a package

### Building & Testing

```bash
# Build the game
dotnet build Trucked.slnx

# Run the suite
dotnet test Trucked.slnx
```

### Updating the engine

The engine arrives as `Fornach.Spatial`, `Fornach.Domain` and `Fornach.Engine` from the sibling
checkout's local feed (`../fornach/packages`), pinned by `FornachVersion` in
`Directory.Build.props`. After changing the engine:

```bash
cd ../fornach
dotnet pack src/Fornach.Spatial -c Release -o packages
dotnet pack src/Fornach.Domain  -c Release -o packages
dotnet pack src/Fornach.Engine  -c Release -o packages
```

Then bump `FornachVersion` here and `dotnet restore`. Re-packing under an existing version will not
be picked up: NuGet caches by version, so the version has to move.

---

## Running the Game

### 1. Interactive Menu Mode
Launch the interactive terminal interface:
```bash
dotnet run --project src/Trucked.Cli
```

The menu provides:
1. ⚔️ **Interactive Duel Arena**: Playable turn-by-turn combat with categorized menus (Physical, Social, Arcane, Recovery, Finisher), dynamic ANSI health/morale meters, tactical roll breakdown, and combat event logs.
2. 📊 **Monte-Carlo Balance Simulator**: Select any two combatants and simulate 50 to 1000 iterations to evaluate statistical balance and round pacing.
3. 🛠️ **Custom Combatant Builder**: Design combatants with custom stat allotments and immediately test them in combat.
4. 📜 **View Archetype Roster**: Inspect preset character sheets across Novice, Veteran, Master, and GrandMaster tiers.

### 2. Headless Simulation Mode (Scriptable / CI)
Run batch simulations directly from the command line:

```bash
# Balanced tactical duel (Veteran vs. Veteran)
dotnet run --project src/Trucked.Cli -- --sim -a1 "Veteran Fighter" -a2 "Veteran Mage" -n 100

# Skewed matchup verification (GrandMaster vs. Novice)
dotnet run --project src/Trucked.Cli -- --sim -a1 "Grandmaster Fighter" -a2 "Soldier" -n 50

# Mirror match balance check
dotnet run --project src/Trucked.Cli -- --sim -a1 "Veteran Mage" -a2 "Veteran Mage" -n 100
```

#### CLI Simulation Flags:
- `--sim` / `-s`: Run in headless simulation mode (skips interactive menu).
- `-a1` / `--archetype1 <name>`: First combatant name (default: `"Veteran Fighter"`).
- `-a2` / `--archetype2 <name>`: Second combatant name (default: `"Veteran Mage"`).
- `-n` / `--iterations <count>`: Number of simulated duels (default: `100`).

---

## Where the Work Is Tracked

- **`TODO.md`** — open work only, each item carrying the ledger number it comes from.
- **`docs/agents/domain.md`** — §1 what was built, §5 the numbered ledger. `TODO.md` must not
  invent a second numbering.
- **`docs/STORY_AND_TOWER.md`** — story, tower and Aspect design.

Balance numbers quoted in either file are measured through the engine's `CombatSimulation` harness,
so they can be reproduced from this repo with a single `dotnet test`.

---

## License

MIT
