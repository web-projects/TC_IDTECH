using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface
{
    public enum NOTIFICATION_TYPE
    {
        NT_INITIALIZE_DEVICE,
        NT_UNLOAD_DEVICE_CONFIGDOMAIN,
        NT_PROCESS_CARDDATA,
        NT_PROCESS_CARDDATA_ERROR,
        NT_GET_DEVICE_CONFIGURATION,
        NT_SET_DEVICE_CONFIGURATION,
        NT_SET_DEVICE_MODE,
        NT_SET_EXECUTE_RESULT,
        NT_SHOW_JSON_CONFIG
    }

    [Serializable]
    public class DeviceNotificationEventArgs
    {
        public NOTIFICATION_TYPE NotificationType { get; set; }
        public object [] Message { get; set; }
    }
}
