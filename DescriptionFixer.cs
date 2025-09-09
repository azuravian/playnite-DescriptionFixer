using DescriptionFixer.Services;
using DescriptionFixer.Views;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using HtmlAgilityPack;
using DescriptionFixer.Utilities;

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
                    ProcessGames(gmeArgs);
                }
            };
        }

        private void ProcessGames(GameMenuItemActionArgs gmeArgs)
        {
            VideoService videoSvc = new VideoService(SettingsVM.Settings, logger, GetPluginUserDataPath(), PlayniteApi);
            ImageService imageSvc = new ImageService(SettingsVM.Settings, logger, GetPluginUserDataPath(), PlayniteApi);
            EmojiService emojiSvc = new EmojiService(SettingsVM.Settings, logger, GetPluginUserDataPath(), PlayniteApi);
            CleanService cleanSvc = new CleanService(SettingsVM.Settings, logger, GetPluginUserDataPath(), PlayniteApi);

            int changesVideo = 0;
            int changesAvifToPng = 0;
            int changesAvifToWebp = 0;
            int changesWebpToPng = 0;
            int changesGif = 0;
            int changesEmoji = 0;
            int changesClean = 0;
            HtmlDocument maindoc;
            List<HtmlNode> gifNodes;
            List<string> changedGames = new List<string>();

            foreach (var game in gmeArgs.Games)
            {
                if (!string.IsNullOrEmpty(game.Description))
                {
                    string originalDescription = game.Description;
                    string fixedDescription;

                    // Image processing
                    int cAvifToPng = 0;
                    int cAvifToWebp = 0;
                    int cWebpToPng = 0;
                    (maindoc, cAvifToPng, cAvifToWebp, cWebpToPng, gifNodes) = imageSvc.ProcessImages(game, originalDescription);
                    changesAvifToPng += cAvifToPng;
                    changesAvifToWebp += cAvifToWebp;
                    changesWebpToPng += cWebpToPng;

                    // Gif processing
                    int cGif = 0;
                    (gifNodes, cGif) = imageSvc.ProcessGifs(game, gifNodes);
                    changesGif += cGif;
                    fixedDescription = maindoc.DocumentNode.OuterHtml;

                    // Video processing
                    int cvideo = 0;
                    (fixedDescription, cvideo) = videoSvc.ProcessVideos(game, fixedDescription);
                    changesVideo += cvideo;

                    // Set Image Max-Width
                    ImageUtils.SetMaxWidth(fixedDescription);                    

                    // Emoji processing
                    int cEmoji = 0;
                    (fixedDescription, cEmoji) = emojiSvc.ProcessEmojis(fixedDescription);
                    changesEmoji += cEmoji;

                    // Cleaning
                    int cClean = 0;
                    (fixedDescription, cClean) = cleanSvc.CleanDescription(fixedDescription);
                    changesClean += cClean;

                    game.Description = fixedDescription;
                    if (game.Description != originalDescription)
                    {
                        PlayniteApi.Database.Games.Update(game);
                        changedGames.Add(game.Name);
                    }

                    logger.Info($"Fixed description for game: {game.Name}");
                }
            }

            // Show results
            if (changedGames.Count > 0)
            {
                var reportControl = new ResultReportControl();

                reportControl.SetResults(
                    changedGames: changedGames,
                    videoImageChanges: new Dictionary<string, int>
                    {
                            { "Videos converted to single image", changesVideo },
                            { "Animated Gifs converted to single image", changesGif },
                            { "Avif converted to Png", changesAvifToPng },
                            { "Avif converted to WebP", changesAvifToWebp },
                            { "WebP converted to Png", changesWebpToPng }
                    },
                    emojiRepaired: changesEmoji,
                    formattingChanges: changesClean
                );

                var reportWindow = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                {
                    ShowCloseButton = true,
                    ShowMaximizeButton = false,
                    ShowMinimizeButton = false
                });

                reportWindow.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                reportWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                reportWindow.Content = reportControl;
                reportWindow.Title = "Description Fixer - Results";
                reportWindow.SizeToContent = SizeToContent.WidthAndHeight;

                reportWindow.ShowDialog();
            }
            else
            {
                logger.Info("No changes were made to the descriptions of the selected games.");
            }
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