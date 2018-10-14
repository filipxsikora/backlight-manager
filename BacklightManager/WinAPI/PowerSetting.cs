using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace BacklightManager.WinAPI
{
    public static class PowerSetting
    {
        public delegate void MonitorStateChanged(MonitorStateEventArgs eventArgs);
        public static event MonitorStateChanged OnMonitorStateChanged;

        private const int WM_POWERBROADCAST = 0x218;
        private const int PBT_POWERSETTINGCHANGE = 0x8013;

        private static Guid monitorGuid = new Guid("6fe69556-704a-47a0-8f24-c28d936fda47");
        private static IntPtr _windowHandle;
        private static HwndSource source;
        private static HwndSourceHook hook;

        [DllImport("User32", SetLastError = true, EntryPoint = "RegisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, Int32 Flags);

        [DllImport("User32", EntryPoint = "UnregisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnregisterPowerSettingNotification(IntPtr handle);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public uint DataLength;
            public byte Data;
        }

        public static void Init(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            RegisterPowerSettingNotification(_windowHandle, ref monitorGuid, 0);
            source = HwndSource.FromHwnd(_windowHandle);
            hook = new HwndSourceHook(WndProc);
            source.AddHook(hook);
        }

        public static void Dispose()
        {
            UnregisterPowerSettingNotification(_windowHandle);
            source.RemoveHook(hook);
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_POWERBROADCAST) //Intercept System Command
            {
                int intValue = wParam.ToInt32();

                switch (intValue)
                {
                    case PBT_POWERSETTINGCHANGE:
                        var powerSettings = (POWERBROADCAST_SETTING)Marshal.PtrToStructure(lParam, typeof(POWERBROADCAST_SETTING));

                        if (powerSettings.PowerSetting == monitorGuid)
                        {
                            OnMonitorStateChanged?.Invoke(new MonitorStateEventArgs(powerSettings.Data));
                        }
                        break;
                }
            }

            return IntPtr.Zero;
        }
    }
}
