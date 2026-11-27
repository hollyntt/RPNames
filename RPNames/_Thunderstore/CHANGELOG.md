## Changelog
### 1.6.7 - x.x.x
**Improved:**
- The glyph fix from 1.6.6 now also NFKC-normalizes Steam names before stripping: fullwidth/halfwidth compatibility characters fold to the ordinary glyphs the game font ships (U+FF13 ３ → "3", ideographic space U+3000 → space, …). A name like "s0apy３" reads "s0apy3" instead of silently losing the "3".

### 1.6.6 - x.x.x
**Fixed:**
- Stopped the "The character with Unicode value … was not found in the terminal-grotesque font asset" console spam for anyone whose Steam persona name (or a lobby-mate's, via the global-nickname labels) contains characters the game's font chain can't draw. RPNames now trims such characters out of Steam names *before* they're written into the nickname field / player labels — the label only ever sees text it can draw, so TMP never logs the missing-glyph warning again.
  - The supported charset is built from the nickname labels' font + fallback chain (each font's serialized character table, re-checked every 5s until found).
  - When no label is up yet the charset is unknown and no stripping happens (a slightly wrong name is never worse than a broken one); the existing 1s refresh rewrites the nickname once the charset is known.
  - Surrogate pairs (e.g. emoji in a persona name) are kept only when the font chain can actually render them.

### 1.6.5 - x.x.x
**Security:**
- Closed the fail-open owner gate on full-sync (found on a decomp of a public tool named "plvwsmvwvre"). When the lobby-owner lookup couldn't run (lobby handle not yet valid, torn-down window, post-kick), 1.6.1–1.6.4 fell through to applying the payload from any sender; now those packets are dropped instead. Legit full-syncs are on-demand, so the requester simply re-asks once the lobby is up — no permanent loss.

### 1.6.4 - x.x.x
**Build / Compatibility:**
- Migrated off the two CodeYapper APIs the 2.2.4 update marked obsolete (`RegisterBinaryListener` -> `RegisterListener`, `SendNetworkPacket(x)` -> `SendNetworkPacket(x, default)`). Behavior is unchanged: the `default` overload still sends uncompressed, and the new `RegisterListener<T>` overload takes the same handler signature the old one did. This clears the obsolete-call warnings the update introduced and keeps the build clean under warnings-as-errors / IDE escalation settings.
- Cleaned up the remaining nullable warnings the 1.6.3 `CodeYapperWorkaround` guard introduced, returning the project to its pre-update warning baseline. No functional changes.

### 1.6.3 - x.x.x
**Security / Stability:**
- Guarded the silent force-close that started with the CodeYapper 2.2.4 update. That update ships a broken `PacketWrapper.PacketBytes` getter that reads itself and stack-overflows (no crash log, no popup) whenever a GZip/LZ4/ZStd-compressed packet is *received* — so an exploiter can just send one compressed packet to kill the host. RPNames now installs a version-gated guard on that getter that pre-decompresses and stops the recursion, so receiving a compressed packet degrades to a dropped packet instead of a silent game-closing.
  - Targets only the broken 2.2.4 build; it's a no-op on 2.2.3 and on any future CodeYapper version that Soggy ships with the real fix, so it won't fight the official fix.
  - Also stops RPNames from ever sending compressed packets (full-sync replies are now uncompressed), which was the other trigger.

### 1.6.2 - x.x.x
**Security:**
- More network hardening against exploiters:
  - Malformed/truncated packets (garbage string lengths, cut-off bodies, a `0xFFFFFFFF` profile count) are now dropped with a rate-limited warning instead of throwing inside the network pipeline.
  - Network animation/color floats are now validated: NaN/Inf and out-of-range values from other players fall back to the defaults instead of making the per-frame loop spin or garble colors.
  - The global nickname part of the HUD name label is now stripped of rich-text tags, same as Title/Pronouns.

### 1.6.1 - x.x.x
**Fixed:**
- Full-sync replies are no longer GZip-compressed. A 2.2.4 Codeyapper bug makes receiving a compressed binary packet stack-overflow (its lazy-decompress getter re-enters itself), which took the game down on lobby join. Sending uncompressed keeps the reply safe on every Codeyapper version.

**Security:**
- Hardened the network input path so a lobby mate can't mess with your game:
  - Title/Pronoun text from other players is now stripped of TMP rich-text tags (and capped at 64 chars), so nobody can push `<size=...>`/`<font=...>` or similar into your HUD name labels.
  - Hex color fields from the network are validated; garbage falls back to a safe default instead of being able to break out of the color tag.
  - `MarqueeWidth` is clamped (1–64) and zero/negative animation speeds fall back to the default — a malicious value used to throw an exception every frame (and could allocate a multi-GB string).
  - Network animation/color floats are now validated: NaN/Inf and out-of-range values from other players fall back to the defaults instead of making the per-frame loop spin or garble colors.
  - Malformed/truncated packets (garbage string lengths, cut-off bodies, a `0xFFFFFFFF` profile count) are dropped with a rate-limited warning instead of throwing inside the network pipeline.
  - Full-sync payloads: the profile count is capped at 512 and only entries for players actually in the lobby are applied, and a full-sync is only accepted from the lobby owner — so a fake full-sync can't spoof or erase other players' titles or get echoed back lobby-wide by the host.

### 1.6.0 - x.x.x
**Added:**
- Titles are now saved per character through Multitool, so they follow the character instead of a slot number in the config. Multitool is a new dependency.
- EasySettings support: the Steam toggle and the menu key are on the main Mods page. EasySettings is optional.
- "Import old title from config" button in the settings menu, shown for slots that still have a title in the old config.

**Fixed:**
- Sharing a mod profile or config no longer gives other people your titles on their characters.
- The Steam nickname bookkeeping is no longer written to the config, so a shipped config can't overwrite anyone's nickname state.
- The cfg no longer fills up with every character's profile JSON.

**Updated:**
- `CharacterProfiles` in the config is now legacy-only and is cleared per slot as old titles are imported.
- Without Multitool installed, titles fall back to the old config storage and a warning is logged.

- **SGN:** Steam ID in the state: the nickname state now records which Steam account filled the name, and that ID is saved along with the rest of the state.
  Filled in by the mod: if the field still holds the name RPNames put there and it differs from the current Steam name, it’s replaced with the current account’s name. An account switch logs “Steam account changed”. The same path handles someone renaming their Steam persona.
  Typed by the player: if they entered their own nickname, it’s left alone, and the old bookkeeping is cleared as before. That custom nickname stays when they switch accounts.
  Toggle off: still restores the original nickname.

### 1.5.0
**Added:**
- Added a `Steamworks` config toggle to use Steam persona names for the global nickname and player labels. It is enabled by default and can be disabled to use the local nickname.

**Fixed:**
- SteamGlobalName is no longer a dependency; its functionality is integrated into RPNames.
- Steam persona updates are paused during gameplay so they cannot overwrite RPNames' in-game tag.
- Existing local global nicknames are now preserved; Steam names only fill blank nicknames.
- Steam autofill waits while the local nickname field is focused, allowing a local name to be entered without racing the Steam name update.
- Mod screenshot for Thunderstore

**Updated:**
- The look of the icon is more better than before, I gave it blue instead of green - Matching the UI in-game. Ui design my beloved

### 1.4.7
**Fixed:**
- Game would lock up at spawn if launched modded without Steam initialized. RPNames' networking calls would throw repeatedly (once per frame, forever) when Steamworks wasn't available, instead of failing gracefully — this now logs a single warning and disables networked titles for that session instead of hammering the game with exceptions. (Bug found by Xesy)

### 1.4.6
**Fixed:**
- Tags for everyone already on the server could disappear and never come back if you disconnected and reconnected without fully restarting the game (dedicated servers only). The full sync request now also fires off the `IN_GAME` condition change instead of relying solely on gaining player authority, with a backup poll check as well, so a reconnect can't slip through uncaught anymore.
- Stale profile data no longer lingers forever for players who've already left — a player's entry is now cleaned up the moment their object is destroyed instead of sitting around as dead weight. (Initial version of this fix relied on a Harmony patch that may not have actually applied — reworked it to use a small marker component instead so it's guaranteed to fire.)

**Debug:**
- Added logging around the full-sync request/response chain so a failure here actually shows up in the log instead of just... nothing.

### 1.4.x - x.x.5
**Fixed:**
- The scroll position variable was being reset to 0 every frame inside the GUI function. I moved it to a class-level variable so it remembers where you scrolled.
- "hex color sliders are syncing with others." bug.
- Changelog error

**Added:**
- Added Confirmation to "Clear All": Clicking "Clear All" now swaps the button to a "Confirm Clear? [Yes] / [No]" prompt to prevent accidents.
- Custom Presets:
  Added a "Save as Preset" button next to the Title text field.
  Added a "Delete" (X) button next to custom presets in the list.
  These save to the config file so they persist between restarts.

### 1.4.3
- **Fixed:** Implemented proper listener cleanup to prevent registration errors when re-hosting a lobby in the same game session.
- **Fixed:** Corrected a critical issue where network listeners were being registered multiple times, causing errors and preventing title synchronization when joining or re-hosting a lobby. All listeners are now correctly registered only once at startup.

### 1.4.2
- **Fixed Again:** Corrected an issue where network listeners were being registered multiple times...?

### 1.4.1
- **Dependencies:** Codeyapper updated to -> 2.2.0

### 1.4.0
- **Dependencies:** Codeyapper updated to -> 2.1.0 (Tested on experimental branch  (v2.2.x))
- **Fixed:** Corrected an issue where network listeners were being registered multiple times, causing errors and preventing title synchronization when joining a lobby. Listeners are now correctly registered only once when the client connects.

## 1.3.2 - x.x.8
- **UI:** Title/Pronoun limit is now 32 -> 50 Chars max.
- Changed pictures on Readme.
- **Fixed:** Late-Join Sync issue.
- **Fixed:** 3 found Codetalker issues.
- **Codetalker:** Update yo (Added what soggy asked to add.. Compression and Decompression).

### 1.3 - x.x.1
- **Feature:** Added a dedicated Pronouns system. You can now add pronouns and select a bracket style for them in the settings menu, separate from the main title.
- **Fixed:** Resolved a critical desynchronization bug that caused player titles to randomly not appear for other players. The fix ensures title visibility is consistent for everyone in the lobby.
- **UI:** Minor layout improvements to the settings menu for clarity.
- **Feature:** Added full customization for pronouns. They can now have their own unique coloring, gradients, and animated effects, independent of the main title.
- **Feature:** Added a "Share Title Coloring" option for pronouns for easy color matching.
- **UI:** The settings menu has been reorganized with collapsible sections and a scrollbar to accommodate the new options without clutter.
- **Fixed:** (NOT RLLY A FIX I ONLY JUST TOUCHED THE README A LITTLE BIT YOU CAN IGNORE THIS LINE)

### 1.2.2
- **Fixed:** Resolved a bug that caused player titles to disappear for the host when changing maps.
- **Fixed:** Resolved a bug that caused the animation titles going crazy if mixed with rainbow/gradient animations.

### 1.2.1
- **Fixed:** Resolved a critical bug that caused the game to hang on the character screen when leaving one server and joining another. The mod's network state is now correctly reset on disconnect.
- **Fixed:** The mod's custom UI style no longer "bleeds" into the main menu or other UIs after disconnecting from a server.
- **Fixed:** Titles now display correctly for players who do not have a `@GlobalNickname` enabled.
- **Fixed:** Local player's title color now loads correctly on entering the game, instead of appearing black until re-saved.
- **Added:** A "Color Speed" slider to control the animation speed for Rainbow, Wave, and animated Gradient effects.

### 1.2.0
- **Massive Network Overhaul:** Replaced all network packets with highly efficient binary serialization to eliminate JSON overhead and drastically reduce packet size.
- **Bundled Syncing & Client-Side Animations:** Reworked the entire networking model to eliminate packet spam and reduce traffic on player join.
- **Added Animation Speed Slider:** You can now control the speed of all text animations.

### 1.0.0
- Initial release.