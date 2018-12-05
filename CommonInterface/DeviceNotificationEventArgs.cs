using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface
{
    public enum NOTIFICATION_TYPE
    {
        NT_INITIALIZE_DEVICE          = 1,
        NT_UNLOAD_DEVICE_CONFIGDOMAIN = 2,
        NT_PROCESS_CARDDATA           = 3,
        NT_PROCESS_CARDDATA_ERROR     = 4,
        NT_GET_DEVICE_CONFIGURATION   = 5,
        NT_SET_DEVICE_CONFIGURATION   = 6,
        NT_SET_DEVICE_MODE            = 7,
        NT_SET_EXECUTE_RESULT         = 8,
        NT_SHOW_TERMINAL_DATA         = 9,
        NT_SHOW_JSON_CONFIG           = 10
    }

    [Serializable]
    public class DeviceNotificationEventArgs
    {
        public NOTIFICATION_TYPE NotificationType { get; set; }
        public object [] Message { get; set; }
    }
}
