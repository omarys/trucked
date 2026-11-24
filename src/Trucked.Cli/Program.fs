namespace Trucked.Cli

open System
open Spectre.Console
open Fornach.Domain
open Fornach.Engine
open Trucked.Story

module Program =

  let private printBanner () =
    let termWidth = Math.Max(80, AnsiConsole.Profile.Width)
    let termHeight =
      if Console.IsOutputRedirected || Console.WindowHeight <= 0 then 30
      else Console.WindowHeight

    AnsiConsole.Write(
      FigletText("TRUCKED")
        .Centered()
        .Color(Theme.ColorPurple)
    )

    let telemetryGrid = Grid()
    telemetryGrid.AddColumn(GridColumn().Centered()) |> ignore
    telemetryGrid.AddColumn(GridColumn().Centered()) |> ignore
    telemetryGrid.AddColumn(GridColumn().Centered()) |> ignore
    telemetryGrid.AddRow(
      Markup(sprintf "[grey]󰍹 Screen:[/] [bold %s]%dx%d[/]" Theme.Cyan termWidth termHeight),
      Markup(sprintf "[grey]󰘚 Engine:[/] [bold %s]v0.3.0 Turbo[/]" Theme.Green),
      Markup(sprintf "[grey]󰌌 Input:[/] [bold %s]Vim (j/k/h/l) + Pad[/]" Theme.Pink)
    ) |> ignore
    telemetryGrid.Expand <- true

    let telemetryPanel =
      Panel(telemetryGrid)
        .Border(BoxBorder.Rounded)
        .BorderStyle(Theme.StyleCurrentLine)
        .Header(Display.stripNerdGlyphs (sprintf "[bold %s] 󰒋 COMMAND COCKPIT & SYSTEM TELEMETRY [/]" Theme.Yellow))
        .Expand()

    AnsiConsole.Write(telemetryPanel)
    AnsiConsole.WriteLine()

  let private promptSelectArchetype (title: string) (roster: ArchetypeInfo list) : ArchetypeInfo =
    let prompt =
      SelectionPrompt<ArchetypeInfo>()
        .Title(title)
        .PageSize(10)
        .HighlightStyle(Theme.StylePromptHighlight)
        .UseConverter(fun a ->
          let tierColor =
            match a.Tier with
            | Novice -> Theme.Comment
            | Veteran -> Theme.Cyan
            | Master -> Theme.Yellow
            | GrandMaster -> Theme.Purple
          sprintf "[bold %s][[%A]][/] [bold %s]󰀝 %-22s[/] ([%s]%A[/]) ── [%s]%s[/]"
            tierColor a.Tier Theme.Foreground a.Name Theme.Pink a.Discipline Theme.Comment a.Description)

    prompt.AddChoices(roster) |> ignore
    Display.promptWithVim prompt

  let parseActionChoice (choice: string) : ActionIntent =
    if choice.Contains("EXECUTE FINISHER (Physical") then
      ExecuteStrike Physical
    elif choice.Contains("EXECUTE FINISHER (Mental") then
      ExecuteStrike Mental
    elif choice.Contains("Fortitude Guard") then
      ActiveDefense (FortitudeGuard (choice.Contains("Braced")))
    elif choice.Contains("Reflex Slip") then
      ActiveDefense (ReflexSlip (choice.Contains("Evasive")))
    elif choice.Contains("Resolve Anchor") then
      ActiveDefense (ResolveAnchor (choice.Contains("Iron Will")))
    elif choice.Contains("Intuition Perception") then
      ActiveDefense (IntuitionPerception (choice.Contains("Insight")))
    elif choice.Contains("Force Strike: Standard Cleave") then
      StandardAttack (ForceStrike false)
    elif choice.Contains("Force Strike: Wild Blow") then
      StandardAttack (ForceStrike true)
    elif choice.Contains("Force Strike: Insight Cleave") then
      StandardAttack ForceInsightStrike
    elif choice.Contains("Finesse Cadence: Rapid Probing") then
      StandardAttack (FinesseCadence false)
    elif choice.Contains("Finesse Cadence: Relentless Blitz") then
      StandardAttack (FinesseCadence true)
    elif choice.Contains("Finesse Cadence: Threaded Opening") then
      StandardAttack FinesseInsightStrike
    elif choice.Contains("Prowess Strike: Stance Pressure") then
      StandardAttack (ProwessStrike false)
    elif choice.Contains("Prowess Strike: Invitational Bait") then
      StandardAttack (ProwessStrike true)
    elif choice.Contains("Shield Bash") then
      // The menu only offers this during a telegraphed wind-up, so it is always the committed slam.
      StandardAttack (ShieldBash true)
    elif choice.Contains("Authority Decree: Imperious Command") then
      StandardAttack (AuthorityDecree false)
    elif choice.Contains("Authority Decree: Frustrationing Demand") then
      StandardAttack (AuthorityDecree true)
    elif choice.Contains("Guile Deception: Rhetorical Misdirection") then
      StandardAttack (GuileDeception false)
    elif choice.Contains("Guile Deception: Confidence Trap") then
      StandardAttack (GuileDeception true)
    elif choice.Contains("Prowess Interrogation: Procedural Pressure") then
      StandardAttack (AcumenInterrogation false)
    elif choice.Contains("Prowess Interrogation: Socratic Checkmate") then
      StandardAttack (AcumenInterrogation true)
    elif choice.Contains("Arcane Cataclysm: Elemental Blast") then
      StandardAttack (ArcaneCataclysm false)
    elif choice.Contains("Arcane Cataclysm: Overchannel") then
      StandardAttack (ArcaneCataclysm true)
    elif choice.Contains("Synaptic Glamour: Neural Static") then
      StandardAttack (SynapticGlamour false)
    elif choice.Contains("Synaptic Glamour: Mind Fracture") then
      StandardAttack (SynapticGlamour true)
    elif choice.Contains("Mirror Illusion: Phantasmal Decoys") then
      StandardAttack (MirrorIllusion false)
    elif choice.Contains("Mirror Illusion: Decoy Swarm") then
      StandardAttack (MirrorIllusion true)
    elif choice.Contains("Runic Ward Trap: Abjuration Glyph") then
      StandardAttack (RunicWardTrap false)
    elif choice.Contains("Runic Ward Trap: Anomalous Glyph") then
      StandardAttack (RunicWardTrap true)
    elif choice.Contains("Disorienting Shockwave: Balance Disruption") then
      StandardAttack (DisorientingShockwave false)
    elif choice.Contains("Disorienting Shockwave: Staggering Pulse") then
      StandardAttack (DisorientingShockwave true)
    elif choice.Contains("Staff Siphon") then
      StandardAttack StaffSiphon
    elif choice.Contains("Shift Stance: Power Stance") then
      ShiftStance CombatStance.PowerStance
    elif choice.Contains("Shift Stance: Agility Stance") then
      ShiftStance CombatStance.AgilityStance
    elif choice.Contains("Shift Stance: Discipline Stance") then
      ShiftStance CombatStance.DisciplineStance
    elif choice.Contains("Calculated Flaw Strike") then
      StandardAttack CalculatedFlawStrike
    elif choice.Contains("Masterful Disarm") then
      StandardAttack (MasterfulDisarm 3)
    elif choice.Contains("Deploy Prep: Shockwave Slam") then
      DeployPreparation (PreparationType.ShockwaveSlam, None)
    elif choice.Contains("Deploy Prep: Berserk Tincture") then
      DeployPreparation (PreparationType.BerserkTincture, None)
    elif choice.Contains("Deploy Prep: Dread Warhorn") then
      DeployPreparation (PreparationType.DreadWarhorn, None)
    elif choice.Contains("Deploy Prep: Synaptic Brand") then
      DeployPreparation (PreparationType.SynapticBrand, None)
    elif choice.Contains("Deploy Prep: Caltrop Pouch") then
      DeployPreparation (PreparationType.CaltropPouch, None)
    elif choice.Contains("Deploy Prep: Concealed Blade") then
      DeployPreparation (PreparationType.ConcealedBlade, None)
    elif choice.Contains("Deploy Prep: Mirror Mirage") then
      DeployPreparation (PreparationType.MirrorMirage, None)
    elif choice.Contains("Deploy Prep: Prismatic Flare") then
      DeployPreparation (PreparationType.PrismaticFlare, None)
    elif choice.Contains("Deploy Prep: Bastion Zone Control") then
      DeployPreparation (PreparationType.BastionZoneControl, None)
    elif choice.Contains("Deploy Prep: Aegis of Retribution") then
      DeployPreparation (PreparationType.AegisOfRetribution, None)
    elif choice.Contains("Deploy Prep: Heraldic Treatise") then
      DeployPreparation (PreparationType.HeraldicTreatise, None)
    elif choice.Contains("Deploy Prep: Socratic Dossier") then
      DeployPreparation (PreparationType.SocraticDossier, None)
    elif choice.Contains("Denial Phase Shift") then
      StandardAttack (TraumaAttack FinesseGambit)
    elif choice.Contains("Basalt Eruption") then
      StandardAttack (TraumaAttack ForceGambit)
    elif choice.Contains("Coercive Bargain") then
      StandardAttack (TraumaAttack AcumenGambit)
    elif choice.Contains("Apathy Doldrums") then
      StandardAttack (TraumaAttack FortitudeGambit)
    elif choice.Contains("Serene Resolution") then
      StandardAttack (TraumaAttack ComposureGambit)
    elif choice.Contains("Thread Form: Resonance Spike") then
      ThreadComplexForm ComplexForm.ResonanceSpike
    elif choice.Contains("Thread Form: Phantasmal Diffusion") then
      ThreadComplexForm ComplexForm.PhantasmalDiffusion
    elif choice.Contains("Thread Form: Aegis Lattice") then
      ThreadComplexForm ComplexForm.AegisLattice
    elif choice.Contains("Raise Shield") then
      Wait
    elif choice.Contains("Wait") then
      Wait
    elif choice.Contains("Center Mind") then
      RecoveryAction CenterMind
    else
      RecoveryAction SteadyForm

  /// True when the telegraphed intent is a heavy wind-up that a Shield Bash can break. Shield Bash is
  /// gated on this: there is nothing to interrupt otherwise.
  let isInterruptibleWindup (enemyIntentOpt: ActionIntent option) : bool =
    match enemyIntentOpt with
    | Some (StandardAttack atk) ->
      match atk with
      | ForceStrike true | FinesseCadence true | ProwessStrike true | ArcaneCataclysm true | TraumaAttack _ -> true
      | _ -> false
    | _ -> false

  /// True when the player can actually read the wind-up. A heavy strike the scrutiny ladder has masked as
  /// "Obscured" earns no interrupt in the menu: offering it would hand back exactly what the ladder just
  /// took away, and the player would spend a turn bashing a windup they were never shown.
  let isWindupReadable (player: Combatant) (enemy: Combatant) (enemyIntentOpt: ActionIntent option) : bool =
    match enemyIntentOpt with
    | Some intent when isInterruptibleWindup enemyIntentOpt ->
      let _, _, _, vector = Display.formatTelegraphedIntentWithLadder player enemy intent
      vector <> "Obscured"
    | _ -> false

  let private formatPrepOption (slot: PreparationSlot) : string =
    let catTag =
      match slot.Type.Category with
      | CrowdControl -> "Crowd Control"
      | SingleTargetDuel -> "Duelist Edge"
    let shortDesc =
      match slot.Type with
      | ShockwaveSlam -> "Surplus NetHits (>=3) splash flat damage to flankers"
      | BerserkTincture -> "Spike Recklessness into Fever Pitch for bonus dice & damage"
      | DreadWarhorn -> "Inflict +25 Cognitive Fatigue on all active foes"
      | SynapticBrand -> "Mark target: criticals deal 2x Morale damage & Rupture"
      | CaltropPouch -> "Blind flankers, stripping multi-opponent defense penalty"
      | ConcealedBlade -> "Quick-draw Nach counter: puncture incoming strike & seize Vor"
      | MirrorMirage -> "Flankers strike illusions (+20 Confusion & lose action)"
      | PrismaticFlare -> "Blinding burst: enemy Recklessness spikes trigger Morale shock"
      | BastionZoneControl -> "Plant polearm/shield: cap simultaneous attackers to 3"
      | AegisOfRetribution -> "-35% damage taken & reflect 50% back as radiant damage"
      | HeraldicTreatise -> "Grant +2 Combat Ramp across all engaged foes"
      | SocraticDossier -> "Convert target's Recklessness into unmitigated Morale damage"
    sprintf
      "🎒 [bold %s]Deploy Prep: %s[/] (%s • %s) [[%d/%d uses]]"
      Theme.Orange
      slot.Type.Name
      catTag
      shortDesc
      slot.RemainingUses
      slot.MaxUses

  let private buildActionChoicesCore (player: Combatant) (enemy: Combatant) (enemyIntentOpt: ActionIntent option) : string list =
    let prepSource =
      if not player.Preparations.IsEmpty then player.Preparations
      else player.Progression.Preparations
    let availablePreps =
      prepSource
      |> List.filter (fun s ->
        s.RemainingUses > 0
        && not (player.HasActivePreparation s.Type)
        // Only the kit grounded in the active stance can be readied while that stance is held.
        && s.Type.Stance = player.Stance)

    let pressAdvTag =
      if enemy.IsStaggered then sprintf " [bold %s]★ PRESS THE ADVANTAGE (Target Staggered!)[/]" Theme.Green
      else ""

    [
      // Finisher (only for the player's operative discipline)
      if enemy.IsExecuteEligible then
        if player.Plane = Physical then
          sprintf "☠️  [bold blink %s]EXECUTE FINISHER (Physical Strike)[/]" Theme.Red
        else
          sprintf "☠️  [bold blink %s]EXECUTE FINISHER (Mental Strike)[/]" Theme.Red

      // Stage Manifestation & High-Ramp Environmental Gambits (when in Vor)
      if not player.IsInNach then
        if enemy.Name.Contains("Acceptance") || player.Name.Contains("Acceptance") then
          sprintf "🕊️  [bold %s]Trauma: Serene Resolution[/] (Pacifist Release: Drain Recklessness & Frustration to 0, +50 Morale)" Theme.Cyan
        elif enemy.Name.Contains("Denial") || player.Name.Contains("Denial") || (player.Plane = Mental && player.Flow >= 3) then
          sprintf "🌫️  [bold %s]Env Gambit: Denial Phase Shift[/] (Mist Shroud: 1.25x Agility strike, +25 Confusion, -15 Frustration)" Theme.Green
        elif enemy.Name.Contains("Anger") || enemy.Name.Contains("Guilt") || player.Name.Contains("Anger") || (player.Plane = Physical && player.Momentum >= 3) then
          sprintf "🌋 [bold %s]Env Gambit: Basalt Eruption[/] (Quarry Slag Smash: 1.5x Power cleave, shred 25 Armor, +30 Frustration)" Theme.Red
        // AcumenGambit is a Social/Mental move (Actions.fs classifies it CombatMode.Social) — the engine offers it
        // only as the Bargaining-stage story device, never as a generic Physical unlock (a Fighter cannot coerce).
        elif enemy.Name.Contains("Bargaining") || player.Name.Contains("Bargaining") then
          sprintf "⚖️  [bold %s]Env Gambit: Coercive Bargain[/] (Predatory Leverage: Steal 35 Morale to restore +25 HP, +25 Provoke)" Theme.Purple
        elif enemy.Name.Contains("Depression") || player.Name.Contains("Depression") || (player.Plane = Mental && player.Momentum >= 3) then
          sprintf "⚓ [bold %s]Trauma: Apathy Doldrums[/] (Submerged Weight: Inflict +40 Cognitive Fatigue & +25 Exhaustion)" Theme.Comment

      let assess = TacticalAssessment.assess player enemy

      // Tactical Counterplay Badges based on Telegraphed Enemy Intent
      let powerCounterTag, agilityCounterTag, disciplineCounterTag, shieldInterruptTag, raiseShieldTag =
        match enemyIntentOpt with
        | Some (StandardAttack atk) ->
          let isHeavyGambit = isInterruptibleWindup enemyIntentOpt

          // Only an interruptible wind-up earns interrupt wording: a plain Power strike has nothing to
          // break, and advertising an opportunity the menu cannot honour is noise.
          let interruptBadge =
            if isHeavyGambit then sprintf " [bold %s]★ SHIELD INTERRUPT (Disrupts Windup!)[/]" Theme.Yellow
            else ""

          let powTag = if atk.Vector = Vector.Discipline then sprintf " [bold %s]★ Guard Break[/]" Theme.Red else ""
          let agiTag = if atk.Vector = Vector.Power then sprintf " [bold %s]★ Counter-Evasion[/]" Theme.Green else ""
          let disTag = if atk.Vector = Vector.Agility then sprintf " [bold %s]★ Bastion Counter[/]" Theme.Purple else ""
          // The merged hold widens the Indes window against every incoming vector while its plating is
          // up -- the old buckler heuristic (Agility only) undersold the hold.
          let rShieldTag = sprintf " [bold %s]★ Parry Opening (Widened Indes)[/]" Theme.Green

          powTag, agiTag, disTag, interruptBadge, rShieldTag
        | _ -> "", "", "", "", ""

      let isCounterPower =
        match enemyIntentOpt with
        | Some (StandardAttack atk) -> atk.Vector = Vector.Power
        | _ -> false

      let isCounterAgility =
        match enemyIntentOpt with
        | Some (StandardAttack atk) -> atk.Vector = Vector.Agility
        | _ -> false

      /// A defence readout for one attack: the player's attacking stat minus the defender's value, so a
      /// positive number is a favourable contest. Both sides read stance-adjusted stats because that is
      /// what the resolver contests -- the raw stat understated the defender by their stance multiplier.
      let contestSuffix (attackStat: StatId) (defenseStat: StatId) : string =
        match assess.InsightLevel with
        | Obscured -> ""
        | Discerning ->
          if fst assess.WeakestDefense = defenseStat then sprintf " - [bold %s]Opening[/]" Theme.Green
          elif fst assess.BestDefense = defenseStat then sprintf " - [bold %s]Apex Guard[/]" Theme.Red
          else ""
        | _ ->
          let delta = player.GetCombatStat attackStat - enemy.GetCombatStat defenseStat
          let deltaText = if delta >= 0 then sprintf "+%d" delta else string delta
          if fst assess.WeakestDefense = defenseStat then sprintf " - [bold %s]%s ★ Opening[/]" Theme.Green deltaText
          elif fst assess.BestDefense = defenseStat then sprintf " - [bold %s]%s ⚠ Apex Guard[/]" Theme.Red deltaText
          else sprintf " - [%s]%s[/]" Theme.Comment deltaText

      // Orders attack groups by overall contest margin: attack-minus-defence on the strike itself, plus
      // how well the player's own guard holds on that same vector. Estimated damage breaks ties, so a
      // gambit that lands harder still wins between two equal margins. Obscured insight means nothing is
      // known, so the archetype's own priority order stands.
      let rankStrikes (groups: (AttackClassification * string list) list) : string list =
        if assess.InsightLevel = Obscured then
          groups |> List.collect snd
        else
          groups
          |> List.map (fun (attack, labels) ->
            (AttackEstimator.contestMargin player enemy attack, AttackEstimator.estimate player enemy attack), labels)
          |> List.sortBy (fun ((margin, damage), _) -> -float margin, -damage)
          |> List.collect snd

      if player.IsInNach then
        // In Nach (Defender): Action menu offers active defenses, Nach-reactive preparations, and recoveries (ADR 0009)
        if player.Plane = Physical then
          let fortCounterTag = if isCounterPower then sprintf " [bold %s]★ Affinity Counter (Mitigates Force)[/]" Theme.Green else ""
          let refCounterTag = if isCounterAgility then sprintf " [bold %s]★ Affinity Counter (Mitigates Finesse)[/]" Theme.Green else ""
          sprintf "🛡️  [%s]Fortitude Guard: Braced Guard[/] (Power Defense • Counter Force Strikes; +25%% mitigation, seize Vor)%s" Theme.Cyan fortCounterTag
          sprintf "🛡️  [%s]Reflex Slip: Evasive Slip[/] (Agility Defense • Counter Finesse Cadences; +25%% mitigation, seize Vor)%s" Theme.Green refCounterTag
          for slot in availablePreps |> List.filter (fun s -> s.Type = ConcealedBlade || s.Type = CaltropPouch || s.Type = BastionZoneControl) do
            formatPrepOption slot
          // Raise Shield absorbed Wait for the physical disciplines: one hold that erects the shield
          // plating, banks guard stacks, widens the Indes window, and cedes the Vor.
          sprintf "🛡️  [bold %s]Raise Shield[/] (Bastion Hold: Boost Armor ~36%%+ of max as held plating (scales with Fortitude), Gain 2 Guard Stacks, Bleed Recklessness — cedes the Vor, so you strike only if the foe provokes)%s" Theme.Green raiseShieldTag
        else
          let resCounterTag = if isCounterPower then sprintf " [bold %s]★ Affinity Counter (Mitigates Power)[/]" Theme.Green else ""
          let intCounterTag = if isCounterAgility then sprintf " [bold %s]★ Affinity Counter (Mitigates Agility)[/]" Theme.Green else ""
          sprintf "🧠 [%s]Resolve Anchor: Iron Will[/] (Mental Power Defense • Counter Intellect; +25%% mitigation, seize Vor)%s" Theme.Cyan resCounterTag
          sprintf "👁️ [%s]Intuition Perception: Insight[/] (Mental Agility Defense • Counter Acuity; +25%% mitigation, seize Vor)%s" Theme.Green intCounterTag
          for slot in availablePreps |> List.filter (fun s -> s.Type = AegisOfRetribution || s.Type = MirrorMirage) do
            formatPrepOption slot
          sprintf "🧠 [%s]Center Mind[/] (Mental Reset: Drain Recklessness, clear Confusion, restore Arcane Ward)" Theme.Cyan
      elif player.Plane = Physical then
        // In Vor or Indes (Attacker): Power and Agility vectors plus preparations and recoveries
        let fortSuffix = contestSuffix StatId.Force StatId.Fortitude
        let refSuffix = contestSuffix StatId.Finesse StatId.Reflex
        let powerStanceTag =
          if player.Stance = CombatStance.PowerStance then sprintf " [bold %s]✓ In Stance[/]" Theme.Green
          else sprintf " [%s](shifts after swing)[/]" Theme.Comment
        let agilityStanceTag =
          if player.Stance = CombatStance.AgilityStance then sprintf " [bold %s]✓ In Stance[/]" Theme.Green
          else sprintf " [%s](shifts after swing)[/]" Theme.Comment

        let powerStrikes = [
          sprintf "⚔️  [%s]Force Strike: Standard Cleave[/] (Power Stance • Cleave vs. Fortitude%s)%s%s" Theme.Red fortSuffix powerStanceTag powerCounterTag
          sprintf "⚡ [bold %s]Force Strike: Wild Blow[/] (Power Stance Gambit: +30 Recklessness, 1.5x Dmg)%s" Theme.Red pressAdvTag
          if player.Momentum >= 2 then
            sprintf "🎯 [bold %s]Force Strike: Insight Cleave[/] (Power Stance Gambit: Burn %d Momentum for a Read Opening, 0 Recklessness)%s" Theme.Red player.Momentum pressAdvTag
        ]

        let agilityStrikes = [
          sprintf "⚔️  [%s]Finesse Cadence: Rapid Probing[/] (Agility Stance • Probing Cadence vs. Reflex%s)%s%s" Theme.Green refSuffix agilityStanceTag agilityCounterTag
          sprintf "⚡ [bold %s]Finesse Cadence: Relentless Blitz[/] (Agility Stance Gambit: +25 Recklessness)%s" Theme.Green pressAdvTag
          if player.Flow >= 2 then
            sprintf "🎯 [bold %s]Finesse Cadence: Threaded Opening[/] (Agility Stance Gambit: Burn %d Flow to Thread a Vital Opening, 0 Recklessness)%s" Theme.Green player.Flow pressAdvTag
          if isWindupReadable player enemy enemyIntentOpt then
            sprintf "🛡️  [bold %s]Shield Bash[/] (Interrupt the windup — Power: knockdown, Agility: parry + counter window)%s" Theme.Cyan shieldInterruptTag
        ]

        let martialGroups =
          match player.Class.Vector with
          | Vector.Power ->
            [ ForceStrike false, powerStrikes
              FinesseCadence false, agilityStrikes ]
          | _ ->
            [ FinesseCadence false, agilityStrikes
              ForceStrike false, powerStrikes ]

        yield! rankStrikes martialGroups

        // Tactical Preparations
        for slot in availablePreps do
          formatPrepOption slot

        // Active Armament Recovery: the shield hold -- Raise Shield and Wait are one action for the
        // physical disciplines, so there is no separate pass here.
        sprintf "🛡️  [bold %s]Raise Shield[/] (Bastion Hold: Boost Armor ~36%%+ of max as held plating (scales with Fortitude), Gain 2 Guard Stacks, Bleed Recklessness — cedes the Vor, so you strike only if the foe provokes)%s" Theme.Green raiseShieldTag

      else
        // Arcane Spellcraft (Mental Characters in Vor or Indes)
        let resolveSuffix = contestSuffix StatId.Intellect StatId.Resolve
        let intuitionSuffix = contestSuffix StatId.Acuity StatId.Intuition
        let resonanceFormTag =
          if player.ComplexForm = Some ComplexForm.ResonanceSpike then sprintf " [bold %s]✓ Threaded[/]" Theme.Green
          else sprintf " [%s](threads after cast)[/]" Theme.Comment
        let diffusionFormTag =
          if player.ComplexForm = Some ComplexForm.PhantasmalDiffusion then sprintf " [bold %s]✓ Threaded[/]" Theme.Green
          else sprintf " [%s](threads after cast)[/]" Theme.Comment

        let powerSpells = [
          sprintf "✨ [%s]Arcane Cataclysm: Elemental Blast[/] (Resonance Spike Form • Intellect vs. Resolve%s)%s%s" Theme.Pink resolveSuffix resonanceFormTag powerCounterTag
          if player.ComplexForm <> Some ComplexForm.AegisLattice then
            sprintf "⚡ [bold %s]Arcane Cataclysm: Overchannel[/] (Resonance Spike Gambit: +35 Recklessness, Splash)%s" Theme.Pink pressAdvTag
        ]

        let agilitySpells = [
          sprintf "✨ [%s]Synaptic Glamour: Neural Static[/] (Phantasmal Diffusion Form • Acuity vs. Intuition%s)%s%s" Theme.Purple intuitionSuffix diffusionFormTag agilityCounterTag
          sprintf "⚡ [bold %s]Synaptic Glamour: Mind Fracture[/] (Phantasmal Diffusion Gambit: +25 Recklessness)%s" Theme.Purple pressAdvTag
          sprintf "🪞 [%s]Mirror Illusion: Phantasmal Decoys[/] (Phantasmal Diffusion Form • Weave Mirror Clones)" Theme.Purple
          sprintf "⚡ [bold %s]Mirror Illusion: Decoy Swarm[/] (Phantasmal Diffusion Gambit: +25 Recklessness, Extra Clones)" Theme.Purple
        ]

        let spellGroups =
          match player.Class.Vector with
          | Vector.Power ->
            [ ArcaneCataclysm false, powerSpells
              SynapticGlamour false, agilitySpells ]
          | _ ->
            [ SynapticGlamour false, agilitySpells
              ArcaneCataclysm false, powerSpells ]

        yield! rankStrikes spellGroups

        // Tactical Preparations
        for slot in availablePreps do
          formatPrepOption slot

        // Mental Defensive Reset
        sprintf "🧠 [%s]Center Mind[/] (Mental Reset: Drain Recklessness, clear Confusion, restore Arcane Ward)" Theme.Cyan

    ]

  let buildActionChoicesWithTelegraph (player: Combatant) (enemy: Combatant) (enemyIntentOpt: ActionIntent option) (allowIndes: bool) : string list =
    let choices = buildActionChoicesCore player enemy enemyIntentOpt

    // Seize Indes: the wager. Answer the incoming strike with one of your own — out-roll it and the blow is
    // stopped and the Vor changes hands; lose it and the blow lands half again as hard with every banked
    // stack spent. Only offered where a defence slot exists (tower combat), and the counter is always a
    // strike of the player's own vector so the offer is never one the build cannot make.
    if not (allowIndes && player.IsInNach) then
      choices
    else
      let counterStrike =
        // The counter is whatever the player is actually fighting in. This used to read the class vector,
        // so a Power-vector build that slipped into Agility Stance was offered a Force Strike gambit it
        // had not trained for and would not have picked. Stance is written by the very defence that put
        // the player in the Nach, which is exactly the vector the wager is being made in, and
        // `AttackClassification.IsAllowedFor` gates on plane alone -- so any stance-driven strike is one
        // the build can legally make.
        let stanceVector =
          match player.Stance with
          | CombatStance.AgilityStance -> Vector.Agility
          | CombatStance.DisciplineStance -> Vector.Discipline
          | CombatStance.PowerStance -> Vector.Power

        if player.Plane = Physical then
          match stanceVector with
          | Vector.Agility -> "Finesse Cadence: Rapid Probing"
          | _ -> "Force Strike: Standard Cleave"
        else
          match stanceVector with
          | Vector.Agility -> "Guile Deception: Rhetorical Misdirection"
          | Vector.Discipline -> "Prowess Interrogation: Procedural Pressure"
          | _ -> "Authority Decree: Imperious Command"

      choices
      @ [ sprintf
            "⚔️  [bold %s]Seize Indes: %s[/] (Gambit: win the roll to stop the strike and take the Vor; lose it and the blow lands 1.5x with every banked stack spent)"
            Theme.Yellow
            counterStrike ]

  let buildActionChoices (player: Combatant) (enemy: Combatant) : string list =
    buildActionChoicesWithTelegraph player enemy None false

  let private runInteractiveDuel (playerArch: ArchetypeInfo) (enemyArch: ArchetypeInfo) =
    let rng = Random()
    let roller : DiceRoller = fun min max -> rng.Next(min, max + 1)

    // The arena picks both fighters out of the same plain-data registry, so the side that is the player
    // has to be declared here: without the mark the ramp meters no-op and the player never compounds.
    let mutable player = playerArch.Factory () |> Combatant.seizeVor |> fun p -> { p with PlayerStyle = true }
    let mutable enemy = enemyArch.Factory () |> Combatant.yieldVor |> fun c -> { c with PlayerStyle = false }
    let mutable round = 1
    let mutable combatOver = false
    let mutable outcome = "PlayerDefeated"
    let mutable enemyInterrupted = false

    let session =
      CombatLogger.startSession
        (sprintf "%s vs %s" playerArch.Name enemyArch.Name)
        "Arena Tactical Duel"
        player
        enemy

    while not combatOver do
      // A collapsed foe cannot hold the Vor: uncontested, the player takes it.
      player <- ActionResolver.settleTempoVacuum player [ enemy ]
      let playerHasVor = player.IsInVor || not enemy.IsInVor
      let enemyIntent = AI.chooseIntentUsing Policies.all enemy player

      let isLoneBeastFleeEligible =
        enemy.IsBeast
        && float enemy.Health.Current / float enemy.Health.Maximum < 0.30
        && float player.Health.Current / float player.Health.Maximum > 0.70

      if isLoneBeastFleeEligible then
        Display.renderHUDWithTelegraph player enemy round (Some enemyIntent) None
        let fleePromptTitle =
          sprintf "[bold %s]🐾 The critically wounded %s is whimpering, limping away into the shadows![/]" Theme.Yellow enemy.Name
        let fleeChoices =
          [ sprintf "🕊️ [bold %s]Spare the beast[/] (Mercy: Clear Recklessness to 0, restore +20 Morale, gain Bestiary Prowess)" Theme.Green
            sprintf "☠️ [bold %s]Execute the beast[/] (Coup de Grâce: Harvest trophies/pelts, +15 Recklessness, gain Bestiary Prowess)" Theme.Red ]
        let fleeChoice =
          Display.promptSelectionWithHelp
            fleePromptTitle
            fleeChoices
            (Some 2)
            (fun () -> Display.renderHUDWithTelegraph player enemy round (Some enemyIntent) None)

        if fleeChoice.Contains("Spare") then
          player <-
            player
            |> Combatant.updateMeters (fun m -> { m with Recklessness = Meter.Zero })
            |> (fun p -> { p with Morale = p.Morale.ApplyDelta 20 })
            |> Combatant.growFromEncounter [ enemy ]
          Display.logEvent (CombatEvent.BeastFled(enemy.Id, enemy.Name))
          Display.logEvent (CombatEvent.BeastSpared(player.Id, enemy.Id))
          combatOver <- true
          outcome <- "PlayerVictorious"
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]🕊️ You lowered your weapon. The wounded %s scrambled into the darkness. You steadied your composure and gained martial insight.[/]" Theme.Green enemy.Name)
        else
          player <-
            player
            |> Combatant.updateMeters (fun m -> { m with Recklessness = m.Recklessness + 15 })
            |> Combatant.growFromEncounter [ enemy ]
          Display.logEvent (CombatEvent.BeastExecuted(player.Id, enemy.Id))
          combatOver <- true
          outcome <- "PlayerVictorious"
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]☠️ You struck down the fleeing %s without hesitation (+15 Recklessness, Bestiary Prowess gained).[/]" Theme.Red enemy.Name)

      if not combatOver then
        Display.renderHUDWithTelegraph player enemy round (Some enemyIntent) None

        let choices = buildActionChoicesWithTelegraph player enemy (Some enemyIntent) true
        let choice =
          Display.promptSelectionWithHelp
            (sprintf "[bold %s]Round %d - Select Tactical Action for %s (Press '?' or F1 for Symbol Legend):[/]" Theme.Yellow round player.Name)
            choices
            (Some 12)
            (fun () -> Display.renderHUDWithTelegraph player enemy round (Some enemyIntent) None)

        let playerIntent = parseActionChoice choice
        let playerActiveDefenseOpt =
          match playerIntent with
          | ActiveDefense def -> Some(Guard def)
          | StandardAttack atk when choice.Contains("Seize Indes") -> Some(SeizeIndes atk)
          | _ -> None

        let doPlayerTurn (turnNumber: int) =
          let playerBefore = player
          let enemyBefore = enemy

          let actionLabel = Display.intentLogLabel playerIntent player
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]%s executes %s...[/]" Theme.Green (Markup.Escape player.Name) (Markup.Escape actionLabel))
          let enemyDefOpt =
            if enemy.IsHumanoid && enemy.IsInNach && not enemy.IsStaggered then
              Some(Guard(AI.chooseActiveDefense enemy player))
            else None
          let playerResult = ActionResolver.resolveExWithDefense roller playerIntent enemyDefOpt player enemy 0
          player <- playerResult.Actor
          enemy <- playerResult.Target

          enemyInterrupted <-
            playerResult.Events
            |> List.exists (function CombatEvent.ActionInterrupted _ -> true | _ -> false)

          CombatLogger.recordTurn session round turnNumber choice playerBefore enemyBefore playerResult

          Display.renderRollBreakdown (Markup.Escape actionLabel) player.Name playerResult.Contest
          Display.logDamageSummary playerResult.Events enemyBefore enemy
          playerResult.Events |> List.iter Display.logEvent

          let playerExecutedEnemy =
            playerResult.Events
            |> List.exists (function CombatEvent.Executed _ -> true | _ -> false)

          if playerExecutedEnemy || enemy.Health.IsDepleted || enemy.Morale.IsDepleted then
            combatOver <- true
            outcome <- "PlayerVictorious"
            AnsiConsole.WriteLine()
            AnsiConsole.Write(
              Rule(sprintf "[bold %s]★★★ VICTORY: %s HAS PREVAILED OVER %s! ★★★[/]" Theme.Green player.Name enemy.Name)
                .Centered()
                .RuleStyle(Theme.StyleGreen)
            )

        let doEnemyTurn (turnNumber: int) (playerDefOpt: DefenseChoice option) =
          let enemyBeforeTurn = enemy
          let playerBeforeTurn = player

          let enemyResult =
            if enemyInterrupted then
              AnsiConsole.MarkupLine(sprintf "\n[bold %s]⚡ %s is staggered from the technique interrupt! Attack defused![/]" Theme.Yellow enemy.Name)
              let recoverIntent = RecoveryAction SteadyForm
              ActionResolver.resolve roller recoverIntent enemy player
            else
              let tempoMsg = if not playerHasVor then " (Holding the Vor)" else ""
              AnsiConsole.MarkupLine(sprintf "\n[bold %s]%s executes telegraphed action%s...[/]" Theme.Pink enemy.Name tempoMsg)
              ActionResolver.resolveExWithDefense roller enemyIntent playerDefOpt enemy player 0

          enemy <- enemyResult.Actor
          player <- enemyResult.Target

          let intentDesc = if enemyInterrupted then "Staggered Recovery (Interrupted)" else Display.intentLogLabel enemyIntent enemyBeforeTurn
          CombatLogger.recordTurn session round turnNumber intentDesc enemyBeforeTurn playerBeforeTurn enemyResult

          Display.renderRollBreakdown intentDesc enemy.Name enemyResult.Contest
          Display.logDamageSummary enemyResult.Events playerBeforeTurn player
          enemyResult.Events |> List.iter Display.logEvent
          if enemyInterrupted then enemyInterrupted <- false

          let enemyExecutedPlayer =
            enemyResult.Events
            |> List.exists (function CombatEvent.Executed _ -> true | _ -> false)

          if enemyExecutedPlayer || player.Health.IsDepleted || player.Morale.IsDepleted then
            combatOver <- true
            outcome <- "PlayerDefeated"
            AnsiConsole.WriteLine()
            AnsiConsole.Write(
              Rule(sprintf "[bold %s]☠☠☠ DEFEAT: %s HAS FALLEN TO %s! ☠☠☠[/]" Theme.Red player.Name enemy.Name)
                .Centered()
                .RuleStyle(Theme.StyleRed)
            )

        if playerHasVor then
          doPlayerTurn 1
          if not combatOver then
            doEnemyTurn 2 None
        else
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]⚡ %s holds the Vor and strikes first![/]" Theme.Yellow enemy.Name)
          doEnemyTurn 1 playerActiveDefenseOpt
          let wasActionConsumed =
            match playerIntent with
            | ActiveDefense _ -> true
            | StandardAttack _ when choice.Contains("Seize Indes") -> true
            | _ -> false
          if not combatOver && not wasActionConsumed then
            doPlayerTurn 2

      if not combatOver then
        AnsiConsole.WriteLine()
        AnsiConsole.Markup(sprintf "[%s]Press any key to proceed to the next round...[/]" Theme.Comment)
        Console.ReadKey(true) |> ignore
        round <- round + 1

    let latestLog, _ = CombatLogger.writeSession session outcome player enemy
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine(sprintf "[dim grey]Tactical combat log saved to: %s[/]" latestLog)
    AnsiConsole.Markup(sprintf "[bold %s]Combat concluded. Press any key to return to menu...[/]" Theme.Yellow)
    Console.ReadKey(true) |> ignore

  let private runBalanceSimulator (archA: ArchetypeInfo) (archB: ArchetypeInfo) =
    let iterations =
      Display.promptWithVim(
        SelectionPrompt<int>()
          .Title(sprintf "[bold %s]Select number of simulation iterations:[/]" Theme.Yellow)
          .AddChoices([ 50; 100; 250; 500; 1000 ])
      )

    let summary = Simulation.runBatch archA archB iterations
    Simulation.renderDashboard summary (archA.Factory()) (archB.Factory())

    AnsiConsole.Markup(sprintf "[bold %s]Simulation complete. Press any key to return to menu...[/]" Theme.Yellow)
    Console.ReadKey(true) |> ignore

  let private runGroupBalanceSimulator (soloArch: ArchetypeInfo) (mobArch: ArchetypeInfo) =
    let mobCount =
      Display.promptWithVim(
        SelectionPrompt<int>()
          .Title(sprintf "[bold %s]Select Swarm Size (Number of Opponents fighting simultaneously):[/]" Theme.Yellow)
          .AddChoices([ 2; 3; 4; 5; 6; 8; 10; 20; 50; 100 ])
      )

    let iterations =
      Display.promptWithVim(
        SelectionPrompt<int>()
          .Title(sprintf "[bold %s]Select number of simulation iterations:[/]" Theme.Yellow)
          .AddChoices([ 50; 100; 250; 500; 1000 ])
      )

    let summary = Simulation.runGroupBatch soloArch mobArch mobCount iterations
    Simulation.renderGroupDashboard summary (soloArch.Factory()) (mobArch.Factory())

    AnsiConsole.Markup(sprintf "[bold %s]Simulation complete. Press any key to return to menu...[/]" Theme.Yellow)
    Console.ReadKey(true) |> ignore

  let private showRoster () =
    let table = Table().Border(TableBorder.Rounded).BorderColor(Theme.ColorCurrentLine)
    table.AddColumn(TableColumn(sprintf "[bold %s]Tier[/]" Theme.Foreground)) |> ignore
    table.AddColumn(TableColumn(sprintf "[bold %s]Name[/]" Theme.Foreground)) |> ignore
    table.AddColumn(TableColumn(sprintf "[bold %s]Discipline[/]" Theme.Foreground)) |> ignore
    table.AddColumn(TableColumn(sprintf "[bold %s]HP / Morale[/]" Theme.Foreground)) |> ignore
    table.AddColumn(TableColumn(sprintf "[bold %s]Armor (Soak)[/]" Theme.Foreground)) |> ignore
    table.AddColumn(TableColumn(sprintf "[bold %s]Key Attributes[/]" Theme.Foreground)) |> ignore

    for arch in Archetypes.allArchetypes do
      let sample = arch.Factory()
      let tierColor =
        match arch.Tier with
        | Novice -> Theme.Comment
        | Veteran -> Theme.Cyan
        | Master -> Theme.Yellow
        | GrandMaster -> Theme.Purple

      let soakPct = int (sample.Armor.AbsorptionRatio * 100.0)
      let keyStats =
        match arch.Discipline with
        | CombatMode.Physical ->
          sprintf "Force: %d, Fort: %d, Prow: %d, Poise: %d"
            (sample.GetStat Force) (sample.GetStat Fortitude) (sample.GetStat Prowess) (sample.GetStat Poise)
        | CombatMode.Social ->
          sprintf "Presence: %d, Will: %d, Leverage: %d"
            (sample.GetStat Intellect) (sample.GetStat Resolve) (sample.GetStat Prowess)
        | CombatMode.Arcane ->
          sprintf "Intellect: %d, Acuity: %d, Resolve: %d"
            (sample.GetStat Intellect) (sample.GetStat Acuity) (sample.GetStat Resolve)

      table.AddRow(
        Markup(sprintf "[bold %s]%A[/]" tierColor arch.Tier),
        Markup(sprintf "[bold %s]%s[/]" Theme.Foreground arch.Name),
        Markup(sprintf "[%s]%A[/]" Theme.Pink arch.Discipline),
        Markup(sprintf "[%s]%d[/] / [%s]%d[/]" Theme.Red sample.Health.Maximum Theme.Cyan sample.Morale.Maximum),
        Markup(sprintf "[%s]%d (%d%%)[/]" Theme.Yellow sample.Armor.Max soakPct),
        Markup(sprintf "[%s]%s[/]" Theme.Comment keyStats)
      ) |> ignore

    AnsiConsole.Clear()
    printBanner()
    AnsiConsole.Write(table)
    AnsiConsole.WriteLine()
    AnsiConsole.Markup(sprintf "[bold %s]Press any key to return to menu...[/]" Theme.Yellow)
    Console.ReadKey(true) |> ignore

  let private createCustomCombatantInteractive () : ArchetypeInfo =
    AnsiConsole.Clear()
    printBanner()
    AnsiConsole.Write(
      Rule(sprintf "[bold %s]Custom Combatant Builder[/]" Theme.Purple)
        .LeftJustified()
        .RuleStyle(Theme.StyleCurrentLine)
    )
    AnsiConsole.WriteLine()

    let name = Display.askString(sprintf "[%s]Enter combatant name:[/] " Theme.Foreground, "Gladiator")
    let hp = Display.askInt(sprintf "[%s]Enter Max Health (HP):[/] " Theme.Red, 2000)
    let morale = Display.askInt(sprintf "[%s]Enter Max Morale:[/] " Theme.Cyan, 2000)
    let armor = Display.askInt(sprintf "[%s]Enter Armor Durability (0-100):[/] " Theme.Yellow, 50)

    AnsiConsole.MarkupLine(sprintf "\n[bold %s]Assign Attributes (Uncapped 30–500):[/]" Theme.Yellow)
    let force = Display.askInt(sprintf "  [%s]Force (Physical Offense Power):[/] " Theme.Red, 120)
    let fort = Display.askInt(sprintf "  [%s]Fortitude (Physical Defense Power):[/] " Theme.Red, 110)
    let finesse = Display.askInt(sprintf "  [%s]Finesse (Physical Offense Agility):[/] " Theme.Green, 80)
    let reflex = Display.askInt(sprintf "  [%s]Reflex (Physical Defense Agility):[/] " Theme.Green, 80)
    let prowess = Display.askInt(sprintf "  [%s]Prowess (Martial Offense Discipline):[/] " Theme.Purple, 100)
    let poise = Display.askInt(sprintf "  [%s]Poise (Martial Defense Discipline):[/] " Theme.Purple, 100)

    let intellect = Display.askInt(sprintf "  [%s]Intellect / Presence (Mental Offense Power):[/] " Theme.Cyan, 60)
    let resolve = Display.askInt(sprintf "  [%s]Resolve / Will (Mental Defense Power):[/] " Theme.Cyan, 60)
    let acuity = Display.askInt(sprintf "  [%s]Acuity / Guile (Mental Offense Agility):[/] " Theme.Pink, 60)
    let intuition = Display.askInt(sprintf "  [%s]Intuition / Insight (Mental Defense Agility):[/] " Theme.Pink, 60)
    let acumen = Display.askInt(sprintf "  [%s]Prowess / Leverage (Mental Offense Discipline):[/] " Theme.Orange, 60)
    let composure = Display.askInt(sprintf "  [%s]Poise (Mental Defense Discipline):[/] " Theme.Orange, 60)

    let statsList = [
      Force, force; Fortitude, fort
      Finesse, finesse; Reflex, reflex
      Prowess, prowess; Poise, poise
      Intellect, intellect; Resolve, resolve
      Acuity, acuity; Intuition, intuition
      Prowess, acumen; Poise, composure
    ]

    let customFactory () =
      Archetypes.createCustom name hp morale armor statsList

    { Name = name
      Tier = Veteran
      Discipline = if force + prowess >= intellect + acumen then CombatMode.Physical else CombatMode.Arcane
      Description = "Custom player-crafted combatant."
      Factory = customFactory }

  let parseCliArgs (args: string array) : CliOptions * DebugOptions =
    let hasFlag (f: string) = args |> Array.exists (fun a -> a = f)
    let hasGroupFlag = hasFlag "--group" || hasFlag "-g"

    let findArg (flags: string list) (defaultVal: string) =
      args
      |> Array.mapi (fun i a -> (i, a))
      |> Array.tryFind (fun (i, a) ->
        List.contains a flags && i + 1 < args.Length && not (args.[i + 1].StartsWith("-")))
      |> function
        | Some (i, _) -> args.[i + 1]
        | None -> defaultVal

    let isStandaloneSimFlag =
      args
      |> Array.tryFindIndex (fun a -> a = "-s")
      |> Option.map (fun i -> i + 1 >= args.Length || args.[i + 1].StartsWith("-"))
      |> Option.defaultValue false

    let isBestiarySwarm = hasFlag "--bestiary-swarm" || hasFlag "--monster-swarm" || hasFlag "-ms"
    let isPeerMatrix = hasFlag "--peer-matrix" || hasFlag "--peer-balance" || hasFlag "-pbm"
    let isMatrix = hasFlag "--balance-matrix" || hasFlag "--matrix" || hasFlag "-b"
    let isSim = hasFlag "--sim" || hasGroupFlag || isStandaloneSimFlag || hasFlag "-a1" || hasFlag "--solo"

    let debugOptions =
      let forcedSeed =
        args
        |> Array.mapi (fun i a -> (i, a))
        |> Array.tryFind (fun (i, a) -> a = "--seed" && i + 1 < args.Length)
        |> Option.bind (fun (i, _) ->
          match Int32.TryParse args.[i + 1] with
          | true, v -> Some v
          | _ -> None)

      { Enabled = hasFlag "--debug" || hasFlag "-d"
        ForcedSeed = forcedSeed }

    let mode =
      if isBestiarySwarm then
        let tierArg = findArg [ "-t"; "--tier" ] ""
        let tierOpt =
          match tierArg.ToLowerInvariant() with
          | "novice" -> Some CombatTier.Novice
          | "veteran" -> Some CombatTier.Veteran
          | "master" -> Some CombatTier.Master
          | "grandmaster" | "gm" -> Some CombatTier.GrandMaster
          | _ -> None
        RunBestiarySwarmMatrix tierOpt
      elif isPeerMatrix then
        RunPeerBalanceMatrix
      elif isMatrix then
        RunBalanceMatrix
      elif hasFlag "--run-sim" then
        let numberArg (flags: string list) (defaultVal: int) =
          match Int32.TryParse(findArg flags (string defaultVal)) with
          | true, v -> Math.Max(1, v)
          | _ -> defaultVal

        RunProgressionSimulation(numberArg [ "--run-sim" ] 25, numberArg [ "--floors" ] 10)
      elif not isSim then
        InteractiveMenu
      else
        let arch1 = findArg [ "--solo"; "-s"; "-a1"; "--archetype1" ] "Veteran Fighter"
        let arch2 = findArg [ "--mob"; "--enemy"; "-e"; "-a2"; "--archetype2" ] "Veteran Mage"
        let itersStr = findArg [ "-n"; "--iterations" ] "100"
        let iters = match Int32.TryParse itersStr with true, v -> Math.Max(1, v) | _ -> 100

        let countStr = findArg [ "-k"; "--count" ] (if hasGroupFlag then "3" else "0")
        let mobCount = match Int32.TryParse countStr with true, v when v > 1 -> v | _ -> 0

        if mobCount > 1 || hasGroupFlag then
          let effectiveCount = Math.Max(2, mobCount)
          RunGroupSimulation(arch1, arch2, effectiveCount, iters)
        else
          RunSimulation(arch1, arch2, iters)

    mode, debugOptions

  /// Appends one turn's header plus per-event lines to a rolling combat log, using semantic
  /// color-coding and guaranteed Spectre markup escaping.
  let appendTurnTo (log: System.Collections.Generic.List<string>) (nameOf: CombatantId -> string) (playerId: CombatantId) (header: string) (events: CombatEvent list) =
    log.Add(sprintf "[bold %s]%s[/]" Theme.Yellow (Markup.Escape header))
    for evt in events do
      log.Add(Display.formatCombatLogEvent nameOf playerId evt)

  let private runStoryDuel
    (floorNumber: int)
    (player: Combatant)
    (boss: Combatant)
    (flankOf: CombatantId -> Fornach.Spatial.Flank)
    (hazardHook: bool -> Combatant -> Combatant * CombatEvent list) : CombatOutcome * Combatant =
    let rng = Random()
    let roller : DiceRoller = fun min max -> rng.Next(min, max + 1)

    // The opening tempo is the caller's: an unseen foe arrives holding the Vor with the player in the
    // Nach (TowerSession.ambushTempo), a foe already in view yields it. Whoever comes in with no stance
    // of their own is given the complement of the player's so the duel never opens with two holders.
    let mutable currentCombatant = player

    let mutable currentBoss =
      if player.IsInNach then Combatant.seizeVor boss else Combatant.yieldVor boss
    let mutable round = 1
    let mutable combatOver = false
    let mutable outcome = CombatOutcome.PlayerDefeated
    let mutable enemyInterrupted = false
    let mutable lastPlayerDefense : DefenseChoice option = None

    let session =
      CombatLogger.startSession
        (sprintf "%s vs %s" currentCombatant.Name currentBoss.Name)
        "Story / Tower Tactical Duel"
        currentCombatant
        currentBoss

    RuntimeLog.write (sprintf "combat start: %s vs %s (round 1)" currentCombatant.Name currentBoss.Name)

    let combatLog = System.Collections.Generic.List<string>()
    let nameOf (id: CombatantId) =
      if id = currentCombatant.Id then currentCombatant.Name else currentBoss.Name
    let logLines () = combatLog |> List.ofSeq

    while not combatOver do
      currentCombatant <- ActionResolver.settleTempoVacuum currentCombatant [ currentBoss ]
      let playerHasVor = currentCombatant.IsInVor || not currentBoss.IsInVor
      let enemyIntent = AI.chooseIntentUsing Policies.all currentBoss currentCombatant

      let isLoneBeastFleeEligible =
        currentBoss.IsBeast
        && float currentBoss.Health.Current / float currentBoss.Health.Maximum < 0.30
        && float currentCombatant.Health.Current / float currentCombatant.Health.Maximum > 0.70

      if isLoneBeastFleeEligible then
        Display.renderHUDWithTelegraph currentCombatant currentBoss round (Some enemyIntent) (Some (logLines ()))
        let fleePromptTitle =
          sprintf "[bold %s]🐾 The critically wounded %s is whimpering, limping away into the shadows![/]" Theme.Yellow currentBoss.Name
        let fleeChoices =
          [ sprintf "🕊️ [bold %s]Spare the beast[/] (Mercy: Clear Recklessness to 0, restore +20 Morale, gain Bestiary Prowess)" Theme.Green
            sprintf "☠️ [bold %s]Execute the beast[/] (Coup de Grâce: Harvest trophies/pelts, +15 Recklessness, gain Bestiary Prowess)" Theme.Red ]
        let fleeChoice =
          Display.promptSelectionWithHelp
            fleePromptTitle
            fleeChoices
            (Some 2)
            (fun () -> Display.renderHUDWithTelegraph currentCombatant currentBoss round (Some enemyIntent) (Some (logLines ())))

        if fleeChoice.Contains("Spare") then
          currentCombatant <-
            currentCombatant
            |> Combatant.updateMeters (fun m -> { m with Recklessness = Meter.Zero })
            |> (fun p -> { p with Morale = p.Morale.ApplyDelta 20 })
            |> Combatant.growFromEncounter [ currentBoss ]
            |> Combatant.growFromStressedVictory
          Display.logEvent (CombatEvent.BeastFled(currentBoss.Id, currentBoss.Name))
          Display.logEvent (CombatEvent.BeastSpared(currentCombatant.Id, currentBoss.Id))
          combatOver <- true
          outcome <- CombatOutcome.PlayerVictorious
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]🕊️ You lowered your weapon. The wounded %s scrambled into the darkness. You steadied your composure and gained martial insight.[/]" Theme.Green currentBoss.Name)
        else
          currentCombatant <-
            currentCombatant
            |> Combatant.updateMeters (fun m -> { m with Recklessness = m.Recklessness + 15 })
            |> Combatant.growFromEncounter [ currentBoss ]
            |> Combatant.growFromStressedVictory
          Display.logEvent (CombatEvent.BeastExecuted(currentCombatant.Id, currentBoss.Id))
          combatOver <- true
          outcome <- CombatOutcome.PlayerVictorious
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]☠️ You struck down the fleeing %s without hesitation (+15 Recklessness, Bestiary Prowess gained).[/]" Theme.Red currentBoss.Name)

      if not combatOver then
        Display.renderHUDWithTelegraph currentCombatant currentBoss round (Some enemyIntent) (Some (logLines ()))

        let choices = buildActionChoicesWithTelegraph currentCombatant currentBoss (Some enemyIntent) true
        let choice =
          Display.promptSelectionWithHelp
            (sprintf "[bold %s]Round %d - Tactical Action against %s (Press '?' or F1 for Symbol Legend):[/]" Theme.Yellow round currentBoss.Name)
            choices
            (Some 12)
            (fun () -> Display.renderHUDWithTelegraph currentCombatant currentBoss round (Some enemyIntent) (Some (logLines ())))

        let playerIntent = parseActionChoice choice
        let playerActiveDefenseOpt =
          match playerIntent with
          | ActiveDefense def -> Some(Guard def)
          | StandardAttack atk when choice.Contains("Seize Indes") -> Some(SeizeIndes atk)
          | _ -> None

        let doPlayerTurn (turnNumber: int) =
          let playerBefore = currentCombatant
          let bossBefore = currentBoss

          let actionLabel = Display.intentLogLabel playerIntent currentCombatant
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]%s executes %s...[/]" Theme.Green (Markup.Escape currentCombatant.Name) (Markup.Escape actionLabel))
          let bossDefOpt =
            if currentBoss.IsHumanoid && currentBoss.IsInNach && not currentBoss.IsStaggered then
              Some(Guard(AI.chooseActiveDefense currentBoss currentCombatant))
            else None
          let playerResult = ActionResolver.resolveExWithDefense roller playerIntent bossDefOpt currentCombatant currentBoss 0
          currentCombatant <- playerResult.Actor
          currentBoss <- playerResult.Target

          let hasKnockback =
            playerResult.Events
            |> List.exists (function CombatEvent.CombatantKnockedBack _ -> true | _ -> false)
          if hasKnockback then
            let knockedBoss, hazardEvts = hazardHook false currentBoss
            currentBoss <- knockedBoss
            for evt in hazardEvts do
              Display.logEvent evt

          enemyInterrupted <-
            playerResult.Events
            |> List.exists (function CombatEvent.ActionInterrupted _ -> true | _ -> false)

          CombatLogger.recordTurn session round turnNumber choice playerBefore bossBefore playerResult
          RuntimeLog.write (
            sprintf "combat round %d player turn: %s (%d events)" round actionLabel playerResult.Events.Length)

          appendTurnTo combatLog nameOf currentCombatant.Id
            (sprintf "Round %d — %s executes %s" round currentCombatant.Name actionLabel)
            playerResult.Events

          Display.renderRollBreakdown (Markup.Escape actionLabel) currentCombatant.Name playerResult.Contest
          Display.logDamageSummary playerResult.Events bossBefore currentBoss
          playerResult.Events |> List.iter Display.logEvent

          let playerExecutedEnemy =
            playerResult.Events
            |> List.exists (function CombatEvent.Executed _ -> true | _ -> false)

          if playerExecutedEnemy || currentBoss.Health.IsDepleted || currentBoss.Morale.IsDepleted then
            combatOver <- true
            outcome <- CombatOutcome.PlayerVictorious
            currentCombatant <-
              currentCombatant
              |> Combatant.growFromEncounter [ currentBoss ]
              |> Combatant.growFromStressedVictory
            AnsiConsole.WriteLine()
            AnsiConsole.Write(
              Rule(sprintf "[bold %s]★★★ MANIFESTATION RESOLVED: %s PREVAILED! ★★★[/]" Theme.Green currentCombatant.Name)
                .Centered()
                .RuleStyle(Theme.StyleGreen)
            )

        let doEnemyTurn (turnNumber: int) (playerDefOpt: DefenseChoice option) =
          let bossBeforeTurn = currentBoss
          let playerBeforeTurn = currentCombatant

          let isRepeatedGuard = AI.adaptToDefensiveRepetition currentBoss lastPlayerDefense playerDefOpt
          let effectiveEnemyIntent =
            if isRepeatedGuard && (currentBoss.Name.StartsWith("Aspect of") || currentBoss.Level >= 2) then
              match playerDefOpt with
              | Some (Guard d) ->
                let converted = AI.feintConvertVector enemyIntent d
                if converted <> enemyIntent then
                  AnsiConsole.MarkupLine(sprintf "\n[bold red]⚡ FEINT DETECTED! %s anticipates your predictable guard, converting the strike into a slicing cut![/]" currentBoss.Name)
                  converted
                else enemyIntent
              | _ -> enemyIntent
            else enemyIntent

          lastPlayerDefense <- playerDefOpt
          let flankMult = flankOf currentBoss.Id |> TowerSession.flankDamageMultiplier

          let enemyResult =
            if enemyInterrupted then
              AnsiConsole.MarkupLine(sprintf "\n[bold %s]⚡ %s is staggered from the technique interrupt! Attack defused![/]" Theme.Yellow currentBoss.Name)
              let recoverIntent = RecoveryAction SteadyForm
              ActionResolver.resolve roller recoverIntent currentBoss currentCombatant
            else
              let tempoMsg = if not playerHasVor then " (Holding the Vor)" else ""
              AnsiConsole.MarkupLine(sprintf "\n[bold %s]%s executes telegraphed action%s...[/]" Theme.Pink currentBoss.Name tempoMsg)
              if floorNumber = 1 then
                // Tutorial grace: the level-1 protagonist cannot be one-shot on the opening stage.
                ActionResolver.resolveWithoutLethalWithDefenseFlank roller effectiveEnemyIntent playerDefOpt currentBoss currentCombatant 0 flankMult
              else
                ActionResolver.resolveExWithDefenseFlank roller effectiveEnemyIntent playerDefOpt currentBoss currentCombatant 0 flankMult

          currentBoss <- enemyResult.Actor
          currentCombatant <- enemyResult.Target

          let hasKnockback =
            enemyResult.Events
            |> List.exists (function CombatEvent.CombatantKnockedBack _ -> true | _ -> false)
          if hasKnockback then
            let knockedPlayer, hazardEvts = hazardHook true currentCombatant
            currentCombatant <- knockedPlayer
            for evt in hazardEvts do
              Display.logEvent evt

          let intentDesc = if enemyInterrupted then "Staggered Recovery (Interrupted)" else Display.intentLogLabel enemyIntent bossBeforeTurn
          CombatLogger.recordTurn session round turnNumber intentDesc bossBeforeTurn playerBeforeTurn enemyResult
          RuntimeLog.write (
            sprintf "combat round %d enemy turn: %s (%d events)" round intentDesc enemyResult.Events.Length)

          appendTurnTo combatLog nameOf currentCombatant.Id
            (sprintf "Round %d — %s executes %s" round currentBoss.Name intentDesc)
            enemyResult.Events

          Display.renderRollBreakdown intentDesc currentBoss.Name enemyResult.Contest
          Display.logDamageSummary enemyResult.Events playerBeforeTurn currentCombatant
          enemyResult.Events |> List.iter Display.logEvent
          if enemyInterrupted then enemyInterrupted <- false

          let enemyExecutedPlayer =
            enemyResult.Events
            |> List.exists (function CombatEvent.Executed _ -> true | _ -> false)

          if enemyExecutedPlayer || currentCombatant.Health.IsDepleted || currentCombatant.Morale.IsDepleted then
            combatOver <- true
            outcome <- CombatOutcome.PlayerDefeated
            AnsiConsole.WriteLine()
            AnsiConsole.Write(
              Rule(sprintf "[bold %s]☠☠☠ DEFEATED BY TRAUMA: %s HAS FALLEN! ☠☠☠[/]" Theme.Red currentCombatant.Name)
                .Centered()
                .RuleStyle(Theme.StyleRed)
            )

        if playerHasVor then
          doPlayerTurn 1
          if not combatOver then
            doEnemyTurn 2 None
        else
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]⚡ %s holds the Vor and strikes first![/]" Theme.Yellow currentBoss.Name)
          doEnemyTurn 1 playerActiveDefenseOpt
          let wasActionConsumed =
            match playerIntent with
            | ActiveDefense _ -> true
            | StandardAttack _ when choice.Contains("Seize Indes") -> true
            | _ -> false
          if not combatOver && not wasActionConsumed then
            doPlayerTurn 2

      if not combatOver then
        AnsiConsole.WriteLine()
        AnsiConsole.Markup(sprintf "[%s]Press any key to proceed to next round...[/]" Theme.Comment)
        Console.ReadKey(true) |> ignore
        round <- round + 1

    let outcomeStr = if outcome = CombatOutcome.PlayerVictorious then "PlayerVictorious" else "PlayerDefeated"
    let latestLog, _ = CombatLogger.writeSession session outcomeStr currentCombatant currentBoss
    RuntimeLog.write (sprintf "combat end: %s (log %s)" outcomeStr latestLog)
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine(sprintf "[dim grey]Tactical combat log saved to: %s[/]" latestLog)

    // Defeat still grows the player (+10 discipline) and banks the encounter - losing to a species
    // teaches you it just as meeting one does. The caller writes this combatant into the run state
    // before its trauma reset, which restores pools but keeps the scars.
    outcome,
    (if outcome = CombatOutcome.PlayerDefeated then
       currentCombatant
       |> Combatant.growFromEncounter [ currentBoss ]
       |> Combatant.growFromDefeat
     else
       currentCombatant)

  /// Formats a target choice in multi-enemy combat so identical foes are distinguishable by live
  /// HP percentage, initiative badge, and active status markers.
  let formatGroupTargetChoice (index: int) (e: TowerEnemy) : string =
    let c = e.Combatant
    let hpPct =
      if c.Health.Maximum <= 0 then 0
      else int (Math.Round(float c.Health.Current / float c.Health.Maximum * 100.0))
    let hpColor =
      if hpPct > 60 then Theme.Green
      elif hpPct >= 25 then Theme.Yellow
      else Theme.Red
    let tags =
      [ if c.IsStaggered then sprintf "[bold %s]STAGGERED[/]" Theme.Green
        if c.BleedStacks > 0 then sprintf "[%s]BLEED %d[/]" Theme.Red c.BleedStacks ]
    let tagPart = if tags.IsEmpty then "" else " · " + String.concat " · " tags
    sprintf "%d. [bold %s]%s[/]  [%s]HP %d%%[/] [grey](%d/%d)[/]  %s%s"
      index Theme.Pink (Markup.Escape e.Name) hpColor hpPct c.Health.Current c.Health.Maximum (Display.tempoBadge c) tagPart

  /// Renders the multi-enemy engagement HUD: the full player panel on the left, a one-row-per-foe pack
  /// table on the right, and a combat-log pane spanning the bottom. The pack used to be one bordered
  /// panel per enemy, so a large pack pushed the player card off the top of the screen.
  let private renderGroupHud (player: Combatant) (enemies: TowerEnemy list) (round: int) (logLines: string list) (intentFor: (Combatant * ActionIntent) option) =
    AnsiConsole.Clear()
    AnsiConsole.Write(
      Rule(sprintf "[bold %s]⚔ MULTI-ENEMY ENGAGEMENT — ROUND %d[/]" Theme.Yellow round)
        .Centered()
        .RuleStyle(Theme.StyleYellow)
    )

    let isStanding (e: TowerEnemy) =
      not e.IsDefeated
      && not e.Combatant.Health.IsDepleted
      && not e.Combatant.Morale.IsDepleted
      && not (CollapseState.isCollapsed e.Combatant.Collapse)

    // Who opens the exchange, read from live tempo rather than assumed: a pack that has seized the
    // Vor strikes before the player does, and that has to be visible before the player commits.
    let packHasVor =
      (not player.IsInVor) && (enemies |> List.exists (fun e -> isStanding e && e.Combatant.IsInVor))

    AnsiConsole.MarkupLine(
      if packHasVor then
        sprintf "[bold %s]⚡ THE PACK HOLDS THE VOR — it strikes before you can answer.[/]" Theme.Yellow
      elif player.IsInVor then
        sprintf "[bold %s]⚡ %s holds the Vor — you act first, the pack answers from the Nach.[/]" Theme.Yellow (Markup.Escape player.Name)
      else
        sprintf "[%s]◈ Neither side holds the Vor — the exchange opens from the Nach.[/]" Theme.Comment)

    // Deadliest first, so the foes that fall off the bottom are the ones that matter least: standing
    // before down, then level, then sheer bulk.
    let ordered =
      enemies
      |> List.sortBy (fun e ->
        (if isStanding e then 0 else 1),
        -e.Combatant.Level,
        -(e.Combatant.Health.Maximum + e.Combatant.Morale.Maximum),
        e.Name)

    // Seven rows keep the pack panel inside the height of the player card beside it.
    let maxRows = 7
    let shown = ordered |> List.truncate maxRows
    let hidden = ordered.Length - shown.Length
    let focusedId = intentFor |> Option.map (fun (actor, _) -> actor.Id)

    let pack = Grid()
    pack.AddColumn(GridColumn().PadRight(3)) |> ignore
    pack.AddColumn(GridColumn().PadRight(2)) |> ignore
    pack.AddColumn(GridColumn().PadRight(2)) |> ignore
    pack.AddColumn(GridColumn().PadRight(2)) |> ignore
    pack.AddColumn(GridColumn().PadRight(2)) |> ignore
    pack.AddColumn(GridColumn()) |> ignore

    pack.AddRow(
      Markup(sprintf "[bold %s]Foe[/]" Theme.Comment),
      Markup(sprintf "[bold %s]HP[/]" Theme.Comment),
      Markup(sprintf "[bold %s]Morale[/]" Theme.Comment),
      Markup(sprintf "[bold %s]Armor[/]" Theme.Comment),
      Markup(sprintf "[bold %s]Tempo[/]" Theme.Comment),
      Markup(sprintf "[bold %s]Status[/]" Theme.Comment)
    )
    |> ignore

    for e in shown do
      let c = e.Combatant
      let standing = isStanding e
      let isFocused = focusedId = Some c.Id
      let focusPrefix = if isFocused then sprintf "[bold %s]▸ [/]" Theme.Yellow else "  "
      let hpRatio = if c.Health.Maximum <= 0 then 0.0 else float c.Health.Current / float c.Health.Maximum
      let hpBarColor =
        if not standing then Theme.Comment
        elif hpRatio > 0.60 then Theme.Green
        elif hpRatio >= 0.25 then Theme.Yellow
        else Theme.Red
      let hpCell =
        sprintf "%s [%s]%d/%d[/]"
          (Display.renderMiniBar c.Health.Current c.Health.Maximum 5 hpBarColor)
          (if standing then hpBarColor else Theme.Comment)
          c.Health.Current
          c.Health.Maximum
      let armorText =
        // Shredded armor with a raised shield is still soaking (plating counts in AbsorptionRatio) -- show the
        // live number, or the plating looks like it does nothing at 0 durability.
        if c.Armor.IsShredded && c.Armor.Boost <= 0 then sprintf "[%s]SHREDDED[/]" Theme.Red
        elif c.Armor.IsShredded then sprintf "[%s]0%% +%d%% 🛡[/]" Theme.Yellow (int (c.Armor.AbsorptionRatio * 100.0))
        else sprintf "[%s]%d%%[/]" Theme.Yellow (int (c.Armor.AbsorptionRatio * 100.0))
      let weaponTag =
        match c.WeaponCondition with
        | WeaponCondition.Pristine -> ""
        | condition ->
          let theme =
            match condition with
            | WeaponCondition.Notched -> Theme.Yellow
            | WeaponCondition.Damaged -> Theme.Orange
            | _ -> Theme.Red

          let name = (WeaponCondition.displayNameFor c.Plane c.WeaponCondition).ToUpperInvariant()
          sprintf "[%s]%s[/]" theme name
      let assessment = TacticalAssessment.assess player c
      let threat =
        match assessment.ThreatWarning, assessment.VulnerabilitySummary with
        | Some _, Some _ -> sprintf "[bold %s]⚠ 🎯[/]" Theme.Red
        | Some _, None -> sprintf "[bold %s]⚠[/]" Theme.Red
        | None, Some _ -> sprintf "[%s]🎯[/]" Theme.Green
        | None, None -> ""
      let status =
        [ if e.Combatant.Health.IsDepleted || e.Combatant.Morale.IsDepleted || e.IsDefeated then
            sprintf "[bold %s]DOWN[/]" Theme.Red
          elif CollapseState.isCollapsed c.Collapse then
            match c.Collapse with
            | CollapseState.Collapsed reason -> sprintf "[bold %s]COLLAPSED (%A)[/]" Theme.Red reason
            | CollapseState.Stable -> ""
          threat
          weaponTag
          if c.BleedStacks > 0 then sprintf "[%s]BLEED %d[/]" Theme.Red c.BleedStacks ]
        |> List.filter (fun s -> s <> "")
        |> String.concat " "

      pack.AddRow(
        Markup(sprintf "%s[bold %s]%s[/]" focusPrefix (if standing then Theme.Pink else Theme.Comment) (Markup.Escape e.Name)),
        Markup(hpCell),
        Markup(sprintf "[%s]%d/%d[/]" (if standing then Theme.Cyan else Theme.Comment) c.Morale.Current c.Morale.Maximum),
        Markup(armorText),
        Markup(if standing then Display.tempoBadge c else ""),
        Markup(status)
      )
      |> ignore

    if hidden > 0 then
      pack.AddRow(
        Markup(sprintf "[%s]… +%d more[/]" Theme.Comment hidden),
        Markup(""),
        Markup(""),
        Markup(""),
        Markup(""),
        Markup(sprintf "[%s]below the fold[/]" Theme.Comment)
      )
      |> ignore

    let standing = enemies |> List.filter isStanding |> List.length
    let body = Grid()
    body.AddColumn(GridColumn().PadRight(3)) |> ignore
    body.AddColumn(GridColumn()) |> ignore
    body.AddRow(
      Display.createCombatantPanel player Theme.ColorGreen Theme.Green None,
      Panel(pack)
        .Header(sprintf "[bold %s] THE PACK — %d STANDING [/]" Theme.Pink standing)
        .Border(BoxBorder.Rounded)
        .BorderColor(Theme.ColorPink)
        .Expand()
    )
    |> ignore
    body.Expand <- true
    AnsiConsole.Write(body)

    // A pack fight has no wind-up line of its own until the blow lands, so the pack HUD prints the same
    // telegraph the lone-duel HUD does; otherwise a telegraphed heavy strike reads as an empty log.
    match intentFor with
    | Some (actor, intent) -> Display.writeTelegraphBanner player actor intent
    | None -> ()

    Display.writeCombatLogPanel logLines
    AnsiConsole.WriteLine()

  /// Interactive duel against a pack of enemies, modelled on runStoryDuel. The lone player resolves
  /// actions through the group-turn resolver (cleaves/splashes) while each surviving enemy strikes
  /// back in turn. When canFlee, the player may disengage at the cost of one parting strike per foe.
  let private runStoryGroupDuel (floorNumber: int) (player: Combatant) (enemies: TowerEnemy list) (canFlee: bool) (reinforce: TowerDisplay.GroupDuelReinforce option) (flankOf: CombatantId -> Fornach.Spatial.Flank) : TowerDisplay.GroupDuelResult =
    let rng = Random()
    let roller : DiceRoller = fun min max -> rng.Next(min, max + 1)

    let mutable currentPlayer = player
    let mutable currentEnemies = enemies
    let mutable round = 1
    let mutable priorDefenses = 0
    let mutable combatOver = false
    let mutable outcome = TowerDisplay.GroupDuelOutcome.Victorious

    let combatLog = System.Collections.Generic.List<string>()

    let session =
      CombatLogger.startSession
        (sprintf "%s vs the pack [%s]" currentPlayer.Name (currentEnemies |> List.map (fun e -> e.Name) |> String.concat ", "))
        "Story / Tower Group Engagement"
        currentPlayer
        (currentEnemies |> List.tryHead |> Option.map (fun e -> e.Combatant) |> Option.defaultValue currentPlayer)

    // Every damage line names the combatant it landed on: an unattributed "Damage Dealt: 42" is
    // unreadable in a pack, where the reader has to know whose pool moved.
    let nameOf (id: CombatantId) =
      if id = currentPlayer.Id then currentPlayer.Name
      else
        currentEnemies
        |> List.tryFind (fun e -> e.Combatant.Id = id)
        |> Option.map (fun e -> e.Name)
        |> Option.defaultValue "Unknown"

    let appendTurn (header: string) (events: CombatEvent list) =
      appendTurnTo combatLog nameOf currentPlayer.Id header events

    let logLines () = combatLog |> List.ofSeq

    let isLiving (e: TowerEnemy) =
      // A collapsed foe takes no further action and is execute-eligible, so it stops counting as living:
      // once the whole pack collapses the fight ends in victory instead of running forever.
      not e.IsDefeated
      && not e.Combatant.Health.IsDepleted
      && not e.Combatant.Morale.IsDepleted
      && not (CollapseState.isCollapsed e.Combatant.Collapse)

    /// The player is out of the fight when a pool empties or any status meter breaks it.
    let playerDown () =
      currentPlayer.Health.IsDepleted
      || currentPlayer.Morale.IsDepleted
      || CollapseState.isCollapsed currentPlayer.Collapse

    let living () = currentEnemies |> List.filter isLiving

    // Who opens the exchange: the pack, when it holds the Vor and the player does not.
    let packHasVor () =
      (not currentPlayer.IsInVor) && (living () |> List.exists (fun e -> e.Combatant.IsInVor))

    let mutable turnNum = 1
    // The foe whose technique the player interrupted this round; its strike defuses into a recovery.
    let mutable interruptedTarget : CombatantId option = None
    // Active defense chosen by the player this round to counter the primary incoming attack.
    let mutable playerActiveDefenseOpt : DefenseChoice option = None

    // The pack's phase: each surviving frontline foe that can reach the player strikes.
    // Capped at 3 with BastionZoneControl, or 4 normal frontline attackers, so tactical counterplay
    // remains viable and the player cannot be hit by 8 attackers at once.
    let packPhase () =
      let maxPackSwings =
        if currentPlayer.HasActivePreparation PreparationType.BastionZoneControl then 3
        else 4

      for e in living () |> List.truncate maxPackSwings do
        if not combatOver then
          let playerBeforeTurn = currentPlayer
          let isStaggered = Some e.Combatant.Id = interruptedTarget
          // Resolved once so the log line below can name the technique the foe actually used.
          let intent =
            if isStaggered then RecoveryAction SteadyForm
            else AI.chooseIntentUsing Policies.all e.Combatant currentPlayer
          let defOpt =
            if priorDefenses = 0 && currentPlayer.IsHumanoid then
              match playerActiveDefenseOpt with
              | Some def -> Some def
              | None -> Some(Guard(AI.chooseActiveDefense currentPlayer e.Combatant))
            else None
          let flankMult = flankOf e.Combatant.Id |> TowerSession.flankDamageMultiplier
          let r =
            if isStaggered then
              AnsiConsole.MarkupLine(sprintf "\n[bold %s]⚡ %s is staggered from the technique interrupt! Attack defused![/]" Theme.Yellow e.Name)
              ActionResolver.resolve roller intent e.Combatant currentPlayer
            elif floorNumber = 1 then
              ActionResolver.resolveWithoutLethalWithDefenseFlank roller intent defOpt e.Combatant currentPlayer priorDefenses flankMult
            else
              ActionResolver.resolveExWithDefenseFlank roller intent defOpt e.Combatant currentPlayer priorDefenses flankMult
          CombatLogger.recordTurn session round turnNum (sprintf "%A" intent) e.Combatant playerBeforeTurn r
          turnNum <- turnNum + 1
          appendTurn (sprintf "Round %d — %s strikes %s — %s" round e.Name currentPlayer.Name (Display.intentLogLabel intent e.Combatant)) r.Events
          currentPlayer <- r.Target
          currentEnemies <- currentEnemies |> List.map (fun en -> if en.Id = e.Id then { en with Combatant = r.Actor } else en)
          priorDefenses <- priorDefenses + 1
          let executed = r.Events |> List.exists (function CombatEvent.Executed _ -> true | _ -> false)
          if executed || playerDown () then
            outcome <- TowerDisplay.GroupDuelOutcome.Defeated
            combatOver <- true
            AnsiConsole.WriteLine()
            AnsiConsole.Write(
              Rule(sprintf "[bold %s]☠☠☠ OVERWHELMED: %s HAS FALLEN! ☠☠☠[/]" Theme.Red currentPlayer.Name)
                .Centered()
                .RuleStyle(Theme.StyleRed)
            )

    while not combatOver && not (living ()).IsEmpty do
      priorDefenses <- 0
      interruptedTarget <- None
      playerActiveDefenseOpt <- None
      // A foe that collapsed, fell or was never in the Vor cannot hold the initiative. Once nothing
      // standing holds it the exchange is uncontested and the player takes the Vor, so a pack that
      // exhausted itself hands over the initiative instead of leaving the player in the Nach.
      currentPlayer <-
        ActionResolver.settleTempoVacuum currentPlayer (currentEnemies |> List.map (fun e -> e.Combatant))
      renderGroupHud currentPlayer currentEnemies round (logLines ()) None
      let livingNow = living ()
      RuntimeLog.write (sprintf "group combat round %d: %d foe(s) standing" round livingNow.Length)

      // 1. Target selection (only meaningful with more than one foe standing)
      let target =
        match livingNow with
        | [ only ] -> only
        | many ->
          let indexed = many |> List.mapi (fun i e -> (formatGroupTargetChoice (i + 1) e, e))
          let pick =
            Display.promptSelectionWithHelp
              (sprintf "[bold %s]Choose your target:[/]" Theme.Yellow)
              (indexed |> List.map fst)
              None
              (fun () -> renderGroupHud currentPlayer currentEnemies round (logLines ()) None)
          indexed |> List.find (fun (label, _) -> label = pick) |> snd

      // 2. Action selection (with an optional disengage)
      let targetCombatant = target.Combatant
      let targetIntent = AI.chooseIntentUsing Policies.all targetCombatant currentPlayer
      let actionChoices =
        buildActionChoicesWithTelegraph currentPlayer targetCombatant (Some targetIntent) true
        @ (if canFlee then [ "🏃  Disengage (withdraw — each foe strikes as you break away)" ] else [])

      let choice =
        Display.promptSelectionWithHelp
          (sprintf "[bold %s]Round %d - Action against %s (Press '?' or F1 for Symbol Legend):[/]" Theme.Yellow round target.Name)
          actionChoices
          (Some 12)
          (fun () -> renderGroupHud currentPlayer currentEnemies round (logLines ()) (Some (targetCombatant, targetIntent)))

      if choice.Contains("Disengage") then
        // Parting strikes from the frontline living enemies (at most 4).
        AnsiConsole.MarkupLine(sprintf "\n[bold %s]%s breaks away, but the pack lashes out![/]" Theme.Yellow currentPlayer.Name)
        let mutable survived = true
        for e in living () |> List.truncate 4 do
          let intent = AI.chooseIntentUsing Policies.all e.Combatant currentPlayer
          let playerBeforeTurn = currentPlayer
          let flankMult = flankOf e.Combatant.Id |> TowerSession.flankDamageMultiplier
          let r =
            if floorNumber = 1 then
              ActionResolver.resolveWithoutLethalWithDefenseFlank roller intent None e.Combatant currentPlayer priorDefenses flankMult
            else
              ActionResolver.resolveExWithDefenseFlank roller intent None e.Combatant currentPlayer priorDefenses flankMult
          CombatLogger.recordTurn session round turnNum (sprintf "%A" intent) e.Combatant playerBeforeTurn r
          turnNum <- turnNum + 1
          appendTurn (sprintf "Round %d — %s strikes %s (parting blow) — %s" round e.Name currentPlayer.Name (Display.intentLogLabel intent e.Combatant)) r.Events
          currentPlayer <- r.Target
          currentEnemies <- currentEnemies |> List.map (fun en -> if en.Id = e.Id then { en with Combatant = r.Actor } else en)
          priorDefenses <- priorDefenses + 1
          if playerDown () then survived <- false
        if survived then
          outcome <- TowerDisplay.GroupDuelOutcome.Fled
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]You slip away from the pack![/]" Theme.Green)
        else
          outcome <- TowerDisplay.GroupDuelOutcome.Defeated
        combatOver <- true
      else
        let intent = parseActionChoice choice
        match intent with
        | ActiveDefense def -> playerActiveDefenseOpt <- Some(Guard def)
        | StandardAttack atk when choice.Contains("Seize Indes") -> playerActiveDefenseOpt <- Some(SeizeIndes atk)
        | _ -> playerActiveDefenseOpt <- None

        // Every guard stack this round pays per foe still standing, so the pack size is stamped once here,
        // before either side acts, and re-stamped each round as the pack thins out.
        currentPlayer <- { currentPlayer with EnemiesFaced = (living ()).Length }

        // 3a. The pack opens when it holds the Vor and the player does not: it strikes before the
        // player's action, and its end-of-round answer is skipped so neither side strikes twice.
        let openedByPack = packHasVor ()
        if openedByPack then
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]⚡ The pack holds the Vor and strikes before you can answer![/]" Theme.Yellow)
          packPhase ()

          if (living ()).IsEmpty then
            outcome <- TowerDisplay.GroupDuelOutcome.Victorious
            currentPlayer <- Combatant.growFromEncounter (currentEnemies |> List.map (fun e -> e.Combatant)) currentPlayer
            combatOver <- true
            AnsiConsole.WriteLine()
            AnsiConsole.Write(
              Rule(sprintf "[bold %s]★★★ PACK VANQUISHED: %s PREVAILED! ★★★[/]" Theme.Green currentPlayer.Name)
                .Centered()
                .RuleStyle(Theme.StyleGreen)
            )

        let wasGuardConsumedByPack =
          openedByPack
          && (match intent with ActiveDefense _ -> true | _ -> false)

        if not combatOver && not wasGuardConsumedByPack then
          // 3. Player action via the group-turn resolver (cleave/splash onto nearby foes).
          // Read target and secondaries afresh from currentEnemies so packPhase state
          // (damage, meters, collapse, tempo) is preserved rather than overwritten by stale snapshots.
          let freshTarget =
            currentEnemies
            |> List.tryFind (fun e -> e.Id = target.Id)
            |> Option.map (fun e -> e.Combatant)
            |> Option.defaultValue targetCombatant
          let freshOtherLiving =
            currentEnemies
            |> List.filter isLiving
            |> List.filter (fun e -> e.Id <> target.Id)
            |> List.map (fun e -> e.Combatant)

          let actionLabel = Display.intentLogLabel intent currentPlayer
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]%s executes %s...[/]" Theme.Green (Markup.Escape currentPlayer.Name) (Markup.Escape actionLabel))
          let playerBefore = currentPlayer
          let targetBefore = freshTarget
          let res = ActionResolver.resolveGroupTurn roller intent currentPlayer freshTarget freshOtherLiving
          let isTargetInterrupted =
            res.Events
            |> List.exists (function CombatEvent.ActionInterrupted(_, tId, _, _) when tId = target.Combatant.Id -> true | _ -> false)
          // An interrupted foe recovers instead of striking when the pack's phase comes round.
          interruptedTarget <- if isTargetInterrupted then Some target.Combatant.Id else None
          CombatLogger.recordTurn session round turnNum choice playerBefore targetBefore
            { Actor = res.Actor; Target = res.PrimaryTarget; Events = res.Events; Contest = None }
          turnNum <- turnNum + 1
          appendTurn (sprintf "Round %d — %s uses %s on %s" round currentPlayer.Name actionLabel target.Name) res.Events
          currentPlayer <- res.Actor
          currentEnemies <-
            currentEnemies
            |> List.map (fun e ->
              if e.Id = target.Id then { e with Combatant = res.PrimaryTarget }
              else
                match res.SecondaryTargets |> List.tryFind (fun (c: Combatant) -> c.Id = e.Combatant.Id) with
                | Some c -> { e with Combatant = c }
                | None -> e)

          if (living ()).IsEmpty then
            outcome <- TowerDisplay.GroupDuelOutcome.Victorious
            currentPlayer <- Combatant.growFromEncounter (currentEnemies |> List.map (fun e -> e.Combatant)) currentPlayer
            combatOver <- true
            AnsiConsole.WriteLine()
            AnsiConsole.Write(
              Rule(sprintf "[bold %s]★★★ PACK VANQUISHED: %s PREVAILED! ★★★[/]" Theme.Green currentPlayer.Name)
                .Centered()
                .RuleStyle(Theme.StyleGreen)
            )

        // 4. Enemy answer: skipped when the pack already opened this round.
        if not combatOver && not openedByPack then
          AnsiConsole.MarkupLine(sprintf "\n[bold %s]The pack retaliates...[/]" Theme.Pink)
          packPhase ()

      if not combatOver then
        // Between rounds the pack may grow if the frontline is not saturated (< 4 active foes):
        if (living ()).Length < 4 then
          match reinforce with
          | Some join ->
            let before = currentEnemies.Length
            let playerAfterJoin, enemiesAfterJoin = join currentPlayer currentEnemies
            currentPlayer <- playerAfterJoin
            currentEnemies <- enemiesAfterJoin
            let joined = currentEnemies.Length - before
            if joined > 0 then
              RuntimeLog.write (sprintf "group combat round %d: %d reinforcement(s) joined" round joined)
              AnsiConsole.MarkupLine(sprintf "\n[bold %s]%d more foe(s) close in and join the fight![/]" Theme.Pink joined)
          | None -> ()

        // The Vor the player holds is held against one foe at a time: the one engaged this round. Every
        // other foe stands in its own engagement and keeps -- or regains -- the initiative against the
        // player, so a foe the player yielded stays out only while it is the focus. Without this, taking
        // the Vor once held the whole pack in the Nach for the rest of the fight.
        // A collapsed, defeated, or outmatched foe that ceded initiative never reclaims the Vor.
        if not combatOver && currentPlayer.IsInVor then
          currentEnemies <-
            currentEnemies
            |> List.map (fun e ->
              if e.Id <> target.Id
                 && e.Combatant.IsInNach
                 && isLiving e
                 && not (ActionResolver.cedesInitiative currentPlayer e.Combatant) then
                { e with Combatant = Combatant.seizeVor e.Combatant }
              else
                e)

        AnsiConsole.WriteLine()
        AnsiConsole.Markup(sprintf "[%s]Press any key to proceed to next round...[/]" Theme.Comment)
        Console.ReadKey(true) |> ignore
        round <- round + 1

    let outcomeStr =
      match outcome with
      | TowerDisplay.GroupDuelOutcome.Victorious -> "PlayerVictorious"
      | TowerDisplay.GroupDuelOutcome.Defeated -> "PlayerDefeated"
      | TowerDisplay.GroupDuelOutcome.Fled -> "PlayerFled"

    let finalOpponent =
      currentEnemies |> List.tryHead |> Option.map (fun e -> e.Combatant) |> Option.defaultValue currentPlayer

    let latestLog, _ = CombatLogger.writeSession session outcomeStr currentPlayer finalOpponent
    RuntimeLog.write (sprintf "group combat end: %s (log %s)" outcomeStr latestLog)
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine(sprintf "[dim grey]Tactical combat log saved to: %s[/]" latestLog)

    let finalEnemies =
      currentEnemies
      |> List.map (fun e -> if isLiving e then e else { e with IsDefeated = true })

    // Victory banks stress growth (+5 paid-for / +1 above half Recklessness); defeat banks the
    // scar growth (+10). The caller writes this player into the run state before its trauma reset,
    // which restores pools but keeps Stats and Progression.
    let grownPlayer =
      match outcome with
      | TowerDisplay.GroupDuelOutcome.Victorious -> Combatant.growFromStressedVictory currentPlayer
      | TowerDisplay.GroupDuelOutcome.Defeated -> Combatant.growFromDefeat currentPlayer
      | TowerDisplay.GroupDuelOutcome.Fled -> currentPlayer

    { Outcome = outcome; Player = grownPlayer; Enemies = finalEnemies }

  let private createProloguePlayer (className: string) : Combatant =
    StoryBosses.createProloguePlayer className

  let private runInteractiveStory (debug: bool) (forcedSeed: int option) =
    AnsiConsole.Clear()
    AnsiConsole.Write(
      Rule(sprintf "[bold %s]TRUCKED: THE TRAUMA LOOP & THE GRIEF STAGES[/]" Theme.Yellow)
        .Centered()
        .RuleStyle(Theme.StylePurple)
    )
    AnsiConsole.WriteLine()

    // 1. Atmospheric Prologue Introduction
    let introText =
      "The asphalt was slick with evening drizzle... Headlights tore through the downpour, metal screamed, and the cold darkness took you.\n\n" +
      "Now, the roar of screaming metal recedes into the steady hiss of rain through black pine boughs.\n" +
      "You peel your face out of the wet loam of a rain-soaked forest. Your past is an empty, dark vault.\n\n" +
      "Beside you among the roots lies a battered chest, its iron lock already broken and hanging loose."

    let introPanel =
      Panel(Markup(sprintf "[bold %s]%s[/]" Theme.Foreground (Markup.Escape introText)))
        .Header(sprintf "[bold %s] PROLOGUE: THE FATAL CROSSING [/]" Theme.Cyan)
        .Border(BoxBorder.Rounded)
        .BorderStyle(Style(foreground = Nullable Theme.ColorCyan))

    AnsiConsole.Write(introPanel)
    AnsiConsole.WriteLine()

    // 2. Scavenger Chest Plane Selection — the only thing chosen is the combat plane; the class
    // label (Fighter / Mage) is derived from the kit's stat distribution (ADR f1f14149).
    let choices =
      [ "⚔️ Physical — Tempered Arming Sword & Reinforced Shield: a martial master of kinetic momentum, agile parries, and bastion guard"
        "🪄 Mental — Carved Ash Staff & Runic Robes: an arcane practitioner channeling cognitive resonance, phantasmal static, and protective wards" ]

    let choice =
      Display.promptSelectionWithHelp
        (sprintf "[bold %s]Scavenge the battered chest (Choose your combat plane):[/]" Theme.Yellow)
        choices
        None
        (fun () -> ())

    let chosenPlane = if choice.StartsWith("⚔️") then "physical" else "mental"

    let player = StoryBosses.createProloguePlayer chosenPlane
    let weapon =
      player.EquippedItems
      |> List.map (fun w -> w.Name)
      |> String.concat ", "
    let desc =
      match player.Class with
      | CharacterClass.Mage -> "Arcane Practitioner • Resonance Spikes, Phantasmal Weaving & Aegis Lattices"
      | _ -> "Martial Master • Kinetic Force, Finesse Cadence & Bastion Defense"

    let panel =
      Panel(Markup(sprintf "[bold %s]󰓥 CLASS AWAKENED: %s[/] [grey](%s)[/]\n[italic white]Equipped: %s  •  Combat Stance: %s[/]\n[grey]Bundle of sharpened caltrops recovered inside lid.[/]"
        Theme.Green (player.Class.Name.ToUpperInvariant()) desc (Markup.Escape weapon) (Display.stanceDisplayName player.Stance)))
        .Border(BoxBorder.Heavy)
        .BorderStyle(Style(foreground = Nullable Theme.ColorGreen))

    AnsiConsole.Clear()
    AnsiConsole.Write(panel)
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine(sprintf "[bold %s]A child's terrified scream cuts through the hiss of the rain and the creak of the pines...[/]" Theme.Yellow)
    AnsiConsole.MarkupLine(sprintf "[italic %s]Ahead lies the rain-soaked forest floor. Navigate, explore, and fight your way to the bandit camp at the cave-mouth descending into the quarry.[/]" Theme.Comment)
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine(sprintf "[%s]Press any key to enter the forest and begin your expedition...[/]" Theme.Comment)
    Console.ReadKey(true) |> ignore

    // 3. Launch 2D Spatial Map Loop for Story Campaign
    TowerDisplay.runStoryCrawl player 1 runStoryDuel runStoryGroupDuel DeathAnimation.playTruckReplay debug forcedSeed

  [<EntryPoint>]
  let main (args: string array) =
    let cliOptions, debugOptions = parseCliArgs args

    if args |> Array.exists (fun a -> a = "--verbose" || a = "-v" || a = "--log") then
      RuntimeLog.``begin`` args |> ignore

    let onUnhandled (e: UnhandledExceptionEventArgs) =
      match e.ExceptionObject with
      | :? exn as ex -> RuntimeLog.exn "UNHANDLED EXCEPTION" ex
      | other -> RuntimeLog.write (sprintf "UNHANDLED EXCEPTION (non-exception object): %A" other)

    AppDomain.CurrentDomain.UnhandledException.Add onUnhandled

    RuntimeLog.section "MODE SELECTION"
    RuntimeLog.write (sprintf "mode=%A debug=%b forcedSeed=%A" cliOptions debugOptions.Enabled debugOptions.ForcedSeed)

    try
      match cliOptions with
      | RunBestiarySwarmMatrix tierOpt ->
        printBanner()
        Simulation.renderBestiarySwarmMatrix tierOpt
        0

      | RunPeerBalanceMatrix ->
        printBanner()
        Simulation.renderPeerBalanceMatrix()
        0

      | RunBalanceMatrix ->
        printBanner()
        Simulation.renderBalanceMatrix()
        0

      | RunProgressionSimulation (runs, maxFloors) ->
        printBanner()
        Simulation.renderProgressionDashboard(Simulation.runProgressionSim runs maxFloors)
        0

      | RunSimulation (nameA, nameB, iters) ->
        printBanner()
        let optA = Archetypes.findByName nameA
        let optB = Archetypes.findByName nameB
        match optA, optB with
        | Some a, Some b ->
          let summary = Simulation.runBatch a b iters
          Simulation.renderDashboard summary (a.Factory()) (b.Factory())
          0
        | None, _ ->
          AnsiConsole.MarkupLine(sprintf "[bold %s]Error: Archetype '%s' not recognized.[/]" Theme.Red nameA)
          1
        | _, None ->
          AnsiConsole.MarkupLine(sprintf "[bold %s]Error: Archetype '%s' not recognized.[/]" Theme.Red nameB)
          1

      | RunGroupSimulation (soloName, mobName, mobCount, iters) ->
        printBanner()
        let optSolo = Archetypes.findByName soloName
        let optMob = Archetypes.findByName mobName
        match optSolo, optMob with
        | Some solo, Some mob ->
          let summary = Simulation.runGroupBatch solo mob mobCount iters
          Simulation.renderGroupDashboard summary (solo.Factory()) (mob.Factory())
          0
        | None, _ ->
          AnsiConsole.MarkupLine(sprintf "[bold %s]Error: Archetype '%s' not recognized.[/]" Theme.Red soloName)
          1
        | _, None ->
          AnsiConsole.MarkupLine(sprintf "[bold %s]Error: Archetype '%s' not recognized.[/]" Theme.Red mobName)
          1

      | InteractiveMenu ->
        let mutable running = true
        while running do
          try
            AnsiConsole.Clear()
            printBanner()

            let termWidth = Math.Max(80, AnsiConsole.Profile.Width)
            let contentWidth = Math.Min(96, termWidth - 4)
            let margin = Math.Max(0, (termWidth - contentWidth) / 2)
            let pad = String(' ', margin)

            let formatMenuItem icon title desc color =
              sprintf "%s%s [bold %s]%-40s[/] [italic %s]── %s[/]" pad icon color title Theme.Comment desc

            // Center the header and hint on the full terminal width; `pad` is the
            // menu margin, which is narrower than the terminal whenever contentWidth
            // (96) is capped below the terminal width. Count codepoints, not UTF-16
            // units: the Nerd Font icons are non-BMP and occupy a single cell.
            let centerPad (text: string) =
              let visible = (Markup.Remove text).EnumerateRunes() |> Seq.length
              String(' ', Math.Max(0, (termWidth - visible) / 2))

            let promptHeader = "══════════ 󰒋 SELECT EXPEDITION OR BENCHMARK MODE ══════════"
            let promptHint = "      (󰌌 Navigate: [bold white]↑/↓[/] or [bold white]j/k[/]  •  󰌑 Select: [bold white]Enter[/]  •  󰗼 Quit: [bold white]Exit[/])"

            let promptTitle =
              sprintf "%s[bold %s]%s[/]\n%s[grey]%s[/]\n"
                (centerPad promptHeader) Theme.Yellow promptHeader (centerPad promptHint) promptHint

            let choice =
              Display.promptWithVim(
                SelectionPrompt<string>()
                  .Title(promptTitle)
                  .PageSize(12)
                  .HighlightStyle(Theme.StylePromptHighlight)
                  .AddChoices([
                    formatMenuItem "󰈙 " "Interactive Story Mode" "Narrative prologue & aspect battles" Theme.Cyan
                    formatMenuItem "󰒋 " "Ascend The Infinite Tower" "Roguelike procedural dungeon crawl" Theme.Yellow
                    formatMenuItem "󰓥 " "Interactive Duel Arena" "Tactical turn-based combat duel" Theme.Green
                    formatMenuItem "󰓎 " "Monte-Carlo Balance Simulator (1 vs 1)" "Statistical win-rate analysis" Theme.Cyan
                    formatMenuItem " " "1 vs N Encirclement Swarm Simulator" "Swarm overwhelm stress test" Theme.Pink
                    formatMenuItem " " "Custom Combatant Builder" "Interactive stat & stance forge" Theme.Orange
                    formatMenuItem "󰂺 " "View Archetype Roster" (sprintf "Inspect %d mastery archetypes" Archetypes.allArchetypes.Length) Theme.Purple
                    formatMenuItem "󰓥 " "Peer Class 1v1 Balance Matrix" "Cross-tier 100-run pairwise duel benchmark" Theme.Green
                    formatMenuItem "󰈷 " "Swarm Tipping Point Balance Matrix" "128-matchup macro balance benchmark" Theme.Yellow
                    formatMenuItem "󰞁 " "Bestiary Monster Swarm Matrix" "Swarm tipping point for all 21 monsters vs classes" Theme.Red
                    formatMenuItem "󰒋 " "Symbol & Glyph Reference Manual" "Exhaustive guide to icons, meters & hazards" Theme.Yellow
                    formatMenuItem "󰗼 " "Exit" "Close the Trucked Arena" Theme.Red
                  ])
              )

            RuntimeLog.write (sprintf "menu choice: %s" (Markup.Remove choice))

            if choice.Contains("Interactive Story Mode") then
              runInteractiveStory debugOptions.Enabled debugOptions.ForcedSeed
            elif choice.Contains("Ascend The Infinite Tower") then
              let playerArch = promptSelectArchetype (sprintf "[bold %s]Select Expedition Champion:[/]" Theme.Yellow) Archetypes.playerArchetypes
              let player = playerArch.Factory()
              TowerDisplay.runTowerCrawl player 1 runStoryDuel runStoryGroupDuel DeathAnimation.playTruckReplay debugOptions.Enabled debugOptions.ForcedSeed
            elif choice.Contains("Interactive Duel Arena") then
              let playerArch = promptSelectArchetype (sprintf "[bold %s]Select Player Combatant:[/]" Theme.Green) Archetypes.playerArchetypes
              let enemyArch = promptSelectArchetype (sprintf "[bold %s]Select Opponent Combatant:[/]" Theme.Pink) Archetypes.allArchetypes
              runInteractiveDuel playerArch enemyArch

            elif choice.Contains("Monte-Carlo Balance Simulator (1 vs 1)") then
              let archA = promptSelectArchetype (sprintf "[bold %s]Select Combatant A:[/]" Theme.Green) Archetypes.allArchetypes
              let archB = promptSelectArchetype (sprintf "[bold %s]Select Combatant B:[/]" Theme.Pink) Archetypes.allArchetypes
              runBalanceSimulator archA archB

            elif choice.Contains("1 vs N Encirclement Swarm Simulator") then
              let soloArch = promptSelectArchetype (sprintf "[bold %s]Select Solo Champion:[/]" Theme.Green) Archetypes.playerArchetypes
              let mobArch = promptSelectArchetype (sprintf "[bold %s]Select Swarm Opponent Archetype:[/]" Theme.Pink) Archetypes.allArchetypes
              runGroupBalanceSimulator soloArch mobArch

            elif choice.Contains("Custom Combatant Builder") then
              let customArch = createCustomCombatantInteractive ()
              let enemyArch = promptSelectArchetype (sprintf "[bold %s]Select Opponent to Test Against:[/]" Theme.Pink) Archetypes.allArchetypes
              runInteractiveDuel customArch enemyArch

            elif choice.Contains("View Archetype Roster") then
              showRoster ()

            elif choice.Contains("Peer Class 1v1 Balance Matrix") then
              Simulation.renderPeerBalanceMatrix ()
              AnsiConsole.MarkupLine(sprintf "[%s]Press any key to return to menu...[/]" Theme.Comment)
              Console.ReadKey(true) |> ignore

            elif choice.Contains("Swarm Tipping Point Balance Matrix") then
              Simulation.renderBalanceMatrix ()
              AnsiConsole.MarkupLine(sprintf "[%s]Press any key to return to menu...[/]" Theme.Comment)
              Console.ReadKey(true) |> ignore

            elif choice.Contains("Bestiary Monster Swarm Matrix") then
              // The swarm benchmark reports the apex tier and the Legend tier (level 1000) only, so there
              // is nothing to pick; renderBestiarySwarmMatrix ignores its tier argument.
              Simulation.renderBestiarySwarmMatrix None
              AnsiConsole.MarkupLine(sprintf "[%s]Press any key to return to menu...[/]" Theme.Comment)
              Console.ReadKey(true) |> ignore

            elif choice.Contains("Symbol & Glyph Reference Manual") then
              Display.showSymbolAndGlyphLegend ()

            elif choice.Contains("Exit") then
              running <- false
          with
          | MenuCancelled ->
            AnsiConsole.Cursor.Show(true)

        AnsiConsole.MarkupLine(sprintf "[bold %s]Exiting Trucked Arena. Farewell![/]" Theme.Yellow)
        0
    with e ->
      RuntimeLog.exn "FATAL" e
      AnsiConsole.Write(
        Panel(Markup(sprintf "[bold red]Fatal error:[/] %s" (Markup.Escape e.Message)))
          .Header("[bold red]UNHANDLED RUNTIME ERROR[/]")
          .BorderColor(Color.Red)
      )
      if RuntimeLog.enabled then
        AnsiConsole.MarkupLine(sprintf "[%s]Verbose log written to: %s[/]" Theme.Comment RuntimeLog.path)
      Console.Error.WriteLine(e.ToString())
      1
