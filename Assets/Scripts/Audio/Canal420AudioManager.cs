using System.Collections.Generic;
using UnityEngine;

namespace Canal420.Audio
{
    public enum FoleySoundId
    {
        None = 0,
        PaperRustle = 1,
        PaperPutAway = 2,
        PillBottleRattle = 3,
        WaterSplash = 4,
        AnsweringMachinePlay = 5,
        AnsweringMachineEmpty = 6,
        KeyPickup = 7,
        BatterySnap = 8,
        DrawerSlide = 9,
        DrawerLocked = 10,
        FuseClackSpark = 11,
        DoorOpen = 12,
        DoorClose = 13,
        DoorLocked = 14,
        DoorUnlockKey = 15,
        TVChannelClick = 16,
        KeypadBeep = 17,
        KeypadError = 18,
        KeypadUnlock = 19,
        FrameCrash = 20,
        ShadowWhoosh = 21,
        CorridorWhispers = 22,
        MargaretStinger = 23,
        ObjectiveStepTick = 24,
        GenericTouch = 25,
        WoodInspect = 26,
        ClothInspect = 27,
        MetalInspect = 28,
        GlassInspect = 29,
        WardrobeDoor = 30,
        CupboardDoor = 31,
        NightstandDrawer = 32,
        FileCabinetDrawer = 33,
        AnsweringMachineBeep = 34,
        BulbFlickerBuzz = 35,
        DistantFloorCreak = 36,
        DreadSubPulse = 37,
        EerieLullabyHum = 38,
        PhantomStepBehind = 39,
        DoorKnockWood = 40,
        GlassTap = 41,
        NeckBreath = 42,
        TinnitusRing = 43,
        LightSwitchClick = 44,
        PowerCutDown = 45,
        TVSubliminalBurst = 46,
        HeavyDoorSlamTrap = 47,
        VisceralSubShock = 48,
        CassetteTapeHauntD2 = 49,
        DryPillSwallow = 50,
        HeavyHeartbeatThud = 51,
        TVGlassThermalCrack = 52,
        PhoneHalfRingAborted = 53,
        InternalLatchForcedSlow = 54,
        FlashlightClick = 55
    }

    public enum SpeechVoiceType
    {
        None = 0,
        CianThought = 1,
        BroadcastTV = 2,
        AnsweringMachine = 3
    }

    public enum TVAudioLoopMode
    {
        Off = 0,
        NormalBroadcast = 1,
        Canal420Hypnotic = 2,
        NewsStaticBleed = 3
    }

    public class Canal420AudioManager : MonoBehaviour
    {
        public static Canal420AudioManager Instance { get; private set; }

        private const int SampleRate = 44100;

        [System.Serializable]
        public struct FoleyClipOverride
        {
            public FoleySoundId soundId;
            public AudioClip clip;
        }

        [Header("Volumes Gerais")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.80f;
        [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.52f;
        [Range(0f, 1f)] [SerializeField] private float speechBlipVolume = 0.12f;
        [Range(0f, 1f)] [SerializeField] private float roomToneVolume = 0.08f;
        [Range(0f, 1f)] [SerializeField] private float foleyVolume = 0.50f;

        [Header("Sons Importados (Assets/Audio)")]
        [SerializeField] private AudioClip[] customWoodSteps;
        [SerializeField] private AudioClip[] customTileSteps;
        [SerializeField] private AudioClip[] customHeavySteps;
        [SerializeField] private AudioClip customRoomTone;
        [SerializeField] private AudioClip customTVNormal;
        [SerializeField] private AudioClip customTV420;
        [SerializeField] private AudioClip customTVBleed;
        [SerializeField] private AudioClip customSpeechCian;
        [SerializeField] private AudioClip customSpeechBroadcast;
        [SerializeField] private AudioClip customSpeechTape;
        [SerializeField] private List<FoleyClipOverride> foleyOverrides = new List<FoleyClipOverride>();
        private AudioSource ui2DSource;
        private AudioSource uiMenuSource;
        private AudioSource speechBlipSource;
        private AudioSource footstepSourceA;
        private AudioSource footstepSourceB;
        private bool footstepSourceFlip = false;
        private AudioSource roomToneSource;
        private AudioSource dreadProximitySource;
        private AudioSource tvSpatialSource;
        private AudioSource binauralCueSource;
        private const int Foley2DPoolSize = 4;
        private AudioSource[] foley2DPool;
        private int foley2DPoolCursor = 0;
        private const int BinauralPoolSize = 3;
        private AudioSource[] binauralPool;
        private int binauralPoolCursor = 0;
        private const int Foley3DPoolSize = 12;
        private AudioSource[] foley3DPool;
        private int foley3DPoolCursor = 0;
        private readonly List<AudioClip> runtimeCreatedClips = new List<AudioClip>();

        private readonly Dictionary<FoleySoundId, AudioClip> foleyClips = new Dictionary<FoleySoundId, AudioClip>();
        private AudioClip[] woodStepClips;
        private AudioClip[] tileStepClips;
        private AudioClip[] heavyStepClips;

        private AudioClip speechCianClip;
        private AudioClip speechBroadcastClip;
        private AudioClip speechTapeClip;

        private AudioClip tvNormalLoopClip;
        private AudioClip tv420LoopClip;
        private AudioClip tvBleedLoopClip;
        private AudioClip roomToneLoopClip;
        private AudioClip dreadProximityLoopClip;

        private float targetRoomToneVolume = 0.08f;
        private float targetRoomTonePitch = 1.0f;
        private float targetDreadWeight = 0f;
        private float baseTvSpatialTargetVolume = 0f;
        private float tvFadeMultiplier = 1f;
        private float tvFadeOutRate = 0f;
        private float silenceDuckTimer = 0f;
        private float silenceDuckMultiplier = 1f;
        private float silenceDuckRecoverySpeed = 1.2f;
        private float stimulantClarity01 = 0f;

        private int lastStepIndex = -1;
        private float lastSpeechBlipTime = -10f;
        private float userMasterVolume = 0.85f;
        private float lastPreviewTickUnscaledTime = -10f;

        public float UserMasterVolume => userMasterVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

#if UNITY_EDITOR
            AutoAssignImportedClips();
#endif

            ApplyImportedOverrides();
            SynthesizeMissingProceduralFallbacks();

            SetupCoreAudioSources();
            userMasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Canal420_MasterVolume", 0.85f));
            AudioListener.volume = userMasterVolume;
        }

        public void SetUserMasterVolume(float volume01, bool playFeedbackTick = false)
        {
            userMasterVolume = Mathf.Clamp01(volume01);
            AudioListener.volume = userMasterVolume;

            if (playFeedbackTick && userMasterVolume > 0.01f && (Time.unscaledTime - lastPreviewTickUnscaledTime) >= 0.09f)
            {
                lastPreviewTickUnscaledTime = Time.unscaledTime;
                PlayMenuClick(0.35f, Random.Range(1.04f, 1.12f));
            }
        }

        public void PlayMenuClick(float volMultiplier = 0.32f, float pitchMultiplier = 0.96f)
        {
            AudioSource target = uiMenuSource != null ? uiMenuSource : ui2DSource;
            if (target == null) return;

            if (foleyClips.TryGetValue(FoleySoundId.TVChannelClick, out AudioClip clip) && clip != null)
            {
                float baseCalibration = GetCalibratedFoleyVolume(FoleySoundId.TVChannelClick);
                float finalVol = Mathf.Clamp01(baseCalibration * foleyVolume * 1.25f * volMultiplier * masterVolume);
                target.pitch = Mathf.Clamp(pitchMultiplier, 0.5f, 2.0f);
                target.PlayOneShot(clip, finalVol);
            }
        }

        private AudioClip RegisterRuntimeClip(AudioClip clip)
        {
            if (clip != null)
            {
                runtimeCreatedClips.Add(clip);
            }
            return clip;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            for (int i = 0; i < runtimeCreatedClips.Count; i++)
            {
                AudioClip clip = runtimeCreatedClips[i];
                if (clip != null)
                {
                    if (Application.isPlaying) Destroy(clip);
                    else DestroyImmediate(clip);
                }
            }
            runtimeCreatedClips.Clear();
        }

        private void SetupCoreAudioSources()
        {
            foley2DPool = new AudioSource[Foley2DPoolSize];
            for (int i = 0; i < Foley2DPoolSize; i++)
            {
                foley2DPool[i] = CreateChild2DSource($"Audio_Foley2D_{i:00}", 1f);
            }
            ui2DSource = foley2DPool[0];

            uiMenuSource = CreateChild2DSource("Audio_MenuUI", 1f);
            uiMenuSource.ignoreListenerPause = true;

            speechBlipSource = CreateChild2DSource("Audio_SpeechBlips", 1f);
            footstepSourceA = CreateChild2DSource("Audio_Footsteps_A", 1f);
            footstepSourceB = CreateChild2DSource("Audio_Footsteps_B", 1f);

            targetRoomToneVolume = roomToneVolume * 1.15f * masterVolume;
            targetRoomTonePitch = 0.92f;
            roomToneSource = CreateChild2DSource("Audio_RoomTone", targetRoomToneVolume);
            roomToneSource.clip = roomToneLoopClip;
            roomToneSource.pitch = targetRoomTonePitch;
            roomToneSource.loop = true;
            roomToneSource.Play();

            dreadProximitySource = CreateChild2DSource("Audio_DreadProximity", 0f);
            dreadProximitySource.clip = dreadProximityLoopClip;
            dreadProximitySource.loop = true;
            dreadProximitySource.Play();

            binauralPool = new AudioSource[BinauralPoolSize];
            for (int i = 0; i < BinauralPoolSize; i++)
            {
                binauralPool[i] = CreateChild2DSource($"Audio_BinauralCue_{i:00}", 1f);
            }
            binauralCueSource = binauralPool[0];

            foley3DPool = new AudioSource[Foley3DPoolSize];
            for (int i = 0; i < Foley3DPoolSize; i++)
            {
                GameObject slotObj = new GameObject($"Audio_Foley3D_{i:00}");
                slotObj.transform.SetParent(transform, false);
                AudioSource src = slotObj.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.dopplerLevel = 0f;
                foley3DPool[i] = src;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0.001f)
                return;

            if (silenceDuckTimer > 0f)
            {
                silenceDuckTimer -= Time.unscaledDeltaTime;
                silenceDuckMultiplier = 0f;
            }
            else if (silenceDuckMultiplier < 1f)
            {
                silenceDuckMultiplier = Mathf.MoveTowards(silenceDuckMultiplier, 1f, Time.unscaledDeltaTime * silenceDuckRecoverySpeed);
            }

            if (roomToneSource != null)
            {
                float clarityRoomScale = Mathf.Lerp(1f, 0.72f, stimulantClarity01);
                float desiredRoomVol = targetRoomToneVolume * clarityRoomScale * silenceDuckMultiplier;
                float moveSpeed = silenceDuckTimer > 0f ? 20f : 0.14f;
                roomToneSource.volume = Mathf.MoveTowards(roomToneSource.volume, desiredRoomVol, Time.unscaledDeltaTime * moveSpeed);
                float desiredPitch = Mathf.Lerp(targetRoomTonePitch, Mathf.Min(1.0f, targetRoomTonePitch + 0.14f), stimulantClarity01);
                roomToneSource.pitch = Mathf.MoveTowards(roomToneSource.pitch, desiredPitch, Time.unscaledDeltaTime * 0.28f);
            }

            if (dreadProximitySource != null)
            {
                float clarityDreadScale = Mathf.Lerp(1f, 0.68f, stimulantClarity01);
                float targetVol = Mathf.Clamp01(targetDreadWeight) * 0.34f * masterVolume * clarityDreadScale * silenceDuckMultiplier;
                float moveSpeed = silenceDuckTimer > 0f ? 20f : 0.45f;
                dreadProximitySource.volume = Mathf.MoveTowards(dreadProximitySource.volume, targetVol, Time.unscaledDeltaTime * moveSpeed);
                dreadProximitySource.pitch = Mathf.Lerp(0.88f, 1.08f, Mathf.Clamp01(targetDreadWeight));
            }

            if (tvSpatialSource != null && tvSpatialSource.isPlaying && tvSpatialSource.clip != null && baseTvSpatialTargetVolume > 0f)
            {
                if (tvFadeOutRate > 0f)
                {
                    tvFadeMultiplier = Mathf.MoveTowards(tvFadeMultiplier, 0f, Time.deltaTime * tvFadeOutRate);
                    if (tvFadeMultiplier <= 0.001f)
                    {
                        SetTVLoopMode(TVAudioLoopMode.Off);
                        return;
                    }
                }

                float tvTarget = baseTvSpatialTargetVolume * silenceDuckMultiplier * tvFadeMultiplier;
                float moveSpeed = silenceDuckTimer > 0f
                    ? 20f
                    : (tvFadeOutRate > 0f
                        ? Mathf.Max(0.8f, baseTvSpatialTargetVolume * tvFadeOutRate * 1.4f)
                        : Mathf.Max(0.45f, silenceDuckRecoverySpeed * 0.45f));
                tvSpatialSource.volume = Mathf.MoveTowards(tvSpatialSource.volume, tvTarget, Time.unscaledDeltaTime * moveSpeed);
            }
        }

        private AudioSource CreateChild2DSource(string childName, float vol)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = vol;
            return src;
        }

        public void ApplyImportedOverrides()
        {
            AudioClip[] preparedWood = PrepareTightStepClips(customWoodSteps);
            if (preparedWood != null && preparedWood.Length > 0)
            {
                woodStepClips = preparedWood;
            }
            AudioClip[] preparedTile = PrepareTightStepClips(customTileSteps);
            if (preparedTile != null && preparedTile.Length > 0)
            {
                tileStepClips = preparedTile;
            }
            AudioClip[] preparedHeavy = PrepareTightStepClips(customHeavySteps);
            if (preparedHeavy != null && preparedHeavy.Length > 0)
            {
                heavyStepClips = preparedHeavy;
            }

            if (customRoomTone != null)
            {
                roomToneLoopClip = customRoomTone;
                if (roomToneSource != null)
                {
                    roomToneSource.clip = roomToneLoopClip;
                    if (!roomToneSource.isPlaying) roomToneSource.Play();
                }
            }
            if (customTVNormal != null) tvNormalLoopClip = customTVNormal;
            if (customTV420 != null) tv420LoopClip = customTV420;
            if (customTVBleed != null) tvBleedLoopClip = customTVBleed;

            if (customSpeechCian != null) speechCianClip = customSpeechCian;
            if (customSpeechBroadcast != null) speechBroadcastClip = customSpeechBroadcast;
            if (customSpeechTape != null) speechTapeClip = customSpeechTape;

            if (foleyOverrides != null)
            {
                for (int i = 0; i < foleyOverrides.Count; i++)
                {
                    if (foleyOverrides[i].soundId == FoleySoundId.ObjectiveStepTick)
                        continue;

                    if (foleyOverrides[i].clip != null)
                    {
                        foleyClips[foleyOverrides[i].soundId] = foleyOverrides[i].clip;
                    }
                }
            }
        }

        private AudioClip[] PrepareTightStepClips(AudioClip[] sourceClips)
        {
            if (sourceClips == null || sourceClips.Length == 0)
                return null;

            List<AudioClip> result = new List<AudioClip>(sourceClips.Length);
            for (int i = 0; i < sourceClips.Length; i++)
            {
                AudioClip src = sourceClips[i];
                if (src == null)
                    continue;

                AudioClip tight = TrimLeadingSilenceIfPossible(src);
                if (tight != null)
                {
                    result.Add(tight);
                }
            }
            return result.Count > 0 ? result.ToArray() : null;
        }

        private AudioClip TrimLeadingSilenceIfPossible(AudioClip src)
        {
            if (src == null || src.loadType != AudioClipLoadType.DecompressOnLoad || src.samples <= 256)
                return src;

            try
            {
                int channels = Mathf.Max(1, src.channels);
                float[] data = new float[src.samples * channels];
                if (!src.GetData(data, 0))
                    return src;

                float peak = 0f;
                for (int s = 0; s < data.Length; s++)
                {
                    float a = Mathf.Abs(data[s]);
                    if (a > peak) peak = a;
                }

                if (peak < 0.01f)
                    return src;

                float threshold = Mathf.Max(0.012f, peak * 0.10f);
                int firstFrame = 0;
                int totalFrames = src.samples;
                for (int f = 0; f < totalFrames; f++)
                {
                    float frameMax = 0f;
                    int baseIdx = f * channels;
                    for (int c = 0; c < channels; c++)
                    {
                        float a = Mathf.Abs(data[baseIdx + c]);
                        if (a > frameMax) frameMax = a;
                    }
                    if (frameMax >= threshold)
                    {
                        firstFrame = f;
                        break;
                    }
                }

                int preRollFrames = Mathf.RoundToInt(src.frequency * 0.002f);
                int trimStartFrame = Mathf.Max(0, firstFrame - preRollFrames);

                if (trimStartFrame < Mathf.RoundToInt(src.frequency * 0.005f))
                    return src;

                int keptFrames = totalFrames - trimStartFrame;
                if (keptFrames < 256)
                    return src;

                float[] trimmed = new float[keptFrames * channels];
                System.Array.Copy(data, trimStartFrame * channels, trimmed, 0, trimmed.Length);

                int fadeSamples = Mathf.Min(32, keptFrames);
                for (int f = 0; f < fadeSamples; f++)
                {
                    float gain = f / (float)fadeSamples;
                    for (int c = 0; c < channels; c++)
                    {
                        trimmed[f * channels + c] *= gain;
                    }
                }

                AudioClip tightClip = RegisterRuntimeClip(AudioClip.Create(src.name + "_Tight", keptFrames, channels, src.frequency, false));
                tightClip.SetData(trimmed, 0);
                return tightClip;
            }
            catch
            {
                return src;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Buscar e Conectar Áudios de Assets/Audio")]
        public void AutoAssignImportedClips()
        {
            var woodList = new List<AudioClip>();
            for (int i = 1; i <= 10; i++)
            {
                string path = $"Assets/Audio/Footsteps/step_wood_{i:D2}.wav";
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) woodList.Add(clip);
            }
            if (woodList.Count > 0) customWoodSteps = woodList.ToArray();

            var tileList = new List<AudioClip>();
            for (int i = 1; i <= 10; i++)
            {
                string path = $"Assets/Audio/Footsteps/step_tile_{i:D2}.wav";
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) tileList.Add(clip);
            }
            if (tileList.Count > 0) customTileSteps = tileList.ToArray();

            var heavyList = new List<AudioClip>();
            for (int i = 1; i <= 10; i++)
            {
                string path = $"Assets/Audio/Footsteps/step_heavy_{i:D2}.wav";
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) heavyList.Add(clip);
            }
            if (heavyList.Count > 0) customHeavySteps = heavyList.ToArray();

            customRoomTone = TryLoadClip("Ambience/house_room_tone");
            customTVNormal = TryLoadClip("Ambience/tv_static_normal");
            customTV420 = TryLoadClip("Ambience/tv_420_hypnotic");
            customTVBleed = TryLoadClip("Ambience/tv_static_bleed") ?? customTVNormal;

            foleyOverrides.Clear();
            MapFoley(FoleySoundId.DoorOpen, "Foley/door_open");
            MapFoley(FoleySoundId.DoorClose, "Foley/door_close");
            MapFoley(FoleySoundId.DoorLocked, "Foley/door_locked");
            MapFoley(FoleySoundId.DoorUnlockKey, "Foley/door_unlock_key");
            MapFoley(FoleySoundId.PaperRustle, "Foley/paper_open");
            MapFoley(FoleySoundId.PaperPutAway, "Foley/paper_close");
            MapFoley(FoleySoundId.PillBottleRattle, "Foley/pill_bottle_rattle");
            MapFoley(FoleySoundId.WaterSplash, "Foley/water_faucet");
            MapFoley(FoleySoundId.KeyPickup, "Foley/keys_pickup");
            MapFoley(FoleySoundId.DrawerSlide, "Foley/drawer_open");
            MapFoley(FoleySoundId.DrawerLocked, "Foley/drawer_locked", "Foley/drawer_open");
            MapFoley(FoleySoundId.FuseClackSpark, "Foley/fuse_spark");
            MapFoley(FoleySoundId.BatterySnap, "Foley/battery_snap");
            MapFoley(FoleySoundId.KeypadBeep, "Foley/keypad_beep");
            MapFoley(FoleySoundId.KeypadError, "Foley/keypad_error");
            MapFoley(FoleySoundId.KeypadUnlock, "Foley/keypad_unlock");
            MapFoley(FoleySoundId.FrameCrash, "Foley/frame_crash");
            MapFoley(FoleySoundId.MargaretStinger, "Foley/margaret_jumpscare");
            MapFoley(FoleySoundId.AnsweringMachineEmpty, "Foley/answering_machine_empty");
            MapFoley(FoleySoundId.AnsweringMachinePlay, "Foley/answering_machine_play");
            MapFoley(FoleySoundId.TVChannelClick, "Foley/tv_channel_click");
            MapFoley(FoleySoundId.GenericTouch, "Foley/generic_touch", "Foley/wood_inspect");
            MapFoley(FoleySoundId.WoodInspect, "Foley/wood_inspect");
            MapFoley(FoleySoundId.ClothInspect, "Foley/cloth_inspect");
            MapFoley(FoleySoundId.MetalInspect, "Foley/metal_inspect");
            MapFoley(FoleySoundId.GlassInspect, "Foley/glass_inspect");
            MapFoley(FoleySoundId.WardrobeDoor, "Foley/wardrobe_door", "Foley/drawer_open");
            MapFoley(FoleySoundId.CupboardDoor, "Foley/cupboard_door", "Foley/drawer_open");
            MapFoley(FoleySoundId.NightstandDrawer, "Foley/nightstand_drawer", "Foley/drawer_open");
            MapFoley(FoleySoundId.FileCabinetDrawer, "Foley/file_cabinet_drawer", "Foley/drawer_open");
            MapFoley(FoleySoundId.ShadowWhoosh, "Foley/shadow_whoosh");
            MapFoley(FoleySoundId.CorridorWhispers, "Foley/corridor_whispers");
            // Foley/eerie_lullaby_hum.mp3 on disk is an exact byte-for-byte duplicate of Ambience/tv_420_hypnotic.mp3;
            // leave EerieLullabyHum unmapped unless a dedicated vocal lullaby recording is provided so CreateEerieLullabyHum() plays.
            MapFoley(FoleySoundId.EerieLullabyHum, "Foley/eerie_lullaby_vocals");
            MapFoley(FoleySoundId.PhantomStepBehind, "Foley/phantom_step_behind", "Footsteps/step_heavy_01");
            MapFoley(FoleySoundId.DoorKnockWood, "Foley/door_knock_wood", "Foley/wood_inspect");
            MapFoley(FoleySoundId.GlassTap, "Foley/glass_tap", "Foley/glass_inspect");
            MapFoley(FoleySoundId.NeckBreath, "Foley/neck_breath");
            MapFoley(FoleySoundId.TinnitusRing, "Foley/tinnitus_ring");
            MapFoley(FoleySoundId.LightSwitchClick, "Foley/light_switch_click", "Foley/tv_channel_click");
            MapFoley(FoleySoundId.PowerCutDown, "Foley/power_cut_down");
            MapFoley(FoleySoundId.TVSubliminalBurst, "Foley/tv_subliminal_burst");
            MapFoley(FoleySoundId.FlashlightClick, "Foley/flashlight_click", "Foley/battery_snap");
            // Leave unmapped when no dedicated long haunt recording exists on disk so
            // SynthesizeMissingProceduralFallbacks() generates the full 6.20s CreateCassetteTapeHauntD2 clip.
            MapFoley(FoleySoundId.CassetteTapeHauntD2, "Foley/cassette_tape_haunt_d2");

            customSpeechCian = TryLoadClip("Voice/speech_cian");
            customSpeechBroadcast = TryLoadClip("Voice/speech_broadcast");
            customSpeechTape = TryLoadClip("Voice/speech_tape");

            UnityEditor.EditorUtility.SetDirty(this);
        }

        private static AudioClip TryLoadClip(string relPathNoExt)
        {
            string[] exts = { ".mp3", ".wav", ".ogg" };
            foreach (var ext in exts)
            {
                string p = $"Assets/Audio/{relPathNoExt}{ext}";
                var c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                if (c != null) return c;
            }
            return null;
        }

        private void MapFoley(FoleySoundId id, string primaryPath, string fallbackPath = null)
        {
            var clip = TryLoadClip(primaryPath) ?? (fallbackPath != null ? TryLoadClip(fallbackPath) : null);
            if (clip != null)
            {
                foleyOverrides.Add(new FoleyClipOverride { soundId = id, clip = clip });
            }
        }
#endif

        public void SetDayAmbienceMood(int dayNumber)
        {
            targetDreadWeight = 0f;
            stimulantClarity01 = 0f;
            switch (dayNumber)
            {
                case 1:
                    targetRoomTonePitch = 0.90f;
                    targetRoomToneVolume = roomToneVolume * 1.15f * masterVolume;
                    break;
                case 2:
                    targetRoomTonePitch = 0.76f;
                    targetRoomToneVolume = roomToneVolume * 1.55f * masterVolume;
                    break;
                case 3:
                    targetRoomTonePitch = 0.62f;
                    targetRoomToneVolume = roomToneVolume * 2.10f * masterVolume;
                    break;
                default:
                    targetRoomTonePitch = 0.84f;
                    targetRoomToneVolume = roomToneVolume * 0.85f * masterVolume;
                    break;
            }

            if (roomToneSource != null && !roomToneSource.isPlaying)
            {
                roomToneSource.pitch = targetRoomTonePitch;
                roomToneSource.volume = targetRoomToneVolume;
                roomToneSource.Play();
            }
        }

        public void SetDreadProximityWeight(float weight01)
        {
            targetDreadWeight = Mathf.Clamp01(weight01);
        }

        public void SetStimulantRoomToneClarity(float clarity01)
        {
            stimulantClarity01 = Mathf.Clamp01(clarity01);
        }

        public void PulseSilenceDread(float silenceDuration, float recoverySpeed = 1.2f)
        {
            silenceDuckTimer = Mathf.Max(silenceDuckTimer, Mathf.Max(0.10f, silenceDuration));
            silenceDuckMultiplier = 0f;
            silenceDuckRecoverySpeed = Mathf.Max(0.20f, recoverySpeed);

            if (roomToneSource != null) roomToneSource.volume = 0f;
            if (dreadProximitySource != null) dreadProximitySource.volume = 0f;
            if (tvSpatialSource != null && tvSpatialSource.isPlaying) tvSpatialSource.volume = 0f;
        }

        private static AudioSource AcquirePoolSlot(AudioSource[] pool, ref int cursor, AudioSource fallback)
        {
            if (pool != null && pool.Length > 0)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    int idx = (cursor + i) % pool.Length;
                    AudioSource candidate = pool[idx];
                    if (candidate != null && !candidate.isPlaying)
                    {
                        cursor = (idx + 1) % pool.Length;
                        return candidate;
                    }
                }
                AudioSource next = pool[cursor];
                cursor = (cursor + 1) % pool.Length;
                if (next != null) return next;
            }
            return fallback;
        }

        public void PlayBinauralCue(FoleySoundId soundId, float panStereo = 0f, float volMultiplier = 1f, float pitchMultiplier = 1f)
        {
            AudioSource targetSource = AcquirePoolSlot(binauralPool, ref binauralPoolCursor, binauralCueSource != null ? binauralCueSource : ui2DSource);
            if (soundId == FoleySoundId.None || targetSource == null)
                return;

            if (foleyClips.TryGetValue(soundId, out AudioClip clip) && clip != null)
            {
                float baseCalibration = GetCalibratedFoleyVolume(soundId);
                float finalVol = Mathf.Clamp01(baseCalibration * foleyVolume * 1.25f * volMultiplier * masterVolume);
                targetSource.panStereo = Mathf.Clamp(panStereo, -1f, 1f);
                targetSource.pitch = Mathf.Clamp(pitchMultiplier * Random.Range(0.96f, 1.04f), 0.5f, 2.0f);
                targetSource.PlayOneShot(clip, finalVol);
            }
        }

        public void PlayBehindPlayer(FoleySoundId soundId, Transform playerTransform, float distanceBehind = 1.15f, float lateralOffset = 0.28f, float volMultiplier = 1f, float panSign = 0f)
        {
            if (soundId == FoleySoundId.None) return;

            float side = Mathf.Abs(panSign) > 0.05f ? Mathf.Sign(panSign) : (Random.value < 0.5f ? -1f : 1f);
            if (playerTransform == null)
            {
                PlayBinauralCue(soundId, side * 0.72f, volMultiplier, 1f);
                return;
            }

            Vector3 fwd = playerTransform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

            Vector3 worldPos = playerTransform.position
                + Vector3.up * 1.48f
                - fwd * Mathf.Max(0.45f, distanceBehind)
                + right * (side * Mathf.Abs(lateralOffset));

            PlayFoley3DPanned(soundId, worldPos, volMultiplier, minDistance: 0.65f, maxDistance: 9.5f);
            PlayBinauralCue(soundId, side * 0.72f, volMultiplier * 0.55f, Random.Range(0.95f, 1.03f));
        }

        public void PlayFootstep(bool isColdTile, bool isDepressantHeavy)
        {
            bool useB = footstepSourceFlip;
            AudioSource src = useB ? footstepSourceB : footstepSourceA;
            footstepSourceFlip = !footstepSourceFlip;
            if (src == null) src = footstepSourceA;
            if (src == null) return;

            AudioClip clip = null;
            float volScale = 1.0f;

            AudioClip[] pool = woodStepClips;
            if (isDepressantHeavy && heavyStepClips != null && heavyStepClips.Length > 0)
            {
                pool = heavyStepClips;
                volScale = 0.92f;
            }
            else if (isColdTile && tileStepClips != null && tileStepClips.Length > 0)
            {
                pool = tileStepClips;
                volScale = 0.88f;
            }
            else if (woodStepClips != null && woodStepClips.Length > 0)
            {
                pool = woodStepClips;
                volScale = 1.0f;
            }

            if (pool != null && pool.Length > 0)
            {
                int nextIdx = lastStepIndex;
                if (pool.Length > 1)
                {
                    int attempts = 0;
                    while (nextIdx == lastStepIndex && attempts < 8)
                    {
                        nextIdx = Random.Range(0, pool.Length);
                        attempts++;
                    }
                }
                else
                {
                    nextIdx = 0;
                }
                lastStepIndex = nextIdx;
                clip = pool[nextIdx];
            }

            if (clip != null)
            {
                src.panStereo = useB ? 0.055f : -0.055f;
                src.pitch = Random.Range(0.96f, 1.03f);
                src.PlayOneShot(clip, Mathf.Clamp01(footstepVolume * 0.68f * volScale * masterVolume));
            }
        }

        public void PlaySpeechBlip(SpeechVoiceType voice)
        {
            if (voice == SpeechVoiceType.None || speechBlipSource == null)
                return;

            if (Time.unscaledTime - lastSpeechBlipTime < 0.055f)
                return;

            lastSpeechBlipTime = Time.unscaledTime;

            AudioClip clip = speechCianClip;
            float pitchMin = 0.94f;
            float pitchMax = 1.06f;

            switch (voice)
            {
                case SpeechVoiceType.CianThought:
                    clip = speechCianClip;
                    pitchMin = 0.92f;
                    pitchMax = 1.08f;
                    break;
                case SpeechVoiceType.BroadcastTV:
                    clip = speechBroadcastClip;
                    pitchMin = 0.97f;
                    pitchMax = 1.03f;
                    break;
                case SpeechVoiceType.AnsweringMachine:
                    clip = speechTapeClip;
                    pitchMin = 0.98f;
                    pitchMax = 1.02f;
                    break;
            }

            if (clip != null)
            {
                speechBlipSource.pitch = Random.Range(pitchMin, pitchMax);
                speechBlipSource.PlayOneShot(clip, Mathf.Clamp01(speechBlipVolume * 0.72f * masterVolume));
            }
        }

        public void PlayFoley2D(FoleySoundId soundId, float volMultiplier = 1f, float pitchMultiplier = 1f)
        {
            AudioSource targetSource = AcquirePoolSlot(foley2DPool, ref foley2DPoolCursor, ui2DSource);
            if (soundId == FoleySoundId.None || targetSource == null)
                return;

            if (foleyClips.TryGetValue(soundId, out AudioClip clip) && clip != null)
            {
                float baseCalibration = GetCalibratedFoleyVolume(soundId);
                float finalVol = Mathf.Clamp01(baseCalibration * foleyVolume * 1.25f * volMultiplier * masterVolume);
                targetSource.pitch = Mathf.Clamp(pitchMultiplier * Random.Range(0.97f, 1.03f), 0.5f, 2.0f);
                targetSource.PlayOneShot(clip, finalVol);
            }
        }

        public void PlayFoley3D(FoleySoundId soundId, Vector3 worldPosition, float volMultiplier = 1f)
        {
            PlayFoley3DPanned(soundId, worldPosition, volMultiplier, minDistance: 0.9f, maxDistance: 15f, pitchMultiplier: 1f);
        }

        public void PlayFoley3DPanned(FoleySoundId soundId, Vector3 worldPosition, float volMultiplier = 1f, float minDistance = 0.8f, float maxDistance = 14f, float pitchMultiplier = 1f)
        {
            if (soundId == FoleySoundId.None)
                return;

            if (foleyClips.TryGetValue(soundId, out AudioClip clip) && clip != null && foley3DPool != null && foley3DPool.Length > 0)
            {
                float baseCalibration = GetCalibratedFoleyVolume(soundId);
                float finalVol = Mathf.Clamp01(baseCalibration * foleyVolume * 1.45f * volMultiplier * masterVolume);
                AudioSource src = null;
                for (int i = 0; i < foley3DPool.Length; i++)
                {
                    int idx = (foley3DPoolCursor + i) % foley3DPool.Length;
                    AudioSource candidate = foley3DPool[idx];
                    if (candidate != null && !candidate.isPlaying)
                    {
                        src = candidate;
                        foley3DPoolCursor = (idx + 1) % foley3DPool.Length;
                        break;
                    }
                }

                if (src == null)
                {
                    src = foley3DPool[foley3DPoolCursor];
                    foley3DPoolCursor = (foley3DPoolCursor + 1) % foley3DPool.Length;
                }

                if (src == null) return;

                src.Stop();
                src.transform.position = worldPosition;
                src.clip = clip;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = Mathf.Max(0.25f, minDistance);
                src.maxDistance = Mathf.Max(src.minDistance + 1f, maxDistance);
                src.dopplerLevel = 0f;
                src.pitch = Mathf.Clamp(pitchMultiplier * Random.Range(0.97f, 1.03f), 0.5f, 2.0f);
                src.volume = finalVol;
                src.Play();
            }
        }

        private static float GetCalibratedFoleyVolume(FoleySoundId id)
        {
            switch (id)
            {
                case FoleySoundId.WaterSplash: return 0.36f;
                case FoleySoundId.PillBottleRattle: return 0.36f;
                case FoleySoundId.KeyPickup: return 0.35f;
                case FoleySoundId.PaperRustle: return 0.34f;
                case FoleySoundId.PaperPutAway: return 0.30f;
                case FoleySoundId.DoorOpen: return 0.44f;
                case FoleySoundId.DoorClose: return 0.46f;
                case FoleySoundId.DoorLocked: return 0.40f;
                case FoleySoundId.DoorUnlockKey: return 0.44f;
                case FoleySoundId.DrawerSlide: return 0.38f;
                case FoleySoundId.WardrobeDoor: return 0.42f;
                case FoleySoundId.CupboardDoor: return 0.35f;
                case FoleySoundId.NightstandDrawer: return 0.34f;
                case FoleySoundId.FileCabinetDrawer: return 0.40f;
                case FoleySoundId.BatterySnap: return 0.32f;
                case FoleySoundId.KeypadBeep: return 0.28f;
                case FoleySoundId.KeypadError: return 0.36f;
                case FoleySoundId.KeypadUnlock: return 0.45f;
                case FoleySoundId.GenericTouch: return 0.28f;
                case FoleySoundId.WoodInspect: return 0.32f;
                case FoleySoundId.ClothInspect: return 0.28f;
                case FoleySoundId.MetalInspect: return 0.32f;
                case FoleySoundId.GlassInspect: return 0.30f;
                case FoleySoundId.TVChannelClick: return 0.38f;
                case FoleySoundId.ObjectiveStepTick: return 0.28f;
                case FoleySoundId.FrameCrash: return 0.72f;
                case FoleySoundId.ShadowWhoosh: return 0.74f;
                case FoleySoundId.CorridorWhispers: return 0.66f;
                case FoleySoundId.MargaretStinger: return 0.88f;
                case FoleySoundId.AnsweringMachineBeep: return 0.20f;
                case FoleySoundId.BulbFlickerBuzz: return 0.30f;
                case FoleySoundId.DistantFloorCreak: return 0.34f;
                case FoleySoundId.DreadSubPulse: return 0.54f;
                case FoleySoundId.EerieLullabyHum: return 0.32f;
                case FoleySoundId.PhantomStepBehind: return 0.56f;
                case FoleySoundId.DoorKnockWood: return 0.78f;
                case FoleySoundId.GlassTap: return 0.62f;
                case FoleySoundId.NeckBreath: return 0.68f;
                case FoleySoundId.TinnitusRing: return 0.40f;
                case FoleySoundId.LightSwitchClick: return 0.48f;
                case FoleySoundId.PowerCutDown: return 0.82f;
                case FoleySoundId.TVSubliminalBurst: return 0.74f;
                case FoleySoundId.HeavyDoorSlamTrap: return 0.94f;
                case FoleySoundId.VisceralSubShock: return 0.92f;
                case FoleySoundId.CassetteTapeHauntD2: return 0.68f;
                case FoleySoundId.DryPillSwallow: return 0.48f;
                case FoleySoundId.HeavyHeartbeatThud: return 0.56f;
                case FoleySoundId.TVGlassThermalCrack: return 0.40f;
                case FoleySoundId.PhoneHalfRingAborted: return 0.60f;
                case FoleySoundId.InternalLatchForcedSlow: return 0.68f;
                case FoleySoundId.AnsweringMachinePlay: return 0.42f;
                case FoleySoundId.AnsweringMachineEmpty: return 0.36f;
                case FoleySoundId.DrawerLocked: return 0.36f;
                case FoleySoundId.FuseClackSpark: return 0.52f;
                case FoleySoundId.FlashlightClick: return 0.34f;
                default: return 0.38f;
            }
        }

        public void AttachTVAudioSource(Transform tvTransform)
        {
            if (tvTransform == null) return;

            tvSpatialSource = tvTransform.GetComponent<AudioSource>();
            if (tvSpatialSource == null)
            {
                tvSpatialSource = tvTransform.gameObject.AddComponent<AudioSource>();
            }

            tvSpatialSource.playOnAwake = false;
            tvSpatialSource.loop = true;
            tvSpatialSource.spatialBlend = 1f;
            tvSpatialSource.minDistance = 1.2f;
            tvSpatialSource.maxDistance = 16.5f;
            tvSpatialSource.rolloffMode = AudioRolloffMode.Linear;
            tvSpatialSource.volume = 0f;
        }

        public void FadeOutAndStopTVLoop(float durationSeconds)
        {
            if (tvSpatialSource == null || !tvSpatialSource.isPlaying || baseTvSpatialTargetVolume <= 0.001f)
            {
                SetTVLoopMode(TVAudioLoopMode.Off);
                return;
            }

            if (durationSeconds <= 0.05f)
            {
                SetTVLoopMode(TVAudioLoopMode.Off);
                return;
            }

            tvFadeOutRate = 1f / durationSeconds;
        }

        public void Stop2DFoleyImmediate(bool stop3DPool = false)
        {
            if (foley2DPool != null)
            {
                for (int i = 0; i < foley2DPool.Length; i++)
                {
                    if (foley2DPool[i] != null) foley2DPool[i].Stop();
                }
            }
            else if (ui2DSource != null)
            {
                ui2DSource.Stop();
            }

            if (binauralPool != null)
            {
                for (int i = 0; i < binauralPool.Length; i++)
                {
                    if (binauralPool[i] != null) binauralPool[i].Stop();
                }
            }
            else if (binauralCueSource != null)
            {
                binauralCueSource.Stop();
            }

            if (stop3DPool && foley3DPool != null)
            {
                for (int i = 0; i < foley3DPool.Length; i++)
                {
                    if (foley3DPool[i] != null) foley3DPool[i].Stop();
                }
            }
        }

        public void ResetTransientAudioForTransition()
        {
            Stop2DFoleyImmediate(stop3DPool: true);
            targetDreadWeight = 0f;
            stimulantClarity01 = 0f;
            silenceDuckTimer = 0f;
            silenceDuckMultiplier = 1f;
            tvFadeMultiplier = 1f;
            tvFadeOutRate = 0f;
            SetTVLoopMode(TVAudioLoopMode.Off);
            if (dreadProximitySource != null)
            {
                dreadProximitySource.volume = 0f;
            }
        }

        public void SetTVLoopMode(TVAudioLoopMode mode, Transform tvTransform = null)
        {
            if (tvSpatialSource == null && tvTransform != null)
            {
                AttachTVAudioSource(tvTransform);
            }

            if (tvSpatialSource == null) return;

            if (mode == TVAudioLoopMode.Off)
            {
                baseTvSpatialTargetVolume = 0f;
                tvFadeMultiplier = 1f;
                tvFadeOutRate = 0f;
                tvSpatialSource.Stop();
                tvSpatialSource.clip = null;
                tvSpatialSource.volume = 0f;
                return;
            }

            AudioClip targetClip = null;
            float targetVol = 0.42f;

            switch (mode)
            {
                case TVAudioLoopMode.NormalBroadcast:
                    targetClip = tvNormalLoopClip;
                    targetVol = 0.38f;
                    break;
                case TVAudioLoopMode.Canal420Hypnotic:
                    targetClip = tv420LoopClip;
                    targetVol = 0.52f;
                    break;
                case TVAudioLoopMode.NewsStaticBleed:
                    targetClip = tvBleedLoopClip;
                    targetVol = 0.45f;
                    break;
            }

            if (targetClip != null)
            {
                tvFadeMultiplier = 1f;
                tvFadeOutRate = 0f;
                baseTvSpatialTargetVolume = targetVol * masterVolume;
                tvSpatialSource.clip = targetClip;
                tvSpatialSource.volume = baseTvSpatialTargetVolume * silenceDuckMultiplier;
                if (!tvSpatialSource.isPlaying) tvSpatialSource.Play();
            }
        }

        private void SynthesizeMissingProceduralFallbacks()
        {
            var rng = new System.Random(4207);

            if (woodStepClips == null || woodStepClips.Length == 0)
            {
                woodStepClips = new[]
                {
                    RegisterRuntimeClip(CreateFootstepWood(rng, 0.11f, 0.032f, 0.18f)),
                    RegisterRuntimeClip(CreateFootstepWood(rng, 0.12f, 0.038f, 0.22f)),
                    RegisterRuntimeClip(CreateFootstepWood(rng, 0.10f, 0.030f, 0.16f))
                };
            }

            if (tileStepClips == null || tileStepClips.Length == 0)
            {
                tileStepClips = new[]
                {
                    RegisterRuntimeClip(CreateFootstepTile(rng, 0.080f, 0.055f)),
                    RegisterRuntimeClip(CreateFootstepTile(rng, 0.088f, 0.065f)),
                    RegisterRuntimeClip(CreateFootstepTile(rng, 0.076f, 0.050f))
                };
            }

            if (heavyStepClips == null || heavyStepClips.Length == 0)
            {
                heavyStepClips = new[]
                {
                    RegisterRuntimeClip(CreateFootstepHeavyDrag(rng, 0.18f)),
                    RegisterRuntimeClip(CreateFootstepHeavyDrag(rng, 0.21f))
                };
            }

            if (speechCianClip == null) speechCianClip = RegisterRuntimeClip(CreateSpeechPip(135f, 0.028f, 0.15f, rng));
            if (speechBroadcastClip == null) speechBroadcastClip = RegisterRuntimeClip(CreateSpeechPip(240f, 0.024f, 0.25f, rng));
            if (speechTapeClip == null) speechTapeClip = RegisterRuntimeClip(CreateSpeechPip(310f, 0.022f, 0.35f, rng));

            if (tvNormalLoopClip == null) tvNormalLoopClip = RegisterRuntimeClip(CreateTVStaticLoop(rng, hypnotic420: false, heavyBleed: false));
            if (tv420LoopClip == null) tv420LoopClip = RegisterRuntimeClip(CreateTVStaticLoop(rng, hypnotic420: true, heavyBleed: false));
            if (tvBleedLoopClip == null) tvBleedLoopClip = RegisterRuntimeClip(CreateTVStaticLoop(rng, hypnotic420: true, heavyBleed: true));
            if (roomToneLoopClip == null) roomToneLoopClip = RegisterRuntimeClip(CreateRoomToneLoop(rng));
            if (dreadProximityLoopClip == null) dreadProximityLoopClip = RegisterRuntimeClip(CreateDreadProximityLoop(rng));

            void EnsureFoley(FoleySoundId id, System.Func<AudioClip> factory)
            {
                if (!foleyClips.TryGetValue(id, out AudioClip existing) || existing == null)
                {
                    foleyClips[id] = RegisterRuntimeClip(factory());
                }
            }

            EnsureFoley(FoleySoundId.PaperRustle, () => CreatePaperRustle(rng, opening: true));
            EnsureFoley(FoleySoundId.PaperPutAway, () => CreatePaperRustle(rng, opening: false));
            EnsureFoley(FoleySoundId.PillBottleRattle, () => CreatePillBottleRattle(rng));
            EnsureFoley(FoleySoundId.WaterSplash, () => CreateWaterFaucet(rng));
            EnsureFoley(FoleySoundId.AnsweringMachinePlay, () => CreateAnsweringMachineSequence(rng, hasTape: true));
            EnsureFoley(FoleySoundId.AnsweringMachineEmpty, () => CreateAnsweringMachineSequence(rng, hasTape: false));
            EnsureFoley(FoleySoundId.KeyPickup, () => CreateKeyJingle(rng));
            EnsureFoley(FoleySoundId.BatterySnap, () => CreateMechanicalSnap(rng, 780f, 1450f));
            EnsureFoley(FoleySoundId.DrawerSlide, () => CreateDrawerSlide(rng, locked: false));
            EnsureFoley(FoleySoundId.DrawerLocked, () => CreateDrawerSlide(rng, locked: true));
            EnsureFoley(FoleySoundId.FuseClackSpark, () => CreateFuseSpark(rng));
            EnsureFoley(FoleySoundId.DoorOpen, () => CreateDoorSound(rng, opening: true));
            EnsureFoley(FoleySoundId.DoorClose, () => CreateDoorSound(rng, opening: false));
            EnsureFoley(FoleySoundId.DoorLocked, () => CreateDoorLockedKnob(rng));
            EnsureFoley(FoleySoundId.DoorUnlockKey, () => CreateDoorKeyTurn(rng));
            EnsureFoley(FoleySoundId.TVChannelClick, () => CreateTVRotarySwitch(rng));
            EnsureFoley(FoleySoundId.KeypadBeep, () => CreateKeypadTone(rng, 880f));
            EnsureFoley(FoleySoundId.KeypadError, () => CreateKeypadTone(rng, 330f, isError: true));
            EnsureFoley(FoleySoundId.KeypadUnlock, () => CreateKeypadUnlockRelay(rng));
            EnsureFoley(FoleySoundId.FrameCrash, () => CreateFrameCrashSound(rng));
            EnsureFoley(FoleySoundId.ShadowWhoosh, () => CreateShadowWhooshSound(rng));
            EnsureFoley(FoleySoundId.CorridorWhispers, () => CreateCorridorWhispersSound(rng));
            EnsureFoley(FoleySoundId.MargaretStinger, () => CreateMargaretStingerSound(rng));
            EnsureFoley(FoleySoundId.ObjectiveStepTick, () => CreateObjectiveTick(rng));
            EnsureFoley(FoleySoundId.GenericTouch, () => CreateGenericTouchTap(rng));
            EnsureFoley(FoleySoundId.WoodInspect, () => CreateGenericTouchTap(rng));
            EnsureFoley(FoleySoundId.ClothInspect, () => CreatePaperRustle(rng, opening: false));
            EnsureFoley(FoleySoundId.MetalInspect, () => CreateKeypadTone(rng, 640f));
            EnsureFoley(FoleySoundId.GlassInspect, () => CreateKeypadTone(rng, 1750f));
            EnsureFoley(FoleySoundId.WardrobeDoor, () => CreateDrawerSlide(rng, locked: false));
            EnsureFoley(FoleySoundId.CupboardDoor, () => CreateDrawerSlide(rng, locked: false));
            EnsureFoley(FoleySoundId.NightstandDrawer, () => CreateDrawerSlide(rng, locked: false));
            EnsureFoley(FoleySoundId.FileCabinetDrawer, () => CreateDrawerSlide(rng, locked: false));
            EnsureFoley(FoleySoundId.AnsweringMachineBeep, () => CreateAnsweringMachineBeep(rng));
            EnsureFoley(FoleySoundId.BulbFlickerBuzz, () => CreateBulbFlickerBuzz(rng));
            EnsureFoley(FoleySoundId.DistantFloorCreak, () => CreateDistantFloorCreak(rng));
            EnsureFoley(FoleySoundId.DreadSubPulse, () => CreateDreadSubPulse(rng));
            EnsureFoley(FoleySoundId.EerieLullabyHum, () => CreateEerieLullabyHum(rng));
            EnsureFoley(FoleySoundId.PhantomStepBehind, () => CreatePhantomStepBehind(rng));
            EnsureFoley(FoleySoundId.DoorKnockWood, () => CreateDoorKnockWood(rng));
            EnsureFoley(FoleySoundId.GlassTap, () => CreateGlassTap(rng));
            EnsureFoley(FoleySoundId.NeckBreath, () => CreateNeckBreath(rng));
            EnsureFoley(FoleySoundId.TinnitusRing, () => CreateTinnitusRing(rng));
            EnsureFoley(FoleySoundId.LightSwitchClick, () => CreateLightSwitchClick(rng));
            EnsureFoley(FoleySoundId.PowerCutDown, () => CreatePowerCutDown(rng));
            EnsureFoley(FoleySoundId.TVSubliminalBurst, () => CreateTVSubliminalBurst(rng));
            EnsureFoley(FoleySoundId.HeavyDoorSlamTrap, () => CreateHeavyDoorSlamTrap(rng));
            EnsureFoley(FoleySoundId.VisceralSubShock, () => CreateVisceralSubShock(rng));
            EnsureFoley(FoleySoundId.CassetteTapeHauntD2, () => CreateCassetteTapeHauntD2(rng));
            EnsureFoley(FoleySoundId.DryPillSwallow, () => CreateDryPillSwallow(rng));
            EnsureFoley(FoleySoundId.HeavyHeartbeatThud, () => CreateHeavyHeartbeatThud(rng));
            EnsureFoley(FoleySoundId.TVGlassThermalCrack, () => CreateTVGlassThermalCrack(rng));
            EnsureFoley(FoleySoundId.PhoneHalfRingAborted, () => CreatePhoneHalfRingAborted(rng));
            EnsureFoley(FoleySoundId.InternalLatchForcedSlow, () => CreateInternalLatchForcedSlow(rng));
            EnsureFoley(FoleySoundId.FlashlightClick, () => CreateMechanicalSnap(rng, 1180f, 2400f));
        }

        private static AudioClip CreateFootstepWood(System.Random rng, float dur, float lowCutoff, float scuffDelay)
        {
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp1 = 0f, lp2 = 0f;
            float hp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp1 += lowCutoff * (raw - lp1);
                lp2 += lowCutoff * (lp1 - lp2);
                float thudEnv = Mathf.Exp(-t * 52f);
                float thud = lp2 * thudEnv * 3.8f;

                float bodyEnv = Mathf.Exp(-t * 65f);
                float body = (lp1 - lp2) * bodyEnv * 1.6f;

                hp += 0.24f * (raw - hp);
                float scuffEnv = Mathf.Exp(-Mathf.Abs(t - scuffDelay) * 90f);
                float scuff = (raw - hp) * scuffEnv * 0.16f;

                buf[i] = Mathf.Clamp(thud + body + scuff, -0.95f, 0.95f);
            }
            return MakeClip("Step_Wood", buf);
        }

        private static AudioClip CreateFootstepTile(System.Random rng, float dur, float tapCutoff)
        {
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp1 = 0f, lp2 = 0f;
            float hp1 = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                hp1 += 0.36f * (raw - hp1);
                float clickEnv = Mathf.Exp(-t * 110f);
                float click = (raw - hp1) * clickEnv * 0.78f;

                lp1 += tapCutoff * (raw - lp1);
                lp2 += tapCutoff * (lp1 - lp2);
                float tapEnv = Mathf.Exp(-t * 70f);
                float tap = lp2 * tapEnv * 2.4f;

                float gritEnv = Mathf.Exp(-Mathf.Abs(t - 0.012f) * 120f);
                float grit = (raw - hp1) * gritEnv * 0.12f;

                buf[i] = Mathf.Clamp(click + tap + grit, -0.95f, 0.95f);
            }
            return MakeClip("Step_Tile", buf);
        }

        private static AudioClip CreateFootstepHeavyDrag(System.Random rng, float dur)
        {
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp1 = 0f, lp2 = 0f;
            float hp1 = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp1 += 0.026f * (raw - lp1);
                lp2 += 0.026f * (lp1 - lp2);
                float att = Mathf.Clamp01(t / 0.025f);
                float dec = Mathf.Exp(-Mathf.Max(0f, t - 0.025f) * 26f);
                float thud = lp2 * att * dec * 4.0f;

                hp1 += 0.18f * (raw - hp1);
                float dragEnv = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI) * 0.20f;
                float drag = (raw - hp1) * dragEnv;

                buf[i] = Mathf.Clamp(thud + drag, -0.95f, 0.95f);
            }
            return MakeClip("Step_HeavyDrag", buf);
        }

        private static AudioClip CreateSpeechPip(float baseFreq, float dur, float grit, System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float norm = i / (float)n;
                float attack = Mathf.Clamp01(norm * 14f);
                float decay = Mathf.Exp(-t * 90f);

                float raw = NextSigned(rng);
                lp += 0.18f * (raw - lp);

                float s = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * 0.65f + lp * grit * 0.22f;
                buf[i] = s * attack * decay * 0.38f;
            }
            return MakeClip($"SpeechPip_{Mathf.RoundToInt(baseFreq)}", buf);
        }

        private static AudioClip CreateKeypadTone(System.Random rng, float freq, bool isError = false)
        {
            float dur = isError ? 0.14f : 0.045f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * (isError ? 24f : 55f));
                float s = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.45f;
                if (isError)
                {
                    s += Mathf.Sin(2f * Mathf.PI * (freq * 0.95f) * t) * 0.35f;
                }
                buf[i] = s * env;
            }
            return MakeClip(isError ? "Keypad_Error" : "Keypad_Beep", buf);
        }

        private static AudioClip CreateObjectiveTick(System.Random rng)
        {
            float dur = 0.095f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float band = 0f;
            float hp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float norm = t / dur;
                float strokeEnv = Mathf.Sin(Mathf.Clamp01(norm) * Mathf.PI) * (0.65f + 0.35f * Mathf.Sin(t * 170f));
                float raw = NextSigned(rng);
                band += 0.24f * (raw - band);
                hp += 0.42f * (band - hp);
                buf[i] = Mathf.Clamp((band - hp) * strokeEnv * 0.48f, -0.75f, 0.75f);
            }
            return MakeClip("Objective_GraphiteStroke", buf);
        }

        private static AudioClip CreateGenericTouchTap(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.045f);
            float[] buf = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp += 0.12f * (raw - lp);
                float env = Mathf.Exp(-t * 70f);
                buf[i] = lp * env * 0.65f;
            }
            return MakeClip("Generic_Touch", buf);
        }

        private static AudioClip CreatePaperRustle(System.Random rng, bool opening)
        {
            float dur = opening ? 0.32f : 0.24f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float band = 0f, hp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float raw = NextSigned(rng);
                band += 0.18f * (raw - band);
                hp += 0.45f * (band - hp);
                float crackle = (rng.NextDouble() > 0.94) ? NextSigned(rng) * 0.45f : 0f;
                buf[i] = Mathf.Clamp((band - hp) * env * 0.55f + crackle * env, -0.9f, 0.9f);
            }
            return MakeClip(opening ? "Paper_Open" : "Paper_Close", buf);
        }

        private static AudioClip CreatePillBottleRattle(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.38f);
            float[] buf = new float[n];
            float lp = 0f;

            for (int click = 0; click < 6; click++)
            {
                float offsetT = click * 0.052f + (float)rng.NextDouble() * 0.015f;
                int start = Mathf.RoundToInt(offsetT * SampleRate);
                int clickLen = Mathf.RoundToInt(0.022f * SampleRate);

                for (int i = 0; i < clickLen && (start + i) < n; i++)
                {
                    float t = i / (float)SampleRate;
                    float raw = NextSigned(rng);
                    lp += 0.32f * (raw - lp);
                    float clickEnv = Mathf.Exp(-t * 110f);
                    buf[start + i] += lp * clickEnv * 0.35f;
                }
            }
            return MakeClip("PillBottle_Rattle", buf);
        }

        private static AudioClip CreateWaterFaucet(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.55f);
            float[] buf = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.55f) * Mathf.PI);
                float raw = NextSigned(rng);
                lp += 0.16f * (raw - lp);
                float bubble = Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.08f;
                buf[i] = Mathf.Clamp((lp + bubble) * env * 0.48f, -0.9f, 0.9f);
            }
            return MakeClip("Water_Faucet", buf);
        }

        private static AudioClip CreateAnsweringMachineSequence(System.Random rng, bool hasTape)
        {
            float dur = hasTape ? 0.72f : 0.45f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float clunk = 0f;
                if (t < 0.06f)
                {
                    clunk = Mathf.Sin(2f * Mathf.PI * 88f * t) * Mathf.Exp(-t * 45f) * 0.65f;
                }
                float hiss = (hasTape && t > 0.08f) ? NextSigned(rng) * 0.04f : 0f;
                buf[i] = Mathf.Clamp(clunk + hiss, -0.85f, 0.85f);
            }
            return MakeClip(hasTape ? "AnsweringMachine_Tape" : "AnsweringMachine_Empty", buf);
        }

        private static AudioClip CreateKeyJingle(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.32f);
            float[] buf = new float[n];

            for (int j = 0; j < 4; j++)
            {
                float startT = j * 0.065f;
                int start = Mathf.RoundToInt(startT * SampleRate);
                int ringLen = Mathf.RoundToInt(0.045f * SampleRate);
                float freq = 2200f + j * 380f;

                for (int i = 0; i < ringLen && (start + i) < n; i++)
                {
                    float t = i / (float)SampleRate;
                    float env = Mathf.Exp(-t * 90f);
                    buf[start + i] += Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.22f;
                }
            }
            return MakeClip("Key_Jingle", buf);
        }

        private static AudioClip CreateMechanicalSnap(System.Random rng, float f1, float f2)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.075f);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 70f);
                float snap = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.35f;
                buf[i] = snap * env * 0.75f;
            }
            return MakeClip("Mechanical_Snap", buf);
        }

        private static AudioClip CreateDrawerSlide(System.Random rng, bool locked)
        {
            float dur = locked ? 0.18f : 0.42f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp += 0.15f * (raw - lp);
                if (locked)
                {
                    float thud = Mathf.Sin(2f * Mathf.PI * 115f * t) * Mathf.Exp(-t * 35f) * 0.72f;
                    buf[i] = Mathf.Clamp(thud + lp * Mathf.Exp(-t * 22f) * 0.35f, -0.9f, 0.9f);
                }
                else
                {
                    float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                    buf[i] = Mathf.Clamp(lp * env * 0.45f, -0.8f, 0.8f);
                }
            }
            return MakeClip(locked ? "Drawer_Locked" : "Drawer_Slide", buf);
        }

        private static AudioClip CreateFuseSpark(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.16f);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float clack = Mathf.Sin(2f * Mathf.PI * 140f * t) * Mathf.Exp(-t * 40f) * 0.65f;
                float buzz = (t < 0.08f) ? Mathf.Sin(2f * Mathf.PI * 60f * t) * NextSigned(rng) * 0.35f : 0f;
                buf[i] = Mathf.Clamp(clack + buzz, -0.9f, 0.9f);
            }
            return MakeClip("Fuse_Spark", buf);
        }

        private static AudioClip CreateDoorSound(System.Random rng, bool opening)
        {
            float dur = opening ? 0.65f : 0.48f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp += 0.08f * (raw - lp);

                if (opening)
                {
                    float creak = Mathf.Sin(2f * Mathf.PI * (165f + 45f * Mathf.Sin(t * 18f)) * t) * Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI) * 0.28f;
                    buf[i] = Mathf.Clamp(creak + lp * 0.22f, -0.85f, 0.85f);
                }
                else
                {
                    float thud = Mathf.Sin(2f * Mathf.PI * 72f * t) * Mathf.Exp(-t * 22f) * 0.85f;
                    buf[i] = Mathf.Clamp(thud + lp * Mathf.Exp(-t * 30f) * 0.35f, -0.95f, 0.95f);
                }
            }
            return MakeClip(opening ? "Door_Open" : "Door_Close", buf);
        }

        private static AudioClip CreateDoorLockedKnob(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.24f);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                // Crisp double brass latch clack + dull hollow door thud (zero white-noise hiss)
                float click1 = Mathf.Sin(2f * Mathf.PI * 115f * t) * Mathf.Exp(-t * 36f) * 0.55f
                             + Mathf.Sin(2f * Mathf.PI * 520f * t) * Mathf.Exp(-t * 75f) * 0.28f;
                float dt2 = t - 0.085f;
                float click2 = (dt2 > 0f)
                    ? Mathf.Sin(2f * Mathf.PI * 102f * dt2) * Mathf.Exp(-dt2 * 40f) * 0.45f
                    + Mathf.Sin(2f * Mathf.PI * 460f * dt2) * Mathf.Exp(-dt2 * 85f) * 0.22f
                    : 0f;
                buf[i] = Mathf.Clamp(click1 + click2, -0.9f, 0.9f);
            }
            return MakeClip("Door_Locked", buf);
        }

        private static AudioClip CreateDoorKeyTurn(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.38f);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float scrape = (t < 0.18f) ? NextSigned(rng) * 0.18f * Mathf.Exp(-t * 18f) : 0f;
                float clunk = (t >= 0.16f) ? Mathf.Sin(2f * Mathf.PI * 88f * (t - 0.16f)) * Mathf.Exp(-(t - 0.16f) * 32f) * 0.80f : 0f;
                buf[i] = Mathf.Clamp(scrape + clunk, -0.9f, 0.9f);
            }
            return MakeClip("Door_UnlockKey", buf);
        }

        private static AudioClip CreateTVRotarySwitch(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.065f);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 75f);
                float clack = Mathf.Sin(2f * Mathf.PI * 260f * t) * 0.65f + NextSigned(rng) * 0.35f;
                buf[i] = clack * env * 0.85f;
            }
            return MakeClip("TV_RotarySwitch", buf);
        }

        private static AudioClip CreateKeypadUnlockRelay(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.45f);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float solenoid = (t >= 0.14f) ? Mathf.Sin(2f * Mathf.PI * 75f * (t - 0.14f)) * Mathf.Exp(-(t - 0.14f) * 22f) * 0.85f : 0f;
                buf[i] = Mathf.Clamp(solenoid, -0.9f, 0.9f);
            }
            return MakeClip("Keypad_Unlock", buf);
        }

        private static AudioClip CreateFrameCrashSound(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.62f);
            float[] buf = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp += 0.14f * (raw - lp);
                float woodThud = Mathf.Sin(2f * Mathf.PI * 68f * t) * Mathf.Exp(-t * 18f) * 0.85f;
                float glass = (t > 0.04f && t < 0.28f) ? NextSigned(rng) * Mathf.Exp(-(t - 0.04f) * 24f) * 0.55f : 0f;
                buf[i] = Mathf.Clamp(woodThud + glass + lp * Mathf.Exp(-t * 15f) * 0.35f, -0.95f, 0.95f);
            }
            return MakeClip("Frame_Crash", buf);
        }

        private static AudioClip CreateShadowWhooshSound(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 0.75f);
            float[] buf = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.75f) * Mathf.PI);
                float raw = NextSigned(rng);
                lp += (0.04f + 0.18f * env) * (raw - lp);
                buf[i] = Mathf.Clamp(lp * env * 0.75f, -0.9f, 0.9f);
            }
            return MakeClip("Shadow_Whoosh", buf);
        }

        private static AudioClip CreateCorridorWhispersSound(System.Random rng)
        {
            // Muffled behind-wood air breath + subtle wood thuds (non-tonal fallback)
            int n = Mathf.RoundToInt(SampleRate * 1.90f);
            float[] buf = new float[n];
            float lp1 = 0f, lp2 = 0f, hp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float fade = Mathf.Clamp01(t / 0.14f) * Mathf.Clamp01((1.90f - t) / 0.28f);

                float breath1 = Mathf.Exp(-Mathf.Pow((t - 0.52f) / 0.24f, 2f));
                float breath2 = Mathf.Exp(-Mathf.Pow((t - 1.28f) / 0.28f, 2f)) * 0.85f;
                float raw = NextSigned(rng);
                lp1 += 0.16f * (raw - lp1);
                hp += 0.03f * (lp1 - hp);
                float breathAir = (lp1 - hp) * (breath1 + breath2) * 0.85f;

                float tapA = (t > 0.34f) ? lp1 * Mathf.Exp(-(t - 0.34f) * 32f) * 0.35f : 0f;
                float tapB = (t > 0.96f) ? lp1 * Mathf.Exp(-(t - 0.96f) * 36f) * 0.30f : 0f;

                float mixed = (breathAir + tapA + tapB) * fade;
                lp2 += 0.14f * (mixed - lp2);
                buf[i] = Mathf.Clamp(lp2, -0.82f, 0.82f);
            }
            return MakeClip("Corridor_Whispers", buf);
        }

        private static AudioClip CreateMargaretStingerSound(System.Random rng)
        {
            int n = Mathf.RoundToInt(SampleRate * 1.6f);
            float[] buf = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float impact = Mathf.Sin(2f * Mathf.PI * 45f * t) * Mathf.Exp(-t * 6f) * 0.90f;
                float dissonance = Mathf.Sin(2f * Mathf.PI * 432f * t) * 0.35f
                                 + Mathf.Sin(2f * Mathf.PI * 468f * t) * 0.35f;
                float raw = NextSigned(rng);
                lp += 0.25f * (raw - lp);
                float decay = Mathf.Exp(-t * 2.2f);
                buf[i] = Mathf.Clamp((impact + dissonance * decay + lp * decay * 0.4f), -0.98f, 0.98f);
            }
            return MakeClip("Margaret_Stinger", buf);
        }

        private static AudioClip CreateTVStaticLoop(System.Random rng, bool hypnotic420, bool heavyBleed)
        {
            int n = SampleRate * 2;
            float[] buf = new float[n];
            float lp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp += 0.28f * (raw - lp);
                float hum60 = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.12f;
                float flyback15k = Mathf.Sin(2f * Mathf.PI * 15734f * t) * 0.025f;

                float drone = 0f;
                if (hypnotic420)
                {
                    drone = (Mathf.Sin(2f * Mathf.PI * 110f * t) + Mathf.Sin(2f * Mathf.PI * 116.5f * t)) * 0.16f;
                }

                float bleedHiss = heavyBleed ? NextSigned(rng) * 0.18f : 0f;
                buf[i] = Mathf.Clamp(lp * 0.35f + hum60 + flyback15k + drone + bleedHiss, -0.9f, 0.9f);
            }
            return MakeClip("TV_Static_Loop", buf);
        }

        private static AudioClip CreateRoomToneLoop(System.Random rng)
        {
            int n = SampleRate * 3;
            float[] buf = new float[n];
            float lp1 = 0f, lp2 = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp1 += 0.022f * (raw - lp1);
                lp2 += 0.022f * (lp1 - lp2);
                float subHum = Mathf.Sin(2f * Mathf.PI * 52f * t) * 0.035f
                             + Mathf.Sin(2f * Mathf.PI * 78.5f * t) * 0.018f;
                buf[i] = Mathf.Clamp(lp2 * 0.24f + subHum, -0.55f, 0.55f);
            }
            return MakeClip("House_RoomTone", buf);
        }

        private static AudioClip CreateDreadProximityLoop(System.Random rng)
        {
            int n = SampleRate * 4;
            float[] buf = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float pulse = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 0.75f * t);
                float sub = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.42f
                          + Mathf.Sin(2f * Mathf.PI * 91.5f * t) * 0.16f;
                float raw = NextSigned(rng);
                lp += 0.018f * (raw - lp);
                buf[i] = Mathf.Clamp((sub * pulse) + lp * 0.20f, -0.85f, 0.85f);
            }
            return MakeClip("Dread_Proximity_Loop", buf);
        }

        private static AudioClip CreateAnsweringMachineBeep(System.Random rng)
        {
            float dur = 0.085f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float tone = Mathf.Sin(2f * Mathf.PI * 1040f * t) * 0.40f
                           + Mathf.Sin(2f * Mathf.PI * 2080f * t) * 0.10f;
                buf[i] = Mathf.Clamp(tone * env, -0.65f, 0.65f);
            }
            return MakeClip("AnsweringMachine_Beep", buf);
        }

        private static AudioClip CreateBulbFlickerBuzz(System.Random rng)
        {
            float dur = 0.24f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float bp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float gate = (Mathf.Sin(t * 95f) > -0.15f) ? 1f : 0.15f;
                float env = Mathf.Exp(-t * 8.5f) * gate;
                float raw = NextSigned(rng);
                bp += 0.30f * (raw - bp);
                float hum = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.45f
                          + Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.25f;
                buf[i] = Mathf.Clamp((hum + (raw - bp) * 0.32f) * env * 0.65f, -0.85f, 0.85f);
            }
            return MakeClip("Bulb_Flicker_Buzz", buf);
        }

        private static AudioClip CreateDistantFloorCreak(System.Random rng)
        {
            float dur = 0.78f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float stickSlip = 0.55f + 0.45f * Mathf.Sin(t * 46f);
                float f = 148f - 26f * (t / dur) + 9f * Mathf.Sin(t * 14f);
                float creak = Mathf.Sin(2f * Mathf.PI * f * t) * 0.26f
                            + Mathf.Sin(2f * Mathf.PI * (f * 1.98f) * t) * 0.11f;
                float raw = NextSigned(rng);
                lp += 0.05f * (raw - lp);
                buf[i] = Mathf.Clamp((creak * stickSlip + lp * 0.18f) * env, -0.80f, 0.80f);
            }
            return MakeClip("Distant_Floor_Creak", buf);
        }

        private static AudioClip CreateDreadSubPulse(System.Random rng)
        {
            float dur = 1.15f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float att = Mathf.Clamp01(t / 0.04f);
                float dec = Mathf.Exp(-t * 2.8f);
                float freq = Mathf.Lerp(64f, 36f, Mathf.Clamp01(t / dur));
                float sub = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.72f;
                float raw = NextSigned(rng);
                lp += 0.03f * (raw - lp);
                buf[i] = Mathf.Clamp((sub + lp * 0.22f) * att * dec, -0.92f, 0.92f);
            }
            return MakeClip("Dread_Sub_Pulse", buf);
        }

        private static AudioClip CreateEerieLullabyHum(System.Random rng)
        {
            float dur = 2.20f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI) * Mathf.Exp(-t * 0.65f);
                float detune = 3.2f * Mathf.Sin(t * 3.8f);
                float note1 = Mathf.Sin(2f * Mathf.PI * (523.25f + detune) * t) * 0.22f;
                float note2 = (t > 0.65f) ? Mathf.Sin(2f * Mathf.PI * (493.88f - detune) * (t - 0.65f)) * 0.20f : 0f;
                buf[i] = Mathf.Clamp((note1 + note2) * env, -0.70f, 0.70f);
            }
            return MakeClip("Eerie_Lullaby_Hum", buf);
        }

        private static AudioClip CreatePhantomStepBehind(System.Random rng)
        {
            float dur = 0.28f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float thudEnv = Mathf.Exp(-t * 22f);
                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(115f, 52f, Mathf.Clamp01(t / 0.14f)) * t) * thudEnv * 0.52f;
                float creakEnv = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI) * Mathf.Exp(-t * 7f);
                float creak = (Mathf.Sin(2f * Mathf.PI * (240f + 35f * Mathf.Sin(t * 28f)) * t)
                    + 0.45f * Mathf.Sin(2f * Mathf.PI * 395f * t)) * creakEnv * 0.22f;
                float raw = NextSigned(rng);
                lp += 0.08f * (raw - lp);
                buf[i] = Mathf.Clamp(thud + creak + lp * thudEnv * 0.35f, -0.88f, 0.88f);
            }
            return MakeClip("Phantom_Step_Behind", buf);
        }

        private static AudioClip CreateDoorKnockWood(System.Random rng)
        {
            float dur = 0.24f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float impactEnv = Mathf.Exp(-t * 24f);
                float cavityEnv = Mathf.Exp(-t * 14f);
                float cavity = Mathf.Sin(2f * Mathf.PI * 142f * t) * 0.58f
                    + Mathf.Sin(2f * Mathf.PI * 224f * t) * 0.28f
                    + Mathf.Sin(2f * Mathf.PI * 84f * t) * 0.36f;
                float raw = NextSigned(rng);
                lp += 0.24f * (raw - lp);
                float transient = (t < 0.012f) ? raw * (1f - t / 0.012f) * 0.45f : 0f;
                buf[i] = Mathf.Clamp(cavity * cavityEnv + lp * impactEnv * 0.45f + transient, -0.94f, 0.94f);
            }
            return MakeClip("Door_Knock_Wood", buf);
        }

        private static AudioClip CreateGlassTap(System.Random rng)
        {
            float dur = 0.16f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float s = 0f;
                float[] hits = { 0.0f, 0.068f };
                for (int h = 0; h < hits.Length; h++)
                {
                    float dt = t - hits[h];
                    if (dt < 0f) continue;
                    float amp = h == 0 ? 0.72f : 0.90f;
                    float ringEnv = Mathf.Exp(-dt * 48f);
                    float tick = Mathf.Sin(2f * Mathf.PI * (2480f + h * 190f) * dt) * 0.42f
                        + Mathf.Sin(2f * Mathf.PI * 3920f * dt) * 0.24f
                        + NextSigned(rng) * Mathf.Exp(-dt * 190f) * 0.40f;
                    s += tick * ringEnv * amp;
                }
                buf[i] = Mathf.Clamp(s, -0.90f, 0.90f);
            }
            return MakeClip("Glass_Tap", buf);
        }

        private static AudioClip CreateNeckBreath(System.Random rng)
        {
            float dur = 0.92f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp1 = 0f;
            float lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float u = t / dur;
                float env = Mathf.Sin(Mathf.Pow(u, 0.65f) * Mathf.PI);
                float raw = NextSigned(rng);
                lp1 += 0.26f * (raw - lp1);
                lp2 += 0.07f * (raw - lp2);
                float airBand = (lp1 - lp2) * 1.45f;
                float rasp = (Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.55f
                    + Mathf.Sin(2f * Mathf.PI * 245f * t) * 0.35f) * (0.35f + 0.65f * Mathf.Abs(lp2));
                buf[i] = Mathf.Clamp((airBand * 0.78f + rasp * 0.16f) * env, -0.82f, 0.82f);
            }
            return MakeClip("Neck_Breath", buf);
        }

        private static AudioClip CreateTinnitusRing(System.Random rng)
        {
            float dur = 2.40f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float att = Mathf.Clamp01(t / 0.055f);
                float dec = Mathf.Exp(-t * 1.35f) * Mathf.Clamp01((dur - t) / 0.35f);
                float ring = Mathf.Sin(2f * Mathf.PI * 3840f * t) * 0.32f
                    + Mathf.Sin(2f * Mathf.PI * 3990f * t) * 0.16f
                    + NextSigned(rng) * 0.025f;
                buf[i] = Mathf.Clamp(ring * att * dec, -0.45f, 0.45f);
            }
            return MakeClip("Tinnitus_Ring", buf);
        }

        private static AudioClip CreateLightSwitchClick(System.Random rng)
        {
            float dur = 0.068f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float click1 = Mathf.Exp(-t * 145f) * (Mathf.Sin(2f * Mathf.PI * 1450f * t) * 0.45f + NextSigned(rng) * 0.55f);
                float click2 = (t > 0.018f)
                    ? Mathf.Exp(-(t - 0.018f) * 180f) * (Mathf.Sin(2f * Mathf.PI * 2180f * (t - 0.018f)) * 0.52f + NextSigned(rng) * 0.42f)
                    : 0f;
                buf[i] = Mathf.Clamp((click1 + click2) * 0.90f, -0.92f, 0.92f);
            }
            return MakeClip("Light_Switch_Click", buf);
        }

        private static AudioClip CreatePowerCutDown(System.Random rng)
        {
            float dur = 0.68f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp = 0f;
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float u = Mathf.Clamp01(t / dur);
                float clack = (t < 0.045f) ? Mathf.Exp(-t * 95f) * (NextSigned(rng) * 0.65f + Mathf.Sin(2f * Mathf.PI * 185f * t) * 0.55f) : 0f;
                float humFreq = Mathf.Lerp(120f, 34f, u);
                phase += (2f * Mathf.PI * humFreq) / SampleRate;
                float hum = (Mathf.Sin(phase) * 0.58f + Mathf.Sin(phase * 2.0f) * 0.22f) * Mathf.Exp(-t * 3.8f);
                float raw = NextSigned(rng);
                lp += 0.10f * (raw - lp);
                float pop = (t > 0.025f && t < 0.14f) ? lp * Mathf.Exp(-(t - 0.025f) * 28f) * 0.42f : 0f;
                buf[i] = Mathf.Clamp(clack + hum + pop, -0.95f, 0.95f);
            }
            return MakeClip("Power_Cut_Down", buf);
        }

        private static AudioClip CreateTVSubliminalBurst(System.Random rng)
        {
            float dur = 0.26f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 15f);
                float noise = NextSigned(rng) * 0.52f;
                float flyback = Mathf.Sin(2f * Mathf.PI * 1560f * t) * Mathf.Exp(-t * 28f) * 0.28f;
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(95f, 40f, t / dur) * t) * 0.64f;
                buf[i] = Mathf.Clamp((noise + flyback + sub) * env, -0.94f, 0.94f);
            }
            return MakeClip("TV_Subliminal_Burst", buf);
        }

        private static AudioClip CreateHeavyDoorSlamTrap(System.Random rng)
        {
            float dur = 0.85f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp1 = 0f, lp2 = 0f;
            float subPhase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp1 += 0.18f * (raw - lp1);
                lp2 += 0.045f * (lp1 - lp2);

                // Massive low-end wooden frame impact + room cavity resonance (continuous phase integration)
                float thumpFreq = Mathf.Lerp(92f, 38f, Mathf.Clamp01(t / 0.18f));
                subPhase += (2f * Mathf.PI * thumpFreq) / SampleRate;
                float subThump = Mathf.Sin(subPhase) * Mathf.Exp(-t * 11f) * 0.96f;
                float woodCrack = (t < 0.032f) ? (lp1 * 0.85f + Mathf.Sin(2f * Mathf.PI * 210f * t) * 0.45f) * Mathf.Exp(-t * 68f) : 0f;
                float wallRumble = lp2 * Mathf.Exp(-t * 6.5f) * 0.65f;

                // Iron latch bolt snapping shut 45ms after wood impact
                float dtLatch = t - 0.045f;
                float latchClack = (dtLatch >= 0f && dtLatch < 0.09f)
                    ? (Mathf.Sin(2f * Mathf.PI * 640f * dtLatch) * 0.38f + Mathf.Sin(2f * Mathf.PI * 1420f * dtLatch) * 0.24f + raw * 0.25f) * Mathf.Exp(-dtLatch * 75f)
                    : 0f;

                buf[i] = Mathf.Clamp(subThump + woodCrack + wallRumble + latchClack, -0.98f, 0.98f);
            }
            return MakeClip("Heavy_Door_Slam_Trap", buf);
        }

        private static AudioClip CreateVisceralSubShock(System.Random rng)
        {
            float dur = 0.62f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp = 0f;
            float bp = 0f;
            float subPhase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp += 0.09f * (raw - lp);
                bp += 0.28f * (raw - bp);

                // Chest-compressing sub drop (54Hz -> 29Hz) + choked dissonant formant + abrupt cutoff
                float subFreq = Mathf.Lerp(54f, 29f, Mathf.Clamp01(t / 0.35f));
                subPhase += (2f * Mathf.PI * subFreq) / SampleRate;
                float subDrop = Mathf.Sin(subPhase) * Mathf.Exp(-t * 4.8f) * 0.92f;
                float bowedDissonance = (Mathf.Sin(2f * Mathf.PI * 311f * t) * 0.28f
                    + Mathf.Sin(2f * Mathf.PI * 329.6f * t) * 0.26f
                    + Mathf.Sin(2f * Mathf.PI * 659f * t) * 0.14f) * Mathf.Exp(-t * 6.2f);
                float chokeTransient = (t < 0.09f) ? (bp - lp) * Mathf.Exp(-t * 28f) * 0.62f : 0f;

                buf[i] = Mathf.Clamp(subDrop + bowedDissonance + chokeTransient + lp * Mathf.Exp(-t * 8f) * 0.35f, -0.98f, 0.98f);
            }
            return MakeClip("Visceral_Sub_Shock", buf);
        }

        private static AudioClip CreateCassetteTapeHauntD2(System.Random rng)
        {
            const float dur = 6.20f;
            const float stopTime = 6.00f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];

            float hissLpHigh = 0f;
            float hissLpLow = 0f;
            float breathLpHigh = 0f;
            float breathLpLow = 0f;
            float knockLp = 0f;
            float cryLp = 0f;
            float cryPhase = 0f;

            float[] knockTimes = { 2.22f, 2.64f, 3.24f };
            float[] knockPitch = { 104f, 96f, 110f };

            float[] cryStarts = { 3.42f, 4.24f, 5.02f };
            float[] cryDurs = { 0.56f, 0.58f, 0.48f };
            float[] cryBaseHz = { 540f, 505f, 465f };

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);

                // 1) Cliques mecânicos de inserção / play no início
                float startClick = 0f;
                if (t >= 0.012f && t < 0.075f)
                {
                    float dt = t - 0.012f;
                    startClick += (Mathf.Sin(2f * Mathf.PI * 105f * dt) * 0.52f + Mathf.Sin(2f * Mathf.PI * 680f * dt) * 0.24f + raw * 0.28f) * Mathf.Exp(-dt * 72f);
                }
                if (t >= 0.088f && t < 0.155f)
                {
                    float dt = t - 0.088f;
                    startClick += (Mathf.Sin(2f * Mathf.PI * 140f * dt) * 0.44f + Mathf.Sin(2f * Mathf.PI * 980f * dt) * 0.26f) * Mathf.Exp(-dt * 95f);
                }

                // 2) Parada seca no fim da fita (auto-stop seco aos 6.00s)
                float stopClick = 0f;
                if (t >= stopTime && t < 6.08f)
                {
                    float dt = t - stopTime;
                    stopClick = (Mathf.Sin(2f * Mathf.PI * 96f * dt) * 0.56f + Mathf.Sin(2f * Mathf.PI * 840f * dt) * 0.34f + raw * 0.30f) * Mathf.Exp(-dt * 85f);
                }

                // Transporte da fita ativo entre 0.11s e stopTime (corte seco após stopTime)
                float transportEnv = 0f;
                if (t >= 0.11f && t < stopTime)
                {
                    float rampIn = Mathf.Clamp01((t - 0.11f) / 0.06f);
                    transportEnv = rampIn;
                }
                else if (t >= stopTime && t < stopTime + 0.004f)
                {
                    transportEnv = 1f - (t - stopTime) / 0.004f;
                }

                float flutter = 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 1.9f * t) + 0.0025f * Mathf.Sin(2f * Mathf.PI * 5.7f * t);

                // Chiado magnético de fita cassete + hum de 60/120Hz
                hissLpHigh += 0.28f * (raw - hissLpHigh);
                hissLpLow += 0.032f * (raw - hissLpLow);
                float tapeHiss = (hissLpHigh - hissLpLow) * 0.135f
                    + (Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.024f + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.012f);

                // 3) Respiração pesada e trêmula filtrada (0.42s .. 2.25s)
                breathLpHigh += 0.18f * (raw - breathLpHigh);
                breathLpLow += 0.045f * (raw - breathLpLow);
                float breath = 0f;
                if (t >= 0.42f && t <= 2.26f)
                {
                    float cycle = (t - 0.42f) / 0.92f;
                    float local = cycle - Mathf.Floor(cycle);
                    float bEnv = Mathf.Sin(Mathf.Pow(Mathf.Clamp01(local), 0.72f) * Mathf.PI);
                    float throatRasp = (Mathf.Sin(2f * Mathf.PI * 158f * flutter * t) * 0.48f
                        + Mathf.Sin(2f * Mathf.PI * 242f * flutter * t) * 0.30f) * (0.35f + 0.65f * Mathf.Abs(breathLpLow));
                    breath = ((breathLpHigh - breathLpLow) * 1.15f * 0.56f + throatRasp * 0.14f) * bEnv;
                }

                // 4) Batidas abafadas na madeira (2.22s .. 3.60s)
                knockLp += 0.09f * (raw - knockLp);
                float knocks = 0f;
                for (int k = 0; k < knockTimes.Length; k++)
                {
                    float dt = t - knockTimes[k];
                    if (dt >= 0f && dt < 0.28f)
                    {
                        float kEnv = Mathf.Exp(-dt * 19f);
                        float cavity = Mathf.Sin(2f * Mathf.PI * knockPitch[k] * dt) * 0.62f
                            + Mathf.Sin(2f * Mathf.PI * (knockPitch[k] * 1.56f) * dt) * 0.24f;
                        knocks += (cavity + knockLp * 0.38f) * kEnv * 0.42f;
                    }
                }

                // 5) Choro infantil abafado submerso no chiado da fita (3.42s .. 5.50s)
                float rawCry = 0f;
                float cryMask = 0f;
                for (int c = 0; c < cryStarts.Length; c++)
                {
                    float dt = t - cryStarts[c];
                    float cdur = cryDurs[c];
                    if (dt >= 0f && dt < cdur)
                    {
                        float u = dt / cdur;
                        float sobPulse = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 4.4f * dt);
                        float cEnv = Mathf.Sin(Mathf.Pow(u, 0.55f) * Mathf.PI) * sobPulse;
                        float pitchHz = cryBaseHz[c] * Mathf.Lerp(1.06f, 0.78f, u) * flutter * (1f + 0.025f * Mathf.Sin(2f * Mathf.PI * 6.3f * dt));
                        cryPhase += (2f * Mathf.PI * pitchHz) / SampleRate;
                        float formant = Mathf.Sin(cryPhase) * 0.55f
                            + Mathf.Sin(cryPhase * 1.98f) * 0.24f
                            + Mathf.Sin(cryPhase * 3.04f) * 0.10f;
                        float breathCatch = (breathLpHigh - breathLpLow) * 0.38f;
                        rawCry += (formant * 0.44f + breathCatch) * cEnv;
                        cryMask = Mathf.Max(cryMask, cEnv);
                    }
                }
                // Passa-baixa forte para soar distante/atrás da porta e submerso na fita magnética
                cryLp += 0.085f * (rawCry - cryLp);
                float submergedCry = cryLp * (0.58f + 0.16f * cryMask);

                float tapeMix = (tapeHiss + breath + knocks + submergedCry) * transportEnv;
                buf[i] = Mathf.Clamp(startClick + tapeMix + stopClick, -0.90f, 0.90f);
            }

            return MakeClip("Cassette_Tape_Haunt_D2", buf);
        }

        private static AudioClip CreateDryPillSwallow(System.Random rng)
        {
            const float dur = 0.72f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float foilLp = 0f, foilHp = 0f;
            float throatLp = 0f;
            float gulpPhase = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);

                // 1) Blister foil pop + dry gelatin capsule tap (0.00..0.15s)
                foilLp += 0.42f * (raw - foilLp);
                foilHp += 0.14f * (foilLp - foilHp);
                float foilPop = 0f;
                if (t < 0.12f)
                {
                    float popEnv = Mathf.Exp(-t * 44f);
                    float crackle = (foilLp - foilHp) * (0.65f + 0.35f * Mathf.Sin(t * 210f));
                    float pillClick = (t >= 0.032f && t < 0.068f)
                        ? Mathf.Sin(2f * Mathf.PI * 2450f * (t - 0.032f)) * Mathf.Exp(-(t - 0.032f) * 150f) * 0.38f
                        : 0f;
                    foilPop = crackle * popEnv * 0.72f + pillClick;
                }

                // 2) Dry swallow without water (0.24..0.60s): muscular gulp thump + dry throat friction + tight exhale
                throatLp += 0.09f * (raw - throatLp);
                float swallow = 0f;
                if (t >= 0.24f && t < 0.54f)
                {
                    float dt = t - 0.24f;
                    float u = dt / 0.30f;
                    float gulpFreq = Mathf.Lerp(112f, 64f, u);
                    gulpPhase += (2f * Mathf.PI * gulpFreq) / SampleRate;
                    float gulpEnv = Mathf.Sin(Mathf.Pow(Mathf.Clamp01(u), 0.70f) * Mathf.PI);
                    float muscular = Mathf.Sin(gulpPhase) * 0.46f * gulpEnv;
                    float dryFriction = (foilLp - throatLp) * Mathf.Exp(-Mathf.Pow((dt - 0.11f) / 0.055f, 2f)) * 0.34f;
                    swallow = muscular + dryFriction;
                }

                float dryExhale = 0f;
                if (t >= 0.50f && t < 0.70f)
                {
                    float dt = t - 0.50f;
                    float exEnv = Mathf.Sin(Mathf.Clamp01(dt / 0.20f) * Mathf.PI);
                    dryExhale = (foilLp - throatLp) * exEnv * 0.16f;
                }

                buf[i] = Mathf.Clamp(foilPop + swallow + dryExhale, -0.88f, 0.88f);
            }
            return MakeClip("Dry_Pill_Swallow", buf);
        }

        private static AudioClip CreateHeavyHeartbeatThud(System.Random rng)
        {
            const float dur = 0.52f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp = 0f;
            float phase1 = 0f;
            float phase2 = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp += 0.025f * (raw - lp);

                // First systolic thud ("lub") at t = 0.00s
                float f1 = Mathf.Lerp(66f, 32f, Mathf.Clamp01(t / 0.16f));
                phase1 += (2f * Mathf.PI * f1) / SampleRate;
                float att1 = Mathf.Clamp01(t / 0.014f);
                float env1 = att1 * Mathf.Exp(-t * 17.5f);
                float lub = (Mathf.Sin(phase1) * 0.82f + Mathf.Sin(phase1 * 2f) * 0.16f + lp * 0.24f) * env1;

                // Second diastolic thud ("dub") at t = 0.175s
                float dub = 0f;
                float dt2 = t - 0.175f;
                if (dt2 >= 0f)
                {
                    float f2 = Mathf.Lerp(56f, 29f, Mathf.Clamp01(dt2 / 0.14f));
                    phase2 += (2f * Mathf.PI * f2) / SampleRate;
                    float att2 = Mathf.Clamp01(dt2 / 0.012f);
                    float env2 = att2 * Mathf.Exp(-dt2 * 22f);
                    dub = (Mathf.Sin(phase2) * 0.62f + Mathf.Sin(phase2 * 2f) * 0.12f + lp * 0.18f) * env2;
                }

                buf[i] = Mathf.Clamp(lub + dub, -0.92f, 0.92f);
            }
            return MakeClip("Heavy_Heartbeat_Thud", buf);
        }

        private static AudioClip CreateTVGlassThermalCrack(System.Random rng)
        {
            const float dur = 0.15f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float hp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                hp += 0.28f * (raw - hp);
                float crispNoise = raw - hp;

                // Primary dry CRT tube glass thermal contraction tick
                float tick1 = (Mathf.Sin(2f * Mathf.PI * 3180f * t) * 0.42f
                    + Mathf.Sin(2f * Mathf.PI * 5240f * t) * 0.28f
                    + Mathf.Sin(2f * Mathf.PI * 940f * t) * 0.22f
                    + crispNoise * 0.48f) * Mathf.Exp(-t * 155f);

                // Tiny secondary plastic cabinet settle tick 24ms later
                float dt2 = t - 0.024f;
                float tick2 = dt2 >= 0f
                    ? (Mathf.Sin(2f * Mathf.PI * 1680f * dt2) * 0.28f
                        + Mathf.Sin(2f * Mathf.PI * 2890f * dt2) * 0.18f
                        + crispNoise * 0.24f) * Mathf.Exp(-dt2 * 185f)
                    : 0f;

                buf[i] = Mathf.Clamp(tick1 + tick2, -0.88f, 0.88f);
            }
            return MakeClip("TV_Glass_Thermal_Crack", buf);
        }

        private static AudioClip CreatePhoneHalfRingAborted(System.Random rng)
        {
            // ~0.11s aborted 1990s electromechanical twin-gong metallic ring + dead mechanical cutoff tail
            const float dur = 0.19f;
            const float cutTime = 0.108f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float[] strikeTimes = { 0.002f, 0.038f, 0.074f };

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                float bell = 0f;

                for (int s = 0; s < strikeTimes.Length; s++)
                {
                    float dt = t - strikeTimes[s];
                    if (dt < 0f) continue;
                    float gongOffset = (s % 2 == 0) ? 1.0f : 1.085f;
                    float strikeEnv = Mathf.Exp(-dt * 28f);
                    float hammerTransient = (dt < 0.0035f) ? raw * (1f - dt / 0.0035f) * 0.42f : 0f;
                    float ringModes = Mathf.Sin(2f * Mathf.PI * (1520f * gongOffset) * dt) * 0.42f
                        + Mathf.Sin(2f * Mathf.PI * (1835f * gongOffset) * dt) * 0.34f
                        + Mathf.Sin(2f * Mathf.PI * (2960f * gongOffset) * dt) * 0.20f
                        + Mathf.Sin(2f * Mathf.PI * (4180f * gongOffset) * dt) * 0.12f;
                    bell += (ringModes * strikeEnv + hammerTransient) * (s == 2 ? 0.84f : 0.95f);
                }

                // Abrupt electromechanical cutoff around 0.108s leaving only damped brass shell decay
                float gate = t < cutTime ? 1f : Mathf.Exp(-(t - cutTime) * 52f);
                float cutClick = (t >= cutTime && t < cutTime + 0.012f)
                    ? (Mathf.Sin(2f * Mathf.PI * 520f * (t - cutTime)) * 0.22f + raw * 0.18f) * Mathf.Exp(-(t - cutTime) * 180f)
                    : 0f;

                buf[i] = Mathf.Clamp(bell * gate * 0.78f + cutClick, -0.92f, 0.92f);
            }
            return MakeClip("Phone_HalfRing_Aborted", buf);
        }

        private static AudioClip CreateInternalLatchForcedSlow(System.Random rng)
        {
            // 3D slow press & strain of an internal brass lever against a door deadbolted from outside
            const float dur = 1.35f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            float[] buf = new float[n];
            float lp = 0f, hp = 0f;
            float scrapePhase = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float raw = NextSigned(rng);
                lp += 0.14f * (raw - lp);
                hp += 0.04f * (lp - hp);
                float metalFrictionNoise = (lp - hp);

                float sig = 0f;

                // Phase 1 (0.04..0.48s): slow downward rotation of brass handle under spring tension
                if (t >= 0.04f && t < 0.48f)
                {
                    float dt = t - 0.04f;
                    float u = dt / 0.44f;
                    float env = Mathf.Sin(u * Mathf.PI);
                    float stickSlip = 0.55f + 0.45f * Mathf.Sin(dt * 54f);
                    float squeakHz = Mathf.Lerp(620f, 840f, u) + 38f * Mathf.Sin(dt * 23f);
                    scrapePhase += (2f * Mathf.PI * squeakHz) / SampleRate;
                    float brassCreak = (Mathf.Sin(scrapePhase) * 0.24f + Mathf.Sin(scrapePhase * 2.02f) * 0.10f) * stickSlip;
                    float springClick = (Mathf.Abs(dt - 0.19f) < 0.014f)
                        ? Mathf.Sin(2f * Mathf.PI * 1120f * (dt - 0.176f)) * Mathf.Exp(-Mathf.Abs(dt - 0.19f) * 180f) * 0.28f
                        : 0f;
                    sig += (brassCreak + metalFrictionNoise * 0.42f) * env + springClick;
                }

                // Phase 2 (0.46..0.66s): latch hits exterior deadbolt (dull hollow wood + iron bolt knock)
                if (t >= 0.46f && t < 0.68f)
                {
                    float dt = t - 0.46f;
                    float woodStop = (Mathf.Sin(2f * Mathf.PI * 126f * dt) * 0.58f + Mathf.Sin(2f * Mathf.PI * 212f * dt) * 0.24f) * Mathf.Exp(-dt * 26f);
                    float boltClink = (Mathf.Sin(2f * Mathf.PI * 640f * dt) * 0.34f + Mathf.Sin(2f * Mathf.PI * 1280f * dt) * 0.18f) * Mathf.Exp(-dt * 42f);
                    sig += woodStop + boltClink + metalFrictionNoise * Mathf.Exp(-dt * 30f) * 0.28f;
                }

                // Phase 3 (0.76..0.98s): second slow desperate press/strain against the locked bolt
                if (t >= 0.76f && t < 0.98f)
                {
                    float dt = t - 0.76f;
                    float u = dt / 0.22f;
                    float env = Mathf.Sin(u * Mathf.PI);
                    float woodFlex = Mathf.Sin(2f * Mathf.PI * 108f * dt) * 0.42f * Mathf.Exp(-dt * 14f);
                    float boltGrind = (Mathf.Sin(2f * Mathf.PI * 510f * dt) * 0.24f + metalFrictionNoise * 0.36f) * env;
                    sig += woodFlex + boltGrind;
                }

                // Phase 4 (1.04..1.30s): handle slowly released back up into rest position
                if (t >= 1.04f && t < 1.30f)
                {
                    float dt = t - 1.04f;
                    float env = Mathf.Sin(Mathf.Clamp01(dt / 0.26f) * Mathf.PI);
                    float retScrape = metalFrictionNoise * env * 0.24f;
                    float restClick = (dt >= 0.16f)
                        ? (Mathf.Sin(2f * Mathf.PI * 490f * (dt - 0.16f)) * 0.32f + Mathf.Sin(2f * Mathf.PI * 970f * (dt - 0.16f)) * 0.18f) * Mathf.Exp(-(dt - 0.16f) * 65f)
                        : 0f;
                    sig += retScrape + restClick;
                }

                buf[i] = Mathf.Clamp(sig * 0.90f, -0.92f, 0.92f);
            }
            return MakeClip("Internal_Latch_Forced_Slow", buf);
        }

        private static float NextSigned(System.Random rng)
        {
            return (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        private static AudioClip MakeClip(string name, float[] buffer)
        {
            AudioClip clip = AudioClip.Create(name, buffer.Length, 1, SampleRate, false);
            clip.SetData(buffer, 0);
            return clip;
        }
    }
}
