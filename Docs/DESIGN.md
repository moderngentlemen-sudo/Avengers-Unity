# AVENGERS: ASSEMBLE RE:FORGED — Game Design Document

*A 2.5D, 1–4 player co-op beat-'em-up for modern hardware. Unity 6 (URP), characters built in Blender.*

> **Licensing:** The Avengers are Marvel/Disney IP. A commercial release requires a Marvel Games license.
> The systems are character-agnostic, so the roster could be swapped for original heroes.

**Revision 3 changes:**
- Ant-Man replaces Scarlet Witch.
- Vision replaces Spider-Man, and Spider-Man becomes an unlockable.
- Every hero has **two Ultimates**.
- New **Assist Callout** system.
- Destructible environments, interactive debris, and multi-level stages.

---

## 1. Vision

The 90s arcade cabinet you remember, rebuilt the way you *thought* it looked: comic-book visuals, fighting-game combo depth, and four friends on one screen setting off huge effects at the same time.

**Inspirations:**
- *Captain America and the Avengers* (Data East, 1991)
- Konami's *X-Men* and *TMNT*
- Capcom's *The Punisher* and *Alien vs. Predator*
- Modern reference points: *Shredder's Revenge* and *Streets of Rage 4* (modern beat-'em-ups), *Marvel vs. Capcom* (combo grammar and assists), *Devil May Cry* (style ranking), *Spider-Verse* (art direction)

**Pillars:**
1. **Every button matters.** Every input on the controller does something in combat, and modifiers add more moves on top.
2. **Combos are the language.** Cancels, juggles, bounces, assists, relays, and the environment itself.
3. **A comic book in motion.** 3D characters animated with 2D-style timing, ink impact frames, and onomatopoeia rendered as physical effects.
4. **The world breaks.** Floors, walls, and props give way and become weapons.

---

## 2. Roster

| # | Hero | Archetype | Unique Mechanic |
|---|---|---|---|
| 1 | **Captain America** | All-rounder / setups | **Shield Ricochet**: the shield bounces between enemies, and catching it on the return refunds gauge and cancels recovery. D-pad sets the throw angle and charges extra ricochets |
| 2 | **Iron Man** | Zoner / air | **Heat Gauge**: repulsors and beams build heat. Hold Jump in the air to hover (builds heat). D-pad ↓ vents 300+ heat as an area blast. Overheating locks out heat moves, hover and air jumps for 2s. D-pad ←/→ aims repulsors |
| 3 | **Thor** | Heavy / AoE | **Mjölnir Recall**: throw, then recall through enemies. Storm Charge powers up the next special |
| 4 | **Hulk** | Grappler / armor | **Rage tiers** (Hulk → Savage → World Breaker): more armor and new throws |
| 5 | **Black Widow** | Rushdown / technical | **Widow's Bite Marks** for electric bonus damage. Grapple-line swings between lanes and levels |
| 6 | **Hawkeye** | Ranged / tactical | **Trick Quiver** cycled with the D-pad: explosive, EMP, net, and sonic arrows. Arrows can pin debris as platforms |
| 7 | **Black Panther** | Speed / counter | **Kinetic Absorption**: blocked or parried damage is stored and discharged into the next move |
| 8 | **Captain Marvel** | Power / mobility | **Binary Build**: absorbing energy fills Binary Mode, which grants flight, beams, and no chip damage |
| 9 | **Ant-Man** | Shape-shifter / trickster | **Size Shift** between Small, Normal, and Giant at any time, even mid-combo. Pym discs shrink or enlarge enemies *and debris* |
| 10 | **Vision** | Phase-shifter / controller | **Density Control**: Phase to pass through attacks, enemies, walls, and *floors*. Diamond for armor, heavier hits, and floor breaking. Mind Stone beams |

**Unlockables:** Spider-Man (web tethers, wall-crawl, spider-sense auto-dodge), Scarlet Witch, Wasp, and Falcon.

**Assist-only cameos:** Nick Fury, War Machine, Shuri, and Okoye.

---

## 3. Controls: Every Button Has a Job

Shown with Xbox labels. PlayStation, Switch, and keyboard layouts are mapped 1:1, and every input can be rebound.

| Input | Function |
|---|---|
| Left Stick | Move, including belt-scroll depth. ↑/↓ while attacking selects directional moves |
| Right Stick | **Directional Evade**: flick into or out of a lane, or roll. Hold to aim. Flicking on hit is a **Lane Cancel** |
| A | Jump. Double jump or flight. ↓+A drops through one-way floors |
| X | Light strings (vary by direction) |
| Y | Heavy. ↑+Y launcher, →+Y wall-splat, ↓+Y sweep/OTG. Air ↓+Y is a **Ground Pound / Floor Break** |
| B | Specials (neutral, ↑, ↓, → variants) |
| RB | Grab/Throw (direction aims the throw). **Picks up debris**, and when holding debris, X/Y/RB throws it |
| LB | Block. Pressing it within 6 frames of a hit is a **Parry** |
| RT (hold) | Hero modifier: RT + face button gives four **Hero Specials** (1 bar each) |
| LT | **Assist Callout** (LT, LT+→, LT+↓ call three different assist moves). Contextual **Combo Relay** |
| RT + LT | **Super** (2 bars) |
| L3 + R3 | **Ultimate I** (3 bars). With the Assemble meter full, this is your **Team Ultimate vote** |
| RT + L3 + R3 | **Ultimate II** (3 bars) |
| D-pad ↑ | Taunt: refills style and a little gauge, but leaves you vulnerable |
| D-pad ← / → / ↓ | Hero tools (Cap: shield angle and ricochet charge; Hawkeye: quiver; Ant-Man: size; Vision: density; Thor: storm charge) |
| View | **Callout ping** (stick direction picks the callout) |
| Menu | Pause, move list, drop-in/drop-out |

There is also an **Assist control scheme** for accessibility: mashing auto-combos, specials on single buttons, and the scheme is labeled in co-op.

---

## 4. Combat System

- **Cancel hierarchy:** Light → Heavy → Special → Hero Special → Super → Ultimate. A move that has connected can cancel into any higher tier.
- **Light strings** chain even on whiff.
- **Launchers** can be jump-cancelled for free into a super jump. Other jump cancels and **lane cancels** cost 10 gauge.
- **Bounces:** wall, ground, and **ceiling** bounces, plus one OTG hit, each usable once per combo.
  - A wall bounce into a **breakable** wall smashes the victim through it, and the combo continues in the next room.
  - A ceiling bounce sends the victim **up through the floor** to the level above.
- **Juggle decay:**
  - Each hit adds juggle points.
  - Gravity on the victim rises 4% per point.
  - At 40 points the victim drops out and can't be hit until they land.
  - Damage scaling falls to a minimum of 30%.
- **Hitstop** varies per move. Ultimates freeze all enemies during their startup.
- **Parry** (6 frames): staggers the attacker for 30 frames, strips armor, and gives +30 gauge plus brief invulnerability.
- **Armor:** brutes absorb N hits. Only Heavy-tier and higher hits wear armor down, while throws and parries strip it all at once. Armor regenerates when the enemy hasn't been hit for 3 seconds.
- **Meters:**
  - **Hero Gauge:** 3 bars.
  - **Style Rank:** D → C → B → A → S → SS → **ASSEMBLE!** Repeating a move from your last four earns a third of the style. Environmental hits add bonus style.
  - **Assemble Meter:** shared by the whole team.

---

## 5. Two Ultimates per Hero

**Design rule:**
- **Ultimate I** is a cinematic finisher built for damage.
- **Ultimate II** changes the battlefield: a team buff, a field effect, or a transformation.

Both cost 3 bars, so choosing between them is a real decision.

| Hero | Ultimate I (L3+R3) | Ultimate II (RT+L3+R3) |
|---|---|---|
| Captain America | **Avengers Assemble**: Cap leads a charge as ghost silhouettes of the team strike | **Star-Spangled Stand**: a shield shockwave launches enemies, and the team takes half damage and regenerates gauge for 10s |
| Iron Man | **Proton Cannon**: a screen-length beam that bores through walls | **Iron Legion**: armors drop in for a second of saturation fire across the whole screen, plus a 5s team buff |
| Thor | **Bring Me Thanos**: Stormbreaker descends | **God of Thunder**: 12s storm form in which every hit chains lightning, with flight |
| Hulk | **World Breaker Smash**: splits the ground and collapses the current floor | **Gamma Rage**: 12s World Breaker form with unbreakable armor |
| Black Widow | **Red Ledger**: a cinematic takedown chain across every enemy on screen | **Widow's Web**: an electrified tripwire field across lanes, where stunned enemies take bonus damage from allies |
| Hawkeye | **Never Miss**: time slows and trick arrows auto-aim at every enemy | **Ronin**: 12s katana stance with a new close-range moveset |
| Black Panther | **Bast's Judgement**: a claw dash through everyone that discharges double the kinetic store | **Ancestral Plane**: spirit panthers fight alongside the team, with team regen |
| Captain Marvel | **Higher, Further, Faster**: a Binary dive that punches down through every level of the stage | **Binary Ignition**: instant Binary Mode for 15s |
| Ant-Man | **Giant-Man**: a skyscraper-sized stomp | **Quantum Tunnel**: shrinks every enemy for 10s; they take critical damage and can be kicked like debris |
| Vision | **Mind Stone Solar Beam**: a sweeping beam through every floor | **Phase Field**: allies become intangible for 6s, and enemies get stuck mid-phase in floors |
| *Spider-Man (unlock)* | *Maximum Spider* | *Web Fortress: webs enemies into throwable debris balls* |

**Team Ultimate:**
- When the shared Assemble meter is full, the HUD shows "ASSEMBLE READY."
- Each living player presses L3+R3 within one second of the others.
- Every enemy on screen is hit, and a unique cinematic plays for the specific combination of heroes present.
- Ultimate damage never feeds the Assemble meter.

---

## 6. Assist Callout System

The design draws on *Marvel vs. Capcom* assists, built for a co-op brawler.

- **Partner selection:** at character select, each player picks an **Assist Partner** from heroes nobody is playing, plus the assist-only cameos.
- **Three callouts per partner, chosen live:**
  - **LT** = α (launcher or juggle)
  - **LT + →** = β (projectile or zoning)
  - **LT + ↓** = γ (utility, such as an armor breaker, crowd pull, or anti-air)
- **How a callout plays out:**
  - The partner arrives from behind you, performs the move, and leaves.
  - Partners are invulnerable and cannot be targeted.
  - Their hits **count toward your combo** at **half juggle cost**, which makes assists a natural combo extender.
  - The cooldown is 6 seconds, shown on the HUD portrait.
- **Combo Relay:** if your juggled victim is near a teammate when you press LT, the press hands the combo off instead of calling your assist.
  - The victim pops toward the teammate.
  - Juggle points are cut by 40%.
  - The combo count stays shared.
  - Cooldown is 3 seconds.
- **Voice and visual callouts:**
  - Every call triggers a hero-pair voice bark ("Tony, light 'em up!") and a comic banner.
  - The **View-button ping** sends contextual co-op callouts by stick direction: "Relay to me!", "Breaking the floor!", "Assist ready", "Need a revive".
- **Example graybox kits:**
  - **Cap:** Charging Star / Shield Toss / Rising Shield Slash
  - **Iron Man:** Repulsor Blast / Unibeam Burst / Boot-Jet Launcher

---

## 7. Destruction, Interactive Debris, and Multi-Level Stages

### Destruction tiers

| Tier | Examples | Effect |
|---|---|---|
| Cosmetic | Crates, glass, signs | Spawns debris and style |
| Tactical | Drywall, cover, cars | Opens routes and turns into hazards |
| Structural | Floors | Collapse moves the fight to the level below |

- Destructibles have visual damage states. Gameplay health lives in the simulation; fractured chunks are cosmetic physics only.
- **Floor Break:** Ground Pound moves (Cap's Shield Drop, Leap of Liberty, Iron Man's Afterburner Dive) damage the floor they land on. When it breaks, everyone standing on it falls.

### Interactive debris

- **Pick up and throw:** RB picks up debris; X throws it level and Y throws it in an arc. Thrown debris is an **environmental hit**: bonus style, +3 juggle points, and it bounces off its target.
- **Bat it mid-combo:** any attack that hits debris launches it as your projectile.
- **Weight:** heavy debris ground-bounces its victim, and light debris wall-bounces them.
- **Hero interactions:**
  - Ant-Man enlarges a dropped can into a wrecking ball.
  - Thor charges metal debris with lightning.
  - Captain Marvel superheats debris into molten projectiles.
  - Hulk wears cars as gauntlets.
  - Hawkeye pins debris to walls as platforms.
  - Vision phases debris *through* walls.

### Multi-level stages

- **Layout:** stages stack street, interior, rooftop, and subway levels. Upper floors are **one-way**: you can jump up through them, and ↓+A drops through.
- **Ceiling Bounce:** sends juggled enemies up through the floor above. Players follow by double jumping, flying, or grappling, or with Vision's phase.
- **Branching routes:** each stage offers two or three routes (rooftops, subway, Ant-Man vents) with different encounters, and they rejoin before the boss.
- **Shared camera:** the camera frames every player across levels. Past the zoom limit, a **Rally** prompt lets the player who fell behind warp to the leader for a small amount of gauge.

---

## 8. Hero Kit Template

Each hero has about **30–35 moves**:
- 3 light strings plus directional lights
- 4 directional heavies
- Air normals and an air heavy
- A ground pound
- 4 specials and their air variants
- 4 Hero Specials
- 1 Super
- 2 Ultimates
- Throws
- 3 assist moves
- Taunt and D-pad tools

### Captain America (implemented in the graybox)

| Category | Moves |
|---|---|
| Light string | Jab → Cross → Shield Bash → Liberty Kick (wall bounce) |
| Directional lights | →X Step-In Elbow, ↓X Low Shield Sweep (OTG) |
| Heavies | Y Haymaker · ↑Y Star-Spangled Uppercut (launcher) · →Y Shield Charge (wall bounce, breaks walls) · ↓Y Shield Sweep (OTG) |
| Air | Air Jab → Air Shield Swipe → Air Hammer Kick (spike, ground bounce) · Air Y Aerial Shield Spike · Air ↓Y **Shield Drop** (floor break) |
| Specials | B Shield Toss (2 ricochets, returns) · Air B · ↑B Rising Shield Slash (invulnerable launcher, **ceiling bounce**) · ↓B Shield Plant (super armor) · →B Charging Star |
| Hero Specials | RT+X Shield Slingshot (4 ricochets) · RT+Y Brooklyn Brawl (10 hits) · RT+A Leap of Liberty (floor break) · RT+B Rally Cry (team buff) |
| Super | Shield of Freedom (8 ricochets) |
| Ultimates | I: Avengers Assemble · II: Star-Spangled Stand |
| Throw | Shoulder Toss (aimed with the stick, wall bounce) |
| Tools | D-pad ←/→ shield angle · D-pad ↓ charge an extra ricochet (25 gauge) |

### Iron Man (graybox slice kit)

| Category | Moves |
|---|---|
| Light string | Gauntlet Jab → Repulsor Palm → Thruster Kick |
| Heavies | Unibeam Burst (wall bounce) · Boot-Jet Launcher |
| Air | Air Palm → Air Thruster Spin (spike) |
| Specials | Repulsor Blast · ↓B Smart Missiles (3 ricochets) |
| Hero Specials | RT+X Pulse Bolts · RT+Y Repulsor Barrage · RT+A Afterburner Dive (floor break) · RT+B Overclock Protocol |
| Super | Unibeam |
| Ultimates | I: Proton Cannon · II: Iron Legion |
| Throw | Jet Toss |

### Vision (design)

| Category | Moves |
|---|---|
| Tools | D-pad ↓ **Phase**: intangible and passes through floors, walls, and enemies, but drains Density Energy. D-pad → **Diamond**: super armor, heavy hits, and floor breaking, but no flight. D-pad ← returns to normal density. Taunt stays on D-pad ↑ |
| Light string | Synthezoid Strikes, ending in a phase-through backfist that crosses the enemy up |
| Heavies | ↑Y Density Uppercut (launcher) · →Y Solid Lance (wall-splat) |
| Specials | B Mind Stone Beam · ↑B **Ascension**: rises while phasing and drags the enemy up through the ceiling (ceiling bounce) · ↓B **Phase Drop**: sinks through the floor and resurfaces beneath the target · →B Intangible Rush |
| Hero Specials | RT+X Solar Lances · RT+Y Diamond Hammer (floor break) · RT+A Phantom Flight · RT+B Mind Stone Pulse (slow-field stun) |
| Super | Synthezoid Overdrive: a beam through every level |
| Ultimates | I: Mind Stone Solar Beam · II: Phase Field |

### Ant-Man (design)

| Category | Moves |
|---|---|
| Tools | D-pad ↓ shrink, D-pad → grow, D-pad ← back to normal size, all usable mid-combo. Size changes cancel any move. Taunt stays on D-pad ↑ |
| Light string | Quick jabs that end in a shrink-dash punch through the enemy |
| Heavies | ↑Y **Growth Spurt** (launcher) · →Y Giant Shoulder (wall-splat that breaks walls) |
| Specials | B Red Pym Disc (shrinks the target) · ↓B Blue Pym Disc (enlarges an enemy or debris) · ↑B Flying Ant Mount (travel between levels) · →B Ant Swarm |
| Hero Specials | RT+X Pinball · RT+Y Giant Stomp · RT+A Quantum Dive · RT+B Colony Call |
| Super | Giant-Man Rampage |
| Ultimates | I: Giant-Man · II: Quantum Tunnel |

---

## 9. Stages

| # | Stage | Levels | Destruction Set-Piece | Boss |
|---|---|---|---|---|
| 1 | NYC Times Square | Street, office tower, rooftop | A falling billboard becomes a ramp | Taskmaster (copies your moves) |
| 2 | Hydra Alps Base | Cliff, hangar, reactor | An avalanche buries the hangar | Red Skull |
| 3 | Quinjet Chase | Sky, carrier deck, interior | Shooting the hull opens the way to board | MODOK |
| 4 | Wakanda | Jungle, mines, tracks | A mine cart chase through collapsing tunnels | Klaw |
| 5 | Asgard Bifrost | Bridge, palace, void | The bridge shatters into floating platforms | Enchantress & Executioner |
| 6 | Sokovia | Streets, surface, engine core | The city tilts and debris slides | Ultron Prime |
| 7 | Gamma Base | Desert, bunker floors B1–B5 | The fight crashes down through every floor | Abomination |
| 8 | Retro Era | 16-bit pixel art, then 2.5D | Pixels shatter into 3D debris | Loki |
| 9 | Chronopolis | Time-shifting levels | Destroyed areas rebuild themselves in reverse | Kang |
| 10 | Titan | Ruins, wreckage, surface | A moon falls on the arena | Thanos |

**Modes:**
- Story/Arcade
- Boss Rush
- Survival Horde
- Combo Trials
- Training Lab (frame data, hitbox view, recordable dummies)
- Daily Challenge
- Retro Mode (CRT filter and chiptune soundtrack)

**Progression:** hero XP unlocks the remaining moves, plus cosmetics. There is no paid power progression.

---

## 10. Art and Audio Direction ("Living Comic")

- **Rendering:**
  - Toon shading with 2–3 lighting steps.
  - Halftone shadows and rim light.
  - Inverted-hull ink outlines that thicken on impact.
- **Animation:**
  - Animated **on 2s**, with supers **on 1s**.
  - Smear frames modeled as Blender shape keys.
  - A 1–2 frame ink impact flash on heavy hits.
- **Effects:**
  - VFX Graph for lightning, beams, and debris.
  - 3D onomatopoeia text, already prototyped in `FeedbackDirector`.
  - Supers cut to comic-panel framings using Timeline and Cinemachine.
- **Readability:**
  - Outlines tinted per player, plus ground rings.
  - An effects-intensity slider.
- **Audio:**
  - An adaptive orchestral-rock score whose layers follow Style Rank.
  - Voice barks for every hero pair.
  - A chiptune remix for Retro Mode.

**Blender pipeline:**
1. Sculpt and retopologize to 30–60k triangles per hero.
2. Rig with Rigify plus prop bones.
3. Paint flat textures with ID and shadow masks.
4. Pre-fracture props with Cell Fracture, naming chunks `*_cell_###`.
5. Batch-export FBX through a scripted exporter.

---

## 11. Technical Architecture (as built)

```
Unity (presentation)                       Reforged.Sim (pure C#, no UnityEngine)
────────────────────                       ──────────────────────────────────────
PlayerInputBridge ──InputFrame──▶ GameSession ──▶ RollbackSession ──▶ SimWorld.Step()
FighterView / EntityView ◀── state ──┘   (60 Hz fixed)                 │ fixed-point Q16.16
DestructibleAuthoring ◀── DestructibleState                            │ fighters, projectiles,
FeedbackDirector / DebugHud ◀── SimEvents                              │ debris, destructibles
CameraDirector (Cinemachine target group)                              └ SaveState/LoadState/Checksum
```

- **Deterministic simulation.**
  - Fixed-point math throughout, with no floats or Unity types inside the sim.
  - Entities update in a fixed order.
  - Every mutable field is part of the snapshot.
- **Rollback netcode** (GGPO-style): missing inputs are predicted, and up to 8 frames are re-simulated when a prediction turns out wrong. Local play goes through the same path.
- **Two-layer destruction.**
  - Gameplay-relevant state (health, broken flags, floor collapse, interactive debris) lives in the sim and is rolled back.
  - Cosmetic chunks are local PhysX and are never rolled back.
- **Data-driven moves.** Frame data lives in `MoveDef`, edited as `HeroAsset` ScriptableObjects. Authored float values are rounded to millimetres so the fixed-point content is identical on every machine; this is verified by a test.
- **Tests.** NUnit suites run in both the Unity Test Runner and `dotnet test`, covering determinism, rollback, combat rules, assists, destruction, and content.

---

## 12. Roadmap and Status

| Phase | Duration | Deliverable | Status |
|---|---|---|---|
| Pre-production | 3 mo | Deterministic combat core, rollback, tools, graybox, art pipeline | **Done** (see README) |
| Vertical Slice | 4 mo | Cap + Iron Man, 1 multi-level stage with destruction, 2P local | In progress: graybox playable, character select, Cap + Iron Man proxies animated, toon shader, all four hero mechanics (Heat, Rage, Density, Size) with graybox Hulk/Vision/Ant-Man |
| Production A | 8 mo | 6 heroes, 5 stages, 4P local, online rollback | — |
| Production B | 7 mo | All 10 heroes, both Ultimates each, modes, team-ups | — |
| Polish/Cert | 6 mo | Balance, accessibility, performance, certification | — |
| **Total** | **~28 mo** | PC, PS5, Xbox Series, Switch 2 | |

**Key risks:**

| Risk | Mitigation |
|---|---|
| IP licensing | Pitch early; the systems work with original heroes |
| Destruction combined with rollback | Two-layer split, with determinism and rollback tests on every build |
| Readability with 4 players | Player tinting and an effects-intensity slider |
| Animation volume (12 heroes × ~200 clips) | Shared skeleton and locomotion, outsourcing |
| Twenty Ultimate cinematics | Shared Timeline templates, one bespoke shot each |
