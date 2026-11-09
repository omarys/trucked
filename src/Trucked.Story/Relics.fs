namespace Trucked.Story

open System
open Fornach.Domain

module Relics =

  /// Kinetic Dynamo: Whiffs grant +1 Momentum instead of resetting combo
  let kineticDynamo : RelicModifier = {
    Id = "relic_kinetic_dynamo"
    Name = "Kinetic Dynamo"
    Description = "Whiffs convert kinetic dissipation into +1 Momentum instead of resetting combo."
    Apply = fun trigger actor targetOpt ->
      match trigger with
      | RelicHookTrigger.OnWhiff true ->
        let updated = { actor with Momentum = actor.Momentum + 1 }
        let evt = CombatEvent.RelicEffectTriggered(actor.Id, "Kinetic Dynamo", "Whiff converted into kinetic acceleration (+1 Momentum)!")
        updated, targetOpt, [ evt ]
      | _ -> actor, targetOpt, []
  }

  /// Vampiric Riposte: Winning Seize Indes restores 10% maximum Health
  let vampiricRiposte : RelicModifier = {
    Id = "relic_vampiric_riposte"
    Name = "Vampiric Riposte"
    Description = "Winning Seize Indes counters siphon vitality, restoring 10% max Health."
    Apply = fun trigger actor targetOpt ->
      match trigger with
      | RelicHookTrigger.OnIndesResolved true ->
        let healAmount = Math.Max(5, actor.Health.Maximum / 10)
        let updated = { actor with Health = actor.Health.ApplyDelta healAmount }
        let evt = CombatEvent.RelicEffectTriggered(actor.Id, "Vampiric Riposte", sprintf "Counter-strike siphoned vitality, restoring +%d Health!" healAmount)
        updated, targetOpt, [ evt ]
      | _ -> actor, targetOpt, []
  }

  /// Fever Pitch Prism: When Recklessness >= 75, landed strikes convert reckless entropy into psychic catharsis
  let feverPitchPrism : RelicModifier = {
    Id = "relic_fever_pitch"
    Name = "Fever Pitch Prism"
    Description = "When Recklessness reaches 75+, landed strikes convert entropy into psychic catharsis (+15 Morale, -20 Recklessness)."
    Apply = fun trigger actor targetOpt ->
      match trigger with
      | RelicHookTrigger.OnStrikeLanded _ when actor.Meters.Recklessness.Value >= 75 ->
        let updated =
          { actor with
              Morale = actor.Morale.ApplyDelta 15
              Meters = { actor.Meters with Recklessness = actor.Meters.Recklessness - 20 } }
        let evt = CombatEvent.RelicEffectTriggered(actor.Id, "Fever Pitch Prism", "Entropy inverted! Reckless fervor converted into psychic catharsis (+15 Morale, -20 Recklessness)!")
        updated, targetOpt, [ evt ]
      | _ -> actor, targetOpt, []
  }

  /// Phantasmal Cloak: Yielding the Vor to the Nach automatically weaves 1 Mirror Clone
  let phantasmalCloak : RelicModifier = {
    Id = "relic_phantasmal_cloak"
    Name = "Phantasmal Cloak"
    Description = "Yielding the Vor to the Nach automatically weaves a defensive Mirror Clone."
    Apply = fun trigger actor targetOpt ->
      match trigger with
      | RelicHookTrigger.OnTempoShifted false ->
        let updated = { actor with MirrorClones = Math.Min(3, actor.MirrorClones + 1) }
        let evt = CombatEvent.RelicEffectTriggered(actor.Id, "Phantasmal Cloak", "Yielding the Vor billowed phantasmal silk, weaving a protective Mirror Clone!")
        updated, targetOpt, [ evt ]
      | _ -> actor, targetOpt, []
  }

  /// All unique story relics available in the tower
  let all : RelicModifier list =
    [ kineticDynamo; vampiricRiposte; feverPitchPrism; phantasmalCloak ]
