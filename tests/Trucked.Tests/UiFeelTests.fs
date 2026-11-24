namespace Trucked.Tests

open System
open System.IO
open Xunit
open Spectre.Console
open Fornach.Domain
open Fornach.Engine
open Trucked.Cli

module UiFeelTests =

  let private renderToString (width: int) (renderable: Rendering.IRenderable) =
    use writer = new StringWriter()
    let settings = AnsiConsoleSettings(Out = AnsiConsoleOutput(writer))
    let console = AnsiConsole.Create(settings)
    console.Profile.Width <- width
    console.Write(renderable)
    writer.ToString()

  [<Fact>]
  let ``stanceDisplayName formats all stances with human-readable spacing`` () =
    Assert.Equal("Power Stance", Display.stanceDisplayName CombatStance.PowerStance)
    Assert.Equal("Agility Stance", Display.stanceDisplayName CombatStance.AgilityStance)
    Assert.Equal("Discipline Stance", Display.stanceDisplayName CombatStance.DisciplineStance)

  [<Fact>]
  let ``renderArmorRow parses cleanly and aligns with renderBar`` () =
    let visible (s: string) =
      Text.RegularExpressions.Regex.Replace(s, @"\[[^\[\]]*\]", "")

    let hpRow = Display.renderBar "󰋑 Health" 500 500 Theme.Red
    let armorRow = Display.renderArmorRow " Armor" (ArmorIntegrity.Create 60)
    Assert.NotNull(Markup(armorRow))

    let hpVisible = visible hpRow
    let armorVisible = visible armorRow
    let hpCol = hpVisible.IndexOf('█', StringComparison.Ordinal)
    let armorValStart = armorVisible.IndexOf("60", StringComparison.Ordinal)
    Assert.True(hpCol > 0)
    // Armor value field starts in the same column band as the bar cell (4-char right-aligned number)
    Assert.Equal(hpCol + 2, armorValStart)

  [<Fact>]
  let ``formatArmorReadout distinguishes shredded, shield-plated, and intact armor`` () =
    let intact = ArmorIntegrity.Create 80
    let shredded = { intact with Current = 0 }
    let plated = { shredded with Boost = 30 }

    let intactMarkup = Display.formatArmorReadout intact
    let shreddedMarkup = Display.formatArmorReadout shredded
    let platedMarkup = Display.formatArmorReadout plated

    Assert.NotNull(Markup(intactMarkup))
    Assert.NotNull(Markup(shreddedMarkup))
    Assert.NotNull(Markup(platedMarkup))
    Assert.Contains("SHREDDED", shreddedMarkup)
    Assert.Contains("shield soak", platedMarkup)

  [<Fact>]
  let ``renderMiniBar handles zero, partial, full, and zero-max cleanly`` () =
    for (curr, maxVal, width) in [ 0, 100, 5; 50, 100, 5; 100, 100, 5; 10, 0, 5 ] do
      let bar = Display.renderMiniBar curr maxVal width Theme.Green
      Assert.NotNull(Markup(bar))
      let stripped = Text.RegularExpressions.Regex.Replace(bar, @"\[[^\[\]]*\]", "")
      Assert.Equal(width, stripped.Length)

  [<Fact>]
  let ``telegraphVisuals escalates border color and style by threat severity`` () =
    let obscuredColor, obscuredBorder, _ =
      Display.telegraphVisuals "Obscured" (ActionIntent.StandardAttack (ForceStrike true))
    Assert.Equal(Theme.ColorComment, obscuredColor)
    Assert.Equal(BoxBorder.Rounded, obscuredBorder)

    let heavyColor, heavyBorder, _ =
      Display.telegraphVisuals "Power" (ActionIntent.StandardAttack (ForceStrike true))
    Assert.Equal(Theme.ColorRed, heavyColor)
    Assert.Equal(BoxBorder.Heavy, heavyBorder)

    let finisherColor, finisherBorder, _ =
      Display.telegraphVisuals "Finisher" (ActionIntent.ExecuteStrike Physical)
    Assert.Equal(Theme.ColorRed, finisherColor)
    Assert.Equal(BoxBorder.Heavy, finisherBorder)

    let normalColor, normalBorder, _ =
      Display.telegraphVisuals "Power" (ActionIntent.StandardAttack (ForceStrike false))
    Assert.Equal(Theme.ColorYellow, normalColor)
    Assert.Equal(BoxBorder.Double, normalBorder)

    let defColor, defBorder, _ =
      Display.telegraphVisuals "Defensive" (ActionIntent.RecoveryAction SteadyForm)
    Assert.Equal(Theme.ColorCyan, defColor)
    Assert.Equal(BoxBorder.Rounded, defBorder)

  [<Fact>]
  let ``formatCombatLogEvent escapes brackets and parses cleanly across event types`` () =
    let playerId = CombatantId.New()
    let enemyId = CombatantId.New()
    let nameOf (id: CombatantId) = if id = playerId then "Hero [I]" else "Aspect [Guilt]"

    let events =
      [ CombatEvent.DamageApplied
          { TargetId = enemyId
            Plane = Physical
            Amount = 88
            IsCritical = true
            IsArmorCompromised = true }
        CombatEvent.ActionInterrupted(playerId, enemyId, "Shield Bash [Heavy]", "Broke [Wild Blow]!")
        CombatEvent.CollapseTriggered(enemyId, CollapseReason.RecklessExposure)
        CombatEvent.TempoShifted(playerId, enemyId, "Seized [Vor] on counter")
        CombatEvent.GambitDeclared(enemyId, "Trauma Gambit: Force", 30)
        CombatEvent.CombatantStaggered(enemyId, "Staggered [1 turn]")
        CombatEvent.Executed(playerId, enemyId, Physical) ]

    for evt in events do
      let line = Display.formatCombatLogEvent nameOf playerId evt
      Assert.NotNull(Markup(line))

  [<Fact>]
  let ``formatGroupTargetChoice parses cleanly and surfaces HP, tempo, and status`` () =
    let foe =
      { Bestiary.slagHound.ToCombatant (CombatantId.New()) with
          Tempo = TacticalTiming.Vor
          IsStaggered = true
          BleedStacks = 3 }
    let enemy : TowerEnemy =
      { Id = "e1"
        Name = "Slag Hound [Alpha]"
        Combatant = { foe with Health = { foe.Health with Current = foe.Health.Maximum / 2 } }
        DropsKeyId = None
        IsDefeated = false
        ChaseCharge = 0.0 }

    let label = Program.formatGroupTargetChoice 1 enemy
    Assert.NotNull(Markup(label))
    Assert.Contains("Slag Hound [[Alpha]]", label)
    Assert.Contains("50%", label)
    Assert.Contains("VOR", label)
    Assert.Contains("STAGGERED", label)
    Assert.Contains("BLEED 3", label)

  [<Fact>]
  let ``TowerDisplay.renderHud shows Mana Integrity for Mental-plane players`` () =
    let mage = StoryBosses.createProloguePlayer "mental"
    let state = TowerSession.initSessionWithMode mage 12345 1 false
    let rendered = renderToString 80 (TowerDisplay.renderHud state)
    Assert.Contains("Mana Integrity", rendered)

  [<Fact>]
  let ``TowerDisplay.renderCharacterSheet displays friendly stance name and XP progress`` () =
    let fighter = StoryBosses.createProloguePlayer "fighter"
    let state = TowerSession.initSessionWithMode fighter 12345 1 false
    let rendered = renderToString 100 (TowerDisplay.renderCharacterSheet state)
    Assert.Contains("Power Stance", rendered)
    Assert.DoesNotContain("PowerStance", rendered)
    Assert.Contains("Experience:", rendered)

  [<Fact>]
  let ``Symbol legend includes boss glyph and Inventory / Chronicle hotkeys`` () =
    let rendered = renderToString 120 (Display.createSymbolAndGlyphLegendPanel ())
    Assert.Contains("Floor Boss / Trauma Aspect", rendered)
    Assert.Contains("Inventory", rendered)
    Assert.Contains("Chronicle Log", rendered)

  [<Fact>]
  let ``Action menu highlights currently held stance vs post-swing transition`` () =
    let fighter =
      { StoryBosses.createProloguePlayer "fighter" with
          Tempo = TacticalTiming.Vor
          Stance = CombatStance.PowerStance }
    let enemy = Bestiary.slagHound.ToCombatant (CombatantId.New())
    let choices = Program.buildActionChoices fighter enemy

    let forceStrike = choices |> List.find (fun c -> c.Contains("Force Strike: Standard Cleave"))
    let finesseStrike = choices |> List.find (fun c -> c.Contains("Finesse Cadence: Rapid Probing"))
    Assert.Contains("In Stance", forceStrike)
    Assert.Contains("shifts after swing", finesseStrike)
    Assert.NotNull(Markup(forceStrike))
    Assert.NotNull(Markup(finesseStrike))
