using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.Helpers
{
    public enum NOTIFICATION_TYPE
    {
        NT_INITIALIZE_DEVICE          = 1,
        NT_DEVICE_UPDATE_CONFIG       = 2,
        NT_UNLOAD_DEVICE_CONFIGDOMAIN = 3,
        NT_PROCESS_CARDDATA           = 4,
        NT_PROCESS_CARDDATA_ERROR     = 5,
        NT_GET_DEVICE_CONFIGURATION   = 6,
        NT_SET_DEVICE_CONFIGURATION   = 7,
        NT_SET_DEVICE_MODE            = 8,
        NT_SET_EXECUTE_RESULT         = 9,
        NT_SHOW_TERMINAL_DATA         = 10,
        NT_SHOW_JSON_CONFIG           = 11,
        NT_ENABLE_MODE_BUTTON         = 12,
        NT_FIRMWARE_UPDATE_COMPLETE   = 13,
    }

    [Serializable]
    public class DeviceNotificationEventArgs
    {
        public NOTIFICATION_TYPE NotificationType { get; set; }
        public object [] Message { get; set; }
    }
}
