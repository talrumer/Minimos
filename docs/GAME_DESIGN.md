# 🎮 Minimos — Game Design Document

> *A P2P multiplayer party game with stylized cartoonish characters, competitive mini-games, and endless fun.*

---

## 📋 Table of Contents

1. [Game Overview](#-game-overview)
2. [Core Vision & Pillars](#-core-vision--pillars)
3. [Art Style & Aesthetic](#-art-style--aesthetic)
4. [Characters & Customization](#-characters--customization)
5. [Teams & Colors](#-teams--colors)
6. [Core Mechanics](#-core-mechanics)
7. [Camera System](#-camera-system)
8. [Mini-Games](#-mini-games)
9. [Match Structure & Party Flow](#-match-structure--party-flow)
10. [Maps & Environments](#-maps--environments)
11. [Game Flow & Scenes](#-game-flow--scenes)
12. [UI/UX Design](#-uiux-design)
13. [Audio & Music](#-audio--music)
14. [Networking & Multiplayer](#-networking--multiplayer)
15. [Progression & Economy](#-progression--economy)
16. [Technical Requirements](#-technical-requirements)
17. [Development Phases](#-development-phases)

---

## 🌟 Game Overview

**Minimos** is a team-based multiplayer party game where 4–12 players split into teams and compete in fast-paced, chaotic mini-games. Think *Fall Guys* meets *Mario Party* meets *Gang Beasts* — but with a unique low-poly cartoonish aesthetic and focused team-based gameplay.

| Detail | Value |
|---|---|
| **Genre** | Party / Competitive Multiplayer |
| **Players** | 4–12 (2–6 teams of 2) |
| **Platform** | PC (Steam), with future console support |
| **Engine** | Unity (URP) |
| **Networking** | P2P via Netcode for GameObjects + Unity Transport |
| **Art Style** | Low-poly, stylized, cartoonish, pastel colors |
| **Match Duration** | ~15–25 minutes (3–5 mini-games per match) |

---

## 🏛️ Core Vision & Pillars

### 🎯 Vision Statement
> *"Easy to pick up, impossible to put down. Every round is a story you'll want to tell."*

### Pillars

1. **🤣 Fun First** — Every mechanic should make players laugh, cheer, or yell. If it's not fun, cut it.
2. **🤝 Teamwork Matters** — 2-player teams create natural cooperation. Strategies emerge from synergy, not solo skill.
3. **⚡ Instantly Readable** — Low-poly + pastel team colors = you always know what's happening, who's on your team, and where to go.
4. **🔄 High Replayability** — Random mini-game selection, map variants, and power-up spawns keep every match fresh.
5. **🎨 Express Yourself** — Character customization lets players build their identity without pay-to-win.

---

## 🎨 Art Style & Aesthetic

### Visual Direction
- **Low-poly** geometry with smooth shading (not flat-shaded)
- **Pastel color palette** — soft, warm, inviting
- **Exaggerated proportions** — big heads, stubby limbs, oversized hands
- **Thick outlines** (optional post-process) for readability
- **Cartoon physics** — squash & stretch on impacts, exaggerated knockback
- **Particle effects** — confetti on wins, dust clouds on dashes, sparkles on pickups

### Color Philosophy
- Environments use **muted, earthy pastels** so team colors POP
- UI uses **clean whites and soft grays** with team color accents
- VFX use **bright, saturated** versions of the base palette for visibility

### Reference Inspirations
- *Fall Guys* (character proportions, chaotic energy)
- *Overcooked* (team cooperation, readable maps)
- *Crossy Road* (low-poly charm)
- *Splatoon* (team colors dominating the visual space)
- *Moving Out* (cartoonish environments, playful physics)

---

## 🧑‍🤝‍🧑 Characters & Customization

### The "Minimo" Character

A **Minimo** is a small, bean-shaped humanoid with:
- Round, soft body (pill/capsule shape)
- Large expressive eyes (animated — blink, squint, surprise)
- Stubby arms and legs
- No visible mouth by default (expressions via eyes + body language)
- **Main body takes on team color** during matches

### Customization Categories

| Category | Examples | How to Obtain |
|---|---|---|
| **🎩 Hats** | Top hat, crown, pirate hat, flower crown, viking helmet | Shop / Achievements |
| **👓 Face Accessories** | Sunglasses, monocle, mustache, bandana | Shop / Drops |
| **🎒 Back Items** | Cape, wings, jetpack (cosmetic), backpack, shield | Shop / Achievements |
| **👟 Shoes** | Sneakers, boots, roller skates (cosmetic), clown shoes | Shop / Drops |
| **🌈 Patterns** | Stripes, polka dots, camo, gradient, stars | Shop / Level Up |
| **✨ Emotes** | Dance, wave, taunt, laugh, cry | Shop / Achievements |
| **🎆 Victory Effects** | Confetti, fireworks, rainbow trail, lightning | Shop / Rare Drops |
| **🔊 Sound Effects** | Custom hit sounds, victory jingles | Shop / Rare |

### Customization Rules
- ✅ All cosmetics are **purely visual** — no gameplay advantage
- ✅ Team color always overrides the base body color during matches
- ✅ Patterns overlay on top of the team color
- ✅ Players can preview full customization in the Character Studio scene

---

## 🏳️ Teams & Colors

### Team Color Palette (Pastel)

| Team | Color Name | Hex (approx) | Usage |
|---|---|---|---|
| 🔴 Team 1 | Coral Red | `#FF6B6B` | Body tint, flag, UI accent |
| 🔵 Team 2 | Sky Blue | `#74B9FF` | Body tint, flag, UI accent |
| 🟢 Team 3 | Mint Green | `#55EFC4` | Body tint, flag, UI accent |
| 🟡 Team 4 | Sunny Yellow | `#FFEAA7` | Body tint, flag, UI accent |
| 🟠 Team 5 | Peach Orange | `#FAB1A0` | Body tint, flag, UI accent |
| 🟣 Team 6 | Lavender Purple | `#A29BFE` | Body tint, flag, UI accent |

### Team System
- Teams are assigned at **lobby time** (random or player-picked)
- Each team gets a **team name** (auto-generated or custom): *"The Coral Crushers"*, *"Mint Maniacs"*, etc.
- Team color applies to: character body, HUD elements, nameplates, minimap dots, score bars

---

## 🕹️ Core Mechanics

### Movement

| Action | Input | Description |
|---|---|---|
| **Walk** | Left Stick / WASD | Standard movement, moderate speed |
| **Run** | Hold LT / Shift | 1.5x speed, slight camera zoom out |
| **Jump** | A / Space | Standard jump with squash & stretch |
| **Double Jump** | A+A / Space+Space | Second jump with a small burst VFX |
| **Dodge Roll** | B / Ctrl | Quick invulnerable roll in movement direction, 2s cooldown |
| **Slide** | Crouch while running | Short slide, maintains momentum, low profile |

### Combat / Interaction

| Action | Input | Description |
|---|---|---|
| **Melee Attack** | X / Left Click | Short-range punch/slap. **1s stun + moderate knockback.** Fast (0.3s wind-up), spammable (0.5s cooldown). |
| **Charged Melee** | Hold X / Hold Left Click | Hold for 1s to charge (character glows + vibrates). **2s stun + big knockback.** Interruptible — getting hit cancels the charge. High risk, high reward. |
| **Ranged Attack** | Y / Right Click | Lob a slow, arcing projectile. **1.5s slow (no stun).** 3s cooldown. Easy to dodge at distance but good for area denial. Limited ammo: 3 shots, recharges 1 per 5s. |
| **Grab** | RB / E | Grab another player or object within melee range. Target can **mash jump to break free** within 1s window. Grab fails if target is mid-dodge. |
| **Throw** | RB (while grabbing) / E | Throw grabbed player/object in aimed direction. Thrown players take **1s stun on landing.** |
| **Use Item** | LB / Q | Use picked-up power-up. One item slot — picking up a new item replaces the old one. |

### Combat Balance Philosophy
- **Melee is king up close** — fast, reliable, but you have to get in range
- **Ranged is for zoning/chasing** — slow projectile + limited ammo means it's a tool, not a primary weapon
- **Grab is the high-skill play** — risky (can be dodged, broken free from) but devastating (throw off ledges, away from objectives)
- **Charged melee punishes crowds** — great when enemies are distracted, terrible in 1v1 (too slow)

### Combat Rules
- ✅ No permanent deaths in most modes — stunned players recover in place
- ✅ Stun duration stacks diminishingly (1s → 1.5s → 1.7s max)
- ✅ Dodge roll grants **i-frames** (invincibility frames) for 0.3s
- ✅ Friendly fire is **OFF** by default (toggleable in custom matches)
- ✅ All attacks have clear **wind-up animations** so opponents can react
- ✅ **Hit cooldown** — a player who was just stunned has 0.5s of reduced knockback to prevent juggling

### Physics & Feel
- **Ragdoll on big hits** — characters go limp briefly before recovering
- **Bouncy collisions** — players bounce off walls and each other slightly
- **Momentum-based movement** — slight acceleration/deceleration, no instant stops
- **Screen shake** on big impacts (subtle, adjustable in settings)

---

## 📷 Camera System

The camera adapts per mini-game category to always give the best view of the action.

### Camera Modes

| Mode | Used By | Description |
|---|---|---|
| **Follow Cam** | CTF, Treasure Hoarder, King of the Hill | 3rd-person behind the player, ~45° angle, Cinemachine follow + aim. Zooms out slightly when running. |
| **Arena Cam** | Last Team Standing, Dodgeball, Hot Potato | Fixed isometric/angled top-down view showing the entire arena. Zooms in as the arena shrinks. |
| **Side-Scroll Cam** | Obstacle Dash, Climb Rush | Side-view tracking camera that follows the player vertically/horizontally. Slight depth to the 3D. |
| **Sports Cam** | Minimo Ball | Broadcast-style camera that follows the ball, tilts toward the action. |
| **Split Zone Cam** | Build & Defend | Camera positioned to show your team's build zone primarily, with enemy zones visible in the distance. |

### Camera Rules
- ✅ All cameras use **Cinemachine** for smooth blending and damping
- ✅ Camera never clips through geometry (Cinemachine Deoccluder)
- ✅ Players at the edge of screen get a **directional arrow indicator** pointing toward them
- ✅ Teammates always have a **visible outline** even through walls (team-colored)
- ✅ Camera shakes on big impacts (intensity adjustable in settings, 0–100%)
- ✅ Smooth transitions between camera modes during mini-game switches

### Spectator Camera (for eliminated players)
- Free orbit camera that can follow any remaining player
- Tab to cycle between players
- No ability to communicate game info to alive teammates (no ghosting)

---

## 🎲 Mini-Games

### Game Mode Categories

| Category | Description |
|---|---|
| 🏁 **Objective** | Teams compete to achieve a goal (flags, zones, payload) |
| 🥊 **Combat** | Direct team-vs-team brawling |
| 🏃 **Race** | Teams race to the finish through obstacles |
| 🧩 **Puzzle** | Teams solve cooperative challenges under pressure |
| 👑 **Survival** | Last team standing wins |

---

### 🏁 1. Capture the Flags

> *Multiple flags, total chaos, pure strategy.*

| Detail | Value |
|---|---|
| **Teams** | 3–6 teams of 2 |
| **Duration** | ~5 minutes or first to 100 points |
| **Map Size** | Medium-Large |

**Rules:**
- 2–3 neutral flags spawn at random positions on the map
- Picking up a flag: hold interact near it for 0.5s
- While holding a flag: **move 20% slower**, **can't attack**, flag is visible above your head
- Each second holding a flag = **+1 point** for your team
- Getting hit = **drop the flag** (it stays where it falls for 3s, then anyone can grab it)
- Teammates can **bodyguard** the flag carrier by fighting off attackers
- Flag respawns at a new random location if untouched for 10s
- **First team to 100 points wins** (or highest score at time limit)

**Mode-Specific Items:**
- 🛡️ **Flag Shield** — Absorb one hit without dropping the flag

> 📝 See [Global Power-Up System](#-global-power-up-system) below for shared power-ups that appear in all modes.

---

### 🥊 2. King of the Hill

> *Hold the zone. Push everyone else out.*

| Detail | Value |
|---|---|
| **Teams** | 2–6 teams of 2 |
| **Duration** | 3 minutes, highest score wins |
| **Map Size** | Small-Medium |

**Rules:**
- A glowing zone spawns at the center of the map
- Zone moves to a new position every 45s
- Teams earn **+2 points/second** for each player inside the zone
- Both teammates in the zone = **+5 points/second** (teamwork bonus!)
- Attacks and knockback are the primary way to push enemies out
- Zone shrinks slightly over time within each 45s phase

---

### 🏃 3. Obstacle Dash

> *Race your team through a chaotic obstacle course.*

| Detail | Value |
|---|---|
| **Teams** | 2–6 teams of 2 |
| **Duration** | ~2–3 minutes per round |
| **Rounds** | 2–3 rounds, different courses |

**Rules:**
- Side-scrolling-style 3D obstacle course
- Swinging hammers, rotating platforms, falling blocks, slippery ramps
- Both teammates must cross the finish line for the team to complete
- One player can **pull ahead and activate shortcuts** for their teammate
- Teams are ranked by finish order: 1st = 10pts, 2nd = 7pts, 3rd = 5pts, etc.
- Players can **shove** opponents off platforms (but not teammates)

---

### 🧩 4. Build & Defend

> *Build your tower. Sabotage theirs.*

| Detail | Value |
|---|---|
| **Teams** | 2–6 teams of 2 |
| **Duration** | 4 minutes |
| **Map Size** | Medium with separated zones (one per team, arranged in a circle) |

**Rules:**
- Each team has a **build zone** with scattered blocks
- One player builds (stacks blocks to hit a height target)
- Other player defends (fights off enemies trying to knock your tower down)
- Players can switch roles at any time
- Throwing blocks at enemy towers deals damage
- First team to reach the height target wins — or tallest tower at time limit
- With 5–6 teams, build zones are smaller but closer together — more chaos, faster rounds

---

### 👑 5. Last Team Standing

> *Shrinking arena. One team survives.*

| Detail | Value |
|---|---|
| **Teams** | 3–6 teams of 2 |
| **Duration** | ~3 minutes |
| **Map Size** | Large → shrinking |

**Rules:**
- Arena floor slowly falls away / lava rises from edges
- Getting knocked off = eliminated (your teammate is still in!)
- If both teammates are eliminated, the team is out
- Last team with at least one player standing wins
- Random **power-up crates** fall from the sky during the match
- **Mega Punch** power-up = massive single knockback hit

---

### ⚽ 6. Minimo Ball

> *It's soccer, but the ball is massive and the physics are absurd.*

| Detail | Value |
|---|---|
| **Teams** | 2–3 teams of 2 |
| **Duration** | 3 minutes or first to 5 goals |
| **Map Size** | Small arena |
| **Multi-team format** | 3 teams → triangular field with 3 goals. Defend yours, score in others. |

**Rules:**
- Oversized ball with exaggerated physics
- Players push the ball by running into it or punching it
- Grab + throw works on the ball too (but it's heavy, so short range)
- Goals are wide and have a fun **explosion animation** on score
- Ball resets to center after each goal
- Slide-tackling near the ball gives it a massive boost
- **3-team variant:** Triangular pitch, each team defends one goal. Goals scored against you = -1 point. Goals you score = +1 point.
- **If 4+ teams in the party:** Bracket tournament — teams are paired, winners play next, losers spectate (quick rounds keep it snappy)

---

### 🏴‍☠️ 7. Treasure Hoarder

> *Collect coins. Steal from others. Bank at your base.*

| Detail | Value |
|---|---|
| **Teams** | 2–6 teams of 2 |
| **Duration** | 4 minutes |
| **Map Size** | Medium |

**Rules:**
- Coins spawn across the map in clusters
- Players collect coins by walking over them
- Getting hit = **drop 50% of your coins** (scatter on ground)
- Return coins to your **team chest** (at your spawn) to bank them
- Banked coins are safe and can't be stolen
- Final 30 seconds: **Golden Coin** spawns (worth 25 coins!)
- Team with most banked coins wins

---

### 🎯 8. Dodgeball Frenzy

> *Throw. Dodge. Survive.*

| Detail | Value |
|---|---|
| **Teams** | 2–6 teams of 2 |
| **Duration** | Best of 3 rounds |
| **Map Size** | Small arena divided into team zones (wedge-shaped for 3+ teams) |

**Rules:**
- Each team zone has balls that respawn periodically
- Hit = eliminated for that round
- Catch a ball mid-air = the thrower is eliminated instead
- Dodge roll is critical (i-frames!)
- Last team with a player standing wins the round
- Balls get faster each round
- **3+ teams:** Arena is divided into wedges. You can throw at any other team. Last team standing wins.

---

### 🎪 9. Hot Potato

> *Don't be holding it when the timer hits zero.*

| Detail | Value |
|---|---|
| **Teams** | 2–6 teams of 2 (team-based) |
| **Duration** | 3 rounds of 30s each |
| **Map Size** | Small |

**Rules:**
- One random player starts with the "hot potato" (glowing bomb)
- Pass it by **running into an opponent** or **throwing it** (can't pass to your own teammate)
- Timer counts down from 30s (hidden for last 5s — tension!)
- Player holding it when it explodes = their **team loses 3 points**
- If neither teammate is holding it when it explodes = their **team gains 2 points**
- Both teammates eliminated is impossible — it's about dodging, not survival
- Between rounds, the potato starts with a player from a different team

---

### 🏔️ 10. Climb Rush

> *Race to the top of a procedurally shifting tower.*

| Detail | Value |
|---|---|
| **Teams** | 2–6 teams of 2 |
| **Duration** | ~2 minutes |
| **Map Size** | Vertical tower |

**Rules:**
- Vertically stacked platforms — jump your way up
- Platforms shift, rotate, disappear, and reappear
- Players can shove opponents off
- Falling resets you to the last checkpoint
- Both teammates must reach the top to win
- One teammate can stand on platforms to **anchor them** (stop moving) for the other

---

## ⚡ Global Power-Up System

Power-ups spawn on all maps in glowing crates (team-neutral). Players walk over them to pick up. **One item slot per player** — picking up a new item replaces the current one.

### Universal Power-Ups (appear in all modes)

| Power-Up | Icon | Effect | Duration | Spawn Rate |
|---|---|---|---|---|
| **Speed Boost** | ⚡ | 2x movement speed | 3s | Common |
| **Mega Punch** | 🥊 | Next melee deals 3x knockback | 1 use | Uncommon |
| **Invisibility** | 👻 | Semi-transparent, no nameplate, can't be targeted by ranged | 4s | Rare |
| **Magnet** | 🧲 | Auto-collect nearby items/coins/flags within a radius | 5s | Common |
| **Freeze Bomb** | 🧊 | Throw to create a small AoE — all enemies inside are rooted for 2s | 1 use | Uncommon |
| **Buddy Shield** | 🛡️ | Both you and your teammate get a 1-hit shield | Instant | Rare |
| **Bouncy Shoes** | 🦘 | 3x jump height + no fall stun | 5s | Common |
| **Swap** | 🔀 | Instantly swap positions with the nearest opponent | 1 use | Rare |

### Power-Up Rules
- ✅ Crates glow white and pulse — easy to spot
- ✅ Crates respawn every 15–20s at random locations from a set of spawn points
- ✅ Cannot use power-ups while stunned or grabbed
- ✅ Power-ups are lost on elimination (survival modes)
- ✅ Trailing teams find crates 20% more often (see Comeback Mechanics)
- ✅ Host-authoritative: host determines what spawns and validates usage

---

## 🏆 Match Structure & Party Flow

### What is a "Party"?
A **Party** is a full session of Minimos — a series of mini-games played back-to-back by the same group of teams. This is the core play loop.

| Setting | Default | Host-Configurable Range |
|---|---|---|
| **Number of Rounds** | 5 | 3–7 |
| **Mini-Game Selection** | Random (no repeats until all played) | Random / Vote / Host Pick |
| **Team Size** | 2 | 2 (v1 fixed) |
| **Number of Teams** | Auto (based on player count) | 2–6 |

### Scoring Across a Party

Each mini-game awards **Party Points** based on placement:

| Placement | Points (6 teams) | Points (4 teams) | Points (2 teams) |
|---|---|---|---|
| 🥇 1st | 10 | 10 | 10 |
| 🥈 2nd | 7 | 7 | 0 |
| 🥉 3rd | 5 | 4 | — |
| 4th | 3 | 1 | — |
| 5th | 2 | — | — |
| 6th | 1 | — | — |

- The team with the **most Party Points** at the end of all rounds wins
- Tiebreaker: team with more 1st-place finishes → then head-to-head record → then sudden death bonus round

### Mini-Game Selection

**Random Mode (Default):**
- Shuffled pool — every mini-game is played once before any repeat
- Games incompatible with the current team count are excluded automatically (e.g., Minimo Ball with 5 teams)
- A brief **"Coming Up Next"** screen shows the mini-game name + a 3-second rule summary

**Vote Mode:**
- 3 random mini-games are presented as options
- Each player gets 1 vote (15s timer)
- Tie = random pick among tied options
- Voted games are removed from the pool for next vote

**Host Pick Mode:**
- Host manually selects each round's mini-game
- Can build a custom playlist before the party starts

### Between Rounds
- **Scoreboard** (5s) — animated team rankings with point changes
- **MVP Callout** — "⭐ [Player] scored 47 points!" (simple stat tracking, no replay needed — just the stat + a zoomed portrait of their Minimo doing a pose)
- **Fun Stats** — "🤜 Most Stuns: Player X (12)" / "🏃 Most Distance: Player Y" — cheap to track, adds personality
- **Next Game Preview** (3s) — name + one-line rule reminder
- Total intermission: ~10–12 seconds (fast, keeps energy up)

### 🔄 Comeback Mechanics

Keeping losing teams engaged is critical. These are **automatic, invisible boosts** — no "blue shell" frustration, just subtle lifts:

| Mechanic | How It Works | Why It Works |
|---|---|---|
| **Underdog Boost** | Teams in last place get +10% movement speed and -15% cooldowns during gameplay | Subtle enough that leading teams won't feel robbed, but trailing teams feel snappier |
| **Catch-Up Spawns** | In objective modes (CTF, Treasure Hoarder), items/flags spawn slightly closer to trailing teams | Reduces travel time disadvantage without feeling unfair |
| **Final Round Bonus** | The last round of a party awards **1.5x Party Points** | Mathematically keeps more teams in contention entering the finale |
| **Mercy Timer** | If a team is ahead by 3+ rounds' worth of points, remaining rounds award +2 bonus points to non-leading teams | Prevents "it's already decided" by round 3 |
| **Lucky Break** | Trailing teams find power-ups 20% more often | More chances to make exciting plays |

### ❌ What Comeback Mechanics Will NOT Do
- ❌ No punishing the leading team directly (no blue shells, no handicaps on winners)
- ❌ No rigging RNG to make leaders lose
- ❌ Boosts are subtle — they create *opportunities*, not guaranteed comebacks

---

### 🔄 Spawn & Respawn System

| Scenario | Behavior |
|---|---|
| **Match Start** | Teams spawn in designated team spawn zones (color-coded, opposite sides of map) |
| **After Stun (combat)** | Player recovers in-place after stun timer expires. No teleportation. |
| **After Knockout (fell off map)** | Respawn at nearest team checkpoint / safe zone after 2s delay |
| **After Elimination (modes with elimination)** | Switch to spectator cam. No respawn. |
| **Respawn Invulnerability** | 2s of invulnerability after respawn (character blinks/flashes to indicate) |
| **Anti-Camp** | Spawn zones have a **no-enemy zone** — opponents are pushed out if they enter (gentle knockback, no damage) |

---

## 🗺️ Maps & Environments

Each mini-game has **2–3 map variants** per environment theme. All environments share the same pastel, low-poly aesthetic but with distinct moods.

### 🌿 1. Sunny Meadows (Default / Nature)
- Rolling green hills, scattered flowers, wooden fences
- Small ponds with lily pads
- Trees with round, puffy canopies
- Background: soft mountains, fluffy clouds, butterflies
- **Mood:** Happy, warm, welcoming
- **Used for:** Capture the Flags, King of the Hill, Minimo Ball
- **🎯 Hazards:** Angry bees (chase nearby players for 3s, cause flinch), mud patches (slow to 50% speed), rolling hay bales (knockback on contact)

### 🤠 2. Dusty Gulch (Wild West)
- Sandy terrain, cacti, tumbleweeds
- Wooden saloon buildings, water towers, mine carts on rails
- Red rock formations and mesas
- Background: desert sunset, distant canyons
- **Mood:** Adventurous, warm-toned
- **Used for:** Treasure Hoarder, Last Team Standing, Dodgeball Frenzy
- **🎯 Hazards:** Mine carts on rails (knockback if hit), tumbleweeds (push players in wind direction), quicksand patches (sink + slow, mash jump to escape)

### 🏖️ 3. Coral Cove (Beach / Tropical)
- Sandy beaches, palm trees, tiki torches
- Shallow turquoise water areas (wadeable, slows you down)
- Wooden piers and beach huts
- Background: ocean waves, distant islands, seagulls
- **Mood:** Relaxed, tropical, colorful
- **Used for:** Hot Potato, Capture the Flags, Obstacle Dash
- **🎯 Hazards:** Rising/falling tide (floods low areas periodically, forces players to high ground), coconuts dropping from palm trees (stun on hit), crabs that pinch ankles (brief root in place)

### 🏰 4. Candy Castle (Fantasy / Sweets)
- Made of candy, cookies, and cake
- Gummy bear trees, lollipop lampposts, chocolate rivers
- Bouncy gelatin platforms, slippery frosting floors
- Background: cotton candy clouds, rainbow arches
- **Mood:** Whimsical, playful, sugary
- **Used for:** Build & Defend, Climb Rush, Obstacle Dash
- **🎯 Hazards:** Sticky caramel floors (slow + can't jump for 1s), chocolate river current (pushes players sideways), popping candy geysers (launch players upward)

### 🏠 5. Cozy Villa (Suburban / Indoor)
- Oversized furniture — players are tiny in a giant house
- Bookshelves as climbable walls, cushions as bouncy pads
- Kitchen counters, bathtubs, toy rooms
- Background: windows showing a backyard garden
- **Mood:** Cozy, quirky, playful scale
- **Used for:** King of the Hill, Hot Potato, Last Team Standing
- **🎯 Hazards:** Roomba patrol (pushes players on contact, follows a set path), dripping faucets (create slippery puddles), cat paw that swipes across the counter (massive knockback, telegraphed with shadow)

### 🌙 6. Neon Nights (Cyberpunk / Night)
- Glowing neon platforms, holographic signs
- Dark background with vibrant colored lighting
- Moving conveyor belts, laser grids (instant knockback)
- Synth-wave aesthetic with pixel art billboards
- **Mood:** Energetic, futuristic, flashy
- **Used for:** Dodgeball Frenzy, Climb Rush, Minimo Ball
- **🎯 Hazards:** Laser walls on timers (knockback, pattern is visible via charging glow), conveyor belts (force movement in one direction), EMP zones (disable dodge roll for 3s, telegraphed with flickering)

---

## 🎬 Game Flow & Scenes

### Scene Map

```
┌─────────────────┐
│   Splash Screen  │ → Auto-transition after 3s
└────────┬────────┘
         ▼
┌─────────────────┐
│   Main Menu      │ → 3D animated background (Sunny Meadows)
│   (Interactive)  │   Characters idle, play, run around
└────────┬────────┘
    ┌────┼────────────────┐
    ▼    ▼                ▼
┌───────┐ ┌────────────┐ ┌──────────┐
│ Play  │ │ Character  │ │ Settings │
│ Lobby │ │ Studio     │ │          │
└───┬───┘ └────────────┘ └──────────┘
    ▼
┌─────────────────┐
│   Team Select /  │ → Auto-assign or manual pick
│   Matchmaking    │
└────────┬────────┘
         ▼
┌─────────────────┐
│   Mini-Game      │ → Random selection from pool
│   Intro Screen   │   (vote system optional)
└────────┬────────┘
         ▼
┌─────────────────┐
│   Gameplay       │ → The actual mini-game
│                  │
└────────┬────────┘
         ▼
┌─────────────────┐
│   Results /      │ → Score breakdown, MVP, highlights
│   Scoreboard     │
└────────┬────────┘
    ┌────┼──────────┐
    ▼               ▼
┌────────┐   ┌────────────┐
│ Next   │   │ Final      │
│ Round  │   │ Results    │
│        │   │ + Podium   │
└────────┘   └────────────┘
```

### 🏠 Main Menu (Interactive 3D Scene)
- Camera orbits around a **mini Sunny Meadows** playground
- Your Minimo character stands in the scene, idle-animating
- Other players' Minimos appear in the background if friends are online
- UI panels **slide in from sides** on button hover — not a flat menu
- Background ambience: birds, gentle wind, distant laughter

### 🧑‍🎨 Character Studio
- Full 3D turntable view of your Minimo
- Category tabs on left (Hats, Face, Back, Shoes, Patterns, Emotes)
- **Live preview** — items appear instantly on character
- "Try Before You Buy" — items glow differently if not owned
- Random outfit button 🎲
- Team color preview toggle (see how your outfit looks with each team color)

### 🏟️ Lobby / Matchmaking
- **Quick Play** — matchmake into a random session
- **Create Private Game** — get a room code to share with friends
- **Join by Code** — enter a friend's room code
- Lobby shows all connected players with their Minimos
- Host can configure: number of rounds, which mini-games, team size
- **Ready up** system — game starts when all players are ready

---

## 🎓 Tutorial & Onboarding

### First-Time Experience
1. **"Welcome to Minimos!"** — short animated intro (your Minimo wakes up in Sunny Meadows)
2. **Training Grounds** — small sandbox area accessible from main menu at any time
   - Movement tutorial (walk → run → jump → double jump → dodge → slide)
   - Combat tutorial (melee → ranged → grab/throw)
   - Objective tutorial (pick up a flag, hold a zone, score a goal)
3. **Each new mini-game** — first time you play a mode, a 5-second rules overlay appears:
   - Icon + 1-sentence rule + control hint
   - "Press [Skip] to dismiss" — veteran players aren't slowed down
4. **Practice Mode** — play any mini-game solo with bots from the main menu (great for learning)

### Bot System (for Training & Incomplete Lobbies)
- Basic AI bots fill empty team slots when lobbies aren't full
- Bots have 3 difficulty levels: **Easy** (tutorial), **Medium** (default filler), **Hard** (practice)
- Bots wear a 🤖 icon above their head so players know they're AI
- Host can configure: allow bots yes/no, bot difficulty

---

## 🏅 Victory Podium & End-of-Party

### Round Results Screen (after each mini-game)
- Team rankings slide in from left, animated with bounce
- Point changes shown with "+10" / "+7" etc. next to team bars
- **⭐ MVP Stat** — simple stat callout per round (no replay needed):
  - "🏃 Fastest Flag Run: [Player] — 4.2 seconds"
  - "🤜 Most Stuns: [Player] — 8"
  - "⚽ Hat Trick: [Player] — 3 goals"
- **Fun Stat of the Round** — one random silly stat:
  - "🦥 Most Time Standing Still: [Player]"
  - "🤡 Most Self-Eliminations: [Player]"
  - "🧲 Most Power-Ups Wasted: [Player]"
- Duration: ~8 seconds, auto-advances to next game preview

### Final Podium (end of party)
- 3D podium scene — top 3 teams standing on 1st/2nd/3rd platforms
- Winning team's Minimos do their equipped **victory emote**
- Other teams stand in background, comedically sad (slumped posture, rain cloud VFX)
- Team-colored confetti and spotlights on the winners
- **Party Stats Summary** shown alongside podium:
  - Total stuns, total points, biggest comeback, closest round
- "Play Again?" / "Return to Menu" / "Change Teams" options
- Background music: triumphant fanfare → fades into chill menu music

---

## 🎭 Party Mutators (Custom Games)

When creating a **Private Game**, the host can toggle fun mutators that remix the gameplay:

| Mutator | Effect | Fun Factor |
|---|---|---|
| **🪨 Big Head Mode** | All characters have 3x head size | Pure comedy, slightly easier to hit |
| **🌙 Low Gravity** | Jump height 2x, fall speed 0.5x | Floaty chaos, changes every mode |
| **💨 Turbo Mode** | All movement 1.5x speed | Frantic energy, harder to control |
| **🥊 Mega Knockback** | All knockback 3x | Players fly across the map on every hit |
| **🚫 No Power-Ups** | Power-up crates don't spawn | Pure skill, no RNG |
| **👻 Invisible Mode** | All players are semi-transparent | Hilarious chaos, rely on nameplates |
| **🔄 Random Loadout** | Each player starts with a random power-up each round | Asymmetric fun |
| **⏩ Sudden Death** | All stuns last 3x longer, one mistake is costly | High tension |

- Mutators are **private games only** — not in matchmaking
- Multiple mutators can stack
- Mutator icon shown in lobby so players know what they're getting into
- Zero extra game logic needed for most — just tweak existing values (gravity, speed, scale)

---

## 👥 Social Features

### Friends & Party System
- **Friends List** — via Steam friends integration (PC), no need for a custom friend system
- **Party Up** — invite friends to your pre-game party before matchmaking. Party sticks together across matches
- **Recent Players** — list of last 20 players you played with, can send friend requests via Steam

### Communication
- **Quick Reactions** during gameplay (see Announcer section)
- **Quick Chat** in lobby — preset messages: "Ready!", "Wait for me!", "GG!", "Rematch?"
- **No text chat in-game** — keeps it clean, no toxicity moderation needed
- **Voice chat** — not in v1. Players use Discord/external. Revisit later.

### Invite Flow
- Host creates private game → gets a **6-character room code**
- Share code via Discord/text/etc.
- Friends enter code in "Join by Code" screen
- **Steam invite** — "Join Game" through Steam friend list also works (deep link to room)

---

## ⚙️ Settings & Options

| Category | Options |
|---|---|
| **🎮 Controls** | Remap all keybinds, gamepad button remapping, sensitivity sliders |
| **🔊 Audio** | Master volume, Music volume, SFX volume, Announcer volume (separate!), UI sounds |
| **🖥️ Graphics** | Resolution, Fullscreen/Windowed/Borderless, VSync, Quality preset (Low/Med/High/Ultra), FPS cap |
| **🎨 Visual** | Screen shake intensity (0–100%), camera FOV slider, HUD scale, colorblind mode |
| **♿ Accessibility** | Colorblind mode (icons + patterns on team indicators), subtitles for announcer, auto-aim assist toggle |
| **🌐 Network** | Region selection, show ping, bandwidth usage display |
| **🔔 Notifications** | Friend invite popups, match found sound |

---

## ⏳ Loading Screens

Loading between mini-games is a perfect moment to keep players engaged:

- **Mini-game rules preview** — next game's name + 3-bullet rule summary + key controls
- **"Did You Know?"** tips — rotating gameplay tips:
  - "💡 You can break free from grabs by mashing Jump!"
  - "💡 Charged melee can be interrupted — watch your back!"
  - "💡 The Buddy Shield protects both you AND your teammate!"
- **Current standings** — show the party leaderboard so players know where they stand
- **Minimo doing a fun idle animation** — juggling, stretching, sleeping — different each time
- Loading screens should last **3–5 seconds max** (low-poly assets = fast loads)

---

## 🖥️ UI/UX Design

### HUD (During Gameplay)
- **Top Center:** Timer + current mini-game name
- **Top Right:** Team scores (colored bars)
- **Bottom Left:** Player abilities / cooldowns
- **Bottom Right:** Minimap (shows team dots)
- **Center Pop-ups:** "+1 Point!", "FLAG DROPPED!", "ZONE MOVING!" etc.

### UI Style Guide
- **Font:** Rounded, bold, playful (think *Fredoka One* or *Baloo 2*)
- **Buttons:** Large, rounded rectangles with slight 3D bevel
- **Animations:** Everything bounces, slides, and pops — no static transitions
- **Colors:** White/light gray base, team color accents, black text
- **Sounds:** Every UI interaction has a satisfying click/pop/swoosh

### Accessibility
- ✅ Colorblind-friendly mode (icons + patterns on team indicators, not just color)
- ✅ Adjustable HUD scale
- ✅ Screen shake toggle / intensity slider
- ✅ Subtitles for game announcements
- ✅ Remappable controls
- ✅ Auto-aim assist for ranged attacks (toggle)

---

## 🔊 Audio & Music

### Music Direction
- **Main Menu:** Upbeat, chill lo-fi / jazzy vibes
- **Lobby:** Low-key anticipation music, builds as more players join
- **Gameplay:** High-energy, genre shifts per environment:
  - Sunny Meadows → Bouncy ukulele / acoustic
  - Dusty Gulch → Twangy guitar / harmonica
  - Coral Cove → Steel drums / tropical house
  - Candy Castle → Chiptune / music box
  - Cozy Villa → Jazzy / playful piano
  - Neon Nights → Synthwave / EDM
- **Results Screen:** Triumphant fanfare (winning team), comical trombone (losers)
- **Final Podium:** Grand orchestral celebration

### Sound Effects
- **Punchy, cartoonish** — every action has clear audio feedback
- Hit sounds: *boing*, *bonk*, *whomp*
- Pickups: *sparkle*, *cha-ching*, *pop*
- UI: *click*, *swoosh*, *ding*
- Crowd reactions: *gasp*, *cheer*, *ohhh* (during dramatic moments)

---

## 📣 Announcer & Game Feel

### Announcer System
An over-the-top, energetic announcer voice reacts to in-game events. Adds personality and audio feedback for big moments without any replay tech.

| Event | Announcer Line Examples |
|---|---|
| **Match Start** | *"Let's GO!"* / *"It's Minimo time!"* |
| **Flag Picked Up** | *"Flag grabbed!"* / *"Run for it!"* |
| **Flag Dropped** | *"Fumble!"* / *"It's loose!"* |
| **Big Knockback** | *"BONK!"* / *"See ya!"* / *"Ohhh!"* |
| **Score Milestone** | *"Halfway there!"* / *"Almost!"* |
| **Comeback** | *"They're catching up!"* / *"Underdog rising!"* |
| **Close Finish** | *"It's neck and neck!"* / *"Photo finish!"* |
| **Final 30 Seconds** | *"Thirty seconds!"* / *"It's now or never!"* |
| **Round Win** | *"Dominant!"* / *"That's how it's done!"* |
| **Party Win** | *"CHAMPIONS!"* / *"Minimo Legends!"* |

### Announcer Rules
- ✅ Announcer lines are **short** (1–3 words) — they punctuate, not narrate
- ✅ Volume adjustable / can be turned off in settings
- ✅ Lines are randomized from a pool per event — doesn't repeat the same line back-to-back
- ✅ Cheap to implement: just trigger audio clips on game events (no AI, no recording)

### 😤 Quick Reactions (In-Game Emotes)

During gameplay, players can quickly express reactions via a **D-pad / number key** without stopping movement:

| Input | Reaction | Visual |
|---|---|---|
| D-pad Up / 1 | 😄 Happy | Smiley face floats above head for 2s |
| D-pad Right / 2 | 😠 Angry | Angry symbol pops above head |
| D-pad Down / 3 | 😢 Sad | Tear drop effect |
| D-pad Left / 4 | 🎉 Celebrate | Small confetti burst |

- Reactions are tiny, non-disruptive floating icons — no animation lock
- 3s cooldown to prevent spam
- Reactions are visible to all players (synced via lightweight RPC)

---

## 🌐 Networking & Multiplayer

### Architecture — 100% P2P, Zero Server Costs
- **P2P with host authority** — one player's machine IS the server
- No dedicated game servers. No monthly hosting bills. No scaling headaches.
- Host manages game state, scoring, timers, spawns
- Client-side prediction for movement (responsive feel)
- Host reconciliation for combat and scoring (prevents basic cheating)
- If host disconnects → **host migration** to the next player in the lobby (Netcode supports this)

### Lobby System
- **Unity Relay** (free tier) for NAT traversal — lets players connect P2P even behind routers
- **Unity Lobby** (free tier) for room codes and matchmaking — lightweight, no game state, just connection brokering
- Room codes for private games (share with friends via Discord, text, etc.)
- Quick play matchmaking by region (via Unity Lobby tags)
- Max 12 players per session (6 teams × 2)
- 📝 Unity's free tier covers: 50 CCU for Relay, 20 req/s for Lobby — plenty for a party game launch

### Sync Strategy
| Data | Sync Method |
|---|---|
| Player position/rotation | NetworkTransform (interpolated) |
| Health / stun state | NetworkVariable |
| Score | NetworkVariable (host-authoritative) |
| Game state (timers, phases) | RPCs from host |
| Cosmetics | Synced on join via NetworkVariable |
| Item pickups | Host-authoritative RPCs |

### Anti-Cheat (Basic)
- Host validates all scoring events
- Movement speed capped server-side
- Cooldown enforcement on host
- ⚠️ Full anti-cheat is out of scope for v1 — focus on fun first

---

## 📈 Progression & Economy

### Player Level System
- XP earned by: playing matches, winning, completing challenges
- Level up → unlock cosmetic rewards at milestones
- Seasonal level track (battle pass style) — optional for v2

### Currency
| Currency | Name | Earned By | Used For |
|---|---|---|---|
| 🪙 **Soft** | Minimo Coins | Playing matches, challenges | Common cosmetics |
| 💎 **Premium** | Sparkles | Real money (optional, v2+) | Exclusive cosmetics |

### Unlockables
- **Level milestones:** Hat at Lv5, Pattern at Lv10, Emote at Lv15, etc.
- **Achievement cosmetics:** "Win 50 CTF games" → exclusive flag-themed hat
- **Daily/weekly challenges:** "Score 200 points in one match" → bonus coins

### ❌ What We Will NOT Do
- ❌ No pay-to-win — ever
- ❌ No loot boxes — direct purchase or earn
- ❌ No gameplay-altering purchases
- ❌ No energy systems or play-gating

---

## ⚙️ Technical Requirements

### Pre-Requisites
- ✅ Unity 6+ with URP
- ✅ Netcode for GameObjects 2.x
- ✅ Unity Input System (gamepad + keyboard/mouse)
- ✅ Cinemachine 3.x (dynamic gameplay cameras)
- ✅ Unity Transport 2.x
- ✅ Unity Multiplayer Services (lobby/matchmaking)
- ✅ TextMesh Pro (UI text)
- ✅ Visual Effect Graph (particles, VFX)

### Minimum Target Specs
| Component | Requirement |
|---|---|
| **OS** | Windows 10+ / macOS 12+ |
| **CPU** | Intel i5 / Ryzen 5 |
| **GPU** | GTX 1060 / RX 580 |
| **RAM** | 8 GB |
| **Network** | Broadband internet |

### Performance Targets
- **60 FPS** minimum on target specs
- **Sub-100ms latency** for responsive multiplayer
- **Low-poly approach** keeps draw calls and poly counts manageable
- **GPU instancing** for team-colored characters

---

## 🎨 Art Pipeline & Asset Strategy

### Approach
We are **not** building every 3D asset from scratch. The low-poly stylized aesthetic has a huge ecosystem of affordable/free asset packs on the Unity Asset Store. The strategy:

1. **Buy/find quality low-poly asset packs** for environments, props, and skyboxes
2. **Unify the art style** with shared shaders, color grading, and post-processing
3. **Build custom** only what must be unique: Minimo characters, team-color shaders, UI, VFX

### Asset Pack Needs (Unity Asset Store / itch.io / free)

| Category | What We Need | Notes |
|---|---|---|
| **🌿 Nature/Meadows** | Low-poly grass, flowers, trees, fences, rocks, ponds | Sunny Meadows environment base |
| **🤠 Wild West** | Saloon buildings, cacti, barrels, mine carts, mesa rocks | Dusty Gulch environment |
| **🏖️ Beach/Tropical** | Palm trees, tiki torches, beach huts, piers, water | Coral Cove environment |
| **🍬 Fantasy/Candy** | Candy-themed props (or modify nature pack with candy shaders) | Candy Castle — may need custom shader work |
| **🏠 Indoor/Furniture** | Oversized furniture, kitchen props, books, toys | Cozy Villa — "tiny character in big house" |
| **🌃 Neon/Cyberpunk** | Neon signs, holographic props, glowing platforms | Neon Nights environment |
| **☁️ Skyboxes** | Stylized/cartoonish skyboxes per theme (sunset, night, candy clouds, tropical) | Critical for mood |
| **🧱 Modular Building** | Modular low-poly building kit (walls, floors, stairs, platforms) | For arena construction and obstacle courses |
| **🎆 VFX/Particles** | Cartoon VFX pack (hit effects, explosions, sparkles, dust) | Stylized, not realistic |
| **🔊 SFX** | Cartoon sound effects pack (hits, pickups, UI sounds) | Punchy and playful |
| **🎵 Music** | Royalty-free tracks per environment mood (or commission) | See Audio section for style per map |

### Shader Unification Strategy
- All environment assets get a **shared toon/cel shader** to unify the look
- Team color shader on characters: HSV hue-shift on the body material
- Post-processing: bloom, color grading (warm/pastel LUT), subtle vignette
- Optional outline post-process for character readability

### What We Build Custom
- ✅ Minimo character model + rig + animations
- ✅ Team color shader system
- ✅ UI (all screens, HUD, menus)
- ✅ VFX for abilities (melee hit, ranged projectile, power-up activation)
- ✅ Game-specific props (flags, goals, podium, hot potato bomb)

---

## 🗓️ Development Phases

### 📦 Phase 1 — Foundation (Weeks 1–4)
- [x] Project setup (Unity + multiplayer template) ✅
- [x] Game Design Document ✅
- [ ] **Source asset packs** — find and purchase low-poly environment packs, VFX, SFX
- [ ] Core character controller (movement, jump, double jump, dodge, slide)
- [ ] Basic combat system (melee, charged melee, ranged, grab/throw)
- [ ] Team system with color assignment + team-color shader
- [ ] P2P networking (Unity Relay + Lobby, player sync, lobby join/leave, host migration)
- [ ] Power-up system (spawn crates, pickup, single-slot inventory)
- [ ] One playable mini-game: **Capture the Flags**
- [ ] One map: **Sunny Meadows** (blockout with asset pack props)
- [ ] Training Grounds tutorial scene
- [ ] Basic bot AI (filler for incomplete lobbies)

### 🎨 Phase 2 — Character & Style (Weeks 5–8)
- [ ] Minimo character model (base mesh + rig + animations)
- [ ] Team color shader (HSV hue-shift on body material)
- [ ] Shader unification pass on all asset packs (shared toon shader)
- [ ] Character customization system (hat/face/back/shoes slots)
- [ ] Character Studio scene
- [ ] URP post-processing setup (bloom, color grading, optional outlines)
- [ ] Basic UI framework (HUD, menus, lobby, settings)
- [ ] Announcer system (audio clips triggered on game events)
- [ ] Quick Reactions (D-pad emotes)

### 🎮 Phase 3 — Mini-Game Expansion (Weeks 9–14)
- [ ] King of the Hill
- [ ] Obstacle Dash
- [ ] Last Team Standing
- [ ] Minimo Ball (including 3-team triangular variant)
- [ ] Treasure Hoarder
- [ ] Mini-game selection system (random / vote / host-pick)
- [ ] Match flow (party structure, scoring, intermissions, results, podium)
- [ ] Comeback mechanics implementation
- [ ] Camera system per mini-game category (follow, arena, side-scroll, sports)

### 🗺️ Phase 4 — Maps & Content (Weeks 15–20)
- [ ] Dusty Gulch environment + hazards
- [ ] Coral Cove environment + hazards
- [ ] Candy Castle environment + hazards
- [ ] Cozy Villa environment + hazards
- [ ] Neon Nights environment + hazards
- [ ] Environmental hazards per map
- [ ] Map variants for each mini-game
- [ ] Audio implementation (music per environment + SFX + announcer lines)
- [ ] Loading screens (tips, standings, rule previews)

### ✨ Phase 5 — Polish & Launch Prep (Weeks 21–26)
- [ ] Interactive 3D main menu
- [ ] Progression system (XP, levels, coin economy, unlocks table)
- [ ] Remaining mini-games (Dodgeball, Hot Potato, Build & Defend, Climb Rush)
- [ ] Victory podium scene
- [ ] Party mutators (Big Head, Low Gravity, Turbo, etc.)
- [ ] Accessibility features (colorblind mode, HUD scale, screen shake control)
- [ ] Settings screen (controls, audio, graphics, visual, accessibility)
- [ ] Social features (Steam friends, party up, recent players)
- [ ] Bug fixing, balancing, playtesting
- [ ] Steam page, trailer, marketing assets

### 🚀 Phase 6 — Post-Launch (Ongoing)
- [ ] Seasonal content (new cosmetics, mini-games, maps)
- [ ] Battle pass system (optional)
- [ ] Community feedback integration
- [ ] Console porting investigation
- [ ] Spectator mode improvements
- [ ] New environment themes
- [ ] New mini-games based on player feedback

---

## 📝 Key Design Decisions & Open Questions

| # | Question | Current Answer | Status |
|---|---|---|---|
| 1 | P2P vs Dedicated Servers? | **P2P always.** Zero server costs. Unity Relay for NAT traversal (free tier). | ✅ Decided |
| 2 | Team size fixed at 2? | Yes for v1. Keeps maps intimate and teamwork tight | ✅ Decided |
| 3 | Cross-platform? | PC-first (Steam), console later | ✅ Decided |
| 4 | Voice chat? | Not in v1 — rely on Discord/external | 💡 Revisit |
| 5 | Replay system? | Out of scope — use stat callouts + fun stats instead (cheap, effective) | ✅ Decided |
| 6 | Map editor? | Out of scope for v1 | 💡 Revisit |
| 7 | Bot players for incomplete lobbies? | Yes — basic AI bots, 3 difficulty levels, 🤖 icon | ✅ Decided |
| 8 | Penalty for disconnects? | Soft — lose XP for that match only | ✅ Decided |
| 9 | Asset creation vs asset packs? | Buy low-poly packs, unify with shared shader. Custom only for characters + game-specific props | ✅ Decided |
| 10 | Comeback mechanics? | Subtle boosts for trailing teams (speed, spawn proximity, power-up frequency). No leader punishment. | ✅ Decided |
| 11 | Monetization model? | Free-to-play with cosmetic shop (v2+). No P2W. No loot boxes. | ✅ Decided |

---

> *"Minimos: Small characters. Big fun."* 🎮✨
