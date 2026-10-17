## Changelog
### 1.5.0 - x.x.2
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