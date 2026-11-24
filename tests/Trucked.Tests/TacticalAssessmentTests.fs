namespace Trucked.Tests

open System
open Xunit
open Fornach.Domain

module TacticalAssessmentTests =

  [<Fact>]
  let ``Prologue Fighter reads Stone Gargoyle primary stat with guarded posture at level 1`` () =
    let player = StoryBosses.createProloguePlayer "fighter"
    let gargoyle = Bestiary.stoneGargoyle.ToCombatant (CombatantId.New())

    let assess = TacticalAssessment.assess player gargoyle

    // Stone Gargoyle stats: Reflex 240, Fortitude 220, Finesse 230, Poise 200, Force 180, Poise 150
    Assert.Equal(1, player.Level)
    Assert.Equal(StatId.Reflex, assess.PrimaryStat)
    Assert.Equal(240, assess.PrimaryStatValue)
    Assert.Equal("Agility Defense", assess.PrimaryStatRole)
    // Since the Prowess/Poise merge the read is a mental contest: Intellect 22 + Intuition 34/4 (8) = 30
    // against the target's Resolve 160 -> Obscured.
    Assert.Equal(TacticalInsightLevel.Obscured, assess.InsightLevel)
    Assert.Equal(22 + 34 / 4, assess.EffectiveAcumen)
    Assert.Equal(160, assess.TargetComposure)
    Assert.True(assess.Ratio < 0.40)
    Assert.Equal((StatId.Fortitude, 220), assess.WeakestDefense)
    Assert.Equal("GUARDED POSTURE", assess.Headline)
    Assert.True(assess.StrategicAdvice.Contains("build Momentum"))

  [<Fact>]
  let ``Prologue Mage scrutiny reads Stone Gargoyle as discerning at level 1`` () =
    // The Mage kit: staff raises Prowess 45 -> 65 and robes raise Intuition 45 -> 60
    let player = StoryBosses.createProloguePlayer "mage"
    let gargoyle = Bestiary.stoneGargoyle.ToCombatant (CombatantId.New())

    let assess = TacticalAssessment.assess player gargoyle

    // The staff sharpens Intellect (47 power-tilted -> 73) and robes lift Intuition (43 agility-tilted -> 57);
    // 73 + 57/4 (14) = 87 vs Resolve 160 (Ratio ~0.54)
    Assert.Equal(TacticalInsightLevel.Discerning, assess.InsightLevel)
    Assert.Equal("DISCERNING READ", assess.Headline)
    Assert.Equal(73 + 57 / 4, assess.EffectiveAcumen)
    Assert.True(assess.VulnerabilitySummary.IsSome)
    Assert.Equal(StatId.Reflex, assess.PrimaryStat)
    Assert.Equal(240, assess.PrimaryStatValue)

  [<Fact>]
  let ``Prologue classes discern Slag Hound primary stat as Force and weakest defense as Reflex`` () =
    let player = StoryBosses.createProloguePlayer "fighter"
    let hound = Bestiary.slagHound.ToCombatant (CombatantId.New())

    let assess = TacticalAssessment.assess player hound

    // Slag Hound stats: Force 105, Fortitude 95, Finesse 80, Reflex 75 (Poise 0 for non-humanoids)
    Assert.Equal(StatId.Force, assess.PrimaryStat)
    Assert.Equal(105, assess.PrimaryStatValue)
    Assert.Equal((StatId.Reflex, 75), assess.WeakestDefense)
    // Fighter Prowess 22 + Intuition 34/4 (8) = 30 vs Poise 50 -> Discerning (0.60)
    Assert.Equal(TacticalInsightLevel.Discerning, assess.InsightLevel)
    Assert.Equal("DISCERNING READ", assess.Headline)
    Assert.True(assess.StrategicAdvice.Contains("immense kinetic striking power"))

  [<Fact>]
  let ``Aspect of Guilt primary stat is Force and defensive flaw is Reflex`` () =
    let player = StoryBosses.createProloguePlayer "fighter"
    let guilt = StoryBosses.createGuiltAspectRaw ()

    let assess = TacticalAssessment.assess player guilt

    // Aspect of Guilt: Force 210, Fortitude 195, Poise 185, Reflex 95, Poise 150
    Assert.Equal(StatId.Force, assess.PrimaryStat)
    Assert.Equal(210, assess.PrimaryStatValue)
    Assert.Equal((StatId.Reflex, 95), assess.WeakestDefense)
    // A level-1 Fighter cannot read a Guilt Aspect's exact form yet.
    Assert.Equal(TacticalInsightLevel.Obscured, assess.InsightLevel)
    Assert.True(assess.StrategicAdvice.Contains("build Momentum"))

  [<Fact>]
  let ``Momentum stacks sharpen tactical insight from Obscured to Discerning`` () =
    let player = StoryBosses.createProloguePlayer "fighter" // Intellect 22 + Intuition 34/4 (8) = 30
    let gargoyle = Bestiary.stoneGargoyle.ToCombatant (CombatantId.New())

    let initialAssess = TacticalAssessment.assess player gargoyle
    Assert.Equal(TacticalInsightLevel.Obscured, initialAssess.InsightLevel)

    // Three Momentum stacks are the real mechanic, not a stat edit: +15 scrutiny each, so
    // 30 + 45 = 75 against Resolve 160 -> Discerning.
    let studiedAssess = TacticalAssessment.assess { player with Momentum = 3 } gargoyle

    Assert.Equal(TacticalInsightLevel.Discerning, studiedAssess.InsightLevel)
    Assert.Equal(75, studiedAssess.EffectiveAcumen)
    Assert.Equal("DISCERNING READ", studiedAssess.Headline)

  [<Fact>]
  let ``a target with no guard does not divide by zero`` () =
    let player = StoryBosses.createProloguePlayer "fighter"

    // Poise and Resolve are the merged guard pair; a floor of 1 keeps the ratio finite however bare the
    // target's sheet is, so the insight ladder can never be computed from a division by zero.
    let bare = Combatant.create (CombatantId.New()) "Bare" 100 100 (StatBlock.Create [])

    // StatBlock floors a missing stat at 10, and the guard clamps the same way: never zero.
    Assert.Equal(10, TacticalAssessment.calculateEffectiveMasking bare)
    let assess = TacticalAssessment.assess player bare
    Assert.True(System.Double.IsFinite assess.Ratio)
    Assert.Equal(TacticalInsightLevel.Penetrating, assess.InsightLevel)

  [<Fact>]
  let ``High composure gap yields Obscured posture with advice to build Momentum`` () =
    let noviceWarrior = TierFactory.createClassLevel CharacterClass.Warrior 1
    let overseer = Bestiary.quarryOverseer.ToCombatant (CombatantId.New()) // Poise 390

    let assess = TacticalAssessment.assess noviceWarrior overseer
    Assert.Equal(TacticalInsightLevel.Obscured, assess.InsightLevel)
    Assert.Equal("GUARDED POSTURE", assess.Headline)
    Assert.True(assess.StrategicAdvice.Contains("build Momentum"))

  [<Fact>]
  let ``Mental plane combatant correctly evaluates mental attributes as primary`` () =
    let player = StoryBosses.createProloguePlayer "fighter"
    let mentalBoss = StoryBosses.createAcceptanceAspect ()

    let assess = TacticalAssessment.assess player mentalBoss
    // Mental boss Plane = Mental; stats: Intellect, Resolve, Acuity, Intuition, Prowess, Poise
    let desc = Attributes.descriptorOf assess.PrimaryStat
    Assert.Equal(Plane.Mental, desc.Plane)

  [<Fact>]
  let ``Tactical assessment threat warning surfaces correctly across insight levels`` () =
    let observer = StoryBosses.createProloguePlayer "fighter" // Base Prowess 22 + Intuition 34/4 (8) = 30
    let hound = Bestiary.slagHound.ToCombatant (CombatantId.New()) // Force 105, Poise 50

    // 1. Obscured insight (high Poise target) -> ThreatWarning is None
    let overseer = Bestiary.quarryOverseer.ToCombatant (CombatantId.New()) // Poise 390
    let obscuredAssess = TacticalAssessment.assess observer overseer
    Assert.Equal(TacticalInsightLevel.Obscured, obscuredAssess.InsightLevel)
    Assert.True(obscuredAssess.ThreatWarning.IsNone, "Obscured insight should not reveal apex threat details")

    // 2. Discerning insight (ratio 30/50 = 0.60 -> Discerning) -> ThreatWarning has approximation
    let discerningAssess = TacticalAssessment.assess observer hound
    Assert.Equal(TacticalInsightLevel.Discerning, discerningAssess.InsightLevel)
    Assert.True(discerningAssess.ThreatWarning.IsSome, "Discerning insight should generate threat warning")
    let warnDiscerning = discerningAssess.ThreatWarning.Value
    Assert.True(warnDiscerning.StartsWith("⚠ THREAT DETECTED"), sprintf "Expected threat detected prefix, got: %s" warnDiscerning)
    Assert.True(warnDiscerning.Contains("Force"), "Should detect Force as the threat stat")

    // 3. Keen insight (elevated scrutiny to reach ratio in [0.75, 1.15))
    // The read is Intellect-based since the merge; the hound's Resolve is 60, so scrutiny ~55 gets Keen.
    let keenObserver = { observer with Stats = observer.Stats.ApplyModifiers [ StatId.Intellect, 25 ] } // 30 + 25 = 55 / 60 = 0.92 -> Keen
    let keenAssess = TacticalAssessment.assess keenObserver hound
    Assert.Equal(TacticalInsightLevel.Keen, keenAssess.InsightLevel)
    Assert.True(keenAssess.ThreatWarning.IsSome)
    let warnKeen = keenAssess.ThreatWarning.Value
    Assert.True(warnKeen.StartsWith("⚠ APEX THREAT:"), sprintf "Expected apex threat prefix, got: %s" warnKeen)
    Assert.True(warnKeen.Contains("Colossal Force (105)"), "Should specify exact apex Force value")

    // 4. Penetrating insight (ratio >= 1.15). Poise and Resolve are the merged guard pair now, so the
    // Anger Aspect no longer has a zeroed mental guard to fall through: the observer is sharpened to
    // reach the tier instead, which is what a Penetrating read costs.
    let anger = StoryBosses.createAngerAspect ()
    let observer = { observer with Stats = observer.Stats.ApplyModifiers [ StatId.Intellect, 400 ] }
    let penAssess = TacticalAssessment.assess observer anger
    Assert.Equal(TacticalInsightLevel.Penetrating, penAssess.InsightLevel)
    Assert.True(penAssess.ThreatWarning.IsSome)
    let warnPen = penAssess.ThreatWarning.Value
    Assert.True(warnPen.StartsWith("⚠ APEX THREAT (CRITICAL):"), sprintf "Expected critical apex threat prefix, got: %s" warnPen)
