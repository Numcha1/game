# SEASONS TOGETHER — FIX INDOOR SCALE, UI SIZE, AND VISUAL HIERARCHY

Repository: https://github.com/Numcha1/game
Branch: feature/premium-game-systems
Engine: Godot 4 .NET
Language: C#

You are acting as a Senior Godot UI Engineer, Technical Artist, UX Designer, and Gameplay Presentation Engineer.

## OBJECTIVE

Refine the indoor presentation of Seasons Together so that the scene feels visually balanced, smooth, readable, and cohesive.

The current indoor shop scene has scale and hierarchy problems:
- The player and NPC sprites appear too small relative to the room.
- The indoor camera shows too much empty floor space.
- The top HUD panels and buttons feel oversized and visually heavy.
- The interaction/help text is too large and duplicated.
- Visual hierarchy is weak, causing attention to go to UI instead of the player, NPC, and interaction target.

Do not redesign the entire art style.
Do not rebuild the whole UI from scratch unless necessary.
Focus on improving scale, layout, readability, and smoothness.

---

## TASKS

### 1. AUDIT THE CURRENT INDOOR SHOP SCENE
Inspect the current indoor shop implementation and identify:
- Player scale
- NPC scale
- Furniture/environment scale
- Camera zoom
- HUD size
- Button sizing
- Text sizing
- Prompt placement
- Safe area spacing
- Visual hierarchy problems

Use the current playable scene as the source of truth.

---

### 2. FIX CHARACTER-TO-ENVIRONMENT SCALE
Adjust the indoor presentation so the player and NPC feel properly sized relative to the room and furniture.

Goals:
- Increase player and NPC visual readability indoors.
- Avoid making the environment feel excessively oversized.
- Make interactions feel closer and more natural.

Preferred approach:
- Slightly increase indoor character presentation scale where appropriate.
- Combine this with a better indoor camera zoom rather than radically resizing all furniture art.

Target:
- Indoor character presence should feel approximately 20–35% larger than the current presentation, if supported by the assets and animation system.

Do not break collision, interaction distances, or animation alignment.

---

### 3. FIX INDOOR CAMERA ZOOM
Tune indoor camera behavior separately from outdoor camera behavior.

Goals:
- Reduce excessive visible empty floor area.
- Make the room feel cozy and intentional.
- Improve focus on the player, NPC, and important interaction points.

Recommended direction:
- Add a dedicated indoor camera zoom profile.
- Start with approximately 1.15x to 1.35x indoor zoom and adjust based on actual scene layout.

Make sure:
- Camera remains stable.
- No edge glitches occur.
- The player remains visible and centered appropriately.
- The camera still supports movement across the full room.

---

### 4. REDUCE HUD BULK AND IMPROVE TOP BAR LAYOUT
The current top-left status panel and top-right buttons are too large and visually heavy.

Refine:
- Top bar height
- Internal padding
- Panel spacing
- Font sizes
- Button sizing
- Safe area margins

Requirements:
- Reduce overall HUD bulk by approximately 15–25% if appropriate.
- Keep the UI readable in Thai.
- Make the top information layout feel compact and elegant.
- Ensure all top buttons use a consistent height, padding, and visual style.

The following should feel like one cohesive system:
- Inventory button
- Shop button
- Menu button

Do not allow one of these buttons to look disproportionately larger or smaller than the others.

---

### 5. FIX INTERACTION PROMPTS
The current interaction/help text is too large and duplicated.

Replace it with a cleaner contextual prompt system.

Requirements:
- Show only the most relevant prompt at the current moment.
- Avoid multiple large stacked messages in the center of the screen.
- Use one subtle prompt zone near the bottom center or another consistent UI-safe location.
- Reduce prompt font size and visual weight.
- Support prompts such as:
  - “F คุยกับมิรา”
  - “F ใช้ประตู”
  - “Esc เมนู”

Prompt rules:
- Show prompt only when relevant.
- Hide prompt when no interaction target is active.
- Prevent duplicate prompt messages.
- Keep prompt readable without dominating the screen.

---

### 6. ESTABLISH A SCALE AND SPACING SYSTEM
Define a reusable presentation system for indoor scenes.

Create or refine consistent design tokens for:
- HUD panel height
- Button height
- Padding
- Gaps
- Font sizes
- Prompt size
- Safe area margins
- Indoor camera zoom
- Indoor character scale rules

Prefer an 8 px or 16 px spacing logic where appropriate.

Document the rules briefly so future indoor scenes remain visually consistent.

---

### 7. TESTING
Test at minimum:
- 1280x720
- 1920x1080

Verify:
- Indoor readability
- Thai text readability
- Prompt clarity
- Camera stability
- No clipping
- No broken collision
- No broken interaction triggers
- No UI overlap
- No animation misalignment after scaling changes

Use actual in-game scene verification.
Capture before/after screenshots if possible.

---

## CONSTRAINTS
- Preserve existing gameplay behavior.
- Preserve save compatibility.
- Do not break indoor interactions.
- Do not break farming controls.
- Do not overcomplicate the architecture.
- Do not claim success without testing the real scene.

---

## DELIVERABLES
After implementing, report:
1. Files changed
2. Indoor scale adjustments made
3. Camera changes made
4. UI size/layout changes made
5. Prompt system improvements
6. Test results
7. Remaining visual issues

Start by auditing the current indoor shop scene, then implement the indoor scale and UI hierarchy improvements first.