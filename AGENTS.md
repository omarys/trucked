# Trucked agent notes

Trucked is the game; [Fornach](https://github.com/opensec-git/Fornach) is the engine it is built on.
The engine arrives as NuGet packages from the sibling checkout (`../fornach/packages`), so this repo
can read engine source but cannot change engine behaviour from here.

## The boundary

- **Engine (do not duplicate here):** dice pools, action resolution, collapse and execution,
  stat scaling, the spatial grid and field of view, and the headless balance harness
  (`Fornach.Engine.CombatSimulation`). If a rule belongs to combat resolution, it belongs upstream.
- **Game (this repo):** the tower run, floors and their enemies, the shrine and loot economy, the Ink
  story and its Aspects, the tactical AI presses registered into the engine's policy table, and both
  clients.
- The engine names no Aspect, no story gambit and no protagonist. If you find yourself writing
  `name.Contains "Guilt"` or `Name = "PlayerCharacter"`, the behaviour belongs in a policy registered
  from here instead — see `src/Trucked.Story/Policies.fs`.

## Changing the engine

Pack it, then bump `FornachVersion` in `Directory.Build.props`:

```bash
mise run pack-engine   # or dotnet pack each project into ../fornach/packages
dotnet restore
```

NuGet caches by version, so re-packing under the same version is invisible to this repo. The version
has to move.

## Where the work is tracked

`TODO.md` holds open work only, and every item carries the ledger number it comes from in
`docs/agents/domain.md` §5. Do not invent a second numbering. Balance claims are measured through
`CombatSimulation`, so a number quoted in the ledger should be reproducible from `dotnet test`.

<!-- openlore-decisions-instructions -->
## Architectural decisions

`record_decision` is **not** part of the wired `substrate` surface, so do not plan around calling it.

When making a significant design choice, record it with the CLI **before** writing the code:

```bash
openlore decisions record --title "Use JWTs for stateless auth" --rationale "Avoids session store in infra" \
  --consequences "Tokens can't be revoked early" --files src/auth/middleware.ts
```

To expose the MCP tool as well: `openlore install --preset full`  — then this section regenerates with the MCP workflow.

## When git commit is blocked by the decisions gate

If `git commit` fails and the output is JSON with `"gated": true`, do NOT retry silently.
Check the `reason` field and act accordingly.

**`reason: "verified"` — decisions await review:**
Present each decision to the user:
> "The commit is blocked — I found N architectural decision(s) to validate:
> 1. **[id]** Title — rationale
Do you approve? (yes/no)"
For each approval run `openlore decisions --approve <id>`, for each rejection `openlore decisions --reject <id>` (both work on every MCP preset).
Then run `openlore decisions --sync` and retry `git commit`.

**`reason: "approved_not_synced"` — decisions approved but not written to specs:**
Run `openlore decisions --sync` then retry `git commit`. Do not skip this step.

**`reason: "drafts_pending_consolidation"` — drafts were recorded but not yet consolidated:**
Present to the user:
> "N decision draft(s) were recorded but never consolidated. Run consolidation now? (~10-30s)"
If yes: run `openlore decisions --consolidate --gate` and handle the result.
If no: retry with `git commit --no-verify` to skip the gate.

**`reason: "no_decisions_recorded"` — source files staged but nothing recorded:**
Present to the user:
> "Source files are staged but no architectural decisions were recorded. Run fallback extraction to check for undocumented decisions? (~10-30s)"
If yes: run `openlore decisions --consolidate --gate` and handle the result.
If no: retry with `git commit --no-verify` to skip the gate.
<!-- end-openlore-decisions-instructions -->
