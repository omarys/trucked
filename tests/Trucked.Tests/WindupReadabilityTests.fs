namespace Trucked.Tests

open Xunit
open Fornach.Domain
open Fornach.Engine
open Trucked.Cli

/// Shield Bash used to appear whenever the *true* intent was a heavy wind-up, even when the scrutiny
/// ladder had masked that wind-up as "Obscured". The menu handed back exactly what the ladder had just
/// taken away, so a turn could be spent bashing a wind-up the player was never shown.
module WindupReadabilityTests =

  let private heavyWindup = ActionIntent.StandardAttack(AttackClassification.ForceStrike true)
  let private plainStrike = ActionIntent.StandardAttack(AttackClassification.ForceStrike false)

  [<Fact>]
  let ``a heavy wind-up is readable when the champion outclasses the foe`` () =
    let player = TierFactory.createClassLevel CharacterClass.Warrior 50
    let fodder = TierFactory.createClassLevel CharacterClass.Warrior 1
    Assert.True(Program.isWindupReadable player fodder (Some heavyWindup))

  [<Fact>]
  let ``the same wind-up is masked when the foe outclasses the champion`` () =
    let novice = TierFactory.createClassLevel CharacterClass.Warrior 1
    let terror = TierFactory.createClassLevel CharacterClass.Warrior 50
    Assert.False(Program.isWindupReadable novice terror (Some heavyWindup))

  [<Fact>]
  let ``a plain strike is never an interrupt, however well it is read`` () =
    let player = TierFactory.createClassLevel CharacterClass.Warrior 50
    let fodder = TierFactory.createClassLevel CharacterClass.Warrior 1
    Assert.False(Program.isWindupReadable player fodder (Some plainStrike))
    Assert.False(Program.isWindupReadable player fodder None)

  [<Fact>]
  let ``a beast wind-up opens the interrupt once the champion can read finesse`` () =
    let player = TierFactory.createClassLevel CharacterClass.Warrior 50
    let boar = TierFactory.createClassLevel CharacterClass.Warrior 1 |> fun c -> { c with MonsterFamily = Some MonsterFamily.Beast }
    Assert.True(Program.isWindupReadable player boar (Some heavyWindup))
