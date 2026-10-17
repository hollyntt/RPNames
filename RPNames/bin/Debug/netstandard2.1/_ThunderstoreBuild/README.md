# RPNames

Give yourself a custom, animated, and colorful roleplaying title above your name! All titles are synced with other players in multiplayer.

<img width="377" height="858" alt="image" src="https://github.com/user-attachments/assets/d6a909fe-dda6-4715-bbcb-81c295a7ab60" />

## Features
- **Fully In-Game GUI:** No more editing config files! Press **F8** (configurable) to open a stylish, animated menu and configure your title on the fly.
- **Per-Character Profiles:** Your title settings are saved for each character slot automatically.
- **Network Synced:** Other players with the mod (and CodeTalker) will see your custom title exactly as you designed it.
- **Steamworks Global Names:** RPNames uses Steam persona names to fill blank global nicknames and player labels by default. Any local name you have entered is preserved. You can turn this off in the Steamworks config section.

### Rich Customization Options:
- **Text Animations:**
    - **Static:** A simple, non-moving title.
    - **Scroll:** The title reveals itself letter by letter, then reverses.
    - **Marquee:** Your title scrolls across a set width, like a news ticker.
    - **Typewriter:** Types out the title, pauses with a blinking cursor, backspaces, and repeats.
- **Coloring Effects:**
    - **Single Color:** Pick any color using an in-game color picker or by entering a hex code.
    - **Gradient:** A smooth transition between two colors.
        - Can be static or animated to pulse between the start and end colors.
        - In-game color pickers for start and end colors.
        - Adjustable animation spread/speed.
    - **Rainbow:** The entire title cycles through the colors of the rainbow.
    - **Wave:** A beautiful rainbow wave effect that flows through the text.
    - **Static Rainbow:** Applies a fixed rainbow pattern across the text.
- **Formatting & Presets:**
    - Choose from over 10 different bracket styles (or none at all).
    - Position the title on a new line, with or without a gap.
    - Select from a list of classic preset titles to get started quickly.
- **Profile Management:**
    - **Copy/Paste:** Easily copy your current design and paste it into another character's profile.

## How to Use
1.  Once in-game with your character loaded, press the **F8** key to open the settings menu.
2.  Type your desired title in the text box at the top.
3.  Use the dropdown menus and sliders to customize the animation, coloring, and formatting.
4.  When you are happy with your title, click **"Set & Save Title"** at the bottom. Your title will be applied and saved for the current character.
5.  To remove a title, simply click the **"Clear Title"** button.

## Installation
- **Requires BepInEx.**
- Download the latest release and unzip it.
- Place `RPNames.dll` into your `BepInEx/plugins` folder.

## Dependencies
- **CodeTalker (Required for Multiplayer):** This mod relies on CodeTalker to send and receive title data between players.
    - If you are playing **single-player**, you do not need CodeTalker.
    - If you are playing **multiplayer**, every player who wants to see or show custom titles **must have both RPNames and CodeTalker installed.**
- **SteamGlobalName (Optional):** Its functionality is now integrated into RPNames. If you keep the standalone mod installed while testing, disable its Steam-name feature when enabling RPNames' integrated option.

## Configuration
The mod's configuration file is located at `BepInEx/config/RPNames.cfg`.
- **Menu Key:** You can change the key used to open the menu (`F8` by default).
- **Steamworks — AutoSetGlobalNameFromSteam:** Enabled by default. Steam persona names only fill blank global nicknames; nickname entry is left alone while its field is focused, and any local name you enter is preserved. Clearing the field allows Steam autofill again. Turning this off may affect related functionality, but RPNames will continue to work normally—whether to disable it is your choice. Updates pause during gameplay so they do not overwrite the in-game RPNames tag.
- **CharacterProfiles:** This stores all your profile data in JSON format. It is strongly recommended to **only edit your titles through the in-game menu** to avoid errors.