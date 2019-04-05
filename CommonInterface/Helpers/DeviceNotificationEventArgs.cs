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
        NT_DEVICE_UPDATE_CONFIG,
        NT_UNLOAD_DEVICE_CONFIGDOMAIN,
        NT_PROCESS_CARDDATA,
        NT_PROCESS_CARDDATA_ERROR,
        NT_GET_DEVICE_CONFIGURATION,
        NT_SET_DEVICE_CONFIGURATION,
        NT_SET_DEVICE_MODE,
        NT_SET_EXECUTE_RESULT,
        NT_SHOW_TERMINAL_DATA,
        NT_SHOW_JSON_CONFIG,
        NT_SHOW_AID_LIST,
        NT_SHOW_CAPK_LIST,
        NT_SHOW_CONFIG_GROUP,
        NT_ENABLE_MODE_BUTTON,
        NT_SET_EMV_MODE_BUTTON,
        NT_FIRMWARE_UPDATE_STEP,
        NT_FIRMWARE_UPDATE_STATUS,
        NT_FIRMWARE_UPDATE_FAILED,
        NT_FIRMWARE_UPDATE_COMPLETE
    }

    [Serializable]
    public class DeviceNotificationEventArgs
    {
        public NOTIFICATION_TYPE NotificationType { get; set; }
        public object [] Message { get; set; }
    }
}
