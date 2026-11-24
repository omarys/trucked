EXTERNAL start_combat(enemy_id)
EXTERNAL award_memory(memory_id)
EXTERNAL check_preparation(prep_name)
EXTERNAL choose_class(class_name)

VAR checkpoint = "waking"
VAR chosen_class = ""
VAR death_count = 0
VAR memories_recovered = 0
VAR vowed_to_boy = false
VAR found_raincoat = false
VAR embraced_rage = false
VAR offered_bargain = false
VAR rested_in_flood = false
VAR asked_identity = false

-> waking

=== waking ===
{
    - death_count == 0:
        The roar of screaming metal and the blinding flash of white light violently recede, leaving only the sound of cold, rhythmic rain drumming against wet shale.
        You peel your face out of the mud. Your head throbs with blinding static; your past is an empty, dark vault.
    - else:
        {The rain hits your face before your lungs remember how to draw breath. You are back in the mud, the taste of copper and wet asphalt still sharp on your tongue.|Cold shale bites your palms again. Somewhere behind your ribs, the phantom horn still rings.|Mud, shale, and the same merciless downpour. The loop drops you right where it broke you, daring you to stand again.}
        Your pulse steadies against the static. You already know what lies beside you in the muck.
}
Beside you in the muck lies a battered chest, its iron lock already broken and hanging loose. {~Rainwater pools in the dented lid.|Rust streaks the wet wood like dried blood.|The hinges groan as a gust sweeps the ridge.}
+ [Draw the tempered arming sword and reinforced shield (Physical)]
    You strap the arming sword to your side and brace the iron-rimmed shield. {~The steel is cold, honest, and balanced for a fight.|Leather tightens across your forearm with muscle memory your mind cannot name.|Iron weight settles into your stance, cutting through the static.} Kinetic momentum, agile parries, an unyielding bastion guard—the instinct of a Fighter.
    Tucked beneath the splintered lid, you unearth a bundle of sharpened caltrops wrapped in oiled cloth and cinch them to your belt.
    ~ chosen_class = "fighter"
    ~ choose_class("fighter")
    ~ memories_recovered += 1
    ~ award_memory("prepared_mind")
    -> boy_encounter
+ [Reach for the carved ash staff and woven runic robes (Mental)]
    Your fingers curl around the polished runic wood, and the heavy robes settle across your shoulders. {~Arcane static snaps along your knuckles, waking dormant focus.|The carved sigils warm against the rain, humming in time with your pulse.|A crisp current races up your spine, sharpening the dark.} Cognitive resonance, phantasmal static, disciplined wards—the instinct of a Mage.
    Tucked beneath the splintered lid, you recover a bundle of sharpened caltrops wrapped in oiled cloth and slip them into the robe's deep pocket.
    ~ chosen_class = "mage"
    ~ choose_class("mage")
    ~ memories_recovered += 1
    ~ award_memory("prepared_mind")
    -> boy_encounter

=== boy_encounter ===
{
    - death_count == 0:
        A sharp, ragged sob cuts through the downpour.
        Curled under a leaning slab of quarry slate sits a young boy, shivering violently in soaked homespun. His tear-streaked face mirrors a half-remembered visage from your forgotten past—a pang of agonizing familiarity hits your chest.
    - else:
        The same ragged sob cuts through the downpour—sharp enough to make your shoulders flinch before you even turn.
        Under the leaning slab of quarry slate, the shivering boy looks up with those same familiar, terrified eyes. The ache in your chest does not dull with repetition.
}
~ memories_recovered += 1
~ award_memory("rain_and_headlights")
"M-mister... please," the boy stammers, teeth chattering. "They took Lyra... my twin sister. The quarry deserters dragged her toward the iron pit. They said she was a tithe!"
+ ["Stay hidden here. I will bring her back."]
    ~ vowed_to_boy = true
    The boy looks up with wide, desperate eyes, knuckles white against the wet slate. "Be careful... their leader carries a hammer from the mine forge. He doesn't speak. He just crushes everything."
    -> quarry_ambush
+ ["Why were you out in this storm?"]
    ~ vowed_to_boy = false
    "We were running... following the headlights... wait, what are headlights?" The boy clutches his head in confusion, then cries out as {~a distant clang rings off the quarry walls|a dull, rhythmic boom shakes the shale|metal groans down in the pit}. "Hurry, please! I can hear the hammer strikes!"
    -> quarry_ambush

=== quarry_ambush ===
You ascend the muddy incline into the amphitheater of the abandoned iron quarry. {~For a heartbeat, the drizzle smells less like wet iron and more like hot brakes on asphalt.|A phantom flicker of white light sweeps the rim, gone before you can blink.|Static crackles faintly at the edge of your hearing, drowned by the rain.}
Steam rises from rainwater sizzling against red-hot slag. At the center of the pit, towering over a rusted iron cage, stands a hulking brute in corroded plate.
He raises an enormous quarry sledgehammer with methodical, unhurried malice.
As he turns toward you, his visor reveals no face—only an abyss of swirling shadows and the crushing weight of an oath you failed to keep.
{
    - vowed_to_boy:
        Your promise to the boy burns cold in your throat as the brute drags the hammer across the wet shale.
    - else:
        The boy's stray word—headlights—still echoes in your skull as the brute plants his boots in the mud.
}
{
    - chosen_class == "fighter":
        You raise the iron-rimmed shield and set your feet; the steel hums against the stifling guilt pressing down on the pit.
    - else:
        You plant the ash staff in the wet shale; runic static crackles off your knuckles against the stifling guilt pressing down on the pit.
}
~ start_combat("Guilt_Aspect")
-> post_combat

=== post_combat ===
The shadowy colossus fractures and shatters into jagged crystalline shards like a broken windshield.
The crushing weight on your chest momentarily lifts, leaving you gasping in the cold rain.
In the puddle at your feet, the dark water refuses to mirror the quarry—for a brief, searing second, it shows a crumpled sedan, a shattered crosswalk signal, and a girl's hand slipping from your grasp.
~ memories_recovered += 1
~ award_memory("shattered_windshield")
The cage door groans open, empty save for {~a scrap of wet ribbon caught on the latch|a faint imprint of small boots in the rust|a lingering chill that smells of rain-soaked wool}. A trail of muddy footsteps leads out of the pit and into the dark, silent treeline.
"This wasn't an accident," you whisper into the rain. "I have to find her."
+ [Follow Lyra's tracks into the dark forest]
    ~ checkpoint = "forest"
    -> forest_entry

=== forest_entry ===
{
    - death_count == 0:
        The dense canopy of the Shrouded Grove swallows the rain into an eerie, hushed stillness.
        Grey mist winds like smoke through the towering pines. Whispering voices drift from the trees: "It didn't happen. It was just a nightmare. She is waiting at home right now."
    - else:
        {The Shrouded Grove closes around you again, its mist eager to erase the impact you just felt.|Back beneath the towering pines of the Shrouded Grove. The fog curls tighter this time, whispering that the collision never happened.|Once more into the Shrouded Grove—the mist thick with the same gentle, poisonous lie.}
        "Stop fighting it," the trees murmur. "She is waiting at home right now."
}
Flickering illusions of {~a warm living room, an untouched dinner table, and an intact car|a porch light left burning, dry coats on a rack, and an uncracked phone screen|a quiet kitchen, a steaming kettle, and keys resting safely on the counter} pulse in the gloom, mocking your senses.
+ [Steel your mind against the illusions and push forward]
    ~ found_raincoat = false
    You fix your eyes on the dark trunks ahead and refuse to look at the warm shapes bleeding through the fog. {~The phantom lamplight sputters and dies behind you.|The whispers hiss like steam as you step straight through a doorway that isn't there.|A phantom turn signal clicks once in the pines, then vanishes.}
    -> denial_ambush
+ [Search the damp needle bed for tracks]
    ~ found_raincoat = true
    You kneel in the wet pine needles, ignoring the phantom warmth at your back. Beneath a gnarled root lies a discarded yellow raincoat, torn at the hem. Folded in its pocket is a sodden scrap of paper: 'Don't look at the headlights.'
    -> denial_ambush

=== denial_ambush ===
The shifting mist condenses into two ethereal figures wielding translucent, flickering daggers.
{
    - found_raincoat:
        Your fist tightens around the sodden note as the twin silhouettes circle you. "Throw that away," they hiss in unison. "Why cling to a scrap of bad dreams?"
    - else:
        "Why bleed for someone who isn't even gone?" their voices chorus from every direction. "Just close your eyes. If you don't acknowledge the truth, it never happened."
}
{
    - chosen_class == "fighter":
        You draw steel, the sharp rasp of the blade cutting cleanly across their hollow chorus.
    - else:
        Arcane static flares along your ash staff, burning jagged holes through the encroaching mirage.
}
~ start_combat("Denial_Aspect")
-> post_denial

=== post_denial ===
The phantom daggers shatter into harmless glass droplets that vanish into the wet needles.
The thick fog recedes like a torn curtain, exposing a crumpled yellow umbrella crushed beneath deep tire treads.
{
    - found_raincoat:
        The same bright yellow as the torn coat in the roots. Your throat tightens as the last illusion gives way.
}
~ memories_recovered += 1
~ award_memory("broken_umbrella")
A sudden violent tremor shakes the earth. Ahead, the pines blacken and wither into scorched stumps as the forest floor splits open into a blazing volcanic caldera.
+ [Ascend into the scorching heat of the Basalt Caldera]
    ~ checkpoint = "caldera"
    -> caldera_ascent

=== caldera_ascent ===
{
    - death_count == 0:
        Choking sulfur stings your throat. Waves of blistering heat radiate from cracked plates of black basalt.
        Beneath the roar of subterranean lava runs an unmistakable phantom sound: the furious snarl of an accelerating engine, metal violently straining against torque.
    - else:
        {Sulfur sears your lungs all over again as the Basalt Caldera welcomes you back with blistered stone.|The phantom engine revs beneath the black plates, louder this time, mocking your return to the caldera.|Heat slams into your face like a furnace door—back on the basalt rim, the roar of redlining metal shaking your teeth.}
}
A burning wave of destructive rage floods your veins: 'Whose fault was this? The driver? The storm? Why didn't anyone stop it?!' {~Embers drift upward like sparks from grinding steel.|Cracks in the stone glow the exact red of brake lights in the rain.|The air shimmers with violent, suffocating heat.}
+ [Channel the blistering fury and advance]
    ~ embraced_rage = true
    You let the heat settle behind your ribs and turn it into fuel. Your jaw locks; every step across the blistered basalt strikes sparks.
    -> anger_ambush
+ [Look for a path through the cooling slag]
    ~ embraced_rage = false
    You trace the darkened crust along the rim, hunting for calmer footing—only for the slag to split open beneath your heel in a hiss of molten orange. There is no detour. The fire demands to be faced head-on.
    -> anger_ambush

=== anger_ambush ===
A towering juggernaut forged of black volcanic basalt and incandescent magma surges out of the rift.
It swings an enormous greatsword of red-hot slag, showers of molten embers erupting with every vicious arc.
{
    - embraced_rage:
        It answers the fury in your chest with a deafening, wordless roar that shatters the basalt pillars around you.
    - else:
        It cuts off the cooling rim with a deafening, wordless roar, shattering the basalt pillars into smoking rubble.
}
{
    - chosen_class == "fighter":
        You brace your shield against the blast of cinders and step into the heat, meeting iron weight with iron weight.
    - else:
        You anchor your stance with the ash staff, weaving a cold lattice of cognitive focus against the roaring furnace.
}
~ start_combat("Anger_Aspect")
-> post_anger

=== post_anger ===
The blazing behemoth fractures down its core, collapsing into cold, brittle charcoal.
The blinding rage in your chest burns out, leaving behind a hollow, ash-gray silence.
You remember: the slammed door, the bitter screaming match in the hallway, the sharp, unforgivable words you hurled into the room just minutes before she ran outside into the storm.
~ memories_recovered += 1
~ award_memory("shouting_in_the_hallway")
A freezing gale sweeps across the rim, scouring away the soot and revealing a narrow coastal ridge suspended over black water.
+ [Follow the freezing sea wind toward the cliffs]
    ~ checkpoint = "promontory"
    -> promontory_ascent

=== promontory_ascent ===
{
    - death_count == 0:
        You step out onto the razor edge of the Shattered Promontory.
        A tempestuous black ocean roars hundreds of feet below, crashing against jagged sea needles.
    - else:
        {Salt spray lashes your face again on the razor edge of the Shattered Promontory.|Back on the storm-scoured cliff, the black ocean churning far below as if no time has passed.|The wind nearly tears you off the Shattered Promontory once more, howling with the same impossible offer.}
}
Sideways rain pelts you like buckshot. {~Lightning forks across the horizon like a cracked windshield.|The gale carries the faint, rhythmic beep of a distant monitor.|Sea spray stings cold and clinical against your cheeks.} A chilling whisper rides the wind: "Everything has a price. You can have her back. Give me your eyes. Give me your hands. Give me your remaining years."
+ ["Take whatever it costs! Just bring her back!"]
    ~ offered_bargain = true
    Your voice cracks against the gale. The storm seems to lean in, greedy and amused, as the dark air ahead congeals into a tall silhouette.
    -> bargaining_ambush
+ ["No more desperate trades. Step aside."]
    ~ offered_bargain = false
    The wind snaps violently at your cloak as though offended. Out of the driving spray, a tall silhouette glides forward over the drop.
    -> bargaining_ambush

=== bargaining_ambush ===
A tall, hooded figure cloaked in sodden oilskin hovers over the cliff's edge.
In one pale hand, it holds an ornate pair of brass scales that creak in the gale; in the other, a whistling rapier.
{
    - offered_bargain:
        The brass pans dip eagerly in the wind. "An eager bidder," the Arbiter murmurs as lightning tears the sky. "Let us weigh what little of you remains."
    - else:
        The brass pans swing empty and bitter. "Refuse the ledger, then," the Arbiter hisses as lightning tears the sky. "We will still negotiate the currency of regret."
}
{
    - chosen_class == "fighter":
        Your fingers tighten on a rain-slick hilt, steel angled into the squall.
    - else:
        Your ash staff crackles with sharp blue static against the salt-drenched wind.
}
~ start_combat("Bargaining_Aspect")
-> post_bargaining

=== post_bargaining ===
The brass scales snap cleanly at the fulcrum and plummet into the boiling abyss.
The cold, devastating truth settles into your bones: Death does not barter.
The memory surges forth with excruciating clarity: the sterile hum of the intensive care unit, the rhythmic beep of heart monitors, and your whispered, desperate bargains made to the cold ceiling in the dark.
~ memories_recovered += 1
~ award_memory("hospital_monitors")
Across the bay, the black tide retreats with a hollow groan, exposing the submerged avenues of a flooded metropolis.
+ [Descend into the sunken city]
    ~ checkpoint = "sunken"
    -> sunken_descent

=== sunken_descent ===
{
    - death_count == 0:
        You wade knee-deep into stagnant floodwaters winding through crumbling concrete and dead stone facades.
        A terrifying, absolute silence blankets the ruins. Your limbs feel heavy as lead, each step through the murky water requiring monumental effort.
    - else:
        {Cold, stagnant water closes around your knees again inside the Sunken Metropolis.|Back in the drowned avenues, the silence heavier than before, pulling at your heels like wet clay.|The flooded ruins hold you still once more—no wind, no echo, only the dead weight of the dark water.}
}
Apathy presses down like the ocean itself. {~Submerged streetlights flicker weakly beneath the black surface, then go dark.|Your reflection ripples beside floating leaves, pale and exhausted.|Even the rain has given up here, reduced to a dull, colorless mist.} What was the point of surviving? Why fight? Nothing can change what happened.
+ [Force your leaden legs through the dead water]
    ~ rested_in_flood = false
    You drive one boot forward, then the next, carving slow wakes through the black mirror. Every muscle protests, but you refuse to stop moving.
    -> depression_ambush
+ [Rest against a damp stone column]
    ~ rested_in_flood = true
    You let your shoulder lean into the weeping concrete for one breath. The water laps higher, cold and numbing, threatening to claim your knees. Through sheer stubborn momentum, you shove yourself upright and wade on.
    -> depression_ambush

=== depression_ambush ===
Rising slowly from the black water is a hunched, weeping colossus draped in soaked burial shrouds, dragging an immense iron anchor.
It speaks no words and makes no sound. Its hollow, sorrowful eyes fix upon you, casting an aura of crushing, inescapable fatigue over your spirit.
{
    - rested_in_flood:
        The chill from the stone column still clings to your spine as the anchor grinds across the drowned pavement.
    - else:
        The black water surges backward around its knees, threatening to undo every hard-won step you forced yourself to take.
}
{
    - chosen_class == "fighter":
        Your arming sword feels like a slab of drowned iron, yet you lift the point anyway.
    - else:
        Your ash staff drags like waterlogged timber, yet a pale spark answers your grip anyway.
}
~ start_combat("Depression_Aspect")
-> post_depression

=== post_depression ===
The sodden burial shroud drifts away on the receding tide; the heavy anchor dissolves into fine silt.
The leaden weight that paralyzed your lungs finally loosens its grip.
You remember: returning home alone to an empty house. Walking past her unopened bedroom door. The deafening silence of a life unlived—and finally, allowing yourself to weep.
~ memories_recovered += 1
~ award_memory("empty_bedroom")
For the first time since you awoke, the storm clouds part. Warm, golden sunlight spills across a radiant hillside carpeted in white blossoms.
+ [Ascend into the sunlit meadow]
    ~ checkpoint = "meadow"
    -> meadow_arrival

=== meadow_arrival ===
{
    - death_count == 0:
        The rain has ceased entirely. The sky above is a vast, calm canopy of sapphire blue.
        You walk into an endless meadow of white lilies swaying gently in the summer breeze.
    - else:
        {The golden warmth of the meadow steadies your breathing after the violent snap of the loop.|Back among the swaying white lilies—even the phantom horn fades against this quiet summer breeze.|Sunlight warms your shoulders once more. The meadow does not judge how many times it took to stand here.}
}
Standing in the center of the flowers is a serene figure dressed in unblemished white linen, leaning upon a smooth birch staff. {~Pollen drifts lazily through the warm air.|Far away, the faintest trace of laughter rides the wind.|White petals brush your boots with quiet, weightless grace.}
As you approach, the guardian turns. It is your own face, unmarred by sorrow, standing poised and peaceful.
+ ["Who are you?"]
    ~ asked_identity = true
    "I am the part of you that is ready to let her go," the reflection answers with a gentle smile.
    -> acceptance_ambush
+ ["Is Lyra here?"]
    ~ asked_identity = false
    "Lyra has been at peace all along," the reflection answers softly. "She has only been waiting for you to stop punishing yourself."
    -> acceptance_ambush

=== acceptance_ambush ===
The reflection offers a respectful nod, raising the birch walking staff.
{
    - asked_identity:
        "One final encounter. Meet yourself without hatred—not to conquer in anger, but to find peace."
    - else:
        "One final encounter. Honor her by living—not to conquer in anger, but to find peace."
}
~ start_combat("Acceptance_Aspect")
-> post_acceptance

=== post_acceptance ===
{
    - chosen_class == "fighter":
        You lower your arming sword into the white blossoms, the iron warm in the sun.
    - else:
        You lower your carved ash staff into the white blossoms, its runic static fading into quiet warmth.
}
The reflection steps forward, clasping your shoulder with warm, reassuring familiarity.
The final memory opens like morning light: Lyra's bright, unclouded smile on a sunny afternoon, her laughter ringing clear, telling you that everything will be alright.
~ memories_recovered += 1
~ award_memory("twin_smile")
-> intersection_transition

=== intersection_transition ===
The radiant meadow of white lilies begins to shimmer and dissolve like mist.
The sweet fragrance of blossoms fades into the sharp smell of gasoline, exhaust fumes, and rain falling on warm asphalt.
The gentle breeze turns into the rhythmic thrum and low idle of dozens of automobile engines.
You blink against the sudden harshness. You are standing in the center of a pedestrian crosswalk under a drizzling evening sky.
To your left and to your right, endless lines of gridlocked cars sit bumper-to-bumper with headlights blazing, forming an impenetrable wall of metal and halogen. {~Raindrops bead and flare across dozens of windshields.|Wiper blades sweep in slow, mechanical unison on either flank.|Red tail lights bleed into the wet pavement behind the glare.}
There is nowhere to turn; neither curb offers an escape.
{
    - death_count == 0:
        You stand whole in the rain, carrying every recovered memory without flinching from the glare.
    - else:
        Every phantom impact that dragged you back through the loop falls silent behind you; the crosswalk no longer owns you.
}
Directly ahead across the white crosswalk lines, rising from the pavement where the collision once occurred, stands an immense, ancient monolith of dark stone piercing the clouds: **The Tower**.
Its monumental bronze doors stand slightly ajar, warm torchlight spilling out across the wet asphalt.
+ [Cross the street and push through the bronze doors of The Tower]
    -> enter_tower

=== enter_tower ===
You take a deep, calm breath and step across the final painted line of the crosswalk.
{
    - chosen_class == "fighter":
        You press your gauntleted palms against the colossal bronze gates. They swing inward with a deep, echoing chime.
    - else:
        You press your runic palms against the colossal bronze gates. They swing inward with a deep, echoing chime.
}
Behind you, the street, the headlights, and the cars vanish into the mist.
Ahead lie endless vaulted stone halls, uncharted chambers, and the boundless ascent of The Tower.
-> END

=== game_over_respawn ===
~ death_count += 1
{
    - death_count == 1:
        The fatal blow lands with the deafening force of an oncoming truck.
        Vision whites out. A phantom horn wails across the void.
        The screech of tires echoes in your skull, tearing through time and space...
    - death_count == 2:
        Steel and bone give way beneath the deafening force of an oncoming truck—again.
        Halogen light floods your sight. A phantom horn wails across the void, closer and harsher than before.
        Wet rubber screams against asphalt as the trauma loop snaps you backward...
    - else:
        The dark collapses with the familiar, deafening force of an oncoming truck.
        Blinding white fills the world while a phantom horn wails across the void.
        {~Shattered glass hangs suspended in the rain for a fraction of a second before the loop drags you back...|A distant radio bursts with white static as tires shriek and time folds inward...|Your breath tears loose in the glare of high beams, hurling you back to the edge of the memory...}
}
{
    - checkpoint == "forest":
        -> forest_entry
    - checkpoint == "caldera":
        -> caldera_ascent
    - checkpoint == "promontory":
        -> promontory_ascent
    - checkpoint == "sunken":
        -> sunken_descent
    - checkpoint == "meadow":
        -> meadow_arrival
    - else:
        -> waking
}
