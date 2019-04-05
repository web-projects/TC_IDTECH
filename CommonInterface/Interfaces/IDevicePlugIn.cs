using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPA.CommonInterface.Helpers;
using IPA.CommonInterface.ConfigIDTech;

namespace IPA.CommonInterface.Interfaces
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
    // DATA READER
    void GetCardData();
    void CardReadNextState(object state);
    // Parse Card Data
    string [] ParseCardData(string data);
    // Settings
    void GetDeviceConfiguration();
    // Configuration
    void SetDeviceConfiguration(object data);
    void SetDeviceMode(string mode);
    string DeviceCommand(string command, bool notify);
    // Messaging
    string GetErrorMessage(string data);
  }
}
