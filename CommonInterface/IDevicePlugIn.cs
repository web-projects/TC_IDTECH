using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface
{
  public interface IDevicePlugIn
  {
    // Device Events back to Main Form
    event EventHandler<DeviceNotificationEventArgs> OnDeviceNotification;

    // INITIALIZATION
    string PluginName { get; }
    void DeviceInit();
    ConfigSerializer GetConfigSerializer();
    // GUI UPDATE
    string [] GetConfig();
    // NOTIFICATION
    void SetFormClosing(bool state);
    // MSR READER
    void GetCardData();
    // Settings
    void GetDeviceConfiguration();
    // Configuration
    void SetDeviceConfiguration(object data);
    void SetDeviceMode(string mode);
    void DeviceCommand(string command);
    // Messaging
    string GetErrorMessage(string data);
  }
}
