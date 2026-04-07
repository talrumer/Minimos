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
7. [Mini-Games](#-mini-games)
8. [Maps & Environments](#-maps--environments)
9. [Game Flow & Scenes](#-game-flow--scenes)
10. [UI/UX Design](#-uiux-design)
11. [Audio & Music](#-audio--music)
12. [Networking & Multiplayer](#-networking--multiplayer)
13. [Progression & Economy](#-progression--economy)
14. [Technical Requirements](#-technical-requirements)
15. [Development Phases](#-development-phases)

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
| **Melee Attack** | X / Left Click | Short-range punch/slap. Stuns target for 1s, slight knockback |
| **Charged Melee** | Hold X / Hold Left Click | Wind-up punch, 2s charge. Big knockback + 2s stun |
| **Ranged Attack** | Y / Right Click | Lob a projectile (changes per game mode). Slows target for 1.5s |
| **Grab** | RB / E | Grab another player or object. Can throw after grabbing |
| **Throw** | RB (while grabbing) / E | Throw grabbed player/object in aimed direction |
| **Use Item** | LB / Q | Use picked-up power-up or game-mode-specific item |

### Combat Rules
- ✅ No permanent deaths in most modes — stunned players recover
- ✅ Stun duration stacks diminishingly (1s → 1.5s → 1.7s max)
- ✅ Dodge roll grants **i-frames** (invincibility frames) for 0.3s
- ✅ Friendly fire is **OFF** by default (toggleable in custom matches)
- ✅ All attacks have clear **wind-up animations** so opponents can react

### Physics & Feel
- **Ragdoll on big hits** — characters go limp briefly before recovering
- **Bouncy collisions** — players bounce off walls and each other slightly
- **Momentum-based movement** — slight acceleration/deceleration, no instant stops
- **Screen shake** on big impacts (subtle, adjustable in settings)

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

**Power-ups (spawn on map):**
- ⚡ **Speed Boost** — 3s of 2x speed (great for flag carriers)
- 🛡️ **Shield** — Absorb one hit without dropping the flag
- 🌀 **Teleport** — Blink to a random location on the map

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
| **Teams** | 2–4 teams of 2 |
| **Duration** | 4 minutes |
| **Map Size** | Medium with separated zones |

**Rules:**
- Each team has a **build zone** with scattered blocks
- One player builds (stacks blocks to hit a height target)
- Other player defends (fights off enemies trying to knock your tower down)
- Players can switch roles at any time
- Throwing blocks at enemy towers deals damage
- First team to reach the height target wins — or tallest tower at time limit

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
| **Teams** | 2 teams of 2–4 |
| **Duration** | 3 minutes or first to 5 goals |
| **Map Size** | Small arena |

**Rules:**
- Oversized ball with exaggerated physics
- Players push the ball by running into it or punching it
- Grab + throw works on the ball too (but it's heavy, so short range)
- Goals are wide and have a fun **explosion animation** on score
- Ball resets to center after each goal
- Slide-tackling near the ball gives it a massive boost

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
| **Teams** | 2 teams of 2–6 |
| **Duration** | Best of 3 rounds |
| **Map Size** | Small divided arena |

**Rules:**
- Each side has balls that respawn periodically
- Hit = eliminated for that round
- Catch a ball mid-air = the thrower is eliminated instead
- Dodge roll is critical (i-frames!)
- Last team with a player standing wins the round
- Balls get faster each round

---

### 🎪 9. Hot Potato

> *Don't be holding it when the timer hits zero.*

| Detail | Value |
|---|---|
| **Teams** | Free-for-all (teams track score across rounds) |
| **Duration** | 5 rounds of 30s each |
| **Map Size** | Small |

**Rules:**
- One player starts with the "hot potato" (glowing bomb)
- Pass it by running into another player or throwing it
- Timer counts down from 30s (hidden for last 5s!)
- Player holding it when it explodes = eliminated that round
- Team with the most surviving members across rounds wins

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

## 🗺️ Maps & Environments

Each mini-game has **2–3 map variants** per environment theme. All environments share the same pastel, low-poly aesthetic but with distinct moods.

### 🌿 1. Sunny Meadows (Default / Nature)
- Rolling green hills, scattered flowers, wooden fences
- Small ponds with lily pads
- Trees with round, puffy canopies
- Background: soft mountains, fluffy clouds, butterflies
- **Mood:** Happy, warm, welcoming
- **Used for:** Capture the Flags, King of the Hill, Minimo Ball

### 🤠 2. Dusty Gulch (Wild West)
- Sandy terrain, cacti, tumbleweeds
- Wooden saloon buildings, water towers, mine carts on rails
- Red rock formations and mesas
- Background: desert sunset, distant canyons
- **Mood:** Adventurous, warm-toned
- **Used for:** Treasure Hoarder, Last Team Standing, Dodgeball Frenzy

### 🏖️ 3. Coral Cove (Beach / Tropical)
- Sandy beaches, palm trees, tiki torches
- Shallow turquoise water areas (wadeable, slows you down)
- Wooden piers and beach huts
- Background: ocean waves, distant islands, seagulls
- **Mood:** Relaxed, tropical, colorful
- **Used for:** Hot Potato, Capture the Flags, Obstacle Dash

### 🏰 4. Candy Castle (Fantasy / Sweets)
- Made of candy, cookies, and cake
- Gummy bear trees, lollipop lampposts, chocolate rivers
- Bouncy gelatin platforms, slippery frosting floors
- Background: cotton candy clouds, rainbow arches
- **Mood:** Whimsical, playful, sugary
- **Used for:** Build & Defend, Climb Rush, Obstacle Dash

### 🏠 5. Cozy Villa (Suburban / Indoor)
- Oversized furniture — players are tiny in a giant house
- Bookshelves as climbable walls, cushions as bouncy pads
- Kitchen counters, bathtubs, toy rooms
- Background: windows showing a backyard garden
- **Mood:** Cozy, quirky, playful scale
- **Used for:** King of the Hill, Hot Potato, Last Team Standing

### 🌙 6. Neon Nights (Cyberpunk / Night)
- Glowing neon platforms, holographic signs
- Dark background with vibrant colored lighting
- Moving conveyor belts, laser grids (instant knockback)
- Synth-wave aesthetic with pixel art billboards
- **Mood:** Energetic, futuristic, flashy
- **Used for:** Dodgeball Frenzy, Climb Rush, Minimo Ball

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

## 🌐 Networking & Multiplayer

### Architecture
- **P2P with host authority** (via Netcode for GameObjects)
- Host manages game state, scoring, timers, spawns
- Client-side prediction for movement (responsive feel)
- Server reconciliation for combat and scoring

### Lobby System
- Built on **Unity Multiplayer Services** (session management)
- Room codes for private games
- Quick play matchmaking by region
- Max 12 players per session (6 teams × 2)

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

## 🗓️ Development Phases

### 📦 Phase 1 — Foundation (Weeks 1–4)
- [x] Project setup (Unity + multiplayer template) ✅
- [ ] Core character controller (movement, jump, double jump, dodge)
- [ ] Basic combat system (melee, ranged, grab/throw)
- [ ] Team system with color assignment
- [ ] Basic networking (player sync, lobby join/leave)
- [ ] One playable mini-game: **Capture the Flags**
- [ ] One map: **Sunny Meadows** (basic blockout)

### 🎨 Phase 2 — Character & Style (Weeks 5–8)
- [ ] Minimo character model (base mesh + rig + animations)
- [ ] Team color shader (dynamic tinting)
- [ ] Character customization system (hat/face/back/shoes slots)
- [ ] Character Studio scene
- [ ] URP post-processing setup (bloom, color grading, outlines)
- [ ] Basic UI framework (HUD, menus, lobby)

### 🎮 Phase 3 — Mini-Game Expansion (Weeks 9–14)
- [ ] King of the Hill
- [ ] Obstacle Dash
- [ ] Last Team Standing
- [ ] Minimo Ball
- [ ] Treasure Hoarder
- [ ] Mini-game selection / voting system
- [ ] Match flow (intro → gameplay → results → next round)

### 🗺️ Phase 4 — Maps & Polish (Weeks 15–20)
- [ ] Dusty Gulch environment
- [ ] Coral Cove environment
- [ ] Candy Castle environment
- [ ] Cozy Villa environment
- [ ] Neon Nights environment
- [ ] Map variants for each mini-game
- [ ] Audio implementation (music + SFX)

### ✨ Phase 5 — Polish & Launch Prep (Weeks 21–26)
- [ ] Interactive 3D main menu
- [ ] Progression system (XP, levels, unlocks)
- [ ] Remaining mini-games (Dodgeball, Hot Potato, Build & Defend, Climb Rush)
- [ ] Accessibility features
- [ ] Bug fixing, balancing, playtesting
- [ ] Steam page, trailer, marketing assets

### 🚀 Phase 6 — Post-Launch (Ongoing)
- [ ] Seasonal content (new cosmetics, mini-games, maps)
- [ ] Battle pass system (optional)
- [ ] Community feedback integration
- [ ] Console porting investigation
- [ ] Spectator mode
- [ ] Custom game modes editor

---

## 📝 Key Design Decisions & Open Questions

| # | Question | Current Answer | Status |
|---|---|---|---|
| 1 | P2P vs Dedicated Servers? | P2P for v1 (lower cost), revisit if needed | ✅ Decided |
| 2 | Team size fixed at 2? | Yes for v1. Keeps maps intimate and teamwork tight | ✅ Decided |
| 3 | Cross-platform? | PC-first, console later | ✅ Decided |
| 4 | Voice chat? | Not in v1 — rely on Discord/external | 💡 Revisit |
| 5 | Replay system? | Out of scope for v1 | 💡 Revisit |
| 6 | Map editor? | Out of scope for v1 | 💡 Revisit |
| 7 | Bot players for incomplete lobbies? | Yes — basic AI bots to fill teams | 📝 TODO |
| 8 | Penalty for disconnects? | Soft penalty — lose XP for that match | 📝 TODO |

---

> *"Minimos: Small characters. Big fun."* 🎮✨
