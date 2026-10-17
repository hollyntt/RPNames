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
using TMPro;
using CodeTalker;
using CodeTalker.Networking;
using CodeTalker.Packets;
using Newtonsoft.Json;
using System.IO.Compression;
using Mirror; 

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
            public override void Deserialize(byte[] data) { using (var ms = new MemoryStream(data)) { using (var reader = new BinaryReader(ms)) { Profile = ProfileSerializer.ReadProfile(reader); } } }
        }

        public class FullSyncPacket : BinaryPacketBase
        {
            public override string PacketSignature => ModInfo.GUID + "_FullSync";
            public Dictionary<uint, CharacterTitleProfile> AllProfiles { get; set; }
            public FullSyncPacket() { }
            public FullSyncPacket(Dictionary<uint, CharacterTitleProfile> profiles) { AllProfiles = profiles; }

            public override byte[] Serialize() { using (var ms = new MemoryStream()) { using (var writer = new BinaryWriter(ms)) { writer.Write(AllProfiles.Count); foreach (var entry in AllProfiles) { writer.Write(entry.Key); ProfileSerializer.WriteProfile(writer, entry.Value); } } return ms.ToArray(); } }
            public override void Deserialize(byte[] data) { using (var ms = new MemoryStream(data)) { using (var reader = new BinaryReader(ms)) { int count = reader.ReadInt32(); AllProfiles = new Dictionary<uint, CharacterTitleProfile>(count); for (int i = 0; i < count; i++) { AllProfiles.Add(reader.ReadUInt32(), ProfileSerializer.ReadProfile(reader)); } } } }
        }

        public class RequestAllTitlesPacket : PacketBase { public override string PacketSourceGUID => ModInfo.GUID; }
    }

    [BepInPlugin(ModInfo.GUID, ModInfo.NAME, ModInfo.VERSION)]
    [BepInDependency("CodeTalker", BepInDependency.DependencyFlags.SoftDependency)]
    public class Main : BaseUnityPlugin
    {
        public static Main instance;
        internal static ManualLogSource Log;
        internal static bool IsReady = false;
        
        private static ConfigEntry<string> _characterProfilesJson;
        // CUSTOM PRESETS CONFIG
        private static ConfigEntry<string> _customPresetsJson;
        private ConfigEntry<bool> _autoSetGlobalNameFromSteam = null!;
        private ConfigEntry<string> _savedOriginalGlobalNickname = null!;
        private ConfigEntry<bool> _hasSavedOriginalGlobalNickname = null!;
        private ConfigEntry<string> _appliedSteamGlobalNickname = null!;
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
            _savedOriginalGlobalNickname = Config.Bind("Steamworks (Internal)", "Original Nickname", "", "Saved nickname used when restoring the original global nickname.");
            _hasSavedOriginalGlobalNickname = Config.Bind("Steamworks (Internal)", "Has Original Nickname", false, "Whether an original global nickname is available to restore.");
            _appliedSteamGlobalNickname = Config.Bind("Steamworks (Internal)", "Applied Steam Nickname", "", "Last Steam nickname applied by RPNames, used to avoid replacing a subsequently edited local nickname.");
            _characterProfilesJson = Config.Bind("2. Data", "CharacterProfiles", "{}", "Stores all title profiles for all character slots in JSON format.");
            _customPresetsJson = Config.Bind("2. Data", "CustomPresetsFull", "{}", "Stores user-defined full title profiles.");

            LoadProfiles();
            LoadCustomPresets(); 
            PopulatePresetTitles();
            
            if (Type.GetType("CodeTalker.Networking.CodeTalkerNetwork, CodeTalker") != null)
            {
                _isCodeTalkerLoaded = true;
                CodeTalkerNetwork.RegisterBinaryListener<Packets.UpdateTitleProfilePacket>(OnProfileUpdate);
                CodeTalkerNetwork.RegisterBinaryListener<Packets.FullSyncPacket>(OnFullSync);
                CodeTalkerNetwork.RegisterListener<Packets.RequestAllTitlesPacket>(OnSyncRequest);
            }
            Harmony.CreateAndPatchAll(typeof(HarmonyPatches));
            Log.LogInfo($"[{ModInfo.NAME} v{ModInfo.VERSION}] has loaded.");
        }

        internal void QueueApplySteamGlobalName(CharacterSelectManager? characterSelect)
        {
            if (!_autoSetGlobalNameFromSteam.Value && !_hasSavedOriginalGlobalNickname.Value)
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

            if (IsPreservingLocalNicknameInput(input))
                yield break;

            if (!_autoSetGlobalNameFromSteam.Value)
            {
                if (_hasSavedOriginalGlobalNickname.Value)
                {
                    if (input.text == _appliedSteamGlobalNickname.Value)
                        SetGlobalNicknameInput(input, _savedOriginalGlobalNickname.Value);

                    _savedOriginalGlobalNickname.Value = "";
                    _hasSavedOriginalGlobalNickname.Value = false;
                    _appliedSteamGlobalNickname.Value = "";
                    Config.Save();
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
            if (!string.IsNullOrWhiteSpace(input.text))
            {
                if (input.text != _appliedSteamGlobalNickname.Value
                    && (_hasSavedOriginalGlobalNickname.Value || !string.IsNullOrEmpty(_appliedSteamGlobalNickname.Value)))
                {
                    _savedOriginalGlobalNickname.Value = "";
                    _hasSavedOriginalGlobalNickname.Value = false;
                    _appliedSteamGlobalNickname.Value = "";
                    Config.Save();
                }

                yield break;
            }

            _savedOriginalGlobalNickname.Value = input.text;
            _hasSavedOriginalGlobalNickname.Value = true;
            _appliedSteamGlobalNickname.Value = steamName;
            Config.Save();
            SetGlobalNicknameInput(input, steamName);
        }

        private static void SetGlobalNicknameInput(InputField input, string text)
        {
            if (input.text == text)
                return;

            input.ActivateInputField();
            input.text = text;
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
                || (!string.IsNullOrWhiteSpace(input.text)
                    && input.text != _appliedSteamGlobalNickname.Value);
        }

        private static string? GetLocalSteamName()
        {
            if (!SteamManager.Initialized)
                return null;

            return Steamworks.SteamFriends.GetPersonaName();
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

            return name;
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
        private void SaveProfiles() { _characterProfilesJson.Value = JsonConvert.SerializeObject(AllCharacterProfiles, Formatting.Indented); Config.Save(); }
        
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
            if (packet is Packets.UpdateTitleProfilePacket p)
            {
                Player sender = FindObjectsOfType<Player>().FirstOrDefault(x => x._steamID == header.SenderID.ToString());
                if (sender != null)
                {
                    ApplyProfileUpdate(sender.netId, p.Profile);
                }
            }
        }
        
        public static void OnFullSync(PacketHeader header, BinaryPacketBase packet)
        {
            if (packet is Packets.FullSyncPacket p)
            {
                Log.LogInfo($"[RPNames] Received full-sync with {p.AllProfiles?.Count ?? 0} profile(s).");
                var localNetIds = HarmonyPatches.PlayerProfiles.Keys.ToList();
                foreach (var localId in localNetIds) if (!p.AllProfiles.ContainsKey(localId)) ApplyProfileUpdate(localId, null);
                foreach (var entry in p.AllProfiles) ApplyProfileUpdate(entry.Key, entry.Value);
            }
        }
        
        private static void ApplyProfileUpdate(uint netId, CharacterTitleProfile profile)
        {
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
                TrySend(() => CodeTalkerNetwork.SendNetworkPacket(header.SenderID, syncPacket, Compressors.CompressionType.GZip));
            } 
        }
        
        public static void SendTitleUpdate(CharacterTitleProfile profile) 
        { 
            if (Player._mainPlayer == null) return;
            var packet = new Packets.UpdateTitleProfilePacket(profile);
            if (TrySend(() => CodeTalkerNetwork.SendNetworkPacket(packet)))
                ApplyProfileUpdate(Player._mainPlayer.netId, profile);
        }
        
        public static void RequestFullTitleSync() 
        { 
            if (_isCodeTalkerLoaded && !_networkSendWarningLogged) 
            {
                Log.LogInfo("[RPNames] Sending full-sync request to server.");
                TrySend(() => CodeTalkerNetwork.SendNetworkPacket(new Packets.RequestAllTitlesPacket()));
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
            
            if (GUILayout.Button("Set & Save")) { AllCharacterProfiles[CurrentCharacterSlot] = JsonConvert.DeserializeObject<CharacterTitleProfile>(JsonConvert.SerializeObject(_uiEditingProfile)); SaveProfiles(); var currentProfile = AllCharacterProfiles[CurrentCharacterSlot]; UpdateGradientCache(currentProfile); SendTitleUpdate(currentProfile); _menuState = MenuState.Closing; }
            
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
    
    // ============== HARMONY PATCHES ==============
    [HarmonyPatch]
    internal static class HarmonyPatches
    {
        internal static readonly Dictionary<uint, CharacterTitleProfile> PlayerProfiles = new Dictionary<uint, CharacterTitleProfile>();
        internal static readonly Dictionary<uint, string> CurrentPlayerTitles = new Dictionary<uint, string>();
        private static readonly FieldInfo _globalNicknameTextMeshField = AccessTools.Field(typeof(Player), "_globalNicknameTextMesh");

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
                
                string globalNamePart = !string.IsNullOrEmpty(__instance._globalNickname) ? "@" + __instance._globalNickname : (hasTitle || hasPronouns ? "@." : "");
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