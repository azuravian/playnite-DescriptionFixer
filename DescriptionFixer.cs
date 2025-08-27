using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using DescriptionFixer.Services;

namespace DescriptionFixer
{
    public class DescriptionFixer : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        public static DescriptionFixer Instance { get; private set; }

        public DescriptionFixerSettingsViewModel SettingsVM { get; private set; }
        public DescriptionFixerSettings Settings => SettingsVM.Settings;

        public override Guid Id { get; } = Guid.Parse("b86cf867-0519-49ef-b7b7-386b6a1e2830");

        public DescriptionFixer(IPlayniteAPI api) : base(api)
        {
            Instance = this;
            
            SettingsVM = new DescriptionFixerSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            yield return new GameMenuItem
            {
                Description = "Fix game description",
                MenuSection = "Description Fixer",
                Action = (gmeArgs) =>
                {
                    // Add code to fix game description here.
                    // For example, you can modify the game.Description property.
                    VideoService videoSvc = new VideoService(SettingsVM.Settings, logger, GetPluginUserDataPath(), PlayniteApi);
                    ImageService imageSvc = new ImageService(SettingsVM.Settings, logger, GetPluginUserDataPath(), PlayniteApi);
                    EmojiService emojiSvc = new EmojiService(SettingsVM.Settings, logger, GetPluginUserDataPath(), PlayniteApi);
                    gmeArgs.Games.ForEach(async game =>
                    {
                        if (!string.IsNullOrEmpty(game.Description))
                        {
                            string originalDescription = game.Description;
                            string fixedDescription;
                            fixedDescription = await videoSvc.ProcessVideos(game, originalDescription);
                            fixedDescription = await imageSvc.ProcessImages(game, fixedDescription);
                            fixedDescription = emojiSvc.ProcessEmojis(fixedDescription);
                            
                            game.Description = fixedDescription;
                            if (game.Description != originalDescription)
                            {
                                PlayniteApi.Database.Games.Update(game);
                            }
                            logger.Info($"Fixed description for game: {game.Name}");
                        }
                    });
                }
            };
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Add code to be executed when Playnite is initialized.
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return SettingsVM;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new DescriptionFixerSettingsView();
        }
    }
}