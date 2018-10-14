using BacklightManager.Logitech;
using BacklightManager.WinAPI;
using Corale.Colore.Core;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ColoreColor = Corale.Colore.Core.Color;

namespace BacklightManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TaskbarIcon taskbarIcon;
        bool turnOffWithStandby = true;
        bool indicateNumLock = true;
        bool adaptiveBacklight = true;
        bool logiSDKStarted = false;
        List<Button> backlightPercentButtons = new List<Button>(4);
        int[] logiBacklightValues = new int[4] { 25, 50, 75, 100 };
        int[] razerBacklightValues = new int[4] { 38 /*15 %*/, 77 /*30 %*/, 140 /*55 %*/, 230 /*90 %*/ };
        int currentBacklightPercentIndex = 1;

        public MainWindow()
        {
            InitializeComponent();

            backlightPercentButtons.Add(new Button() { Content = "25 %", Style = FindResource("ButtonStyle") as Style });
            backlightPercentButtons.Add(new Button() { Content = "50 %", Style = FindResource("ButtonSelectedStyle") as Style });
            backlightPercentButtons.Add(new Button() { Content = "75 %", Style = FindResource("ButtonStyle") as Style });
            backlightPercentButtons.Add(new Button() { Content = "100 %", Style = FindResource("ButtonStyle") as Style });

            foreach (var backlightPercentButton in backlightPercentButtons)
            {
                backlightPercentButton.Click += BacklightPercentButton_Click;
            }

            SourceInitialized += (s, e) =>
            {
#if !UI_DESIGN
                PowerSetting.Init(new WindowInteropHelper(this).Handle);
                PowerSetting.OnMonitorStateChanged += PowerSetting_OnMonitorStateChanged;
                KeyboardHook.Init();
                KeyboardHook.OnNumLockPressed += KeyboardHook_OnNumLockPressed;
#endif
            };

            Loaded += (s, e) =>
            {
                Hide();
            };

            Unloaded += (s, e) =>
            {
#if !UI_DESIGN
                PowerSetting.Dispose();
                KeyboardHook.Dispose();
#endif
            };

            Closed += (s, e) =>
            {
                taskbarIcon.Dispose();
#if !UI_DESIGN
                Chroma.Instance.Unregister();
                Chroma.Instance.Uninitialize();
                LogitechGSDK.LogiLedShutdown();
#endif
            };

            CreateTaskbarIconInit();

#if !UI_DESIGN
            WaitForLEDSDKInit();
#else
            CreateTaskbarIcon();
#endif
        }

        private void BacklightPercentButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var backlightPercentButton in backlightPercentButtons)
            {
                backlightPercentButton.Style = FindResource("ButtonStyle") as Style;
            }

            (sender as Button).Style = FindResource("ButtonSelectedStyle") as Style;

            switch ((sender as Button).Content.ToString())
            {
                case "25 %":
                    currentBacklightPercentIndex = 0;
                    break;
                case "50 %":
                    currentBacklightPercentIndex = 1;
                    break;
                case "75 %":
                    currentBacklightPercentIndex = 2;
                    break;
                case "100 %":
                    currentBacklightPercentIndex = 3;
                    break;
            }

            BacklightOn();
        }

        private void WaitForLEDSDKInit()
        {
            new Thread(() =>
            {
                while (true)
                {
                    foreach (Process clsProcess in Process.GetProcesses())
                    {
                        if (clsProcess.ProcessName == "LCore")
                        {
                            logiSDKStarted = true;
                            break;
                        }
                    }

                    if (logiSDKStarted)
                    {
                        break;
                    }

                    Thread.Sleep(100);
                }
#if !DEBUG
                Thread.Sleep(5000);
#endif
                Chroma.Instance.Initialize();
                LogitechGSDK.LogiLedInit();
                LogitechGSDK.LogiLedSetTargetDevice(LogitechGSDK.LOGI_DEVICETYPE_PERKEY_RGB);
                BacklightOn();

                Dispatcher.Invoke(() => CreateTaskbarIcon());
            }).Start();
        }

        private void KeyboardHook_OnNumLockPressed()
        {
            DoNumlockIndicator();
        }

        private void PowerSetting_OnMonitorStateChanged(MonitorStateEventArgs eventArgs)
        {
            if (turnOffWithStandby)
            {
                if (eventArgs.MonitorState == MonitorState.Off)
                {
                    BacklightOff();
                }
                else
                {
                    BacklightOn();
                }
            }
        }

        private void CreateTaskbarIconInit()
        {
            taskbarIcon = new TaskbarIcon
            {
                Icon = Properties.Resources.icon,
                ToolTipText = "Initializing Backlight manager"
            };
        }

        private void CreateTaskbarIcon()
        {
            taskbarIcon.ToolTipText = "Backlight manager";
            taskbarIcon.ContextMenu = new ContextMenu() { Style = FindResource("StandardContextMenu") as Style };

            CreateTaskbarIconContextMenu();
        }

        private void CreateTaskbarIconContextMenu()
        {
            var headerItem = new MenuItem()
            {
                Header = new TextBlock() { Text = "Backlight Manager", FontSize = 14, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(192, 192, 192)) },
                Icon = FindResource("icon_keyboard") as Viewbox,
                IsEnabled = false
            };

            var backLightOffWithStandbyItem = new MenuItem()
            {
                Header = "Automatic backlight with standby",
                IsCheckable = true,
                IsChecked = true,
                StaysOpenOnClick = true
            };
            backLightOffWithStandbyItem.Click += (s, e) =>
            {
                turnOffWithStandby = backLightOffWithStandbyItem.IsChecked;
            };

            var backlightOnItem = new MenuItem()
            {
                Header = "Backlight ON",
                Icon = FindResource("icon_lightbulb_on") as Viewbox
            };
            backlightOnItem.Click += (s, e) =>
            {
                BacklightOn();
            };

            var backlightOffItem = new MenuItem()
            {
                Header = "Backlight OFF",
                Icon = FindResource("icon_lightbulb_off") as Viewbox
            };
            backlightOffItem.Click += (s, e) =>
            {
                BacklightOff();
            };

            var numLockIndicatorItem = new MenuItem()
            {
                Header = "Indicate Num Lock",
                IsCheckable = true,
                IsChecked = true,
                StaysOpenOnClick = true
            };
            numLockIndicatorItem.Click += (s, e) =>
            {
                indicateNumLock = numLockIndicatorItem.IsChecked;
            };

            var adaptiveBacklightItem = new MenuItem()
            {
                Header = "Adaptive backlight intensity",
                IsEnabled = false
                /*IsCheckable = true,
                IsChecked = true,
                StaysOpenOnClick = true*/
            };
            adaptiveBacklightItem.Click += (s, e) =>
            {
                adaptiveBacklight = adaptiveBacklightItem.IsChecked;
            };

            var backlightIntensityHeader = new MenuItem()
            {
                Header = "Backlight intensity",
                IsEnabled = false,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(192, 192, 192))
            };

            var sp = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };

            foreach (var backlightPercentButton in backlightPercentButtons)
            {
                sp.Children.Add(backlightPercentButton);
            }

            var spItem = new MenuItem() { Header = sp, StaysOpenOnClick = true, Margin = new Thickness(0, 0, 0, 0) };

            var testItem = new MenuItem()
            {
                Header = "Test Item",
                Icon = FindResource("icon_numlock_1") as Viewbox
            };

            var testItem1 = new MenuItem()
            {
                Header = "Test Item",
                Icon = FindResource("icon_numlock_2") as Viewbox
            };

            var quitItem = new MenuItem()
            {
                Header = "Quit",
                Icon = FindResource("icon_close") as Viewbox
            };
            quitItem.Click += (s, e) =>
            {
                Close();
            };

            taskbarIcon.ContextMenu.Items.Add(headerItem);
            taskbarIcon.ContextMenu.Items.Add(new Separator());
            taskbarIcon.ContextMenu.Items.Add(backlightOnItem);
            taskbarIcon.ContextMenu.Items.Add(backlightOffItem);
            taskbarIcon.ContextMenu.Items.Add(new Separator());
            taskbarIcon.ContextMenu.Items.Add(backLightOffWithStandbyItem);
            taskbarIcon.ContextMenu.Items.Add(numLockIndicatorItem);
            taskbarIcon.ContextMenu.Items.Add(adaptiveBacklightItem);
            taskbarIcon.ContextMenu.Items.Add(backlightIntensityHeader);
            taskbarIcon.ContextMenu.Items.Add(spItem);
            /*taskbarIcon.ContextMenu.Items.Add(new Separator());
            taskbarIcon.ContextMenu.Items.Add(testItem);
            taskbarIcon.ContextMenu.Items.Add(testItem1);*/
            taskbarIcon.ContextMenu.Items.Add(new Separator());
            taskbarIcon.ContextMenu.Items.Add(quitItem);
        }

        private void DoNumlockIndicator()
        {
            if (indicateNumLock)
            {
                if (Dispatcher.Invoke(() => System.Windows.Input.Keyboard.IsKeyToggled(Key.NumLock)))
                {
                    NumLockIndicatorOn();
                }
                else
                {
                    NumLockIndicatorOff();
                }
            }
        }

        private void BacklightOff()
        {
#if !UI_DESIGN
            Chroma.Instance.Mouse.SetAll(ColoreColor.Black);
            LogitechGSDK.LogiLedSetLighting(0, 0, 0);
#endif
        }

        private void BacklightOn()
        {
            uint chromaValue = (uint)((razerBacklightValues[currentBacklightPercentIndex] << 16) | (razerBacklightValues[currentBacklightPercentIndex] << 8) | (razerBacklightValues[currentBacklightPercentIndex]));
            int logiValue = logiBacklightValues[currentBacklightPercentIndex];
#if !UI_DESIGN
            Chroma.Instance.Mouse.SetAll(ColoreColor.FromRgb(chromaValue));
            LogitechGSDK.LogiLedSetLighting(logiValue, logiValue, logiValue);

            DoNumlockIndicator();
#endif
        }

        private void NumLockIndicatorOn()
        {
            int logiValue = logiBacklightValues[currentBacklightPercentIndex];
#if !UI_DESIGN
            LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(KeyboardNames.NUM_LOCK, logiValue, (int)(75 * (logiValue / 100.0)), 0);
#endif
        }

        private void NumLockIndicatorOff()
        {
            int logiValue = (logiBacklightValues[currentBacklightPercentIndex] / 2);
#if !UI_DESIGN
            LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(KeyboardNames.NUM_LOCK, logiValue, logiValue, logiValue);
#endif
        }
    }
}
