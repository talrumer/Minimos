using System.Collections.Generic;
using System.Linq;
using Minimos.Announcer;
using Minimos.Audio;
using UnityEditor;
using UnityEngine;

namespace Minimos.Editor
{
    /// <summary>
    /// Auto-populates SFXLibrary, MusicLibrary, and AnnouncerConfig with clips
    /// found in the imported audio asset packs.
    ///
    /// Access via: Minimos → Audio → Auto-Populate All Audio
    /// </summary>
    public static class AudioPopulator
    {
        // Known asset pack paths
        private const string CasualSFXPath = "Assets/Casual Game Sounds U6/CasualGameSounds";
        private const string CoreAudioPath = "Assets/Core/Audio/Clips";
        private const string MusicPath = "Assets/Casual & Relaxing Game Music";
        private const string AnnouncerPath = "Assets/Casual Game Announcer/Normal (fx)";
        private const string DataAudioPath = "Assets/Minimos/Data/Audio";

        [MenuItem("Minimos/Audio/🔊 Auto-Populate All Audio", false, 0)]
        public static void PopulateAll()
        {
            PopulateSFXLibrary();
            PopulateMusicLibrary();
            PopulateAnnouncerConfig();

            AssetDatabase.SaveAssets();
            Debug.Log("✅ [Audio Populator] All audio libraries populated!");
        }

        // =============================================
        // SFX LIBRARY
        // =============================================

        [MenuItem("Minimos/Audio/Populate SFX Library", false, 100)]
        public static void PopulateSFXLibrary()
        {
            var sfxLib = AssetDatabase.LoadAssetAtPath<SFXLibrary>($"{DataAudioPath}/SFXLibrary.asset");
            if (sfxLib == null)
            {
                Debug.LogError("🚨 [Audio Populator] SFXLibrary.asset not found!");
                return;
            }

            var so = new SerializedObject(sfxLib);

            // --- Hit Sounds: Core PlayerHit + some casual SFX ---
            var hitClips = new List<AudioClip>();
            hitClips.AddRange(LoadClipsFromPath(CoreAudioPath + "/Player/Hit Dead And Respawn", "PlayerHit"));
            hitClips.AddRange(LoadClipsByIndex(CasualSFXPath, 1, 2, 3, 4)); // DM-CGS-01 to 04 (impact-like)
            SetClipArray(so, "hitSounds", hitClips);

            // --- Pickup Sounds: Coin + casual SFX ---
            var pickupClips = new List<AudioClip>();
            pickupClips.AddRange(LoadClipsFromPath("Assets/Platformer/Audio/Clips", "Coin"));
            pickupClips.AddRange(LoadClipsByIndex(CasualSFXPath, 10, 11, 12)); // chime-like sounds
            SetClipArray(so, "pickupSounds", pickupClips);

            // --- UI Sounds ---
            var uiClips = new List<AudioClip>();
            uiClips.AddRange(LoadClipsByIndex(CasualSFXPath, 20, 21, 22, 23)); // UI-like clicks/pops
            SetClipArray(so, "uiSounds", uiClips);

            // --- Movement Sounds: Footsteps ---
            var moveClips = new List<AudioClip>();
            moveClips.AddRange(LoadClipsFromPath(CoreAudioPath + "/Player/Footsteps And Landing"));
            SetClipArray(so, "movementSounds", moveClips);

            // --- Celebration Sounds ---
            var celebClips = new List<AudioClip>();
            celebClips.AddRange(LoadClipsByIndex(CasualSFXPath, 40, 41, 42)); // upbeat sounds
            SetClipArray(so, "celebrationSounds", celebClips);

            // --- Countdown ---
            var countdownTick = LoadClipFromPath(CoreAudioPath + "/UI and HUD", "CountdownBeep");
            if (countdownTick != null)
            {
                var tickProp = so.FindProperty("countdownTick");
                if (tickProp != null) tickProp.objectReferenceValue = countdownTick;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sfxLib);

            Debug.Log($"✅ [Audio Populator] SFXLibrary populated: {hitClips.Count} hit, {pickupClips.Count} pickup, {uiClips.Count} UI, {moveClips.Count} movement, {celebClips.Count} celebration.");
        }

        // =============================================
        // MUSIC LIBRARY
        // =============================================

        [MenuItem("Minimos/Audio/Populate Music Library", false, 101)]
        public static void PopulateMusicLibrary()
        {
            var musicLib = AssetDatabase.LoadAssetAtPath<MusicLibrary>($"{DataAudioPath}/MusicLibrary.asset");
            if (musicLib == null)
            {
                Debug.LogError("🚨 [Audio Populator] MusicLibrary.asset not found!");
                return;
            }

            var so = new SerializedObject(musicLib);

            // Map music tracks to game states by name/mood
            var happy = LoadClipFromPath(MusicPath, "Happy");
            var forest = LoadClipFromPath(MusicPath, "Forest");
            var mystery = LoadClipFromPath(MusicPath, "Mystery");
            var spaceWalk = LoadClipFromPath(MusicPath, "Space Walk");
            var darkness = LoadClipFromPath(MusicPath, "Darkness");

            // Main Menu → Happy (upbeat, welcoming)
            SetClipProperty(so, "mainMenuMusic", happy);

            // Lobby → Mystery (anticipation)
            SetClipProperty(so, "lobbyMusic", mystery);

            // Character Studio → Forest (relaxed browsing)
            SetClipProperty(so, "characterStudioMusic", forest);

            // Team Select → Space Walk (chill)
            SetClipProperty(so, "teamSelectMusic", spaceWalk);

            // Mini Game Intro → Happy (energy)
            SetClipProperty(so, "miniGameIntroMusic", happy);

            // Round Results → Forest (calm review)
            SetClipProperty(so, "roundResultsMusic", forest);

            // Final Results → Happy (celebration)
            SetClipProperty(so, "finalResultsMusic", happy);

            // Environment tracks
            var envProp = so.FindProperty("environmentTracks");
            if (envProp != null)
            {
                var environments = new (string name, AudioClip clip)[]
                {
                    ("Sunny Meadows", happy),
                    ("Dusty Gulch", mystery),
                    ("Coral Cove", forest),
                    ("Candy Castle", spaceWalk),
                    ("Cozy Villa", forest),
                    ("Neon Nights", darkness),
                };

                envProp.arraySize = environments.Length;
                for (int i = 0; i < environments.Length; i++)
                {
                    var element = envProp.GetArrayElementAtIndex(i);
                    var nameProp = element.FindPropertyRelative("environmentName");
                    var clipProp = element.FindPropertyRelative("clip");
                    if (nameProp != null) nameProp.stringValue = environments[i].name;
                    if (clipProp != null) clipProp.objectReferenceValue = environments[i].clip;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(musicLib);

            Debug.Log("✅ [Audio Populator] MusicLibrary populated with 5 tracks across states + 6 environments.");
        }

        // =============================================
        // ANNOUNCER CONFIG
        // =============================================

        [MenuItem("Minimos/Audio/Populate Announcer Config", false, 102)]
        public static void PopulateAnnouncerConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<AnnouncerConfig>($"{DataAudioPath}/AnnouncerConfig.asset");
            if (config == null)
            {
                Debug.LogError("🚨 [Audio Populator] AnnouncerConfig.asset not found!");
                return;
            }

            var so = new SerializedObject(config);
            var entriesProp = so.FindProperty("entries");
            if (entriesProp == null)
            {
                Debug.LogError("🚨 [Audio Populator] 'entries' property not found on AnnouncerConfig!");
                return;
            }

            // Map announcer events to voice clip filename patterns
            var eventMappings = new (AnnouncerEvent evt, string[] patterns)[]
            {
                (AnnouncerEvent.MatchStart, new[] { "vo_commence_Let" , "vo_commence_Ready", "vo_commence_Start", "vo_commence_Go" }),
                (AnnouncerEvent.FlagGrabbed, new[] { "vo_action_Grab", "vo_action_Catch", "vo_positive_Nice" }),
                (AnnouncerEvent.FlagDropped, new[] { "vo_finish_Miss", "vo_negative_Oops", "vo_spurt_Ohh" }),
                (AnnouncerEvent.BigKnockback, new[] { "vo_spurt_Ahh", "vo_spurt_Ohh", "vo_spurt_Wow" }),
                (AnnouncerEvent.ScoreMilestone, new[] { "vo_positive_Good", "vo_positive_Nice", "vo_positive_Great" }),
                (AnnouncerEvent.Comeback, new[] { "vo_positive_Do_Your_Best", "vo_positive_Keep", "vo_positive_Almost" }),
                (AnnouncerEvent.CloseFinish, new[] { "vo_finish_It's_A_Tie", "vo_finish_Close", "vo_finish_Photo" }),
                (AnnouncerEvent.FinalSeconds, new[] { "vo_countdown_10", "vo_countdown_05", "vo_time_Time" }),
                (AnnouncerEvent.RoundWin, new[] { "vo_result_The_Winner", "vo_positive_Great", "vo_rank_First" }),
                (AnnouncerEvent.PartyWin, new[] { "vo_result_The_Winner", "vo_positive_Amazing", "vo_rank_First", "vo_positive_Incredible" }),
            };

            entriesProp.arraySize = eventMappings.Length;

            for (int i = 0; i < eventMappings.Length; i++)
            {
                var (evt, patterns) = eventMappings[i];
                var element = entriesProp.GetArrayElementAtIndex(i);

                // Set event type
                var eventProp = element.FindPropertyRelative("eventType");
                if (eventProp != null)
                    eventProp.enumValueIndex = (int)evt;

                // Find matching clips
                var clips = FindAnnouncerClips(patterns, 5); // max 5 clips per event

                var clipsProp = element.FindPropertyRelative("clips");
                if (clipsProp != null)
                {
                    clipsProp.arraySize = clips.Count;
                    for (int j = 0; j < clips.Count; j++)
                    {
                        clipsProp.GetArrayElementAtIndex(j).objectReferenceValue = clips[j];
                    }
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);

            Debug.Log($"✅ [Audio Populator] AnnouncerConfig populated: {eventMappings.Length} events with voice clips.");
        }

        // =============================================
        // HELPERS
        // =============================================

        /// <summary>Load all AudioClips from a folder, optionally filtering by name contains.</summary>
        private static List<AudioClip> LoadClipsFromPath(string folderPath, string nameFilter = null)
        {
            var clips = new List<AudioClip>();
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (nameFilter != null && !System.IO.Path.GetFileNameWithoutExtension(path).Contains(nameFilter))
                    continue;

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) clips.Add(clip);
            }

            return clips;
        }

        /// <summary>Load a single AudioClip by name from a folder.</summary>
        private static AudioClip LoadClipFromPath(string folderPath, string nameContains)
        {
            var clips = LoadClipsFromPath(folderPath, nameContains);
            return clips.Count > 0 ? clips[0] : null;
        }

        /// <summary>Load specific DM-CGS-XX clips by index numbers.</summary>
        private static List<AudioClip> LoadClipsByIndex(string folderPath, params int[] indices)
        {
            var clips = new List<AudioClip>();
            foreach (int idx in indices)
            {
                string fileName = $"DM-CGS-{idx:D2}";
                var clip = LoadClipFromPath(folderPath, fileName);
                if (clip != null) clips.Add(clip);
            }
            return clips;
        }

        /// <summary>Find announcer clips matching any of the given filename patterns.</summary>
        private static List<AudioClip> FindAnnouncerClips(string[] patterns, int maxClips)
        {
            var clips = new List<AudioClip>();
            var allGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { AnnouncerPath });

            foreach (var guid in allGuids)
            {
                if (clips.Count >= maxClips) break;

                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

                foreach (var pattern in patterns)
                {
                    if (fileName.StartsWith(pattern))
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        if (clip != null)
                        {
                            clips.Add(clip);
                            break;
                        }
                    }
                }
            }

            return clips;
        }

        /// <summary>Set an AudioClip[] serialized property from a list.</summary>
        private static void SetClipArray(SerializedObject so, string propertyName, List<AudioClip> clips)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"⚠️ [Audio Populator] Property '{propertyName}' not found.");
                return;
            }

            prop.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            }
        }

        /// <summary>Set a single AudioClip serialized property.</summary>
        private static void SetClipProperty(SerializedObject so, string propertyName, AudioClip clip)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null)
                prop.objectReferenceValue = clip;
            else
                Debug.LogWarning($"⚠️ [Audio Populator] Property '{propertyName}' not found.");
        }
    }
}
