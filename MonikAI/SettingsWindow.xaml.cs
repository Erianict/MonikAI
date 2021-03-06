﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace MonikAI
{
    /// <summary>
    ///     Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly MainWindow mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            this.InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Settings window initialization code
            this.textBoxName.Text = string.IsNullOrWhiteSpace(MonikaiSettings.Default.UserName)
                ? Environment.UserName
                : MonikaiSettings.Default.UserName;
            this.checkBoxPotatoPC.IsChecked = MonikaiSettings.Default.PotatoPC;
            this.checkBoxAutoUpdate.IsChecked = MonikaiSettings.Default.AutoUpdate;

            if (MonikaiSettings.Default.LeftAlign)
            {
                this.radioLeft.IsChecked = true;
            }
            else
            {
                this.radioRight.IsChecked = true;
            }

            var index = 0;
            this.comboBoxScreen.Items.Clear();
            foreach (var screen in Screen.AllScreens)
            {
                this.comboBoxScreen.Items.Add($"{screen.DeviceName} ({screen.Bounds.Width}x{screen.Bounds.Height})");

                if (string.IsNullOrWhiteSpace(MonikaiSettings.Default.Screen) && screen.Primary ||
                    screen.DeviceName == MonikaiSettings.Default.Screen)
                {
                    this.comboBoxScreen.SelectedIndex = index;
                }
                index++;
            }
        }

        private void comboBoxScreen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboBoxScreen.SelectedItem == null)
            {
                return;
            }

            this.mainWindow.MonikaScreen =
                Screen.AllScreens.First(x => this.comboBoxScreen.SelectedItem.ToString().Contains(x.DeviceName));
            this.mainWindow.SetupScale();
            // Not needed but left here for whatever reason you can come up with yourself
            // I'm just a comment, not someone to give meaning to life, you know?
            //this.mainWindow.SetPositionBottomRight(this.mainWindow.MonikaScreen);
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MonikaiSettings.Default.AutoUpdate = this.checkBoxAutoUpdate.IsChecked.GetValueOrDefault(true);
            MonikaiSettings.Default.PotatoPC = this.checkBoxPotatoPC.IsChecked.GetValueOrDefault(false);
            MonikaiSettings.Default.UserName = this.textBoxName.Text;
            MonikaiSettings.Default.Screen =
                Screen.AllScreens.First(x => this.comboBoxScreen.SelectedItem.ToString().Contains(x.DeviceName))
                    .DeviceName;

            MonikaiSettings.Default.Save();
        }

        private void radio_checked_changed(object sender, RoutedEventArgs e)
        {
            MonikaiSettings.Default.LeftAlign = this.radioLeft.IsChecked.GetValueOrDefault(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show("Are you sure? This will reset all your settings.", "Confirm reset",
                    MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                MonikaiSettings.Default.Reset();
                this.Window_Loaded(this, null);
            }
        }

        // Settings hotkey
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await this.HotkeySetTask(this.txtSettings,
                () => MonikaiSettings.Default.HotkeySettings = this.txtSettings.Text);
        }

        // Hide hotkey
        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            await this.HotkeySetTask(this.txtHide, () => MonikaiSettings.Default.HotkeySettings = this.txtHide.Text);
        }

        // Exit hotkey
        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            await this.HotkeySetTask(this.txtExit, () => MonikaiSettings.Default.HotkeySettings = this.txtExit.Text);
        }

        private async Task HotkeySetTask(TextBlock output, Action callback)
        {
            output.Dispatcher.Invoke(() => output.Text = "Press and hold any key combination...");

            await this.WaitForKeyChange();

            var timer = DateTime.Now;
            var state = SettingsWindow.GetKeyboardState().ToList();
            var invalid = true;
            while ((DateTime.Now - timer).TotalSeconds < 1)
            {
                var newState = SettingsWindow.GetKeyboardState().ToList();

                var ctrlPressed = newState.Where(x => x.Item1 == "LeftCtrl" || x.Item1 == "RightCtrl").Any(x => x.Item2);
                var altPressed = newState.Where(x => x.Item1 == "LeftAlt" || x.Item1 == "RightAlt").Any(x => x.Item2);
                var shiftPressed =
                    newState.Where(x => x.Item1 == "LeftShift" || x.Item1 == "RightShift").Any(x => x.Item2);
                var otherKeysPressed =
                    newState.Where(
                        x =>
                            x.Item2 &&
                            !new[] {"LeftCtrl", "RightCtrl", "LeftAlt", "RightAlt", "LeftShift", "RightShift"}.Contains(
                                x.Item1)).ToList();
                invalid = otherKeysPressed.Count != 1;

                if (invalid || !state.SequenceEqual(newState))
                {
                    timer = DateTime.Now;
                }

                output.Dispatcher.Invoke(() =>
                {
                    if (invalid)
                    {
                        output.Text = "Invalid combination";
                    }
                    else
                    {
                        output.Text = otherKeysPressed.Single().Item1;
                        if (shiftPressed)
                        {
                            output.Text = "SHIFT-" + output.Text;
                        }
                        if (altPressed)
                        {
                            output.Text = "ALT-" + output.Text;
                        }
                        if (ctrlPressed)
                        {
                            output.Text = "CTRL-" + output.Text;
                        }
                    }
                });

                state = newState;

                await Task.Delay(10);
            }

            output.Dispatcher.Invoke(() => output.Foreground = Brushes.GreenYellow);
            await Task.Delay(500);
            output.Dispatcher.Invoke(() => output.Foreground = Brushes.Black);

            output.Dispatcher.Invoke(callback);
        }

        private async Task WaitForKeyChange()
        {
            var state = SettingsWindow.GetKeyboardState().ToList();
            while (state.SequenceEqual(SettingsWindow.GetKeyboardState()))
            {
                await Task.Delay(10);
            }
        }

        private static IEnumerable<Tuple<string, bool>> GetKeyboardState()
        {
            return Enum.GetNames(typeof(Key)).Select(x =>
            {
                var key = (Key) Enum.Parse(typeof(Key), x);
                return new Tuple<string, bool>(x, key != Key.None && Keyboard.IsKeyDown(key));
            });
        }
    }
}