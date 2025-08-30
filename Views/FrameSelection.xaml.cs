using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Input;


namespace DescriptionFixer.Views
{
    public partial class FrameSelectionControl : UserControl
    {
        public string SelectedFramePath { get; private set; }

        public FrameSelectionControl(List<string> framePaths)
        {
            InitializeComponent();
            LoadFrames(framePaths);
        }

        private void LoadFrames(List<string> framePaths)
        {
            framePaths.Sort();
            foreach (var path in framePaths)
            {
                if (!File.Exists(path))
                    continue;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new System.Uri(path);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();

                var img = new Image
                {
                    Source = bmp,
                    Width = SliderSize.Value,
                    Margin = new Thickness(5)
                };

                var item = new ListBoxItem { Content = img, Tag = path };
                //item.MouseLeftButtonUp += (s, e) =>
                //{
                //    SelectedFramePath = (string)((ListBoxItem)s).Tag;
                //    DialogResult = true;
                //    Close();
                //};

                ListBoxImages.Items.Add(item);
            }
        }

        private void SliderSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            foreach (ListBoxItem item in ListBoxImages.Items)
            {
                if (item.Content is Image img)
                {
                    img.Width = e.NewValue;
                }
            }
        }

        private void ListBoxImages_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListBoxImages.SelectedItem != null)
            {
                ButtonSelectImage_Click(ButtonSelectImage, null);
            }
        }

        private void ButtonSelectImage_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxImages.SelectedItem is ListBoxItem item)
            {
                SelectedFramePath = (string)item.Tag;
                
                var parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    parentWindow.DialogResult = true; // Set dialog result to true to indicate selection was made
                    parentWindow.Close(); // Close the window
                }
            }
            else
            {
                MessageBox.Show("Please select an image frame first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ListBoxImages_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                double step = SliderSize.TickFrequency > 0 ? SliderSize.TickFrequency : 25;

                if (e.Delta > 0)
                {
                    SliderSize.Value = System.Math.Min(SliderSize.Maximum, SliderSize.Value + step);
                }
                else if (e.Delta < 0)
                {
                    SliderSize.Value = System.Math.Max(SliderSize.Minimum, SliderSize.Value - step);
                }

                e.Handled = true; // Mark the event as handled
            }
            else
            {
                // Allow normal scrolling behavior
                e.Handled = false;
            }
        }
    }
}