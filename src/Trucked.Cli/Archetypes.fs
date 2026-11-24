namespace Trucked.Cli

open System
open Fornach.Domain

module Archetypes =

  // =========================================================================
  // Archetype Tier Factories
  // =========================================================================

  let createClassTier (cls: CharacterClass) (tier: CombatTier) () =
    TierFactory.createClassTier cls tier

  /// Player archetypes carry player-style discipline (fluid stances and Complex Forms, preparations,
  /// a varied attack kit). Which side of a simulation is the player is decided by the simulation, so
  /// the registry itself stays plain data.
  let private asPlayer (c: Combatant) = c

  let createVeteranFighter () = asPlayer (TierFactory.createClassTier CharacterClass.Fighter CombatTier.Veteran)
  let createVeteranMage () = asPlayer (TierFactory.createClassTier CharacterClass.Mage CombatTier.Veteran)

  let createMasterFighter () = asPlayer (TierFactory.createClassTier CharacterClass.Fighter CombatTier.Master)
  let createMasterMage () = asPlayer (TierFactory.createClassTier CharacterClass.Mage CombatTier.Master)

  let createGrandMasterFighter () = asPlayer (TierFactory.createClassTier CharacterClass.Fighter CombatTier.GrandMaster)
  let createGrandMasterMage () = asPlayer (TierFactory.createClassTier CharacterClass.Mage CombatTier.GrandMaster)

  let createGrandmasterFighter = createGrandMasterFighter
  let createGrandmasterMage = createGrandMasterMage

  // =========================================================================
  // Novice Archetypes (Level 1)
  // =========================================================================

  let createNoviceFighter () =
    let c = TierFactory.createClassLevel CharacterClass.Fighter 1
    { c with Name = "Fighter" }

  let createNoviceWarrior () =
    let c = TierFactory.createClassLevel CharacterClass.Warrior 1
    { c with Name = "Warrior" }

  let createNoviceRogue () =
    let c = TierFactory.createClassLevel CharacterClass.Rogue 1
    { c with Name = "Rogue" }

  let createNoviceMage () =
    let c = TierFactory.createClassLevel CharacterClass.Mage 1
    { c with Name = "Mage" }

  // =========================================================================
  // Roster Registry
  // =========================================================================

  let allArchetypes : ArchetypeInfo list = [
    { Name = "Veteran Fighter"
      Tier = Veteran
      Discipline = CombatMode.Physical
      Description = "Normalized Physical Master (sword & shield): fluidly transitions between Force cleaves, Finesse cadences, and Bastion defense."
      Factory = createVeteranFighter }

    { Name = "Veteran Mage"
      Tier = Veteran
      Discipline = CombatMode.Arcane
      Description = "Normalized Mental Master (staff): fluidly threads Resonance Spikes, Phantasmal static, and Aegis Lattices."
      Factory = createVeteranMage }

    { Name = "Master Fighter"
      Tier = Master
      Discipline = CombatMode.Physical
      Description = "Master martial combatant flowing effortlessly across all three stances."
      Factory = createMasterFighter }

    { Name = "Master Mage"
      Tier = Master
      Discipline = CombatMode.Arcane
      Description = "Master arcane practitioner threading complex forms with absolute precision."
      Factory = createMasterMage }

    // =========================================================================
    // GrandMaster Tier (Level 200, 6 Preparations, Max Stats 840+)
    // =========================================================================

    { Name = "Grandmaster Fighter"
      Tier = GrandMaster
      Discipline = CombatMode.Physical
      Description = "Grandmaster martial champion: normalized physical stats with +25% stance specialization."
      Factory = createGrandMasterFighter }

    { Name = "Grandmaster Mage"
      Tier = GrandMaster
      Discipline = CombatMode.Arcane
      Description = "Grandmaster arcane channeler: normalized mental stats with +25% complex form specialization and spell armor."
      Factory = createGrandMasterMage }

    // =========================================================================
    // Novice Archetypes (Level 1)
    // =========================================================================

    { Name = "Fighter"
      Tier = Novice
      Discipline = CombatMode.Physical
      Description = "Novice martial combatant equipped with a sword."
      Factory = createNoviceFighter }

    { Name = "Warrior"
      Tier = Novice
      Discipline = CombatMode.Physical
      Description = "Generic Power NPC: front-line brute with kinetic Force and fortitude (no preparations)."
      Factory = createNoviceWarrior }

    { Name = "Rogue"
      Tier = Novice
      Discipline = CombatMode.Physical
      Description = "Generic Finesse NPC: agile skirmisher relying on Finesse and speed (no preparations)."
      Factory = createNoviceRogue }

    { Name = "Mage"
      Tier = Novice
      Discipline = CombatMode.Arcane
      Description = "Generic Magic NPC: arcane practitioner channeling raw psychic energy (no preparations)."
      Factory = createNoviceMage }
  ]

  // Player-facing roster (ADR f1f14149): only Fighter and Mage archetypes are playable champions.
  // Generic NPC archetypes (Warrior, Rogue) remain selectable as opponents in benchmark modes.
  let playerArchetypes : ArchetypeInfo list =
    allArchetypes |> List.filter (fun a -> a.Name <> "Warrior" && a.Name <> "Rogue")

  let findByName (name: string) : ArchetypeInfo option =
    let trimmed = name.Trim().ToLowerInvariant()
    allArchetypes
    |> List.tryFind (fun a -> a.Name.ToLowerInvariant() = trimmed)
    |> Option.orElseWith (fun () ->
      allArchetypes
      |> List.tryFind (fun a -> a.Name.ToLowerInvariant().Contains(trimmed)))

  let createCustom (name: string) (hp: int) (morale: int) (armor: int) (stats: (StatId * int) seq) : Combatant =
    let id = CombatantId.New()
    let statBlock = StatBlock.Create stats
    let stat k = statBlock.Get k
    // ADR f1f14149: the class label is derived from the stat distribution, never chosen —
    // physical stats dominant means Fighter, mental stats dominant means Mage.
    let derivedClass =
      if stat StatId.Force + stat StatId.Prowess >= stat StatId.Intellect + stat StatId.Prowess then
        CharacterClass.Fighter
      else
        CharacterClass.Mage
    let baseC = Combatant.create id name hp morale statBlock
    { baseC with
        Class = derivedClass
        Progression = ProgressionProfile.create derivedClass 1
        Armor = ArmorIntegrity.Create armor }
