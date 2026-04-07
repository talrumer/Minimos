using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minimos.UI
{
    /// <summary>
    /// Controls the settings panel: audio sliders, graphics options, gameplay tweaks,
    /// and accessibility toggles. Reads from and writes to PlayerPrefs.
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        #region Constants

        private const string PREF_RESOLUTION = "Settings_Resolution";
        private const string PREF_FULLSCREEN = "Settings_Fullscreen";
        private const string PREF_QUALITY = "Settings_Quality";
        private const string PREF_VSYNC = "Settings_VSync";
        private const string PREF_SCREEN_SHAKE = "Settings_ScreenShake";
        private const string PREF_HUD_SCALE = "Settings_HUDScale";
        private const string PREF_CAMERA_FOV = "Settings_CameraFOV";
        private const string PREF_COLORBLIND = "Settings_Colorblind";
        private const string PREF_SUBTITLES = "Settings_Subtitles";
        private const string PREF_AUTOAIM = "Settings_AutoAim";

        #endregion

        #region Fields

        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider announcerVolumeSlider;

        [Header("Graphics")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle vsyncToggle;

        [Header("Gameplay")]
        [SerializeField] private Slider screenShakeSlider;
        [SerializeField] private TMP_Text screenShakeValueText;
        [SerializeField] private Slider hudScaleSlider;
        [SerializeField] private TMP_Text hudScaleValueText;
        [SerializeField] private Slider cameraFOVSlider;
        [SerializeField] private TMP_Text cameraFOVValueText;

        [Header("Accessibility")]
        [SerializeField] private Toggle colorblindToggle;
        [SerializeField] private Toggle subtitlesToggle;
        [SerializeField] private Toggle autoAimToggle;

        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button backButton;

        private Resolution[] availableResolutions;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            PopulateResolutions();
            PopulateQualityLevels();
            LoadSettings();
            BindListeners();
        }

        private void OnDisable()
        {
            UnbindListeners();
        }

        #endregion

        #region Setup

        private void PopulateResolutions()
        {
            if (resolutionDropdown == null) return;

            availableResolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            int currentIndex = 0;
            var options = new System.Collections.Generic.List<string>();

            for (int i = 0; i < availableResolutions.Length; i++)
            {
                var res = availableResolutions[i];
                options.Add($"{res.width} x {res.height} @ {Mathf.RoundToInt((float)res.refreshRateRatio.value)}Hz");

                if (res.width == Screen.currentResolution.width &&
                    res.height == Screen.currentResolution.height)
                {
                    currentIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = PlayerPrefs.GetInt(PREF_RESOLUTION, currentIndex);
        }

        private void PopulateQualityLevels()
        {
            if (qualityDropdown == null) return;

            qualityDropdown.ClearOptions();
            var names = new System.Collections.Generic.List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(names);
            qualityDropdown.value = PlayerPrefs.GetInt(PREF_QUALITY, QualitySettings.GetQualityLevel());
        }

        #endregion

        #region Load / Save

        private void LoadSettings()
        {
            // Audio — read from AudioManager if available, otherwise PlayerPrefs.
            var audio = Audio.AudioManager.HasInstance ? Audio.AudioManager.Instance : null;

            if (masterVolumeSlider != null)
                masterVolumeSlider.value = audio != null ? audio.MasterVolume : PlayerPrefs.GetFloat("Audio_MasterVolume", 1f);

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = audio != null ? audio.MusicVolume : PlayerPrefs.GetFloat("Audio_MusicVolume", 0.7f);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = audio != null ? audio.SFXVolume : PlayerPrefs.GetFloat("Audio_SFXVolume", 1f);

            if (announcerVolumeSlider != null)
                announcerVolumeSlider.value = audio != null ? audio.AnnouncerVolume : PlayerPrefs.GetFloat("Audio_AnnouncerVolume", 1f);

            // Graphics.
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;

            if (vsyncToggle != null)
                vsyncToggle.isOn = PlayerPrefs.GetInt(PREF_VSYNC, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;

            // Gameplay.
            if (screenShakeSlider != null)
            {
                screenShakeSlider.value = PlayerPrefs.GetFloat(PREF_SCREEN_SHAKE, 100f);
                UpdateScreenShakeLabel(screenShakeSlider.value);
            }

            if (hudScaleSlider != null)
            {
                hudScaleSlider.value = PlayerPrefs.GetFloat(PREF_HUD_SCALE, 1f);
                UpdateHUDScaleLabel(hudScaleSlider.value);
            }

            if (cameraFOVSlider != null)
            {
                cameraFOVSlider.value = PlayerPrefs.GetFloat(PREF_CAMERA_FOV, 60f);
                UpdateFOVLabel(cameraFOVSlider.value);
            }

            // Accessibility.
            if (colorblindToggle != null)
                colorblindToggle.isOn = PlayerPrefs.GetInt(PREF_COLORBLIND, 0) == 1;

            if (subtitlesToggle != null)
                subtitlesToggle.isOn = PlayerPrefs.GetInt(PREF_SUBTITLES, 0) == 1;

            if (autoAimToggle != null)
                autoAimToggle.isOn = PlayerPrefs.GetInt(PREF_AUTOAIM, 0) == 1;
        }

        private void ApplySettings()
        {
            // Audio.
            var audio = Audio.AudioManager.HasInstance ? Audio.AudioManager.Instance : null;
            if (audio != null)
            {
                if (masterVolumeSlider != null) audio.MasterVolume = masterVolumeSlider.value;
                if (musicVolumeSlider != null) audio.MusicVolume = musicVolumeSlider.value;
                if (sfxVolumeSlider != null) audio.SFXVolume = sfxVolumeSlider.value;
                if (announcerVolumeSlider != null) audio.AnnouncerVolume = announcerVolumeSlider.value;
            }

            // Graphics — Resolution.
            if (resolutionDropdown != null && availableResolutions != null)
            {
                int resIdx = resolutionDropdown.value;
                if (resIdx >= 0 && resIdx < availableResolutions.Length)
                {
                    var res = availableResolutions[resIdx];
                    bool fs = fullscreenToggle != null && fullscreenToggle.isOn;
                    Screen.SetResolution(res.width, res.height, fs);
                    PlayerPrefs.SetInt(PREF_RESOLUTION, resIdx);
                }
            }

            // Graphics — Fullscreen.
            if (fullscreenToggle != null)
            {
                PlayerPrefs.SetInt(PREF_FULLSCREEN, fullscreenToggle.isOn ? 1 : 0);
            }

            // Graphics — Quality.
            if (qualityDropdown != null)
            {
                QualitySettings.SetQualityLevel(qualityDropdown.value);
                PlayerPrefs.SetInt(PREF_QUALITY, qualityDropdown.value);
            }

            // Graphics — VSync.
            if (vsyncToggle != null)
            {
                QualitySettings.vSyncCount = vsyncToggle.isOn ? 1 : 0;
                PlayerPrefs.SetInt(PREF_VSYNC, vsyncToggle.isOn ? 1 : 0);
            }

            // Gameplay.
            if (screenShakeSlider != null)
                PlayerPrefs.SetFloat(PREF_SCREEN_SHAKE, screenShakeSlider.value);

            if (hudScaleSlider != null)
                PlayerPrefs.SetFloat(PREF_HUD_SCALE, hudScaleSlider.value);

            if (cameraFOVSlider != null)
                PlayerPrefs.SetFloat(PREF_CAMERA_FOV, cameraFOVSlider.value);

            // Accessibility.
            if (colorblindToggle != null)
                PlayerPrefs.SetInt(PREF_COLORBLIND, colorblindToggle.isOn ? 1 : 0);

            if (subtitlesToggle != null)
                PlayerPrefs.SetInt(PREF_SUBTITLES, subtitlesToggle.isOn ? 1 : 0);

            if (autoAimToggle != null)
                PlayerPrefs.SetInt(PREF_AUTOAIM, autoAimToggle.isOn ? 1 : 0);

            PlayerPrefs.Save();
            Debug.Log("[Settings] Settings applied and saved.");
        }

        #endregion

        #region Listeners

        private void BindListeners()
        {
            if (applyButton != null) applyButton.onClick.AddListener(OnApplyClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

            if (screenShakeSlider != null) screenShakeSlider.onValueChanged.AddListener(UpdateScreenShakeLabel);
            if (hudScaleSlider != null) hudScaleSlider.onValueChanged.AddListener(UpdateHUDScaleLabel);
            if (cameraFOVSlider != null) cameraFOVSlider.onValueChanged.AddListener(UpdateFOVLabel);
        }

        private void UnbindListeners()
        {
            if (applyButton != null) applyButton.onClick.RemoveListener(OnApplyClicked);
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);

            if (screenShakeSlider != null) screenShakeSlider.onValueChanged.RemoveAllListeners();
            if (hudScaleSlider != null) hudScaleSlider.onValueChanged.RemoveAllListeners();
            if (cameraFOVSlider != null) cameraFOVSlider.onValueChanged.RemoveAllListeners();
        }

        #endregion

        #region Button Handlers

        private void OnApplyClicked()
        {
            ApplySettings();
        }

        private void OnBackClicked()
        {
            UIManager.Instance?.HidePanel(UIPanel.Settings);
        }

        #endregion

        #region Label Updaters

        private void UpdateScreenShakeLabel(float value)
        {
            if (screenShakeValueText != null)
                screenShakeValueText.text = $"{Mathf.RoundToInt(value)}%";
        }

        private void UpdateHUDScaleLabel(float value)
        {
            if (hudScaleValueText != null)
                hudScaleValueText.text = $"{value:F1}x";
        }

        private void UpdateFOVLabel(float value)
        {
            if (cameraFOVValueText != null)
                cameraFOVValueText.text = $"{Mathf.RoundToInt(value)}";
        }

        #endregion
    }
}
