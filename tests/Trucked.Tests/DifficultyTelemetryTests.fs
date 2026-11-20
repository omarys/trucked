namespace Trucked.Tests
open Xunit
open Xunit.Abstractions
open System
open Fornach.Domain
open Fornach.Engine
open Trucked.Cli

/// Difficulty telemetry: one place that answers "is the story bimodal?" -- fights either mowed down
/// or lost in one round to a condition track nobody saw coming. It prints the profile (deaths by
/// cause and round, mow-downs) instead of asserting a single mean, because the tune it informs is
/// a distribution. The aiPolicy bot is the "competent player" stand-in: good enough to win most
/// fair fights, not optimal enough to hide a broken one.
module DifficultyTelemetryTests =

  let private makeRoller (seed: int) : DiceRoller =
    let rng = Random(seed)
    fun min max -> rng.Next(min, max + 1)

  /// The honest cause of a defeat, matching the banner the player sees (TowerDisplay.defeatCause).
  let private deathCause (c: Combatant) =
    match c.Collapse with
    | CollapseState.Collapsed CollapseReason.RecklessExposure -> "collapse:recklessness"
    | CollapseState.Collapsed CollapseReason.SomaticAnoxia -> "collapse:exhaustion"
    | CollapseState.Collapsed CollapseReason.Exsanguination -> "collapse:frustration"
    | CollapseState.Collapsed CollapseReason.CatatonicStupor -> "collapse:cognitive-fatigue"
    | CollapseState.Collapsed CollapseReason.ContradictionLock -> "collapse:confusion"
    | CollapseState.Collapsed CollapseReason.HystericalMeltdown -> "collapse:provoke"
    | CollapseState.Stable when c.Health.Current <= 0 -> "health"
    | CollapseState.Stable when c.Morale.Current <= 0 -> "morale"
    | _ -> "overwhelmed"

  let private meterPeaks (m: StatusMeters) =
    [ "reck", m.Recklessness.Value
      "exh", m.Exhaustion.Value
      "frus", m.Frustration.Value
      "cogf", m.CognitiveFatigue.Value
      "conf", m.Confusion.Value
      "prov", m.Provoke.Value ]
    |> List.filter (fun (_, v) -> v > 0)
    |> List.map (fun (k, v) -> sprintf "%s=%d" k v)
    |> String.concat " "

  /// One fight's story: outcome, rounds, and on a loss the cause with meter peaks at the end.
  type private FightRow =
    { Floor: int
      Foes: string
      Won: bool
      Rounds: int
      DeathCause: string
      DeathMeters: string
      Untouched: bool }

  /// Climbs the story the way the game does: floor 1 to floor 5 chained, growth carried
  /// between fights and floors, every floor rescaled to the player's live level on entry.
  let private climbStory (className: string) (seed: int) (restBetween: bool) : FightRow list =
    let mutable survivor = StoryBosses.createProloguePlayer className
    let rows = ResizeArray<FightRow>()
    for floorNumber in 1 .. 5 do
      let floor = (TowerSession.initSessionWithMode survivor (7000 + seed) floorNumber true).CurrentFloor
      let chambers = OptimalPlayer.chambersOf floor 3
      chambers
      |> List.iteri (fun i chamber ->
        let foes = chamber |> List.map (fun e -> e.Combatant)
        let roller = makeRoller ((7000 + seed) * 100 + floorNumber * 31 + i)
        let result, after =
          Simulation.runPreparedGroupMatchWith Simulation.aiPolicy roller survivor foes 80
        rows.Add(
          { Floor = floorNumber
            Foes = sprintf "(Lv.%d player) %s" survivor.Level (foes |> List.map (fun f -> f.Name) |> String.concat "+")
            Won = result.SoloWon
            Rounds = result.Rounds
            DeathCause = (if result.SoloWon then "" else deathCause after)
            DeathMeters = (if result.SoloWon then "" else meterPeaks after.Meters)
            Untouched =
              result.SoloWon
              && after.Health.Current = after.Health.Maximum
              && after.Morale.Current = after.Morale.Maximum })
        if result.SoloWon then
          let grown = Combatant.growFromEncounter foes after
          let xp = foes |> List.sumBy (fun f -> f.Level * 10 + 25)
          let levelled, _ = TowerSession.applyExperience grown xp
          // `restBetween` is the fair-fight probe: pools topped up before every chamber, the way the
          // real game lets you walk off damage. It separates "this pack is lethal" from "attrition
          // across a floor with no rest kills you".
          let rested =
            if restBetween then
              { levelled with
                  Health = { Current = levelled.Health.Maximum; Maximum = levelled.Health.Maximum }
                  Morale = { Current = levelled.Morale.Maximum; Maximum = levelled.Morale.Maximum } }
            else
              levelled
          survivor <- { Combatant.clearStacks rested with Meters = StatusMeters.Zero })
    List.ofSeq rows

  /// One aspect duel at the level the story puts in front of you, instrumented the same way.
  let private duelAspect (cls: CharacterClass) (floor: int) (create: unit -> Combatant) (seed: int) : FightRow =
    let kit = (StoryBosses.createProloguePlayer "fighter").EquippedItems
    let player =
      kit
      |> List.fold
        (fun (c: Combatant) item -> Combatant.applyItemBonus item c)
        (TierFactory.createClassLevel cls (ProgressionScale.storyGuardianLevel floor))
      |> fun c -> { c with PlayerStyle = true }
    let boss = create () |> StoryBosses.scaleToStage floor
    let roller = makeRoller (seed + floor * 977)
    let result, after =
      Simulation.runPreparedGroupMatchWith Simulation.aiPolicy roller player [ boss ] 200
    { Floor = floor
      Foes = boss.Name
      Won = result.SoloWon
      Rounds = result.Rounds
      DeathCause = (if result.SoloWon then "" else deathCause after)
      DeathMeters = (if result.SoloWon then "" else meterPeaks after.Meters)
      Untouched =
        result.SoloWon
        && after.Health.Current = after.Health.Maximum
        && after.Morale.Current = after.Morale.Maximum }

  type DifficultyTelemetry(output: ITestOutputHelper) =

    [<Fact>]
    member _.``story difficulty profile prints deaths by cause and mow-downs`` () =
      let aspects =
        [ "Guilt", StoryBosses.createGuiltAspect, 2
          "Denial", StoryBosses.createDenialAspect, 3
          "Anger", StoryBosses.createAngerAspect, 4
          "Bargaining", StoryBosses.createBargainingAspect, 5
          "Depression", StoryBosses.createDepressionAspect, 6
          "Acceptance", StoryBosses.createAcceptanceAspect, 7 ]
      for className in [ "fighter"; "mage" ] do
        for restBetween in [ false; true ] do
        let mode = if restBetween then "fresh pools" else "no rest    "
        let cls = if className = "mage" then CharacterClass.Mage else CharacterClass.Fighter
        let floorRows =
          [ for s in 1 .. 12 do
              yield! climbStory className s restBetween ]
        let aspectRows =
          aspects
          |> List.collect (fun (name, create, floor) ->
            List.init 12 (fun s ->
              let r = duelAspect cls floor create s
              { r with Foes = sprintf "%s aspect (floor %d)" name floor }))
        let rows = floorRows @ aspectRows
        let deaths = rows |> List.filter (fun r -> not r.Won)
        output.WriteLine(
          sprintf
            "%s [%s]: fights %d deaths %d (%.0f%%)"
            className
            mode
            rows.Length
            deaths.Length
            (float deaths.Length / float rows.Length * 100.0))
        for cause, n in deaths |> List.countBy (fun r -> r.DeathCause) |> List.sortByDescending snd do
          output.WriteLine(sprintf "  death by %-28s %d" cause n)
        let oneRound = deaths |> List.filter (fun r -> r.Rounds <= 2)
        output.WriteLine(
          sprintf
            "  deaths in <=2 rounds: %d %s"
            oneRound.Length
            (oneRound
             |> List.countBy (fun r -> r.DeathCause)
             |> List.map (fun (c, n) -> sprintf "%s:%d" c n)
             |> String.concat " "))
        for r in deaths |> List.truncate 24 do
          output.WriteLine(sprintf "    r%d %-54s %-26s [%s]" r.Rounds r.Foes r.DeathCause r.DeathMeters)
        let mows = rows |> List.filter (fun r -> r.Untouched && r.Rounds <= 2)
        output.WriteLine(sprintf "  mow-downs (won <=2 rounds, untouched): %d of %d" mows.Length rows.Length)
        for floor in 1 .. 5 do
          let fr = rows |> List.filter (fun r -> r.Floor = floor && not (r.Foes.Contains "aspect"))
          if not fr.IsEmpty then
            let fd = fr |> List.filter (fun r -> not r.Won)
            let avgRounds = float (List.sumBy (fun r -> r.Rounds) fr) / float fr.Length
            output.WriteLine(
              sprintf
                "  floor %d: fights %d deaths %d avg rounds %.1f untouched %.0f%%"
                floor
                fr.Length
                fd.Length
                avgRounds
                (float (List.length (List.filter (fun r -> r.Untouched) fr)) / float fr.Length * 100.0))
      Assert.True(true)
