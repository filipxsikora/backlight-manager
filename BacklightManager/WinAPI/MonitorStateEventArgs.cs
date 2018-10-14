using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BacklightManager.WinAPI
{
    public enum MonitorState { On, Off, Unknown }

    public class MonitorStateEventArgs
    {
        public MonitorStateEventArgs(byte data)
        {
            switch (data)
            {
                case 0:
                    MonitorState = MonitorState.Off;
                    break;
                case 1:
                    MonitorState = MonitorState.On;
                    break;
                default:
                    MonitorState = MonitorState.Unknown;
                    break;
            }
        }

        public MonitorState MonitorState { get; set; }
    }
}
