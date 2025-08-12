using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptionFixer
{
    public class DescriptionFixerSettings : ObservableObject
    {
        private uint _frameCount = 12;
        private uint _quality = 75;
        private uint _transparencyThreshold = 98; // Threshold for transparency detection
        private bool _useJpeg = false;
        
        public uint FrameCount
        {
            get => _frameCount;
            set => SetValue(ref _frameCount, value);
        }
        public uint Quality
        {
            get => _quality;
            set => SetValue(ref _quality, value);
        }
        public uint TransparencyThreshold
        {
            get => _transparencyThreshold;
            set => SetValue(ref _transparencyThreshold, value);
        }
        public bool UseJpeg
        {
            get => _useJpeg;
            set => SetValue(ref _useJpeg, value);
        }

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        //[DontSerialize]
    }

    public class DescriptionFixerSettingsViewModel : ObservableObject, ISettings
    {
        private readonly DescriptionFixer plugin;
        private DescriptionFixerSettings EditingClone { get; set; }

        private DescriptionFixerSettings settings;
        public DescriptionFixerSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public DescriptionFixerSettingsViewModel(DescriptionFixer plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<DescriptionFixerSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new DescriptionFixerSettings();
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            EditingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = EditingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}