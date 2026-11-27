// ============== USING STATEMENTS ==============
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using CodeTalker;
using CodeTalker.Networking;
using CodeTalker.Packets;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Mirror; 
using Nessie.ATLYSS.EasySettings;

// ============== NAMESPACE DEFINITIONS ==============
namespace RPNames
{
    public class CharacterTitleProfile
    {
        [JsonIgnore] public uint TargetNetID { get; set; }
        public string Title { get; set; } = "";
        public BracketType BracketStyle { get; set; } = BracketType.Parentheses;
        public TextAnimationType TextAnimation { get; set; } = TextAnimationType.Static;
        public ColoringType Coloring { get; set; } = ColoringType.None;
        public float AnimationSpeed { get; set; } = 0.25f;
        public int MarqueeWidth { get; set; } = 16;
        public string SingleHexColor { get; set; } = "FFFFFF";
        public string GradientStartColor { get; set; } = "FF0000";
        public string GradientEndColor { get; set; } = "0000FF";
        public bool AnimateGradient { get; set; } = false;
        public float GradientSpread { get; set; } = 10f;
        public float RainbowWaveSpread { get; set; } = 15f;
        public float ColorAnimationSpeed { get; set; } = 1f;
        public string Pronouns { get; set; } = "";
        public bool ShowPronouns { get; set; } = false;
        public BracketType PronounBracketStyle { get; set; } = BracketType.Parentheses;
        public bool ShareTitleColoring { get; set; } = true;
        public ColoringType PronounColoring { get; set; } = ColoringType.None;
        public string PronounSingleHexColor { get; set; } = "FFFFFF";
        public string PronounGradientStartColor { get; set; } = "FF0000";
        public string PronounGradientEndColor { get; set; } = "0000FF";
        public bool PronounAnimateGradient { get; set; } = false;
        public float PronounGradientSpread { get; set; } = 10f;
        public float PronounRainbowWaveSpread { get; set; } = 15f;
        public float PronounColorAnimationSpeed { get; set; } = 1f;
        public bool TitleOnNewLine { get; set; } = true;
        public bool AddGapAboveTitle { get; set; } = true;
    }

    // Steam nickname bookkeeping. One global copy, but it's written into every character save
    // (via Multitool) so it follows the player's saves instead of a shared config. Newest Stamp wins.
    public class SteamNameState
    {
        public string Original = "";
        public bool HasOriginal;
        public string Applied = "";
        public List<string> AutoNames = new List<string>();
        public ulong SteamId;
        public long Stamp;
    }

    // Everything RPNames keeps inside a character save
    public class CharacterSaveData
    {
        public CharacterTitleProfile? Title;
        public SteamNameState? Steam;
    }

    public enum TextAnimationType { Static, Scroll, Marquee, Typewriter }
    public enum ColoringType { None, SingleColor, Rainbow, Gradient, Wave, StaticRainbow }
    public enum BracketType { None, Parentheses, SquareBrackets, Tilde, Dash, Plus, Equals, Asterisk, Dollar, Hash, Exclamation, Pipe }

    internal class PlayerTitleAnimator
    {
        public uint NetId;
        public float AnimationTimer = 0f;
        public int AnimationIndex = 0;
        public bool IsAnimatingForward = true;
        public float RainbowHue = 0f;
        public Main.TypewriterState TypewriterState = Main.TypewriterState.Typing;
        public float TypewriterPauseTimer = 0f;
        public bool TypewriterCursorVisible = true;
        public float TypewriterBlinkTimer = 0f;
        public PlayerTitleAnimator(uint netId) { NetId = netId; }
    }

    namespace Packets
    {
        internal static class ProfileSerializer
        {
            public static void WriteProfile(BinaryWriter writer, CharacterTitleProfile profile)
            {
                if (profile == null) { writer.Write(false); return; }
                writer.Write(true);
                writer.Write(profile.Title ?? ""); writer.Write((byte)profile.BracketStyle); writer.Write((byte)profile.TextAnimation); writer.Write((byte)profile.Coloring); writer.Write(profile.AnimationSpeed); writer.Write(profile.MarqueeWidth); writer.Write(profile.SingleHexColor ?? "FFFFFF"); writer.Write(profile.GradientStartColor ?? "FF0000"); writer.Write(profile.GradientEndColor ?? "0000FF"); writer.Write(profile.AnimateGradient); writer.Write(profile.GradientSpread); writer.Write(profile.RainbowWaveSpread); writer.Write(profile.ColorAnimationSpeed);
                writer.Write(profile.Pronouns ?? ""); writer.Write(profile.ShowPronouns); writer.Write((byte)profile.PronounBracketStyle); writer.Write(profile.ShareTitleColoring); writer.Write((byte)profile.PronounColoring); writer.Write(profile.PronounSingleHexColor ?? "FFFFFF"); writer.Write(profile.PronounGradientStartColor ?? "FF0000"); writer.Write(profile.PronounGradientEndColor ?? "0000FF"); writer.Write(profile.PronounAnimateGradient); writer.Write(profile.PronounGradientSpread); writer.Write(profile.PronounRainbowWaveSpread); writer.Write(profile.PronounColorAnimationSpeed);
                writer.Write(profile.TitleOnNewLine); writer.Write(profile.AddGapAboveTitle);
            }

            public static CharacterTitleProfile ReadProfile(BinaryReader reader)
            {
                if (!reader.ReadBoolean()) return null;
                return new CharacterTitleProfile {
                    Title = reader.ReadString(), BracketStyle = (BracketType)reader.ReadByte(), TextAnimation = (TextAnimationType)reader.ReadByte(), Coloring = (ColoringType)reader.ReadByte(), AnimationSpeed = reader.ReadSingle(), MarqueeWidth = reader.ReadInt32(), SingleHexColor = reader.ReadString(), GradientStartColor = reader.ReadString(), GradientEndColor = reader.ReadString(), AnimateGradient = reader.ReadBoolean(), GradientSpread = reader.ReadSingle(), RainbowWaveSpread = reader.ReadSingle(), ColorAnimationSpeed = reader.ReadSingle(),
                    Pronouns = reader.ReadString(), ShowPronouns = reader.ReadBoolean(), PronounBracketStyle = (BracketType)reader.ReadByte(), ShareTitleColoring = reader.ReadBoolean(), PronounColoring = (ColoringType)reader.ReadByte(), PronounSingleHexColor = reader.ReadString(), PronounGradientStartColor = reader.ReadString(), PronounGradientEndColor = reader.ReadString(), PronounAnimateGradient = reader.ReadBoolean(), PronounGradientSpread = reader.ReadSingle(), PronounRainbowWaveSpread = reader.ReadSingle(), PronounColorAnimationSpeed = reader.ReadSingle(),
                    TitleOnNewLine = reader.ReadBoolean(), AddGapAboveTitle = reader.ReadBoolean()
                };
            }
        }

        public class UpdateTitleProfilePacket : BinaryPacketBase
        {
            public override string PacketSignature => ModInfo.GUID + "_UpdateProfile";
            public CharacterTitleProfile Profile { get; set; }
            public UpdateTitleProfilePacket() { }
            public UpdateTitleProfilePacket(CharacterTitleProfile profile) { Profile = profile; }

            public override byte[] Serialize() { using (var ms = new MemoryStream()) { using (var writer = new BinaryWriter(ms)) { ProfileSerializer.WriteProfile(writer, Profile); } return ms.ToArray(); } }

            // Malformed bodies (truncated fields, garbage string lengths) must never reach the
            // game: drop the packet, and rate-limit the warning so a spammer can't flood the log.
            public override void Deserialize(byte[] data)
            {
                try
                {
                    using (var ms = new MemoryStream(data))
                    using (var reader = new BinaryReader(ms))
                        Profile = ProfileSerializer.ReadProfile(reader);
                }
                catch
                {
                    Profile = null;
                    MalformedPacketLogger.Warn("UpdateTitleProfile");
                }
            }
        }

        internal static class MalformedPacketLogger
        {
            private static DateTime _lastWarn = DateTime.MinValue;
            internal static void Warn(string packetName)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastWarn).TotalSeconds < 10) return;
                _lastWarn = now;
                Main.Log.LogWarning($"[RPNames] Dropped a malformed {packetName} packet from the network.");
            }
        }

        public class FullSyncPacket : BinaryPacketBase
        {
            public override string PacketSignature => ModInfo.GUID + "_FullSync";
            public Dictionary<uint, CharacterTitleProfile> AllProfiles { get; set; }
            public FullSyncPacket() { }
            public FullSyncPacket(Dictionary<uint, CharacterTitleProfile> profiles) { AllProfiles = profiles; }

            public override byte[] Serialize() { using (var ms = new MemoryStream()) { using (var writer = new BinaryWriter(ms)) { writer.Write(AllProfiles.Count); foreach (var entry in AllProfiles) { writer.Write(entry.Key); ProfileSerializer.WriteProfile(writer, entry.Value); } } return ms.ToArray(); } }
            // count comes from the sender: a negative value used to throw in the Dictionary
            // constructor and duplicate keys threw on Add, both inside the receive pipeline.
            // Cap it at a realistic lobby size, last-write-wins on duplicates, and drop the
            // whole packet if the body is truncated (a 0xFFFF 0xFFFF 0xFFFF 0xFFFF count with
            // one real entry used to throw EndOfStreamException mid-loop).
            private const int MaxProfiles = 512;
            public override void Deserialize(byte[] data)
            {
                try
                {
                    using var ms = new MemoryStream(data);
                    using var reader = new BinaryReader(ms);
                    int count = reader.ReadInt32();
                    var profiles = new Dictionary<uint, CharacterTitleProfile>();
                    for (int i = 0; i < count && i < MaxProfiles; i++)
                    {
                        uint netId = reader.ReadUInt32();
                        profiles[netId] = ProfileSerializer.ReadProfile(reader);
                    }
                    AllProfiles = profiles;
                }
                catch
                {
                    AllProfiles = null;
                    MalformedPacketLogger.Warn("FullSyncPacket");
                }
            }
        }

        public class RequestAllTitlesPacket : PacketBase { public override string PacketSourceGUID => ModInfo.GUID; }
    }

    [BepInPlugin(ModInfo.GUID, ModInfo.NAME, ModInfo.VERSION)]
    [BepInDependency("CodeTalker", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Marioalexsan.Multitool", BepInDependency.DependencyFlags.SoftDependency)]
    public class Main : BaseUnityPlugin
    {
        public static Main instance;
        internal static ManualLogSource Log;
        internal static bool IsReady = false;
        
        private static ConfigEntry<string> _characterProfilesJson;
        // CUSTOM PRESETS CONFIG
        private static ConfigEntry<string> _customPresetsJson;
        private ConfigEntry<bool> _autoSetGlobalNameFromSteam = null!;
        private SteamNameState _steamState = new SteamNameState();
        // Legacy titles still sitting in the cfg (read-only, imported on request)
        private Dictionary<int, CharacterTitleProfile> _legacyProfiles = new Dictionary<int, CharacterTitleProfile>();
        internal static bool UsingMultitool;
        private readonly Dictionary<Player, string> _originalGlobalNicknames = new Dictionary<Player, string>();
        private readonly Dictionary<Player, string> _appliedSteamPlayerNicknames = new Dictionary<Player, string>();
        private readonly HashSet<ulong> _requestedSteamUsers = new HashSet<ulong>();
        private float _globalNameRefreshTimer;
        private bool _steamNameUnavailableWarningLogged;
        
        internal Dictionary<int, CharacterTitleProfile> AllCharacterProfiles = new Dictionary<int, CharacterTitleProfile>();
        internal static int CurrentCharacterSlot = -1;
        
        private ConfigEntry<KeyCode> _menuKey;

        private Rect _windowRect = new Rect(20, 20, 360, 850);
        private CharacterTitleProfile _uiEditingProfile = new CharacterTitleProfile();
        private bool _showTextAnimPicker, _showColoringPicker, _showBracketPicker, _showPronounBracketPicker, _showPresetPicker, _showPronounColoringPicker;
        private Vector2 _scrollPosition = Vector2.zero;
        
        private Vector2 _presetScrollPosition = Vector2.zero; 
        
        // FIX: Confirmation flag for Clear All
        private bool _showClearConfirmation = false;
        // FIX: Name buffer for saving presets
        private string _newPresetName = "My New Style";

        internal static bool _isCodeTalkerLoaded = false;
        
        private static CharacterTitleProfile _copiedProfileBuffer = null;
        
        internal static Dictionary<uint, PlayerTitleAnimator> AllPlayerAnimators = new Dictionary<uint, PlayerTitleAnimator>();
        private Color _gradientStartColorCache, _gradientEndColorCache;
        private static readonly List<string> _presetTitles = new List<string>();
        
        // UPDATED: Custom Presets is now a Dictionary to store the FULL profile
        private static Dictionary<string, CharacterTitleProfile> _customPresets = new Dictionary<string, CharacterTitleProfile>();
        
        internal enum TypewriterState { Typing, Blinking, Backspacing }

        private bool _showSingleColorPicker, _showGradientStartPicker, _showGradientEndPicker;
        private bool _showPronounSingleColorPicker, _showPronounGradientStartPicker, _showPronounGradientEndPicker;
        
        // FIX: Removed global shared color variable to fix syncing bug
        // private Color _uiEditingColor; 
        
        private enum MenuState { Closed, Opening, Open, Closing }
        private MenuState _menuState = MenuState.Closed;
        private float _animationProgress = 0f;
        private const float AnimationDuration = 0.25f;
        private bool _stylesInitialized = false;
        private GUISkin _modSkin;
        public static GUISkin OriginalSkin;

        private void Awake()
        {
            instance = this;
            Log = Logger;
            _menuKey = Config.Bind("1. General", "Menu Key", KeyCode.F8, "The key to press to show/hide the settings menu.");
            _autoSetGlobalNameFromSteam = Config.Bind("Steamworks", "AutoSetGlobalNameFromSteam", true, "Use Steam persona names for the local global nickname and player name labels. Disabling may affect related functionality, but RPNames will continue to work; the choice is yours.");
            _characterProfilesJson = Config.Bind("2. Data", "CharacterProfiles", "{}", "Legacy storage. With Multitool installed, titles are saved with each character instead and this is only kept so old titles can be imported.");
            _customPresetsJson = Config.Bind("2. Data", "CustomPresetsFull", "{}", "Stores user-defined full title profiles (style presets).");

            UsingMultitool = TryRegisterMultitool();
            if (UsingMultitool) LoadLegacyProfiles(); else LoadProfiles();
            LoadSteamState();
            LoadCustomPresets(); 
            PopulatePresetTitles();
            
            if (Type.GetType("CodeTalker.Networking.CodeTalkerNetwork, CodeTalker") != null)
            {
                _isCodeTalkerLoaded = true;
                CodeTalkerNetwork.RegisterListener<Packets.UpdateTitleProfilePacket>(OnProfileUpdate);
                CodeTalkerNetwork.RegisterListener<Packets.FullSyncPacket>(OnFullSync);
                CodeTalkerNetwork.RegisterListener<Packets.RequestAllTitlesPacket>(OnSyncRequest);
            }
            // EasySettings is optional: if the dll is missing the call throws here and we just skip the page
            try { InitEasySettings(); }
            catch (Exception ex) { Log.LogInfo($"[RPNames] EasySettings not available, skipping the settings page ({ex.GetType().Name})"); }
            var harmony = Harmony.CreateAndPatchAll(typeof(HarmonyPatches));
            // CodeYapper 2.2.4's broken lazy-decompress getter stack-overflows on compressed
            // packets; while the library is still in that shape, replace it with a working one.
            if (_isCodeTalkerLoaded)
                CodeYapperWorkaround.Apply(harmony);
            Log.LogInfo($"[{ModInfo.NAME} v{ModInfo.VERSION}] has loaded.");
        }

        internal void QueueApplySteamGlobalName(CharacterSelectManager? characterSelect)
        {
            if (!_autoSetGlobalNameFromSteam.Value && !_steamState.HasOriginal)
                return;

            StartCoroutine(ApplySteamGlobalNameAfterMenuUpdate(characterSelect));
        }

        private IEnumerator ApplySteamGlobalNameAfterMenuUpdate(CharacterSelectManager? characterSelect)
        {
            yield return null;
            yield return new WaitForSeconds(0.25f);

            if (IsGameUiInGame())
                yield break;

            if (characterSelect == null)
            {
                characterSelect = UnityEngine.Object.FindObjectOfType<CharacterSelectManager>();
                if (characterSelect == null)
                {
                    Log.LogWarning("[RPNames] CharacterSelectManager not found while applying Steam global name.");
                    yield break;
                }
            }

            InputField input = characterSelect._globalNickanmeInput;
            if (input == null)
            {
                Log.LogWarning("[RPNames] Character select global nickname input is not available.");
                yield break;
            }

            Log.LogInfo($"[RPNames] Nickname check: field='{input.text}', applied='{_steamState.Applied}', steam='{GetLocalSteamName()}'");

            if (IsPreservingLocalNicknameInput(input))
                yield break;

            if (!_autoSetGlobalNameFromSteam.Value)
            {
                if (_steamState.HasOriginal)
                {
                    if (input.text == _steamState.Applied)
                        SetGlobalNicknameInput(input, _steamState.Original);

                    ResetSteamState();
                }
                yield break;
            }

            string? steamName = GetLocalSteamName();
            if (string.IsNullOrWhiteSpace(steamName))
            {
                LogSteamNameUnavailableOnce("Steam persona name is unavailable; the global nickname was not changed.");
                yield break;
            }

            _steamNameUnavailableWarningLogged = false;
            ulong steamId = GetLocalSteamId();

            if (!string.IsNullOrWhiteSpace(input.text))
            {
                // Only auto names get here (a typed nickname returned above). Follow the account that's playing now.
                if (input.text != steamName)
                {
                    if (_steamState.SteamId != 0 && _steamState.SteamId != steamId)
                        Log.LogInfo("[RPNames] Steam account changed, updating the global nickname.");

                    RememberAutoName(steamName, steamId);
                    yield return ApplyNicknameWithRetry(input, steamName);
                }
                else if (_steamState.Applied != steamName || _steamState.SteamId != steamId)
                {
                    RememberAutoName(steamName, steamId);
                }

                yield break;
            }

            _steamState.Original = input.text;
            _steamState.HasOriginal = true;
            RememberAutoName(steamName, steamId);
            yield return ApplyNicknameWithRetry(input, steamName);
        }

        // ===== Steam nickname state: never in the shared cfg =====
        private static string SteamStatePath => Path.Combine(Application.persistentDataPath, "RPNames_steam_state.json");

        private void LoadSteamState()
        {
            try
            {
                if (File.Exists(SteamStatePath))
                    _steamState = JsonConvert.DeserializeObject<SteamNameState>(File.ReadAllText(SteamStatePath)) ?? new SteamNameState();
            }
            catch { _steamState = new SteamNameState(); }
        }

        private void SaveSteamState()
        {
            _steamState.Stamp = DateTime.UtcNow.Ticks;
            WriteSteamStateFile();
        }

        // The file is what the character select screen reads, characters aren't loaded yet at that point.
        // The copy inside each character save (Multitool) is just a backup that travels with the saves.
        private void WriteSteamStateFile()
        {
            try { File.WriteAllText(SteamStatePath, JsonConvert.SerializeObject(_steamState)); }
            catch (Exception ex) { Log.LogWarning($"[RPNames] Couldn't save Steam nickname state: {ex.Message}"); }
        }

        private void ResetSteamState()
        {
            _steamState = new SteamNameState();
            SaveSteamState();
        }

        // ===== Optional integrations =====
        private static Type? FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(fullName, false);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        private static bool TryRegisterMultitool()
        {
            if (FindType("Marioalexsan.Multitool.SaveUtils.SaveUtilsAPI") == null)
            {
                Log.LogWarning("[RPNames] Multitool not found - titles fall back to the old cfg storage. Install Multitool to keep titles with each character.");
                return false;
            }

            try
            {
                MultitoolHook.Register();
                return true;
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[RPNames] Multitool hook failed, using cfg storage: {ex.Message}");
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitEasySettings()
        {
            Settings.OnInitialized.AddListener(AddSettings);
            Settings.OnApplySettings.AddListener(OnSettingsApplied);
        }

        private void AddSettings()
        {
            SettingsTab tab = Settings.ModTab;

            tab.AddHeader("RPNames (Global)");
            tab.AddToggle("Use Steam name as global nickname", _autoSetGlobalNameFromSteam);
            tab.AddKeyButton("Menu Key", _menuKey);
        }

        internal void OnSettingsApplied()
        {
            Config.Save();
            QueueApplySteamGlobalName(null);
        }

        // Called by Multitool when the game loads / saves / deletes a character
        internal void OnCharacterLoaded(int slot, CharacterSaveData? data)
        {
            if (data?.Title == null) AllCharacterProfiles.Remove(slot);
            else AllCharacterProfiles[slot] = data.Title;

            // every character carries a copy of the Steam state, keep the newest one
            if (data?.Steam != null && data.Steam.Stamp > _steamState.Stamp)
            {
                _steamState = data.Steam;
                WriteSteamStateFile();
            }
        }

        internal CharacterSaveData? OnCharacterSaving(int slot)
        {
            AllCharacterProfiles.TryGetValue(slot, out var title);

            SteamNameState? steam = null;
            if (_steamState.Stamp > 0)
                steam = new SteamNameState { Original = _steamState.Original, HasOriginal = _steamState.HasOriginal, Applied = _steamState.Applied, AutoNames = new List<string>(_steamState.AutoNames), SteamId = _steamState.SteamId, Stamp = _steamState.Stamp };

            if (title == null && steam == null) return null;
            return new CharacterSaveData { Title = title, Steam = steam };
        }

        internal void OnCharacterDeleted(int slot) => AllCharacterProfiles.Remove(slot);

        private void LoadLegacyProfiles()
        {
            try
            {
                var old = JsonConvert.DeserializeObject<Dictionary<int, CharacterTitleProfile>>(_characterProfilesJson.Value);
                if (old != null) _legacyProfiles = old;
            }
            catch { _legacyProfiles = new Dictionary<int, CharacterTitleProfile>(); }
        }

        private void SaveLegacyProfiles()
        {
            _characterProfilesJson.Value = JsonConvert.SerializeObject(_legacyProfiles);
            Config.Save();
        }

        private static void SetGlobalNicknameInput(InputField input, string text)
        {
            if (input.text == text)
                return;

            input.text = text;
            input.onEndEdit.Invoke(text);
        }

        private void RefreshSteamGlobalNames()
        {
            if (IsGameUiInGame())
                return;

            if (!_autoSetGlobalNameFromSteam.Value)
            {
                foreach (var entry in _originalGlobalNicknames)
                    if (entry.Key != null
                        && _appliedSteamPlayerNicknames.TryGetValue(entry.Key, out string? appliedName)
                        && entry.Key._globalNickname == appliedName)
                        entry.Key._globalNickname = entry.Value;

                _originalGlobalNicknames.Clear();
                _appliedSteamPlayerNicknames.Clear();
                return;
            }

            foreach (Player player in FindObjectsOfType<Player>())
            {
                if (player == null || !ulong.TryParse(player._steamID, out ulong steamId))
                    continue;

                if (player.isLocalPlayer && IsPreservingLocalNicknameInput())
                    continue;

                string? steamName = player.isLocalPlayer
                    ? GetLocalSteamName()
                    : GetSteamName(steamId);
                if (string.IsNullOrWhiteSpace(steamName))
                    continue;

                bool alreadyApplied = _originalGlobalNicknames.ContainsKey(player);
                if (alreadyApplied
                    && (!_appliedSteamPlayerNicknames.TryGetValue(player, out string? appliedName)
                        || player._globalNickname != appliedName))
                {
                    _originalGlobalNicknames.Remove(player);
                    _appliedSteamPlayerNicknames.Remove(player);
                    alreadyApplied = false;

                    if (!string.IsNullOrWhiteSpace(player._globalNickname))
                        continue;
                }

                if (!alreadyApplied)
                {
                    if (!string.IsNullOrWhiteSpace(player._globalNickname))
                        continue;

                    _originalGlobalNicknames[player] = player._globalNickname;
                }

                if (player._globalNickname != steamName)
                    player._globalNickname = steamName;
                _appliedSteamPlayerNicknames[player] = steamName;
            }

            foreach (Player gone in _originalGlobalNicknames.Keys.Where(player => player == null).ToList())
            {
                _originalGlobalNicknames.Remove(gone);
                _appliedSteamPlayerNicknames.Remove(gone);
            }
        }

        private bool IsPreservingLocalNicknameInput(InputField? input = null)
        {
            input ??= UnityEngine.Object.FindObjectOfType<CharacterSelectManager>()?._globalNickanmeInput;
            if (input == null)
                return false;

            return input.isFocused
                || (!string.IsNullOrWhiteSpace(input.text) && !IsAutoName(input.text));
        }

        // True for any name RPNames filled in itself (any account), or the current Steam name
        private bool IsAutoName(string text)
        {
            return text == _steamState.Applied
                || _steamState.AutoNames.Contains(text)
                || text == GetLocalSteamName();
        }

        private void RememberAutoName(string name, ulong steamId)
        {
            _steamState.Applied = name;
            _steamState.SteamId = steamId;
            if (!_steamState.AutoNames.Contains(name))
                _steamState.AutoNames.Add(name);
            if (_steamState.AutoNames.Count > 8)
                _steamState.AutoNames.RemoveAt(0);
            SaveSteamState();
        }

        private IEnumerator ApplyNicknameWithRetry(InputField input, string name)
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                if (input == null) yield break;
                SetGlobalNicknameInput(input, name);
                yield return new WaitForSeconds(0.5f);

                if (input == null) yield break;
                if (input.text == name)
                {
                    Log.LogInfo($"[RPNames] Global nickname set to '{name}'.");
                    yield break;
                }
                if (input.isFocused) yield break; // player started typing

                Log.LogInfo($"[RPNames] Nickname field went back to '{input.text}', retrying ({attempt}/3).");
            }
        }

        private static ulong GetLocalSteamId()
        {
            if (!SteamManager.Initialized)
                return 0;

            return Steamworks.SteamUser.GetSteamID().m_SteamID;
        }

        private static string? GetLocalSteamName()
        {
            if (!SteamManager.Initialized)
                return null;

            return StripUnrenderable(Steamworks.SteamFriends.GetPersonaName());
        }

        private string? GetSteamName(ulong steamId)
        {
            if (!Steamworks.SteamAPI.IsSteamRunning())
                return null;

            var userId = new Steamworks.CSteamID(steamId);
            string name = Steamworks.SteamFriends.GetFriendPersonaName(userId);
            if (string.IsNullOrWhiteSpace(name) || name == "[unknown]")
            {
                if (_requestedSteamUsers.Add(steamId))
                    Steamworks.SteamFriends.RequestUserInformation(userId, true);

                return null;
            }

            return StripUnrenderable(name);
        }

        // ===== Glyph support: strip Steam names down to characters the nickname labels can render =====
        // The global-nickname labels are TMP with the game's terminal-grotesque font; a persona name
        // containing characters outside that font chain (e.g. fullwidth digits) makes TMP log a
        // "Unicode value 0x… was not found in the font asset or any potential fallbacks" warning on
        // every re-render, forever. Persona names are the only text RPNames injects into those
        // labels, so strip unrenderable characters at the source — the two getters above.
        private static readonly GlyphSupport _glyphSupport = new GlyphSupport();
        private static string StripUnrenderable(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return name ?? "";
            _glyphSupport.Refresh();
            // NFKC folds fullwidth/halfwidth compatibility characters down to the plain
            // characters the game font actually ships (U+FF13 '３' -> '3', U+3000 -> space,
            // U+2160 -> 'I' ...). Names read the same instead of losing whole characters;
            // anything still unrenderable afterwards is removed by the charset strip below.
            string normalized = name.Normalize(NormalizationForm.FormKC);
            return _glyphSupport.Strip(normalized);
        }

        internal sealed class GlyphSupport
        {
            private HashSet<uint>? _supported;
            private double _lastScanTime;

            public void Refresh()
            {
                if (_supported != null)
                    return; // charset built; the label font chain is static per game
                if (Time.unscaledTime - _lastScanTime < 5d)
                    return; // charset still unknown; don't rescan the scene more than every 5s
                _lastScanTime = Time.unscaledTime;

                try
                {
                    // Any live nickname label works — the chain is the game-wide terminal-grotesque
                    // font. No player in the scene yet (main menu) means it's the character-select
                    // label we're about to fill.
                    TMP_FontAsset? main = null;
                    foreach (var player in FindObjectsOfType<Player>())
                    {
                        if (player == null) continue;
                        if (HarmonyPatches._globalNicknameTextMeshField.GetValue(player) is TextMeshPro { font: { } meshFont })
                        {
                            main = meshFont;
                            break;
                        }
                    }
                    if (main == null)
                        return; // nothing instantiated yet; the next scan retries

                    _supported = BuildSupportedSet(main);
                }
                catch
                {
                    // The charset scan is best-effort and runs on the nickname-write path:
                    // never let it throw into the caller (worst case = names unstripped,
                    // exactly today's behavior, not worse).
                }
            }

            private static HashSet<uint>? BuildSupportedSet(TMP_FontAsset main)
            {
                var supported = new HashSet<uint>();
                var chain = new List<TMP_FontAsset> { main };
                List<TMP_FontAsset>? fallbacks = main.fallbackFontAssetTable;
                if (fallbacks != null)
                {
                    foreach (var fallback in fallbacks)
                        if (fallback != null && fallback != main)
                            chain.Add(fallback);
                }
                foreach (var font in chain)
                {
                    // characterTable is the serialized, authoritative list (the lookup
                    // dictionary is populated lazily by TMP and can still be empty). It
                    // holds only characters, so every entry's code point is renderable.
                    if (font?.characterTable == null)
                        continue;
                    foreach (var entry in font.characterTable)
                        if (entry != null)
                            supported.Add(entry.unicode);
                }
                // No usable charset found yet: keep it null so we never strip on an incomplete set.
                return supported.Count > 0 ? supported : null;
            }

            public string Strip(string? name)
            {
                if (string.IsNullOrEmpty(name))
                    return name ?? "";
                if (_supported == null)
                    return name; // unknown charset: never mangle a real name over an incomplete lookup

                var builder = new StringBuilder(name.Length);
                int i = 0;
                while (i < name.Length)
                {
                    char c = name[i];
                    if (char.IsHighSurrogate(c))
                    {
                        if (i + 1 < name.Length && char.IsLowSurrogate(name[i + 1]))
                        {
                            // A real pair; keep it only if the font chain can draw its code point.
                            int code = char.ConvertToUtf32(c, name[i + 1]);
                            if (_supported.Contains((uint)code))
                            {
                                builder.Append(c);
                                builder.Append(name[i + 1]);
                            }
                            i += 2;
                        }
                        else
                        {
                            // Lone high surrogate: TMP would warn on this too; drop it.
                            i += 1;
                        }
                    }
                    else if (char.IsLowSurrogate(c))
                    {
                        // Lone low surrogate: drop it.
                        i += 1;
                    }
                    else
                    {
                        if (_supported.Contains((uint)c))
                            builder.Append(c);
                        i += 1;
                    }
                }
                return builder.ToString();
            }
        }

        private void LogSteamNameUnavailableOnce(string message)
        {
            if (_steamNameUnavailableWarningLogged)
                return;

            Log.LogWarning($"[RPNames] {message}");
            _steamNameUnavailableWarningLogged = true;
        }

        private static bool IsGameUiInGame()
        {
            return Player._mainPlayer != null
                && Player._mainPlayer._currentGameCondition == GameCondition.IN_GAME;
        }

        private void InitializeGUIStyles()
        {
            if (OriginalSkin == null) OriginalSkin = GUI.skin;
            _modSkin = Instantiate(GUI.skin);
            Texture2D MakeTex(Color col) { var tex = new Texture2D(1, 1); tex.SetPixel(0, 0, col); tex.Apply(); tex.hideFlags = HideFlags.HideAndDontSave; return tex; }
            _modSkin.window = new GUIStyle(GUI.skin.window) { normal = { background = MakeTex(new Color(0.1f, 0.12f, 0.15f, 0.97f)), textColor = new Color(0.9f, 0.9f, 0.9f) }, onNormal = { background = MakeTex(new Color(0.1f, 0.12f, 0.15f, 0.97f)), textColor = new Color(0.9f, 0.9f, 0.9f) }, padding = new RectOffset(15, 15, 30, 15), border = new RectOffset(2, 2, 2, 2), alignment = TextAnchor.UpperCenter, fontSize = 16, fontStyle = FontStyle.Bold };
            _modSkin.box = new GUIStyle(GUI.skin.box) { normal = { background = MakeTex(new Color(0.2f, 0.22f, 0.25f, 0.5f)) } };
            _modSkin.label.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            _modSkin.button = new GUIStyle(GUI.skin.button) { normal = { background = MakeTex(new Color(0.3f, 0.35f, 0.4f)), textColor = Color.white }, hover = { background = MakeTex(new Color(0.4f, 0.45f, 0.5f)), textColor = Color.white }, active = { background = MakeTex(new Color(0.2f, 0.25f, 0.3f)), textColor = Color.white }, onNormal = { background = MakeTex(new Color(0.5f, 0.55f, 0.6f)), textColor = Color.white }, onHover = { background = MakeTex(new Color(0.55f, 0.6f, 0.65f)), textColor = Color.white }, border = new RectOffset(4, 4, 4, 4), padding = new RectOffset(8, 8, 8, 8) };
            _modSkin.toggle = new GUIStyle(_modSkin.toggle) { padding = new RectOffset(25, 0, 3, 3) };
            Texture2D MakeToggleTex(bool on) { var tex = new Texture2D(16, 16); var bgColor = new Color(0.2f, 0.22f, 0.25f); var borderColor = new Color(0.4f, 0.45f, 0.5f); var checkColor = new Color(0.8f, 0.85f, 0.9f); for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) { if (x < 1 || x > 14 || y < 1 || y > 14) tex.SetPixel(x, y, borderColor); else tex.SetPixel(x, y, bgColor); } if (on) for (int y = 4; y < 12; y++) for (int x = 4; x < 12; x++) tex.SetPixel(x, y, checkColor); tex.Apply(); tex.hideFlags = HideFlags.HideAndDontSave; return tex; }
            _modSkin.toggle.normal.background = MakeToggleTex(false); _modSkin.toggle.onNormal.background = MakeToggleTex(true); _modSkin.toggle.hover.background = MakeToggleTex(false); _modSkin.toggle.onHover.background = MakeToggleTex(true);
            _modSkin.textField = new GUIStyle(GUI.skin.textField) { normal = { background = MakeTex(new Color(0.05f, 0.05f, 0.05f)), textColor = Color.white }, padding = new RectOffset(5, 5, 5, 5) };
            _modSkin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider) { normal = { background = MakeTex(new Color(0.15f, 0.17f, 0.2f)), }, fixedHeight = 10, border = new RectOffset(2, 2, 2, 2) };
            _modSkin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb) { normal = { background = MakeTex(new Color(0.6f, 0.65f, 0.7f)) }, hover = { background = MakeTex(new Color(0.7f, 0.75f, 0.8f)) }, active = { background = MakeTex(new Color(0.5f, 0.55f, 0.6f)) }, fixedHeight = 18, fixedWidth = 10 };
            _stylesInitialized = true;
        }

        private void LoadProfiles() { try { var loadedProfiles = JsonConvert.DeserializeObject<Dictionary<int, CharacterTitleProfile>>(_characterProfilesJson.Value); if (loadedProfiles != null) AllCharacterProfiles = loadedProfiles; } catch { AllCharacterProfiles = new Dictionary<int, CharacterTitleProfile>(); } }
        // With Multitool the game's own character save pulls AllCharacterProfiles, so nothing to write here
        private void SaveProfiles() { if (UsingMultitool) return; _characterProfilesJson.Value = JsonConvert.SerializeObject(AllCharacterProfiles, Formatting.Indented); Config.Save(); }
        
        private void LoadCustomPresets() 
        { 
            try 
            { 
                var loaded = JsonConvert.DeserializeObject<Dictionary<string, CharacterTitleProfile>>(_customPresetsJson.Value); 
                if (loaded != null) _customPresets = loaded; 
            } 
            catch { _customPresets = new Dictionary<string, CharacterTitleProfile>(); } 
        }
        private void SaveCustomPresets() { _customPresetsJson.Value = JsonConvert.SerializeObject(_customPresets, Formatting.Indented); Config.Save(); }

        private void PopulatePresetTitles() { _presetTitles.Clear(); _presetTitles.AddRange(new[] { "The Explorer", "The Patient", "Dragonslayer", "Scarab Lord", "The Undying", "The Insane", "Grand Marshal", "High Warlord", "Arena Master", "Salty", "Chef", "Guardian of Cenarius", "Hand of A'dal", "Master Angler" }); }
        
        public static void OnProfileUpdate(PacketHeader header, BinaryPacketBase packet)
        {
            if (packet is Packets.UpdateTitleProfilePacket p && p.Profile != null)
            {
                Player sender = FindObjectsOfType<Player>().FirstOrDefault(x => x._steamID == header.SenderID.ToString());
                if (sender != null)
                {
                    ApplyProfileUpdate(sender.netId, p.Profile);
                }
            }
        }
        
        // Full-sync data is only trusted from the lobby owner (the server-gated sender) and only
        // for players we actually have in the lobby. A malicious client must not be able to attach
        // profiles to arbitrary NetIDs and have the server echo them back to everyone.
        public static void OnFullSync(PacketHeader header, BinaryPacketBase packet)
        {
            if (packet is Packets.FullSyncPacket p)
            {
                if (p.AllProfiles == null)
                    return; // malformed packet already dropped in Deserialize
                Log.LogInfo($"[RPNames] Received full-sync with {p.AllProfiles.Count} profile(s).");
                try
                {
                    if (!header.SenderIsLobbyOwner)
                    {
                        Log.LogInfo($"[RPNames] Ignoring full-sync from non-owner {header.SenderID}.");
                        return;
                    }
                }
                catch
                {
                    // Owner unknown (lobby torn down / handle not yet valid) — fail CLOSED and drop
                    // the payload: an unverified sender must not be able to apply titles while the
                    // lookup can't run. Legit full-syncs are on-demand, so the requestor can simply
                    // ask again once the lobby handle is valid.
                    Log.LogInfo($"[RPNames] Dropping full-sync from {header.SenderID}: could not verify lobby ownership.");
                    return;
                }
                var activeNetIds = new HashSet<uint>(FindObjectsOfType<Player>().Select(x => x.netId));
                var localNetIds = HarmonyPatches.PlayerProfiles.Keys.ToList();
                foreach (var localId in localNetIds) if (!p.AllProfiles.ContainsKey(localId)) ApplyProfileUpdate(localId, null);
                foreach (var entry in p.AllProfiles) if (activeNetIds.Contains(entry.Key)) ApplyProfileUpdate(entry.Key, entry.Value);
            }
        }
        
        // Network profiles come from lobby mates, so sanitize once at ingestion: the per-frame
        // animation loop and TMP label never see raw attacker values after this.
        private static readonly Regex _tagStrip = new Regex("<[^>]*>", RegexOptions.Compiled);
        private static readonly Regex _hexColor = new Regex(@"^[0-9A-Fa-f]{6}$", RegexOptions.Compiled);
        internal static string SanitizeDisplayText(string s)
        {
            // Strip TMP rich-text tags (a lobby mate's Title/Pronouns render with richText on)
            // and cap length so a 4kb packet can't build an enormous per-frame string.
            if (string.IsNullOrEmpty(s)) return "";
            s = _tagStrip.Replace(s, "");
            return s.Length > 64 ? s.Substring(0, 64) : s;
        }

        private static string SanitizeHexColor(string s, string fallback)
        {
            // Raw interpolation of a non-hex string into <color=...> lets an attacker break out of
            // the tag; only accept clean 6-char hex, otherwise fall back to a safe default.
            s = (s ?? "").Replace("#", "");
            return _hexColor.IsMatch(s) ? s : fallback;
        }

        // A network ReadSingle can be NaN/Inf/zero/negative; those won't throw but they make the
        // per-frame animation loop spin or emit garbage colors. Replace non-finite values with a
        // fallback and clamp to a sane range.
        private static float SanitizeFloat(float v, float fallback, float min, float max)
            => float.IsNaN(v) || float.IsInfinity(v) ? fallback : Mathf.Clamp(v, min, max);

        private static void ApplyProfileUpdate(uint netId, CharacterTitleProfile profile)
        {
            if (profile != null)
            {
                profile.Title = SanitizeDisplayText(profile.Title ?? "");
                profile.Pronouns = SanitizeDisplayText(profile.Pronouns ?? "");
                // Negative width threw every frame in Update()/GetAnimatedText, and int.MaxValue
                // would allocate a multi-GB string. The UI slider max is 40, so clamp to a sane range.
                profile.MarqueeWidth = Mathf.Clamp(profile.MarqueeWidth, 1, 64);
                profile.AnimationSpeed = SanitizeFloat(profile.AnimationSpeed, 0.25f, 0.05f, 10f);
                profile.GradientSpread = SanitizeFloat(profile.GradientSpread, 10f, 0.01f, 100f);
                profile.RainbowWaveSpread = SanitizeFloat(profile.RainbowWaveSpread, 15f, 0.01f, 100f);
                profile.ColorAnimationSpeed = SanitizeFloat(profile.ColorAnimationSpeed, 1f, 0f, 10f);
                profile.PronounGradientSpread = SanitizeFloat(profile.PronounGradientSpread, 10f, 0.01f, 100f);
                profile.PronounRainbowWaveSpread = SanitizeFloat(profile.PronounRainbowWaveSpread, 15f, 0.01f, 100f);
                profile.PronounColorAnimationSpeed = SanitizeFloat(profile.PronounColorAnimationSpeed, 1f, 0f, 10f);
                profile.SingleHexColor = SanitizeHexColor(profile.SingleHexColor, "FFFFFF");
                profile.GradientStartColor = SanitizeHexColor(profile.GradientStartColor, "FF0000");
                profile.GradientEndColor = SanitizeHexColor(profile.GradientEndColor, "0000FF");
                profile.PronounSingleHexColor = SanitizeHexColor(profile.PronounSingleHexColor, "FFFFFF");
                profile.PronounGradientStartColor = SanitizeHexColor(profile.PronounGradientStartColor, "FF0000");
                profile.PronounGradientEndColor = SanitizeHexColor(profile.PronounGradientEndColor, "0000FF");
            }

            bool hasContent = profile != null && (!string.IsNullOrEmpty(profile.Title) || (profile.ShowPronouns && !string.IsNullOrEmpty(profile.Pronouns)));
            if (!hasContent) { if (HarmonyPatches.PlayerProfiles.ContainsKey(netId)) HarmonyPatches.PlayerProfiles.Remove(netId); if (HarmonyPatches.CurrentPlayerTitles.ContainsKey(netId)) HarmonyPatches.CurrentPlayerTitles.Remove(netId); if (AllPlayerAnimators.ContainsKey(netId)) AllPlayerAnimators.Remove(netId); }
            else
            {
                profile.TargetNetID = netId;
                if (HarmonyPatches.PlayerProfiles.ContainsKey(netId)) HarmonyPatches.PlayerProfiles[netId] = profile; else HarmonyPatches.PlayerProfiles.Add(netId, profile);
                if (AllPlayerAnimators.ContainsKey(netId)) AllPlayerAnimators.Remove(netId);
                if (instance.ShouldAnimate(profile))
                {
                    var newAnimator = new PlayerTitleAnimator(netId); AllPlayerAnimators.Add(netId, newAnimator);
                    string initialAnimatedText = instance.GetAnimatedText(profile, newAnimator);
                    if (HarmonyPatches.CurrentPlayerTitles.ContainsKey(netId)) HarmonyPatches.CurrentPlayerTitles[netId] = initialAnimatedText; else HarmonyPatches.CurrentPlayerTitles.Add(netId, initialAnimatedText);
                } else { if (HarmonyPatches.CurrentPlayerTitles.ContainsKey(netId)) HarmonyPatches.CurrentPlayerTitles[netId] = profile.Title; else HarmonyPatches.CurrentPlayerTitles.Add(netId, profile.Title); }
            }
        }
        
        private static bool _networkSendWarningLogged = false;

        /* All outbound CodeTalker sends should go through this. If Steamworks isn't initialized
        yet (e.g. game launched modded without Steam running), SendNetworkPacket throws
        InvalidOperationException and since this can get called from a per-frame poll
        fallback (Update()), an uncaught throw there repeats every single frame forever,
        spamming the log and hitching/locking the game. Catch it, warn once, and give up */
        private static bool TrySend(System.Action send)
        {
            try
            {
                send();
                return true;
            }
            catch (System.Exception ex)
            {
                if (!_networkSendWarningLogged)
                {
                    Log.LogWarning($"[RPNames] Couldn't send a network packet (Steamworks may not be initialized) — networked titles will be disabled until this resolves. {ex.GetType().Name}: {ex.Message}");
                    _networkSendWarningLogged = true;
                }
                return false;
            }
        }
        
        public static void OnSyncRequest(PacketHeader header, PacketBase packet) 
        { 
            if (!NetworkServer.active) return;
            if (packet is Packets.RequestAllTitlesPacket) 
            {
                var syncPacket = new Packets.FullSyncPacket(HarmonyPatches.PlayerProfiles);
                Log.LogInfo($"[RPNames] Replying to full-sync request from {header.SenderID} with {HarmonyPatches.PlayerProfiles.Count} profile(s).");
                // Send uncompressed. A full-sync payload is small (0 profiles here) and far under the
                // 4kb lobby-chat limit, and compression is the one thing CodeYapper 2.2.4's lazy-
                // decompress getter can't round-trip (GZip/LZ4/ZStd -> StackOverflow on receive).
                TrySend(() => CodeTalkerNetwork.SendNetworkPacket(header.SenderID, syncPacket));
            } 
        }
        
        public static void SendTitleUpdate(CharacterTitleProfile profile) 
        { 
            if (Player._mainPlayer == null) return;
            var packet = new Packets.UpdateTitleProfilePacket(profile);
            if (TrySend(() => CodeTalkerNetwork.SendNetworkPacket(packet, default)))
                ApplyProfileUpdate(Player._mainPlayer.netId, profile);
        }
        
        public static void RequestFullTitleSync() 
        { 
            if (_isCodeTalkerLoaded && !_networkSendWarningLogged) 
            {
                Log.LogInfo("[RPNames] Sending full-sync request to server.");
                TrySend(() => CodeTalkerNetwork.SendNetworkPacket(new Packets.RequestAllTitlesPacket(), default));
            }
        }

        private bool _pollWasInGame = false;

        private void Update()
        {
            _globalNameRefreshTimer += Time.unscaledDeltaTime;
            if (_globalNameRefreshTimer >= 1f)
            {
                _globalNameRefreshTimer = 0f;
                RefreshSteamGlobalNames();
            }

            /* Fallback for OnGameConditionChange_Postfix: poll directly in case that patch gets
            missed on some entry path (patch ordering with other mods, exceptions earlier in
            the postfix chain, etc.). RequestFullTitleSync/SendTitleUpdate are idempotent, so
            firing this alongside the Harmony patch is harmless if both catch the transition. */
            bool isInGameNow = Player._mainPlayer != null && Player._mainPlayer._currentGameCondition == GameCondition.IN_GAME;
            if (isInGameNow && !_pollWasInGame)
            {
                Log.LogInfo("[RPNames] Detected IN_GAME via poll fallback.");
                RequestFullTitleSync();
            }
            _pollWasInGame = isInGameNow;

            switch (_menuState) { case MenuState.Opening: _animationProgress = Mathf.Clamp01(_animationProgress + Time.unscaledDeltaTime / AnimationDuration); if (_animationProgress >= 1f) _menuState = MenuState.Open; break; case MenuState.Closing: _animationProgress = Mathf.Clamp01(_animationProgress - Time.unscaledDeltaTime / AnimationDuration); if (_animationProgress <= 0f) _menuState = MenuState.Closed; break; }
            if (Input.GetKeyDown(_menuKey.Value)) { if (_menuState == MenuState.Open || _menuState == MenuState.Opening) { _menuState = MenuState.Closing; _showTextAnimPicker = _showColoringPicker = _showBracketPicker = _showPresetPicker = _showPronounBracketPicker = _showPronounColoringPicker = false; } else { _menuState = MenuState.Opening; LoadUIFromProfile(CurrentCharacterSlot >= 0 ? CurrentCharacterSlot : 0); } }

            foreach (var animator in AllPlayerAnimators.Values.ToList())
            {
                if (!HarmonyPatches.PlayerProfiles.TryGetValue(animator.NetId, out var profile) || profile == null) { AllPlayerAnimators.Remove(animator.NetId); continue; }
                bool needsTextFrameUpdate = false; animator.AnimationTimer += Time.deltaTime; if (animator.AnimationTimer >= profile.AnimationSpeed) { animator.AnimationTimer = 0f; needsTextFrameUpdate = true; }
                bool needsColorFrameUpdate = false; if (ShouldAnimateColor(profile) || ShouldAnimatePronounColor(profile)) { animator.RainbowHue = (animator.RainbowHue + (Time.deltaTime * profile.ColorAnimationSpeed * 0.1f)) % 1.0f; needsColorFrameUpdate = true; }
                if (profile.TextAnimation == TextAnimationType.Typewriter) { if (animator.TypewriterState == TypewriterState.Blinking) { animator.TypewriterPauseTimer += Time.deltaTime; animator.TypewriterBlinkTimer += Time.deltaTime; if (animator.TypewriterBlinkTimer >= 0.5f) { animator.TypewriterBlinkTimer = 0f; animator.TypewriterCursorVisible = !animator.TypewriterCursorVisible; needsTextFrameUpdate = true; } if (animator.TypewriterPauseTimer >= 5f) { animator.TypewriterPauseTimer = 0f; animator.TypewriterState = TypewriterState.Backspacing; } } }
                if (needsTextFrameUpdate) { switch (profile.TextAnimation) { case TextAnimationType.Scroll: if (animator.IsAnimatingForward) { animator.AnimationIndex++; if (animator.AnimationIndex >= profile.Title.Length) { animator.AnimationIndex = profile.Title.Length; animator.IsAnimatingForward = false; } } else { animator.AnimationIndex--; if (animator.AnimationIndex <= 1) { animator.AnimationIndex = 1; animator.IsAnimatingForward = true; } } break; case TextAnimationType.Marquee: string p = new string(' ', profile.MarqueeWidth); string f = p + profile.Title + p; animator.AnimationIndex = (animator.AnimationIndex + 1) % (f.Length - profile.MarqueeWidth); break; case TextAnimationType.Typewriter: switch (animator.TypewriterState) { case TypewriterState.Typing: if (animator.AnimationIndex < profile.Title.Length) animator.AnimationIndex++; else { animator.TypewriterState = TypewriterState.Blinking; animator.TypewriterPauseTimer = animator.TypewriterBlinkTimer = 0f; animator.TypewriterCursorVisible = true; } break; case TypewriterState.Backspacing: if (animator.AnimationIndex > 0) animator.AnimationIndex--; else animator.TypewriterState = TypewriterState.Typing; break; } break; } }
                if (needsTextFrameUpdate || needsColorFrameUpdate) { string animatedText = GetAnimatedText(profile, animator); if (HarmonyPatches.CurrentPlayerTitles.ContainsKey(animator.NetId)) HarmonyPatches.CurrentPlayerTitles[animator.NetId] = animatedText; else HarmonyPatches.CurrentPlayerTitles.Add(animator.NetId, animatedText); }
            }
        }
        
        internal string GetAnimatedText(CharacterTitleProfile profile, PlayerTitleAnimator animator) { if (string.IsNullOrEmpty(profile.Title)) return ""; switch (profile.TextAnimation) { case TextAnimationType.Scroll: return profile.Title.Substring(0, animator.AnimationIndex); case TextAnimationType.Marquee: string p = new string(' ', profile.MarqueeWidth); string f = p + profile.Title + p; return f.Substring(animator.AnimationIndex, profile.MarqueeWidth); case TextAnimationType.Typewriter: if (animator.AnimationIndex > profile.Title.Length) animator.AnimationIndex = profile.Title.Length; string v = profile.Title.Substring(0, animator.AnimationIndex); if (animator.TypewriterState == TypewriterState.Blinking) return profile.Title + (animator.TypewriterCursorVisible ? "|" : ""); return v + "|"; default: return profile.Title; } }
        
        internal string ApplyColoring(string text, CharacterTitleProfile p, PlayerTitleAnimator a, bool isForPronoun)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (isForPronoun && p.ShareTitleColoring) isForPronoun = false;
            
            float hue = a?.RainbowHue ?? 0f;
            ColoringType coloring = isForPronoun ? p.PronounColoring : p.Coloring;
            string singleHex = isForPronoun ? p.PronounSingleHexColor : p.SingleHexColor;
            string gradStart = isForPronoun ? p.PronounGradientStartColor : p.GradientStartColor;
            string gradEnd = isForPronoun ? p.PronounGradientEndColor : p.GradientEndColor;
            bool animGrad = isForPronoun ? p.PronounAnimateGradient : p.AnimateGradient;
            float gradSpread = isForPronoun ? p.PronounGradientSpread : p.GradientSpread;
            float waveSpread = isForPronoun ? p.PronounRainbowWaveSpread : p.RainbowWaveSpread;

            switch (coloring)
            {
                case ColoringType.SingleColor: return $"<color=#{singleHex.Replace("#", "")}>{text}</color>";
                case ColoringType.Rainbow: return $"<color=#{ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(hue, 1f, 1f))}>{text}</color>";
                case ColoringType.Gradient:
                    ColorUtility.TryParseHtmlString("#" + gradStart, out Color start); ColorUtility.TryParseHtmlString("#" + gradEnd, out Color end);
                    StringBuilder gb = new StringBuilder();
                    if (animGrad) { for (int i = 0; i < text.Length; i++) { float phase = (hue + (i / gradSpread)); float t = 1f - Mathf.Abs((phase * 2f) % 2f - 1f); Color c = Color.Lerp(start, end, t); gb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text[i]}</color>"); } }
                    else { for (int i = 0; i < text.Length; i++) { float t = text.Length <= 1 ? 0f : (float)i / (text.Length - 1); Color c = Color.Lerp(start, end, t); gb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text[i]}</color>"); } }
                    return gb.ToString();
                case ColoringType.Wave: StringBuilder wb = new StringBuilder(); for (int i = 0; i < text.Length; i++) { float h = (hue + (i / waveSpread)) % 1.0f; wb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(h, 1f, 1f))}>{text[i]}</color>"); } return wb.ToString();
                case ColoringType.StaticRainbow: StringBuilder sb = new StringBuilder(); for (int i = 0; i < text.Length; i++) { float h = (i / waveSpread) % 1.0f; sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(h, 1f, 1f))}>{text[i]}</color>"); } return sb.ToString();
                default: return text;
            }
        }
        
        internal void UpdateGradientCache(CharacterTitleProfile profile) { ColorUtility.TryParseHtmlString("#" + profile.GradientStartColor, out _gradientStartColorCache); ColorUtility.TryParseHtmlString("#" + profile.GradientEndColor, out _gradientEndColorCache); }
        internal bool ShouldAnimate(CharacterTitleProfile p) => p != null && !string.IsNullOrEmpty(p.Title) && (p.TextAnimation != TextAnimationType.Static || ShouldAnimateColor(p) || ShouldAnimatePronounColor(p));
        internal bool ShouldAnimateColor(CharacterTitleProfile p) => p.Coloring == ColoringType.Rainbow || p.Coloring == ColoringType.Wave || (p.Coloring == ColoringType.Gradient && p.AnimateGradient);
        internal bool ShouldAnimatePronounColor(CharacterTitleProfile p) => p.ShowPronouns && (p.ShareTitleColoring ? ShouldAnimateColor(p) : p.PronounColoring == ColoringType.Rainbow || p.PronounColoring == ColoringType.Wave || (p.PronounColoring == ColoringType.Gradient && p.PronounAnimateGradient));

        private void OnGUI()
        {
            if (_menuState == MenuState.Closed) return;
            if (!_stylesInitialized) { try { InitializeGUIStyles(); } catch (Exception) { /* ignored */ } }
            GUI.skin = _modSkin;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            float easeOut(float t) => 1 - Mathf.Pow(1 - t, 3); float currentProgress = easeOut(_animationProgress); float startX = -_windowRect.width; float endX = 20f; Rect animatedRect = _windowRect; animatedRect.x = Mathf.Lerp(startX, endX, currentProgress);
            _windowRect = GUILayout.Window(1863, animatedRect, DrawSettingsWindow, "RPNames Settings");
            GUI.skin = OriginalSkin;
        }
        
        private void LoadUIFromProfile(int slot) { if (slot == -1) { _uiEditingProfile = new CharacterTitleProfile(); return; } if (!AllCharacterProfiles.TryGetValue(slot, out var profile)) { profile = new CharacterTitleProfile(); } _uiEditingProfile = JsonConvert.DeserializeObject<CharacterTitleProfile>(JsonConvert.SerializeObject(profile)); }

        private void DrawSettingsWindow(int windowID)
        {
            if (CurrentCharacterSlot == -1) { GUILayout.FlexibleSpace(); GUILayout.Label("Please load a character to edit their profile."); GUILayout.FlexibleSpace(); GUI.DragWindow(); return; }
            GUILayout.Label($"Editing Profile for Slot: {CurrentCharacterSlot + 1}");

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            void DrawColorCustomization(bool isForPronoun)
            {
                var p = _uiEditingProfile;
                Action<ColoringType> setColoring = val => { if (isForPronoun) p.PronounColoring = val; else p.Coloring = val; };
                Action<string> setSingleHex = val => { if (isForPronoun) p.PronounSingleHexColor = val; else p.SingleHexColor = val; };
                Action<string> setGradStart = val => { if (isForPronoun) p.PronounGradientStartColor = val; else p.GradientStartColor = val; };
                Action<string> setGradEnd = val => { if (isForPronoun) p.PronounGradientEndColor = val; else p.GradientEndColor = val; };
                Action<bool> setAnimGrad = val => { if (isForPronoun) p.PronounAnimateGradient = val; else p.AnimateGradient = val; };
                Action<float> setGradSpread = val => { if (isForPronoun) p.PronounGradientSpread = val; else p.GradientSpread = val; };
                Action<float> setWaveSpread = val => { if (isForPronoun) p.PronounRainbowWaveSpread = val; else p.RainbowWaveSpread = val; };
                Action<float> setColorSpeed = val => { if (isForPronoun) p.PronounColorAnimationSpeed = val; else p.ColorAnimationSpeed = val; };
                ref bool showPicker = ref (isForPronoun ? ref _showPronounColoringPicker : ref _showColoringPicker);
                ref bool showSingle = ref (isForPronoun ? ref _showPronounSingleColorPicker : ref _showSingleColorPicker);
                ref bool showGradS = ref (isForPronoun ? ref _showPronounGradientStartPicker : ref _showGradientStartPicker);
                ref bool showGradE = ref (isForPronoun ? ref _showPronounGradientEndPicker : ref _showGradientEndPicker);
                ColoringType curColoring = isForPronoun ? p.PronounColoring : p.Coloring;
                
                GUILayout.BeginHorizontal(); GUILayout.Label("Effect:", GUILayout.Width(120)); if (GUILayout.Button(curColoring.ToString())) showPicker = !showPicker; GUILayout.EndHorizontal();
                if (showPicker) { string[] names = Enum.GetNames(typeof(ColoringType)); int sel = GUILayout.SelectionGrid((int)curColoring, names, 3); if (sel != (int)curColoring) { setColoring((ColoringType)sel); showPicker = false; showSingle = showGradS = showGradE = false; } }
                
                // FIX: DECOUPLED COLOR PICKER LOGIC
                // Pass the current hex string to DrawColorPicker so it can calculate local slider values
                Action<string, Action<string>> DrawColorPicker = (currentHex, setter) => 
                { 
                    if (!ColorUtility.TryParseHtmlString("#" + currentHex, out Color c)) c = Color.white;
                    
                    GUILayout.BeginVertical(_modSkin.box); 
                    GUILayout.BeginHorizontal(); 
                    GUILayout.BeginVertical(GUILayout.ExpandWidth(true)); 
                    
                    float newR = GUILayout.HorizontalSlider(c.r, 0, 1); 
                    float newG = GUILayout.HorizontalSlider(c.g, 0, 1); 
                    float newB = GUILayout.HorizontalSlider(c.b, 0, 1); 
                    
                    GUILayout.EndVertical(); 
                    
                    var oldBgColor = GUI.backgroundColor; 
                    GUI.backgroundColor = c; 
                    GUILayout.Box("", new GUIStyle { normal = { background = Texture2D.whiteTexture } }, GUILayout.Width(40), GUILayout.Height(40)); 
                    GUI.backgroundColor = oldBgColor; 
                    GUILayout.EndHorizontal(); 
                    
                    if (newR != c.r || newG != c.g || newB != c.b)
                    {
                        c.r = newR; c.g = newG; c.b = newB;
                        setter(ColorUtility.ToHtmlStringRGB(c)); 
                    }
                    GUILayout.EndVertical(); 
                };

                if (curColoring == ColoringType.SingleColor) 
                { 
                    GUILayout.BeginHorizontal(); 
                    GUILayout.Label("Hex Color", GUILayout.Width(80)); 
                    string hex = isForPronoun ? p.PronounSingleHexColor : p.SingleHexColor; 
                    setSingleHex(GUILayout.TextField(hex, 7)); 
                    if (GUILayout.Button("Pick", GUILayout.Width(50))) { showSingle = !showSingle; showGradS = showGradE = false; } 
                    GUILayout.EndHorizontal(); 
                    if (showSingle) DrawColorPicker(hex, setSingleHex); 
                }
                if (curColoring == ColoringType.Gradient) 
                { 
                    bool anim = isForPronoun ? p.PronounAnimateGradient : p.AnimateGradient; 
                    setAnimGrad(GUILayout.Toggle(anim, "Animate Gradient")); 
                    if(anim) { float s = isForPronoun ? p.PronounColorAnimationSpeed : p.ColorAnimationSpeed; GUILayout.Label($"Color Speed: {s:F1}"); setColorSpeed(GUILayout.HorizontalSlider(s, 0.1f, 5f)); } 
                    float gs = isForPronoun ? p.PronounGradientSpread : p.GradientSpread; GUILayout.Label($"Gradient Spread: {gs:F1}"); setGradSpread(GUILayout.HorizontalSlider(gs, 1f, 50f)); 
                    GUI.enabled = !anim; 
                    GUILayout.BeginHorizontal(); GUILayout.Label("Start", GUILayout.Width(50)); 
                    string sHex = isForPronoun ? p.PronounGradientStartColor : p.GradientStartColor; 
                    setGradStart(GUILayout.TextField(sHex, 7)); 
                    if (GUILayout.Button("Pick", GUILayout.Width(50))) { showGradS = !showGradS; showSingle = showGradE = false; } 
                    GUILayout.EndHorizontal(); 
                    if (showGradS) DrawColorPicker(sHex, setGradStart); 
                    GUILayout.BeginHorizontal(); GUILayout.Label("End", GUILayout.Width(50)); 
                    string eHex = isForPronoun ? p.PronounGradientEndColor : p.GradientEndColor; 
                    setGradEnd(GUILayout.TextField(eHex, 7)); 
                    if (GUILayout.Button("Pick", GUILayout.Width(50))) { showGradE = !showGradE; showSingle = showGradS = false; } 
                    GUILayout.EndHorizontal(); 
                    if (showGradE) DrawColorPicker(eHex, setGradEnd); 
                    GUI.enabled = true; 
                }
                if (curColoring == ColoringType.Wave || curColoring == ColoringType.Rainbow) { float s = isForPronoun ? p.PronounColorAnimationSpeed : p.ColorAnimationSpeed; GUILayout.Label($"Color Speed: {s:F1}"); setColorSpeed(GUILayout.HorizontalSlider(s, 0.1f, 5f)); }
                if (curColoring == ColoringType.Wave || curColoring == ColoringType.StaticRainbow) { float s = isForPronoun ? p.PronounRainbowWaveSpread : p.RainbowWaveSpread; GUILayout.Label($"Wave Spread: {s:F1}"); setWaveSpread(GUILayout.HorizontalSlider(s, 5f, 50f)); }
                if (curColoring == ColoringType.Gradient || curColoring == ColoringType.SingleColor) GUILayout.Label("(RRGGBB format, no #)", new GUIStyle(_modSkin.label) { fontStyle = FontStyle.Italic, normal = { textColor = Color.gray } });
            }

            GUILayout.Label("Title", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            _uiEditingProfile.Title = GUILayout.TextField(_uiEditingProfile.Title, 50);
            
            // ============== PRESET UI ==============
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(45));
            _newPresetName = GUILayout.TextField(_newPresetName);
            if (GUILayout.Button("Save Preset")) 
            {
                if (!string.IsNullOrEmpty(_newPresetName))
                {
                    // Clone the current profile to save it
                    var clone = JsonConvert.DeserializeObject<CharacterTitleProfile>(JsonConvert.SerializeObject(_uiEditingProfile));
                    if (_customPresets.ContainsKey(_newPresetName)) _customPresets[_newPresetName] = clone;
                    else _customPresets.Add(_newPresetName, clone);
                    SaveCustomPresets();
                }
            }
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Select a Preset")) _showPresetPicker = !_showPresetPicker;
            if (_showPresetPicker) 
            { 
                _presetScrollPosition = GUILayout.BeginScrollView(_presetScrollPosition, GUILayout.Height(150)); 
                
                GUILayout.Label("- Default Presets (Text Only) -");
                foreach (string preset in _presetTitles) 
                {
                    if (GUILayout.Button(preset)) { _uiEditingProfile.Title = preset; _showPresetPicker = false; } 
                }
                
                if (_customPresets.Count > 0)
                {
                    GUILayout.Label("- Custom Presets (Full Style) -");
                    List<string> toRemove = new List<string>();
                    foreach (var entry in _customPresets)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(entry.Key)) 
                        { 
                            // Deep copy load to avoid reference issues
                            _uiEditingProfile = JsonConvert.DeserializeObject<CharacterTitleProfile>(JsonConvert.SerializeObject(entry.Value)); 
                            _showPresetPicker = false; 
                        }
                        if (GUILayout.Button("X", GUILayout.Width(25))) { toRemove.Add(entry.Key); }
                        GUILayout.EndHorizontal();
                    }
                    if (toRemove.Count > 0) 
                    {
                        foreach(var r in toRemove) _customPresets.Remove(r);
                        SaveCustomPresets();
                    }
                }
                GUILayout.EndScrollView(); 
            }
            // =================================================
            
            GUILayout.Space(10); GUILayout.Label("--- Pronouns ---", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            _uiEditingProfile.ShowPronouns = GUILayout.Toggle(_uiEditingProfile.ShowPronouns, " Show Pronouns");
            if (_uiEditingProfile.ShowPronouns)
            {
                _uiEditingProfile.Pronouns = GUILayout.TextField(_uiEditingProfile.Pronouns, 50);
                GUILayout.BeginHorizontal(); GUILayout.Label("Pronoun Brackets:", GUILayout.Width(120)); if (GUILayout.Button(_uiEditingProfile.PronounBracketStyle.ToString())) _showPronounBracketPicker = !_showPronounBracketPicker; GUILayout.EndHorizontal(); if (_showPronounBracketPicker) { string[] names = Enum.GetNames(typeof(BracketType)); int sel = GUILayout.SelectionGrid((int)_uiEditingProfile.PronounBracketStyle, names, 3); if (sel != (int)_uiEditingProfile.PronounBracketStyle) { _uiEditingProfile.PronounBracketStyle = (BracketType)sel; _showPronounBracketPicker = false; } }
            }

            GUILayout.Space(10); GUILayout.Label("--- Display Options ---", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            _uiEditingProfile.TitleOnNewLine = GUILayout.Toggle(_uiEditingProfile.TitleOnNewLine, " Display on New Line");
            if (_uiEditingProfile.TitleOnNewLine) _uiEditingProfile.AddGapAboveTitle = GUILayout.Toggle(_uiEditingProfile.AddGapAboveTitle, " Add Gap Above");
            GUILayout.BeginHorizontal(); GUILayout.Label("Title Brackets:", GUILayout.Width(120)); if (GUILayout.Button(_uiEditingProfile.BracketStyle.ToString())) _showBracketPicker = !_showBracketPicker; GUILayout.EndHorizontal();
            if (_showBracketPicker) { string[] names = Enum.GetNames(typeof(BracketType)); int sel = GUILayout.SelectionGrid((int)_uiEditingProfile.BracketStyle, names, 3); if (sel != (int)_uiEditingProfile.BracketStyle) { _uiEditingProfile.BracketStyle = (BracketType)sel; _showBracketPicker = false; } }
            
            GUILayout.Space(10); GUILayout.Label("--- Title Animation ---", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            GUILayout.BeginHorizontal(); GUILayout.Label("Animation:", GUILayout.Width(120)); if (GUILayout.Button(_uiEditingProfile.TextAnimation.ToString())) _showTextAnimPicker = !_showTextAnimPicker; GUILayout.EndHorizontal();
            if (_showTextAnimPicker) { string[] names = Enum.GetNames(typeof(TextAnimationType)); int sel = GUILayout.SelectionGrid((int)_uiEditingProfile.TextAnimation, names, 3); if (sel != (int)_uiEditingProfile.TextAnimation) { _uiEditingProfile.TextAnimation = (TextAnimationType)sel; _showTextAnimPicker = false; } }
            if (_uiEditingProfile.TextAnimation != TextAnimationType.Static) { float uiSpeed = Mathf.Lerp(10f, 1f, Mathf.InverseLerp(0.05f, 0.5f, _uiEditingProfile.AnimationSpeed)); GUILayout.Label($"Animation Speed: {uiSpeed:F1}"); uiSpeed = GUILayout.HorizontalSlider(uiSpeed, 1f, 10f); _uiEditingProfile.AnimationSpeed = Mathf.Lerp(0.5f, 0.05f, Mathf.InverseLerp(1f, 10f, uiSpeed)); }
            if (_uiEditingProfile.TextAnimation == TextAnimationType.Marquee) { GUILayout.Label($"Marquee Width: {_uiEditingProfile.MarqueeWidth}"); _uiEditingProfile.MarqueeWidth = (int)GUILayout.HorizontalSlider(_uiEditingProfile.MarqueeWidth, 5, 40); }
            
            GUILayout.Space(10); GUILayout.Label("--- Title Coloring ---", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            DrawColorCustomization(false);
            
            if (_uiEditingProfile.ShowPronouns) { GUILayout.Space(10); GUILayout.Label("--- Pronoun Coloring ---", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }); _uiEditingProfile.ShareTitleColoring = GUILayout.Toggle(_uiEditingProfile.ShareTitleColoring, " Share Title Coloring"); if (!_uiEditingProfile.ShareTitleColoring) DrawColorCustomization(true); }

            GUILayout.EndScrollView();
            
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(); if (GUILayout.Button("Copy")) { _copiedProfileBuffer = JsonConvert.DeserializeObject<CharacterTitleProfile>(JsonConvert.SerializeObject(_uiEditingProfile)); } GUI.enabled = _copiedProfileBuffer != null; if (GUILayout.Button("Paste")) { if (_copiedProfileBuffer != null) { _uiEditingProfile = JsonConvert.DeserializeObject<CharacterTitleProfile>(JsonConvert.SerializeObject(_copiedProfileBuffer)); } } GUI.enabled = true; GUILayout.EndHorizontal();
            
            if (UsingMultitool && !AllCharacterProfiles.ContainsKey(CurrentCharacterSlot) && _legacyProfiles.TryGetValue(CurrentCharacterSlot, out var legacy) && legacy != null)
            {
                if (GUILayout.Button("Import old title from config (this slot)")) _uiEditingProfile = JsonConvert.DeserializeObject<CharacterTitleProfile>(JsonConvert.SerializeObject(legacy));
            }

            if (GUILayout.Button("Set & Save")) { AllCharacterProfiles[CurrentCharacterSlot] = JsonConvert.DeserializeObject<CharacterTitleProfile>(JsonConvert.SerializeObject(_uiEditingProfile)); SaveProfiles(); if (UsingMultitool && _legacyProfiles.Remove(CurrentCharacterSlot)) SaveLegacyProfiles(); var currentProfile = AllCharacterProfiles[CurrentCharacterSlot]; UpdateGradientCache(currentProfile); SendTitleUpdate(currentProfile); _menuState = MenuState.Closing; }
            
            if (_showClearConfirmation)
            {
                GUILayout.Label("Are you sure?", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.yellow } });
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("YES, CLEAR")) 
                { 
                    if (AllCharacterProfiles.ContainsKey(CurrentCharacterSlot)) AllCharacterProfiles.Remove(CurrentCharacterSlot); 
                    SaveProfiles(); 
                    SendTitleUpdate(null); 
                    LoadUIFromProfile(CurrentCharacterSlot); 
                    _showClearConfirmation = false; 
                }
                if (GUILayout.Button("CANCEL")) { _showClearConfirmation = false; }
                GUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button("Clear All")) { _showClearConfirmation = true; }
            }
            
            GUI.DragWindow();
        }
    }
    
    // CodeYapper 2.2.4 ships a broken PacketWrapper.PacketBytes getter: it decompresses via
    // `Decompress(PacketBytes, ...)` - reading the property instead of the _PacketBytes field -
    // and only clears isCompressed afterwards, so receiving a GZip/LZ4/ZStd compressed packet
    // re-enters the getter forever and stack-overflows the whole game (silently, no crash log).
    // An exploiter just has to hand a host one compressed packet to force-close it. While the
    // library is still broken we install a Harmony prefix on the getter that pre-decompresses and
    // clears the flag, so the recursive branch is never reached. It targets only the exact 2.2.4
    // build (a no-op on 2.2.3 and on any future fixed version), arms only when every needed
    // member is found (otherwise the getter is left exactly as the library shipped it), is
    // flag-guarded (safe alongside other mods that patch the same getter), and degrades to a
    // graceful packet-drop in RPNames' malformed-packet handling instead of a hard crash.
    internal static class CodeYapperWorkaround
    {
        private static FieldInfo? _compressed, _bytes, _compression;
        private static Type? _compressionEnum;
        private static MethodInfo? _decompress;
        private static int _levelArity;
        private static object? _levelDefault;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Apply(Harmony harmony)
        {
            try
            {
                // CodeTalker is a soft dependency; if it's absent there's nothing to patch.
                var networkType = Type.GetType("CodeTalker.Networking.CodeTalkerNetwork, CodeTalker");
                if (networkType == null)
                    return;

                // Target only the 2.2.4 build that ships the recursive getter. 2.2.3 and older
                // used a plain field (no bug), and a future 2.2.5+ with the real fix must stay
                // untouched so this guard can't double-decompress. This makes the patch an
                // automatic no-op everywhere except the broken version it was written for.
                var version = networkType.Assembly.GetName().Version;
                if (version?.Major != 2 || version?.Minor != 2 || version?.Build != 4)
                    return;

                // PacketWrapper is internal to CodeTalker; look it up by name.
                var wrapper = networkType.Assembly.GetType("PacketWrapper", false);
                if (wrapper == null)
                    return;

                var compressed = GetField(wrapper, "isCompressed");
                var bytes = GetField(wrapper, "_PacketBytes");
                var compression = GetField(wrapper, "compression");
                if (compressed == null || bytes == null || compression == null)
                    return;
                _compressed = compressed;
                _bytes = bytes;
                _compression = compression;

                var compressionEnum = compression.FieldType;
                _compressionEnum = compressionEnum;
                var decompress = FindDecompress(wrapper.Assembly, compressionEnum);
                if (decompress == null)
                {
                    Main.Log.LogWarning("[RPNames] CodeYapper Decompress signature not found, leaving the broken getter untouched.");
                    return;
                }
                _decompress = decompress;

                // Resolve the optional CompressionLevel default once, ahead of any packet traffic.
                var parameters = decompress.GetParameters();
                _levelArity = parameters.Length;
                if (_levelArity > 2)
                {
                    var levelParam = parameters[2];
                    var levelDefault = levelParam.IsOptional
                        ? levelParam.DefaultValue
                        : Enum.GetValues(levelParam.ParameterType).GetValue(0);
                    if (levelDefault == null)
                    {
                        Main.Log.LogWarning("[RPNames] CodeYapper CompressionLevel default unknown, leaving the broken getter untouched.");
                        return;
                    }
                    _levelDefault = levelDefault;
                }

                var target = wrapper.GetMethod("get_PacketBytes",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (target == null)
                    return;

                harmony.Patch(target, new HarmonyMethod(typeof(CodeYapperWorkaround), nameof(Prefix)));
                Main.Log.LogInfo("[RPNames] Guarded the CodeYapper compressed-packet getter against the recursive stack overflow.");
            }
            catch (Exception ex)
            {
                Main.Log.LogWarning($"[RPNames] Could not guard the CodeYapper getter: {ex.Message}");
            }
        }

        private static FieldInfo? GetField(Type type, string name)
            => type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static MethodInfo? FindDecompress(Assembly asm, Type compressionEnum)
        {
            var compressors = asm.GetType("CodeTalker.Compressors", false);
            if (compressors == null)
                return null;
            foreach (var m in compressors.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name != "Decompress" || m.ReturnType != typeof(byte[]))
                    continue;
                var p = m.GetParameters();
                if (p.Length >= 2 && p[0].ParameterType == typeof(byte[]) && p[1].ParameterType == compressionEnum
                    && (p.Length == 2 || p[2].IsOptional))
                    return m;
            }
            return null;
        }

        // Runs before get_PacketBytes. If the wrapper still holds a compressed payload we
        // decompress it in place and clear the flag, so the original getter takes its safe
        // non-recursive path instead of reading itself forever. Everything is best-effort and
        // fully caught: the worst outcome is leaving the raw bytes in place (which RPNames'
        // malformed-packet guards drop later), never letting an exception escape into the
        // network pipeline.
        private static void Prefix(object __instance)
        {
            var compressedField = _compressed;
            var bytesField = _bytes;
            var compressionField = _compression;
            var decompressMethod = _decompress;
            if (compressedField == null || bytesField == null || compressionField == null)
                return;

            try
            {
                // Only the compressed, non-None cases can recurse. None already has the flag
                // clear and takes the getter's safe fast path; we just get out of its way.
                if (!(compressedField.GetValue(__instance) is bool compressed) || !compressed)
                    return;

                // Brotli is intentionally disabled by the library for a decoder CVE and has its
                // own safe branch (it logs and returns an empty payload). Leave that alone.
                string mode = compressionField.GetValue(__instance)?.ToString() ?? "";
                if (mode == "Brotli" || mode == "None")
                    return;

                // GZip / LZ4 / ZStd: the recursive case. Decompress in place from the raw field
                // and clear the flag so the original getter returns on its safe path instead of
                // reading itself forever.
                object? raw = bytesField.GetValue(__instance);
                byte[] result = raw as byte[] ?? Array.Empty<byte>();
                if (raw != null && decompressMethod != null)
                {
                    try
                    {
                        object[] args = _levelArity > 2
                            ? new object[] { raw, compressionField.GetValue(__instance), _levelDefault! }
                            : new object[] { raw, compressionField.GetValue(__instance) };
                        object? decompressed = decompressMethod.Invoke(null, args);
                        if (decompressed is byte[] decoded)
                            result = decoded;
                    }
                    catch
                    {
                        // Undecodable payload: keep the raw data; RPNames' malformed-packet guards
                        // drop it later instead of the game dying.
                    }
                }

                bytesField.SetValue(__instance, result);
                compressedField.SetValue(__instance, false);
            }
            catch
            {
                // Last resort: clear the flag so the original getter can't recurse even if the
                // payload is left raw.
                try { compressedField.SetValue(__instance, false); } catch { }
            }
        }
    }

    // Separate classes so Multitool / EasySettings types only load when those mods are installed
    internal static class MultitoolHook
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Register()
        {
            Marioalexsan.Multitool.SaveUtils.SaveUtilsAPI.RegisterProfileData<CharacterSaveData>(ModInfo.GUID, Save, Load, Delete);
        }

        private static void Save(CharacterFile file, int slotIndex, out CharacterSaveData? data) => data = Main.instance.OnCharacterSaving(slotIndex);
        private static void Load(CharacterFile file, int slotIndex, CharacterSaveData? data) => Main.instance.OnCharacterLoaded(slotIndex, data);
        private static void Delete(CharacterFile file, int slotIndex) => Main.instance.OnCharacterDeleted(slotIndex);
    }

    // ============== HARMONY PATCHES ==============
    [HarmonyPatch]
    internal static class HarmonyPatches
    {
        internal static readonly Dictionary<uint, CharacterTitleProfile> PlayerProfiles = new Dictionary<uint, CharacterTitleProfile>();
        internal static readonly Dictionary<uint, string> CurrentPlayerTitles = new Dictionary<uint, string>();
        internal static readonly FieldInfo _globalNicknameTextMeshField = AccessTools.Field(typeof(Player), "_globalNicknameTextMesh");

        [HarmonyPostfix, HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Set_MenuCondition))]
        private static void OnSetMenuCondition_Postfix(MainMenuManager __instance)
        {
            if (__instance._mainMenuCondition == MainMenuCondition.CharacterSelect)
                Main.instance.QueueApplySteamGlobalName(__instance._characterSelectManager);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MainMenuManager), "Load_HostSettings")]
        private static void OnLoadHostSettings_Postfix(MainMenuManager __instance)
        {
            Main.instance.QueueApplySteamGlobalName(__instance._characterSelectManager);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ProfileDataSender), "OnSend_CharacterFileData")]
        private static void OnSendCharacterFileData_Postfix()
        {
            Main.instance.QueueApplySteamGlobalName(UnityEngine.Object.FindObjectOfType<CharacterSelectManager>());
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(Player), "OnGameConditionChange")]
        private static void OnGameConditionChange_Postfix(Player __instance, GameCondition _newCondition) { if (__instance != Player._mainPlayer) return; Main.IsReady = (_newCondition == GameCondition.IN_GAME); if (Main.IsReady) { Main.CurrentCharacterSlot = ProfileDataManager._current.SelectedFileIndex; if (Main.instance.AllCharacterProfiles.TryGetValue(Main.CurrentCharacterSlot, out var profile)) { Main.instance.UpdateGradientCache(profile); Main.SendTitleUpdate(profile); } Main.RequestFullTitleSync(); } }
        
        [HarmonyPostfix, HarmonyPatch(typeof(AtlyssNetworkManager), "OnStopClient")]
        private static void OnStopClient_Postfix() 
        { 
            Main.IsReady = false; 
            Main.CurrentCharacterSlot = -1; 
            PlayerProfiles.Clear(); 
            CurrentPlayerTitles.Clear(); 
            Main.AllPlayerAnimators.Clear();
        }
        
        private class PlayerCleanupMarker : MonoBehaviour
        {
            public uint NetId;
            private void OnDestroy()
            {
                PlayerProfiles.Remove(NetId);
                CurrentPlayerTitles.Remove(NetId);
                Main.AllPlayerAnimators.Remove(NetId);
            }
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(Player), "OnStartAuthority")]
        private static void OnPlayerStart_Postfix(Player __instance)
        {
            if (!__instance.isLocalPlayer || !Main._isCodeTalkerLoaded) return;
            Main.RequestFullTitleSync();
        }

        private static string GetFormattedStringWithBrackets(string content, BracketType bracketType) { if (string.IsNullOrEmpty(content)) return ""; switch (bracketType) { case BracketType.Parentheses: return $"({content})"; case BracketType.SquareBrackets: return $"[{content}]"; case BracketType.Tilde: return $"~{content}~"; case BracketType.Dash: return $"-{content}-"; case BracketType.Plus: return $"+{content}+"; case BracketType.Equals: return $"={content}="; case BracketType.Asterisk: return $"*{content}*"; case BracketType.Dollar: return $"${content}$"; case BracketType.Hash: return $"#{content}#"; case BracketType.Exclamation: return $"!{content}!"; case BracketType.Pipe: return $"|{content}|"; default: return content; } }
        
        [HarmonyPostfix, HarmonyPatch(typeof(Player), "Handle_ClientParameters")]
        private static void Handle_ClientParameters_Postfix(Player __instance)
        {
            if (__instance.GetComponent<PlayerCleanupMarker>() == null)
            {
                var marker = __instance.gameObject.AddComponent<PlayerCleanupMarker>();
                marker.NetId = __instance.netId;
            }

            if (_globalNicknameTextMeshField.GetValue(__instance) is not TextMeshPro textMesh) return;
            if (!textMesh.richText) textMesh.richText = true;
            
            CurrentPlayerTitles.TryGetValue(__instance.netId, out var animatedTitleText);
            PlayerProfiles.TryGetValue(__instance.netId, out var playerProfile);
            playerProfile ??= new CharacterTitleProfile();
            Main.AllPlayerAnimators.TryGetValue(__instance.netId, out var animator);

            bool hasTitle = !string.IsNullOrEmpty(animatedTitleText);
            bool hasPronouns = playerProfile.ShowPronouns && !string.IsNullOrEmpty(playerProfile.Pronouns);
            bool hasContent = !string.IsNullOrEmpty(__instance._globalNickname) || hasTitle || hasPronouns;

            string finalDisplayString;
            if (hasContent)
            {
                string coloredTitle = Main.instance.ApplyColoring(animatedTitleText, playerProfile, animator, false);
                string coloredPronouns = Main.instance.ApplyColoring(playerProfile.Pronouns, playerProfile, animator, true);
                
                string formattedTitle = GetFormattedStringWithBrackets(coloredTitle, playerProfile.BracketStyle);
                string formattedPronouns = hasPronouns ? GetFormattedStringWithBrackets(coloredPronouns, playerProfile.PronounBracketStyle) : "";

                string titleAndPronounsPart = "";
                if (hasTitle && hasPronouns) titleAndPronounsPart = $"{formattedTitle} {formattedPronouns}";
                else if (hasTitle) titleAndPronounsPart = formattedTitle;
                else if (hasPronouns) titleAndPronounsPart = formattedPronouns;
                
                string globalNamePart = !string.IsNullOrEmpty(__instance._globalNickname) ? "@" + Main.SanitizeDisplayText(__instance._globalNickname) : (hasTitle || hasPronouns ? "@." : "");
                string prefix = playerProfile.AddGapAboveTitle && playerProfile.TitleOnNewLine ? "\n" : "";
                string separator = playerProfile.TitleOnNewLine ? "\n" : " ";
                
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(globalNamePart)) parts.Add(globalNamePart);
                if (!string.IsNullOrEmpty(titleAndPronounsPart)) parts.Add(titleAndPronounsPart);
                
                finalDisplayString = $"{prefix}{string.Join(separator, parts)}";
            }
            else { finalDisplayString = ""; }
            
            if (textMesh.text != finalDisplayString) { textMesh.text = finalDisplayString; }
        }
    }
}