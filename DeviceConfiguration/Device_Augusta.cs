using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Management;
using System.Threading.Tasks;
using System.Reflection;
using HidLibrary;
using IDTechSDK;

using IPA.CommonInterface;
using IPA.DAL;
using IPA.DAL.RBADAL.Services.Devices.IDTech;
using IPA.Core.Shared.Helpers.StatusCode;
using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Services;

namespace IPA.DAL.RBADAL
{
  [Serializable]
  public class Device_Augusta : MarshalByRefObject, IDevicePlugIn
  {
    /********************************************************************************************************/
    // ATTRIBUTES
    /********************************************************************************************************/
     #region -- attributes --
    HidDevice device;
    Device Device = new Device();

    private IDTechSDK.IDT_DEVICE_Types deviceType;
    private DEVICE_INTERFACE_Types     deviceConnect;
    private DEVICE_PROTOCOL_Types      deviceProtocol;

    private static DeviceInformation deviceInformation;

    // Device Events back to Main Form
//    public event DeviceEventHandler initializeDevice;
//    public event DeviceEventHandler unloadDeviceconfigDomain;
//    public event DeviceEventHandler processCardData;
//    public event DeviceEventHandler processCardDataError;
//    public event DeviceEventHandler getDeviceConfiguration;
//    public event DeviceEventHandler setDeviceConfiguration;
//    public event DeviceEventHandler setDeviceMode;
//    public event DeviceEventHandler setExecuteResult;
//    public event DeviceEventHandler showJsonConfig;

    public event EventHandler<DeviceNotificationEventArgs> OnDeviceNotification;

    private bool useUniversalSDK;
    private bool attached;
    private bool formClosing;

    private readonly object discoveryLock = new object();

    internal static System.Timers.Timer MSRTimer { get; set; }

    private string DevicePluginName;
    public string PluginName { get { return DevicePluginName; } }

    // Configuration handler
    ConfigSerializer serializer;

    // EMV Transactions
    int exponent;
    byte[] additionalTags;
    string amount;

    #endregion

    /********************************************************************************************************/
    // CONSTRUCTION AND INITIALIZATION
    /********************************************************************************************************/
    #region -- construction and initialization --

    public Device_Augusta()
    {
      deviceType = IDT_DEVICE_Types.IDT_DEVICE_NONE;
///      cardReader = new CardReader();
    }

    public void DeviceInit()
    {
      DevicePluginName = "DeviceCfg";

      // Create Device info object
      deviceInformation = new DeviceInformation();

      // Device Discovery
      useUniversalSDK = DeviceDiscovery();

      try
      {
          // Initialize Device
          device = HidDevices.Enumerate(Device_IDTech.IDTechVendorID).FirstOrDefault();

          if (device != null)
          {
            // Get Capabilities
            Debug.WriteLine("");
            Debug.WriteLine("device capabilities ----------------------------------------------------------------");
			Debug.WriteLine("  Usage                          : " + Convert.ToString(device.Capabilities.Usage, 16));
			Debug.WriteLine("  Usage Page                     : " + Convert.ToString(device.Capabilities.UsagePage, 16));
			Debug.WriteLine("  Input Report Byte Length       : " + device.Capabilities.InputReportByteLength);
			Debug.WriteLine("  Output Report Byte Length      : " + device.Capabilities.OutputReportByteLength);
			Debug.WriteLine("  Feature Report Byte Length     : " + device.Capabilities.FeatureReportByteLength);
			Debug.WriteLine("  Number of Link Collection Nodes: " + device.Capabilities.NumberLinkCollectionNodes);
			Debug.WriteLine("  Number of Input Button Caps    : " + device.Capabilities.NumberInputButtonCaps);
			Debug.WriteLine("  Number of Input Value Caps     : " + device.Capabilities.NumberInputValueCaps);
			Debug.WriteLine("  Number of Input Data Indices   : " + device.Capabilities.NumberInputDataIndices);
			Debug.WriteLine("  Number of Output Button Caps   : " + device.Capabilities.NumberOutputButtonCaps);
			Debug.WriteLine("  Number of Output Value Caps    : " + device.Capabilities.NumberOutputValueCaps);
			Debug.WriteLine("  Number of Output Data Indices  : " + device.Capabilities.NumberOutputDataIndices);
			Debug.WriteLine("  Number of Feature Button Caps  : " + device.Capabilities.NumberFeatureButtonCaps);
			Debug.WriteLine("  Number of Feature Value Caps   : " + device.Capabilities.NumberFeatureValueCaps);
			Debug.WriteLine("  Number of Feature Data Indices : " + device.Capabilities.NumberFeatureDataIndices);

            device.Removed += DeviceRemovedHandler;

            //if (!useUniversalSDK)
            if(deviceInformation.deviceMode != IDTECH_DEVICE_PID.AUGUSTA_HID)
            {
                Device.OnNotification += OnNotification;

                Device.Init(SerialPortService.GetAvailablePorts());
                Device.SetDeviceType((int)deviceInformation.deviceMode);
                Device.Connect();
                DeviceInfo di = Device.GetDeviceInfo();
                deviceInformation.SerialNumber = di.SerialNumber;
                deviceInformation.FirmwareVersion = di.FirmwareVersion;
                deviceInformation.ModelNumber = di.ModelNumber;
                deviceInformation.ModelName = di.ModelName;
                deviceInformation.Port = di.Port;
            }
            else
            {
                GetDeviceInformation();
            }

            // Set as Attached
            attached = true;
          }
      }
      catch (Exception xcp)
      {
          //TODO Handle Exception
          throw xcp;
      }

      string [] message = { "COMPLETED" };
      NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_INITIALIZE_DEVICE, Message = message });
    }

    public ConfigSerializer GetConfigSerializer()
    {
        return serializer;
    }

    #endregion

    /********************************************************************************************************/
    // MAIN INTERFACE
    /********************************************************************************************************/
    #region -- main interface --

    public string [] GetConfig()
    {
      if (!attached) 
      { 
        return null; 
      }

      // Get Configuration
      string [] config = new string[5];
        
      config[0] = deviceInformation.SerialNumber;
      config[1] = deviceInformation.FirmwareVersion;
      config[2] = deviceInformation.ModelName;
      config[3] = deviceInformation.ModelNumber;
      config[4] = deviceInformation.Port;
   
      return config;
    }

    public void SetFormClosing(bool state)
    {
      formClosing = state;
    }

    /*protected virtual void OnInitializeDevice(DeviceEventArgs e) 
    {
        if (initializeDevice != null)
        {
          initializeDevice(this, e);
        }
    }

    protected virtual void OnUnloadDeviceConfigDomain(DeviceEventArgs e) 
    {
        if (unloadDeviceconfigDomain != null)
        {
          unloadDeviceconfigDomain(this, e);
        }
    }

    protected virtual void OnProcessCardData(DeviceEventArgs e) 
    {
        if (processCardData != null)
        {
          processCardData(this, e);
        }
    }
    protected virtual void OnProcessCardDataError(DeviceEventArgs e) 
    {
        if (processCardDataError != null)
        {
          processCardDataError(this, e);
        }
    }

    protected virtual void OnGetDeviceConfiguration(DeviceEventArgs e) 
    {
        if (getDeviceConfiguration != null)
        {
          getDeviceConfiguration(this, e);
        }
    }
    
    protected virtual void OnSetDeviceConfiguration(DeviceEventArgs e) 
    {
        if (setDeviceConfiguration != null)
        {
          setDeviceConfiguration(this, e);
        }
    }
    
    protected virtual void OnSetDeviceMode(DeviceEventArgs e) 
    {
        if (setDeviceMode != null)
        {
          setDeviceMode(this, e);
        }
    }
    
    protected virtual void OnSetExecuteResult(DeviceEventArgs e) 
    {
        if (setExecuteResult != null)
        {
          setExecuteResult(this, e);
        }
    }

    protected virtual void OnShowJsonConfig(DeviceEventArgs e) 
    {
        if (showJsonConfig != null)
        {
          showJsonConfig(this, e);
        }
    }*/
    

    protected void OnNotification(object sender, Models.NotificationEventArgs args)
    {
        Debug.WriteLine("device: notification type={0}", args.NotificationType);

        switch (args.NotificationType)
        {

        }
    }

    /*protected void OnDeviceNotification(object sender, DeviceNotificationEventArgs args)
    {
        Debug.WriteLine("device: notification type={0}", args.NotificationType);

        switch (args.NotificationType)
        {

        }
    }*/

    public void NotificationRaise(DeviceNotificationEventArgs e)
    {
        OnDeviceNotification?.Invoke(null, e);
    }

    #endregion

    /********************************************************************************************************/
    // DISCOVERY
    /********************************************************************************************************/
    #region -- device discovery ---
    private bool DeviceDiscovery()
    {
      lock(discoveryLock)
      {
        useUniversalSDK = false;

        var devices = Device.GetUSBDevices();

        if (devices.Count == 1)
        {
            var vendor = devices[0].Vendor;

            switch (devices[0].Vendor)
            {
                case DeviceManufacturer.IDTech:
                {
                    var deviceID = devices[0].DeviceID;
                    string [] worker = deviceID.Split('&');
 
                    // should contain PID_XXXX...
                    if(Regex.IsMatch(worker[1], "PID_"))
                    {
                      string [] worker2 = Regex.Split(worker[1], @"PID_");
                      string pid = worker2[1].Substring(0, 4);

                      // See if device matches
                      int pidId = Int32.Parse(pid);

                      //DeviceEventArgs args = new DeviceEventArgs();
                      object [] message = { "" };

                      switch(pidId)
                      {
                        case (int) IDTECH_DEVICE_PID.AUGUSTA_KYB:
                        {
                          useUniversalSDK = true;
                          deviceInformation.deviceMode = IDTECH_DEVICE_PID.AUGUSTA_KYB;
                          message[0] = "USB-HID";
                          break;
                        }

                        case (int) IDTECH_DEVICE_PID.AUGUSTA_HID:
                        {
                          useUniversalSDK = true;
                          deviceInformation.deviceMode = IDTECH_DEVICE_PID.AUGUSTA_HID;
                          message[0] = "USB-KB";
                          break;
                        }

                        default:
                        {
                            message[0] = "UNKNOWN";
                            break;
                        }
                      }

                      // Notify Main Form
                      //OnSetDeviceMode(args);
                      NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_SET_DEVICE_MODE, Message = message });
                    }

                    break;
                }

                case DeviceManufacturer.Ingenico:
                    //deviceInterface = new Device_Ingenico();
                    //deviceInterface.OnNotification += DeviceOnNotification;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(vendor), vendor, null);
            }
        }
        else if(devices.Count > 1)
        {
            throw new Exception(DeviceStatus.MultipleDevice.ToString());
        }
        else
        {
            throw new Exception(DeviceStatus.NoDevice.ToString());
        }

        // Initialize Universal SDK
        if(useUniversalSDK)
        {
          IDT_Device.setCallback(MessageCallBack);
          IDT_Device.startUSBMonitoring();
        }

        return useUniversalSDK;
      }
    }

    #endregion

    /********************************************************************************************************/
    // DEVICE EVENTS INTERFACE
    /********************************************************************************************************/
    #region -- device event interface ---

    private void DeviceRemovedHandler()
    {
      Debug.WriteLine("\ndevice: removed !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n");

      attached = false;

      // Last device was USDK Type
      if(useUniversalSDK)
      {
          IDT_Device.stopUSBMonitoring();
      }

      // Unload Device Domain
      //OnUnloadDeviceConfigDomain(new DeviceEventArgs());
      NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_UNLOAD_DEVICE_CONFIGDOMAIN });
    }

    private void DeviceAttachedHandler()
    {
      Debug.WriteLine("device: attached ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
    }

    #endregion

    /********************************************************************************************************/
    // UNIVERSAL SDK INTERFACE
    /********************************************************************************************************/
    #region -- universal sdk interface --

    public string ParseFirmwareVersion(string firmwareInfo)
    {
        // Augusta format has no space after V: V1.00
        // Validate the format firmwareInfo see if the version # exists
        var version = firmwareInfo.Substring(firmwareInfo.IndexOf('V') + 1,
                                             firmwareInfo.Length - firmwareInfo.IndexOf('V') - 1).Trim();
        var mReg = Regex.Match(version, @"[0-9]+\.[0-9]+");

        // If the parse succeeded 
        if (mReg.Success)
        {
            version = mReg.Value;
        }

        return version;
    }

    private void SetDeviceConfig()
    {
      if(IDT_DEVICE_Types.IDT_DEVICE_NONE != deviceType)
      {
           // Create Device info object
           if(deviceInformation == null)
           {
              deviceInformation = new DeviceInformation();
           }

           string serialNumber = "";
           RETURN_CODE rt = IDT_Augusta.SharedController.config_getSerialNumber(ref serialNumber);

          if (RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt)
          {
              deviceInformation.SerialNumber = serialNumber;
              Debug.WriteLine("device INFO[Serial Number]: " + deviceInformation.SerialNumber);
          }
          else
          {
            Debug.WriteLine("DeviceCfg::SetDeviceConfig: failed to get serialNumber e={0}", rt);
          }

          if(deviceInformation.deviceMode == IDTECH_DEVICE_PID.AUGUSTA_HID)
          {
              string firmwareVersion = "";
              rt = IDT_Augusta.SharedController.device_getFirmwareVersion(ref firmwareVersion);

              if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
              {
                  deviceInformation.FirmwareVersion = ParseFirmwareVersion(firmwareVersion);
                  Debug.WriteLine("device INFO[Firmware Version]: ", deviceInformation.FirmwareVersion);

                  deviceInformation.Port = firmwareVersion.Substring(firmwareVersion.IndexOf("USB", StringComparison.Ordinal), 7);
                  Debug.WriteLine("device INFO[Port]: ", deviceInformation.Port);
              }

              deviceInformation.ModelName = IDTechSDK.Profile.IDT_DEVICE_String(deviceType, deviceConnect);
              Debug.WriteLine("device INFO[Model Name]: " + deviceInformation.ModelName);
 
              rt = IDT_Augusta.SharedController.config_getModelNumber(ref deviceInformation.ModelNumber);

              if (RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt)
              {
                  Debug.WriteLine("device INFO[Model Number]: " + deviceInformation.ModelNumber);
              }
          }
      }
    }

    private void MessageCallBack(IDTechSDK.IDT_DEVICE_Types type, DeviceState state, byte[] data, IDTTransactionData cardData, EMV_Callback emvCallback, RETURN_CODE transactionResultCode)
    {
      // Setup Connection
      IDTechComm comm = Profile.getComm(type, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_USB);

      if (comm == null)
      {
        comm = Profile.getComm(type, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_SERIAL);
      }

      if (comm == null)
      {
        comm = Profile.getComm(type, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_BT);
      }

      if (comm == null)
      {
        comm = Profile.getComm(type, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_AUDIO_JACK);
      }
      
      deviceConnect = DEVICE_INTERFACE_Types.DEVICE_INTERFACE_UNKNOWN;
      deviceProtocol = DEVICE_PROTOCOL_Types.DEVICE_PROTOCOL_UNKNOWN;

      if (comm != null)
      {
          deviceConnect  = comm.getDeviceConnection();
          deviceProtocol = comm.getDeviceProtocol();
      }

      //Debug.WriteLine("device discovery: state={0}", state);

      switch (state)
      {
        case DeviceState.ToConnect:
        {
          deviceType = type;
          Debug.WriteLine("device connecting: {0}", (object) deviceType.ToString());
          break;
        }

        case DeviceState.Connected:
        {
          deviceType = type;
          attached = true;
          Debug.WriteLine("device connected: {0}", (object) IDTechSDK.Profile.IDT_DEVICE_String(type, deviceConnect));

          // Populate Device Configuration
          SetDeviceConfig();

          break;
        }

        case DeviceState.DefaultDeviceTypeChange:
        {
          break;
        }

        case DeviceState.Notification:
        {
            if (cardData.Notification == EVENT_NOTIFICATION_Types.EVENT_NOTIFICATION_Card_Not_Seated)
            {
                Debug.WriteLine("Notification: ICC Card not Seated\n");
            }
            if (cardData.Notification == EVENT_NOTIFICATION_Types.EVENT_NOTIFICATION_Card_Seated)
            {
                Debug.WriteLine("Notification: ICC Card Seated\n");
            }
            if (cardData.Notification == EVENT_NOTIFICATION_Types.EVENT_NOTIFICATION_Swipe_Card)
            {
                Debug.WriteLine("Notification: MSR Swipe Card\n");
            }

            break;
        }

        case DeviceState.TransactionData:
        {
          if (cardData == null) 
          {
              break;
          }

          //lastCardData = cardData;

          if (type == IDT_DEVICE_Types.IDT_DEVICE_AUGUSTA && deviceProtocol == DEVICE_PROTOCOL_Types.DEVICE_PROTOCOL_KB)
          {
              if (cardData.msr_rawData != null)
              {
                  if (cardData.msr_rawData.Length == 1 && cardData.msr_rawData[0] == 0x18)
                  {
                      Debug.WriteLine("Get MSR Complete! \n");
                      Debug.WriteLine("Get MSR Complete! \n");
                  }
              }

              //clearCallbackData(ref data, ref cardData);

              return;
          }

          if (cardData.Event != EVENT_TRANSACTION_DATA_Types.EVENT_TRANSACTION_PIN_DATA       && 
              cardData.Event != EVENT_TRANSACTION_DATA_Types.EVENT_TRANSACTION_DATA_CARD_DATA && 
              cardData.Event != EVENT_TRANSACTION_DATA_Types.EVENT_TRANSACTION_DATA_EMV_DATA)
          {
             //SoftwareController.MSR_LED_RED_SOLID();
             Debug.WriteLine("MSR Error " + cardData.msr_errorCode.ToString() + "\n");
             Debug.WriteLine("MSR Error " + cardData.msr_errorCode.ToString());
          }
          else
          {
            if (cardData.Event != EVENT_TRANSACTION_DATA_Types.EVENT_TRANSACTION_DATA_EMV_DATA)
            {
              //SoftwareController.MSR_LED_GREEN_SOLID();
            }

            //output parsed card data
            Debug.WriteLine("Return Code: " + transactionResultCode.ToString() + "\r\n");

            // Data Received Processing
            ProcessCardData(cardData);
          }

          break;
        }

        case DeviceState.DataReceived: 
        {
          //SetOutputTextLog(GetTimestamp() +  " IN: " + Common.getHexStringFromBytes(data));
          break;
        }

        case DeviceState.DataSent:
        {
          //SetOutputTextLog(GetTimestamp() + " OUT: " + Common.getHexStringFromBytes(data));
          break;
        }

        case DeviceState.CommandTimeout:
        {
          Debug.WriteLine("Command Timeout\n");
          break;
        }

        case DeviceState.CardAction:
        {
          if (data != null & data.Length > 0)
          {
              CARD_ACTION action = (CARD_ACTION)data[0];
              StringBuilder sb = new StringBuilder("Card Action Request: ");

              if ((action & CARD_ACTION.CARD_ACTION_INSERT) == CARD_ACTION.CARD_ACTION_INSERT)
              {
                sb.Append("INSERT ");
              }

              if ((action & CARD_ACTION.CARD_ACTION_REINSERT) == CARD_ACTION.CARD_ACTION_REINSERT)
              {
                sb.Append("REINSERT ");
              }

              if ((action & CARD_ACTION.CARD_ACTION_REMOVE) == CARD_ACTION.CARD_ACTION_REMOVE)
              {  
                sb.Append("REMOVE ");
              }

              if ((action & CARD_ACTION.CARD_ACTION_SWIPE) == CARD_ACTION.CARD_ACTION_SWIPE)
              {  
                sb.Append("SWIPE ");
              }

              if ((action & CARD_ACTION.CARD_ACTION_SWIPE_AGAIN) == CARD_ACTION.CARD_ACTION_SWIPE_AGAIN)
              {  
                sb.Append("SWIPE_AGAIN ");
              }

              if ((action & CARD_ACTION.CARD_ACTION_TAP) == CARD_ACTION.CARD_ACTION_TAP)
              {  
                sb.Append("TAP ");
              }

              if ((action & CARD_ACTION.CARD_ACTION_TAP_AGAIN) == CARD_ACTION.CARD_ACTION_TAP_AGAIN)
              {  
                sb.Append("TAP_AGAIN ");
              }

              Debug.WriteLine(sb.ToString() + "\n");
          }

          break;
        }

        case DeviceState.MSRDecodeError:
        {
          //SoftwareController.MSR_LED_RED_SOLID();
          Debug.WriteLine("MSR Decode Error\n");
          break;
        }

        case DeviceState.SwipeTimeout:
        {
          Debug.WriteLine("Swipe Timeout\n");
          break;
        }

        case DeviceState.TransactionCancelled:
        {
          Debug.WriteLine("TransactionCancelled.");
          //Debug.WriteLine("");
          //Debug.WriteLine(DeviceTerminalInfo.getDisplayMessage(DeviceTerminalInfo.MSC_ID_WELCOME));
          break;
        }

        case DeviceState.DeviceTimeout:
        {
          Debug.WriteLine("Device Timeout\n");
          break;
        }

        case DeviceState.TransactionFailed:
        {
          if ((int)transactionResultCode == 0x8300)
          {
            //SoftwareController.MSR_LED_RED_SOLID();
          }

          string text =  IDTechSDK.errorCode.getErrorString(transactionResultCode);
          Debug.WriteLine("Transaction Failed: {0}\r\n", (object) text);

          // Allow for GUI Recovery
          //DeviceEventArgs args = new DeviceEventArgs();
          //args.payload[0] = "***** TRANSACTION FAILED: " + text + " *****";
          //OnProcessCardDataError(args);
          string [] message = { "" };
          message[0] = "***** TRANSACTION FAILED: " + text + " *****";
          NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_PROCESS_CARDDATA_ERROR, Message = message });
          break;
        }
      }
    }

    private string TLV_To_Values(byte[] tlv)
    {
      string text = "";
      Dictionary<string, string> dict = Common.processTLVUnencrypted(tlv);
      foreach (KeyValuePair<string, string> kvp in dict) text += kvp.Key + ": " + kvp.Value + "\r\n";
      return text;
    }

    private void ClearCallbackData(ref byte[] data, ref IDTTransactionData cardData)
    {
      if (data != null)
      {
        Array.Clear(data, 0, data.Length);
      }

      if (cardData != null)
      {
        if (cardData.msr_track1 != null) {
          cardData.msr_track1 = "";
        }

        if (cardData.msr_track2 != null) {
          cardData.msr_track2 = "";
        }

        if (cardData.msr_track3 != null) {
          cardData.msr_track3 = "";
        }

        if (cardData.device_RSN != null) {
          cardData.device_RSN = "";
        }

        if (cardData.pin_pinblock != null) {
          cardData.pin_pinblock = "";
        }

        if (cardData.pin_KSN != null) {
          cardData.pin_KSN = "";
        }

        if (cardData.pin_KeyEntry != null) {
          cardData.pin_KeyEntry = "";
        }

        if (cardData.captured_firstPANDigits != null) {
          cardData.captured_firstPANDigits = "";
        }

        if (cardData.captured_lastPANDigits != null) {
          cardData.captured_lastPANDigits = "";
        }

        if (cardData.captured_MACValue != null) {
          Array.Clear(cardData.captured_MACValue, 0, cardData.captured_MACValue.Length);
        }

        if (cardData.captured_MACKSN != null) {
          Array.Clear(cardData.captured_MACKSN, 0, cardData.captured_MACKSN.Length);
        }

        if (cardData.captured_InitialVector != null) {
          Array.Clear(cardData.captured_InitialVector, 0, cardData.captured_InitialVector.Length);
        }

        if (cardData.msr_rawData != null) {
          Array.Clear(cardData.msr_rawData, 0, cardData.msr_rawData.Length);
        }

        if (cardData.msr_encTrack1 != null) {
          Array.Clear(cardData.msr_encTrack1, 0, cardData.msr_encTrack1.Length);
        }

        if (cardData.msr_encTrack2 != null) {
          Array.Clear(cardData.msr_encTrack2, 0, cardData.msr_encTrack2.Length);
        }

        if (cardData.msr_encTrack3 != null) {
          Array.Clear(cardData.msr_encTrack3, 0, cardData.msr_encTrack3.Length);
        }

        if (cardData.msr_KSN != null) {
          Array.Clear(cardData.msr_KSN, 0, cardData.msr_KSN.Length);
        }

        if (cardData.msr_sessionID != null) {
          Array.Clear(cardData.msr_sessionID, 0, cardData.msr_sessionID.Length);
        }

        if (cardData.msr_hashTrack1 != null) {
          Array.Clear(cardData.msr_hashTrack1, 0, cardData.msr_hashTrack1.Length);
        }

        if (cardData.msr_hashTrack2 != null) {
          Array.Clear(cardData.msr_hashTrack2, 0, cardData.msr_hashTrack2.Length);
        }

        if (cardData.msr_hashTrack3 != null) {
          Array.Clear(cardData.msr_hashTrack3, 0, cardData.msr_hashTrack3.Length);
        }

        if (cardData.msr_extendedField != null) {
          Array.Clear(cardData.msr_extendedField, 0, cardData.msr_extendedField.Length);
        }

        if (cardData.emv_clearingRecord != null) {
          Array.Clear(cardData.emv_clearingRecord, 0, cardData.emv_clearingRecord.Length);
        }

        if (cardData.emv_encryptedTags != null) {
          Array.Clear(cardData.emv_encryptedTags, 0, cardData.emv_encryptedTags.Length);
        }

        if (cardData.emv_unencryptedTags != null) {
          Array.Clear(cardData.emv_unencryptedTags, 0, cardData.emv_unencryptedTags.Length);
        }

        if (cardData.emv_maskedTags != null) {
          Array.Clear(cardData.emv_maskedTags, 0, cardData.emv_maskedTags.Length);
        }

        if (cardData.emv_encipheredOnlinePIN != null) {
          Array.Clear(cardData.emv_encipheredOnlinePIN, 0, cardData.emv_encipheredOnlinePIN.Length);
        }

        if (cardData.mac != null) {
          Array.Clear(cardData.mac, 0, cardData.mac.Length);
        }

        if (cardData.macKSN != null) {
          Array.Clear(cardData.macKSN, 0, cardData.macKSN.Length);
        }

        if (cardData.captured_PAN != null) {
          Array.Clear(cardData.captured_PAN, 0, cardData.captured_PAN.Length);
        }

        if (cardData.captured_KSN != null) {
          Array.Clear(cardData.captured_KSN, 0, cardData.captured_KSN.Length);
        }

        if (cardData.captured_Expiry != null) {
          Array.Clear(cardData.captured_Expiry, 0, cardData.captured_Expiry.Length);
        }

        if (cardData.captured_CSC != null) {
          Array.Clear(cardData.captured_CSC, 0, cardData.captured_CSC.Length);
        }
      }
    }

    private void ProcessCardData(IDTTransactionData cardData)
    {
        // Stop Timer
        MSRTimer?.Stop();

        string text = "";

        if (cardData.Event == EVENT_TRANSACTION_DATA_Types.EVENT_TRANSACTION_PIN_DATA)
        {
            Debug.WriteLine("PIN Data received:\r\nKSN: " + cardData.pin_KSN + "\r\nPINBLOCK: " + cardData.pin_pinblock + "\r\nKey Entry: " + cardData.pin_KeyEntry + "\r\n");
            return;
        }

        if (cardData.Event == EVENT_TRANSACTION_DATA_Types.EVENT_TRANSACTION_DATA_CARD_DATA)
        {
            if (cardData.msr_rawData != null)
            {
                Debug.WriteLine("Data received: (Length [" + cardData.msr_rawData.Length.ToString() + "])\n" + string.Concat(cardData.msr_rawData.ToArray().Select(b => b.ToString("X2")).ToArray()) + "\r\n");
                Debug.WriteLine("Data received: (Length [" + cardData.msr_rawData.Length.ToString() + "])\n" + string.Concat(cardData.msr_rawData.ToArray().Select(b => b.ToString("X2")).ToArray()));
            }
        }

        if (cardData.device_RSN != null && cardData.device_RSN.Length > 0)
        {
            text += "Serial Number: " + cardData.device_RSN + "\r\n";
        }

        if (cardData.msr_track1Length > 0)
        {
            text += "Track 1: " + cardData.msr_track1 + "\r\n";
        }

        if (cardData.msr_encTrack1 != null)
        {
            text += "Track 1 Encrypted: " + Common.getHexStringFromBytes(cardData.msr_encTrack1).ToUpper()  + "\r\n";
        }

        if (cardData.msr_hashTrack1 != null)
        {
            text += "Track 1 Hash: " + Common.getHexStringFromBytes(cardData.msr_hashTrack1).ToUpper()  + "\r\n";
        }

        if (cardData.msr_track2Length > 0)
        {
            text += "Track 2: " + cardData.msr_track2 + "\r\n";
        }

        if (cardData.msr_encTrack2 != null)
        {
            text += "Track 2 Encrypted: " + Common.getHexStringFromBytes(cardData.msr_encTrack2).ToUpper() + "\r\n";
        }

        if (cardData.msr_hashTrack2 != null)
        {
            text += "Track 2 Hash: " + Common.getHexStringFromBytes(cardData.msr_hashTrack2).ToUpper()  + "\r\n";
        }

        if (cardData.msr_track3Length > 0)
        {
            text += "Track 3: " + cardData.msr_track3 + "\r\n";
        }

        if (cardData.msr_encTrack3 != null)
        {
            text += "Track 3 Encrypted: " + Common.getHexStringFromBytes(cardData.msr_encTrack3).ToUpper()  + "\r\n";
        }

        if (cardData.msr_hashTrack3 != null)
        {
            text += "Track 3 Hash: " + Common.getHexStringFromBytes(cardData.msr_hashTrack3).ToUpper()  + "\r\n";
        }

        if (cardData.msr_KSN != null)
        {
            text += "KSN: " + Common.getHexStringFromBytes(cardData.msr_KSN).ToUpper()  + "\r\n";
        }

        if (cardData.emv_clearingRecord != null)
        {
            if (cardData.emv_clearingRecord.Length > 0)
            {
                text += "\r\nCTLS Clearing Record: \r\n";
                text += Common.getHexStringFromBytes(cardData.emv_clearingRecord) + "\r\n";
                Dictionary<string, string> dict = Common.processTLVUnencrypted(cardData.emv_clearingRecord);
                foreach (KeyValuePair<string, string> kvp in dict) text += kvp.Key + ": " + kvp.Value + "\r\n";
                text += "\r\n\r\n";
            }
        }
        if (cardData.emv_unencryptedTags != null)
        {
            if (cardData.emv_unencryptedTags.Length > 0)
            {
                text += "\r\n======================== \r\n";

                text += "\r\nUnencrypted Tags: \r\n";
                text += Common.getHexStringFromBytes(cardData.emv_unencryptedTags) + "\r\n\r\n";
                text += TLV_To_Values(cardData.emv_unencryptedTags);
                text += "\r\n======================== \r\n";
            }
        }
        if (cardData.emv_encryptedTags != null)
        {
            if (cardData.emv_encryptedTags.Length > 0)
            {
                text += "\r\n======================== \r\n";
                text += "\r\nEncrypted Tags: \r\n";
                text += Common.getHexStringFromBytes(cardData.emv_encryptedTags) + "\r\n\r\n";
                text += TLV_To_Values(cardData.emv_encryptedTags);
                text += "\r\n======================== \r\n";
            }

        }
        if (cardData.emv_maskedTags != null)
        {
            if (cardData.emv_maskedTags.Length > 0)
            {
                text += "\r\n======================== \r\n";
                text += "\r\nMasked Tags: \r\n";
                text += Common.getHexStringFromBytes(cardData.emv_maskedTags) + "\r\n\r\n";
                text += TLV_To_Values(cardData.emv_maskedTags);
                text += "\r\n======================== \r\n";
            }
        }

        if (cardData.emv_hasAdvise)
        {
          text += "CARD RESPONSE HAS ADVISE" + "\r\n";
        }

        if (cardData.emv_hasReversal)
        {
          text += "CARD RESPONSE HAS REFERSAL" + "\r\n";
        }

        if (cardData.iccPresent == 1)
        {
          text += "ICC Present: TRUE" + "\r\n";
        }

        if (cardData.iccPresent == 2)
        {
          text += "ICC Present: FALSE" + "\r\n";
        }

        if (cardData.isCTLS == 1)
        {
          text += "CTLS Capture: TRUE" + "\r\n";
        }

        if (cardData.isCTLS == 2)
        {
          text += "CTLS Capture: FALSE" + "\r\n";
        }

        if (cardData.msr_extendedField != null && cardData.msr_extendedField.Length > 0)
        {
            text += "Extended Field Bytes: " + Common.getHexStringFromBytes(cardData.msr_extendedField) + "\r\n";
        }

        if (cardData.captureEncryptType == CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_TDES)
        {
            text += "Encryption Type: TDES\r\n";
        }

        if (cardData.captureEncryptType == CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_AES)
        {
            text += "Encryption Type: AES\r\n";
        }

        if (cardData.captureEncryptType == CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_NONE)
        {
            text += "Encryption Type: NONE\r\n";
        }

        if (cardData.captureEncryptType != CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_NONE)
        {
          if (cardData.msr_keyVariantType == KEY_VARIANT_TYPE.KEY_VARIANT_TYPE_DATA)
          {
            text += "Key Type: Data Variant\r\n";
          }
          else if (cardData.msr_keyVariantType == KEY_VARIANT_TYPE.KEY_VARIANT_TYPE_PIN)
          {
            text += "Key Type: PIN Variant\r\n";
          }
        }

        if (cardData.mac != null)
        {
          text += "MAC: " + Common.getHexStringFromBytes(cardData.mac) + "\r\n";
        }

        if (cardData.macKSN != null)
        {
          text += "MAC KSN: " + Common.getHexStringFromBytes(cardData.macKSN) + "\r\n";
        }

        if (cardData.Event == EVENT_TRANSACTION_DATA_Types.EVENT_TRANSACTION_DATA_EMV_DATA)
        {
            if ((cardData.isCTLS != 1) && (cardData.captureEncryptType != CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_NONE))
            {
              text += "Capture Encrypt Type: " + ((cardData.captureEncryptType == CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_TDES) ? "TDES" : "AES") + "\r\n";
            }

            switch (cardData.emv_resultCode)
            {
              case EMV_RESULT_CODE.EMV_RESULT_CODE_APPROVED:
              {
                text += ("RESULT: " + "EMV_RESULT_CODE_APPROVED" + "\r\n");
                //Debug.WriteLineLCD("APPROVED");
                break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_APPROVED_OFFLINE:
              {
                //Debug.WriteLineLCD("APPROVED");
                text += ("ERESULT: " + "EMV_RESULT_CODE_APPROVED_OFFLINE" + "\r\n");
                break; 
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_DECLINED_OFFLINE:
              {
                text += ("RESULT: " + "EMV_RESULT_CODE_DECLINED_OFFLINE" + "\r\n");
                break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_DECLINED:
              {
                text += ("RESULT: " + "EMV_RESULT_CODE_DECLINED" + "\r\n");
                break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_GO_ONLINE:
              {
                text += ("RESULT: " + "EMV_RESULT_CODE_GO_ONLINE" + "\r\n");
                break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_CALL_YOUR_BANK:
              {
                //SetOutputTextLCD("CALL YOUR BANK");
                text += ("RESULT: " + "EMV_RESULT_CODE_CALL_YOUR_BANK" + "\r\n");
                break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_NOT_ACCEPTED:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_NOT_ACCEPTED" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_FALLBACK_TO_MSR:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_FALLBACK_TO_MSR" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_TIMEOUT:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_TIMEOUT" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_AUTHENTICATE_TRANSACTION:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_AUTHENTICATE_TRANSACTION" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_SWIPE_NON_ICC:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_SWIPE_NON_ICC" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_CTLS_TWO_CARDS:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_CTLS_TWO_CARDS" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_CTLS_TERMINATE:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_CTLS_TERMINATE" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_CTLS_TERMINATE_TRY_ANOTHER:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_CTLS_TERMINATE_TRY_ANOTHER" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_GO_ONLINE_CTLS:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_GO_ONLINE_CTLS" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_MSR_SWIPE_CAPTURED:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_MSR_SWIPE_CAPTURED" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_REQUEST_ONLINE_PIN:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_REQUEST_ONLINE_PIN" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_REQUEST_SIGNATURE:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_REQUEST_SIGNATURE" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_ADVISE_REQUIRED:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_ADVISE_REQUIRED" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_REVERSAL_REQUIRED:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_REVERSAL_REQUIRED" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_ADVISE_REVERSAL_REQUIRED:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_ADVISE_REVERSAL_REQUIRED" + "\r\n");
                  break;
              }

              case EMV_RESULT_CODE.EMV_RESULT_CODE_NO_ADVISE_REVERSAL_REQUIRED:
              {
                  text += ("RESULT: " + "EMV_RESULT_CODE_NO_ADVISE_REVERSAL_REQUIRED" + "\r\n");
                  break;
              }

              default:
              {
                string val = errorCode.getErrorString((RETURN_CODE)cardData.emv_resultCode);
                if (val == null || val.Length == 0) val = "EMV_ERROR_ENCOUNTERED";
                text += ("RESULT: " + val + "\r\n");
                break;
              }
            }
        }
        else
        {
            text += ("RESULT: " + "TRANSACTION OVER" + "\r\n");
        }

        if (cardData.emv_transaction_Error_Code > 0)
        {
          text += ("Transaction Error: " + errorCode.getTransError(cardData.emv_transaction_Error_Code) + "\r\n");
        }

        if (cardData.emv_RF_State > 0)
        {
          text += ("RF State: " + errorCode.getRFState(cardData.emv_RF_State) + "\r\n");
        }

        if (cardData.emv_ESC > 0)
        {
          text += ("Extended Status Code: " + errorCode.getExtendedStatusCode(cardData.emv_ESC) + "\r\n");
        }

        if (cardData.emv_appErrorFn > 0)
        {
          text += ("App Error Function: " + errorCode.getEMVAppErrorFn(cardData.emv_appErrorFn) + "\r\n");
        }

        if (cardData.emv_appErrorState > 0)
        {
          text += ("App Error State: " + errorCode.getEMVAppErrorState(cardData.emv_appErrorState) + "\r\n");
        }

        if (cardData.ctlsApplication > 0)
        {
            text += "Contactless Application: ";

            switch (cardData.ctlsApplication)
            {
              case CTLS_APPLICATION.CTLS_APPLICATION_AMEX:
              {
                  text += ("CTLS_APPLICATION_AMEX" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_DISCOVER:
              {
                  text += ("CTLS_APPLICATION_DISCOVER" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_MASTERCARD:
              {
                  text += ("CTLS_APPLICATION_MASTERCARD" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_VISA:
              {
                  text += ("CTLS_APPLICATION_VISA" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_SPEEDPASS:
              {
                  text += ("CTLS_APPLICATION_SPEEDPASS" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_GIFT_CARD:
              {
                  text += ("CTLS_APPLICATION_GIFT_CARD" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_DINERS_CLUB:
              {
                  text += ("CTLS_APPLICATION_DINERS_CLUB" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_EN_ROUTE:
              {
                  text += ("CTLS_APPLICATION_EN_ROUTE" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_JCB:
              {
                  text += ("CTLS_APPLICATION_JCB" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_VIVO_DIAGNOSTIC:
              {
                  text += ("CTLS_APPLICATION_VIVO_DIAGNOSTIC" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_HID:
              {
                  text += ("CTLS_APPLICATION_HID" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_MSR_SWIPE:
              {
                  text += ("CTLS_APPLICATION_MSR_SWIPE" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_RESERVED:
              {
                  text += ("CTLS_APPLICATION_RESERVED" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_DES_FIRE_TRACK_DATA:
              {
                  text += ("CTLS_APPLICATION_DES_FIRE_TRACK_DATA" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_DES_FIRE_RAW_DATA:
              {
                  text += ("CTLS_APPLICATION_DES_FIRE_RAW_DATA" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_RBS:
              {
                  text += ("CTLS_APPLICATION_RBS" + "\r\n");
                  break;
              }

              case CTLS_APPLICATION.CTLS_APPLICATION_VIVO_COMM:
              {
                  text += ("CTLS_APPLICATION_VIVO_COMM" + "\r\n");
                  break;
              }
            }
        }

        Debug.WriteLine(text);

        // Process Card Data
//        DeviceEventArgs args = new DeviceEventArgs();
//        args.payload[0] = text;
//        OnProcessCardData(args);
        string [] message = { text };
        NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_PROCESS_CARDDATA, Message = message });

        byte[] temp = new byte[0];
        ClearCallbackData(ref temp, ref cardData);

        //if (cardData.emv_resultCode == EMV_RESULT_CODE.EMV_RESULT_CODE_GO_ONLINE && cbAutoComplete.Checked)
        //{
        //    tbOutput.AppendText("Auto Complete Executing ");
        //    btnEMVComplete_Click(null, null);
        //}
    }

    /********************************************************************************************************/
    // CONFIGURATION GETS
    /********************************************************************************************************/
    private string GetExpirationMask()
    {
      string result = "";

      byte response = 0x00;
      RETURN_CODE rt = IDT_Augusta.SharedController.msr_getExpirationMask(ref response);

      if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
      {
          result = ((response == 0x30) ? "Masked" : "Unmasked");
          Debug.WriteLine("Expiration Masking: " + ((response == 0x30) ? "Masked" : "Unmasked"));
      }
      else
      {
          result = "Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt) + ": " + IDTechSDK.errorCode.getErrorString(rt);
      }

      return result;
    }

    private string GetClearPANDigits()
    {
      string result = "";

      byte response = 0x00;

      RETURN_CODE rt = IDT_Augusta.SharedController.msr_getClearPANID(ref response);

      if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
      {
          result = (String.Format("{0:X}", (int)(response)));
          Debug.WriteLine("Get Clear PAN Digits Response: " + (int)(response));
      }
      else
      {
          result = "Get Clear PAN Digits Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt) + ": " + IDTechSDK.errorCode.getErrorString(rt);
          Debug.WriteLine("Get Clear PAN Digits Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt));
      }

      return result;
    }

    private string GetSwipeForceEncryption()
    {
        byte format = 0;
        string result = "";

        RETURN_CODE rt = IDT_Augusta.SharedController.msr_getSwipeForcedEncryptionOption(ref format);

        if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
        {
            if ((format & 0x01) == 0x01)
            {
                result = "T1: ON";
                Debug.WriteLine("Track 1 Swipe Force Encryption: ON");
            }
            else
            {
                result = "T1: OFF";
                Debug.WriteLine("Track 1 Swipe Force Encryption: OFF");
            }

            if ((format & 0x02) == 0x02)
            {
                result += ", T2: ON";
                Debug.WriteLine("Track 2 Swipe Force Encryption: ON");
            }
            else
            {
                result += ", T2: OFF";
                Debug.WriteLine("Track 2 Swipe Force Encryption: OFF");
            }

            if ((format & 0x04) == 0x04)
            {
                result += ", T3: ON";
                Debug.WriteLine("Track 3 Swipe Force Encryption: ON");
            }
            else
            {
                result += ", T3: OFF";
                Debug.WriteLine("Track 3 Swipe Force Encryption: OFF");
            }

            if ((format & 0x08) == 0x08)
            {
                result += ", T3 Option 0: ON";
                System.Diagnostics.Debug.WriteLine("Track 3 Option 0 Swipe Force Encryption: ON");
            }
            else
            {
                result += ", T3 Option 0: OFF";
                Debug.WriteLine("Track 3 Option 0 Swipe Force Encryption: OFF");
            }
        }
        else
        {
            result = "Swipe Force Encryption  Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt);
            Debug.WriteLine("Get Swipe Force Encryption  Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt));
        }

        return result;
    }

    private string GetSwipeMaskOption()
    {
      string result = "";

      byte format = 0;

      RETURN_CODE rt = IDT_Augusta.SharedController.msr_getSwipeMaskOption(ref format);

      if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
      {
          if ((format & 0x01) == 0x01)
          {
              result = "T1 Mask: ON";
              System.Diagnostics.Debug.WriteLine("Track 1 Mask Option: ON");
          }
          else
          {
              result = "T1 Mask: OFF";
              Debug.WriteLine("Track 1 Mask: OFF");
          }

          if ((format & 0x02) == 0x02)
          {
              result += ", T2 Mask: ON";
              Debug.WriteLine("Track 2 Mask: ON");
          }
          else
          {
              result += ", T2 Mask: OFF";
              Debug.WriteLine("Track 2 Mask: OFF");
          }
          if ((format & 0x04) == 0x04)
          {
              result += ", T3 Mask: ON";
              Debug.WriteLine("Track 3 Mask: ON");
          }
          else
          {
              result += ", T3 Mask: OFF";
              Debug.WriteLine("Track 3 Mask: OFF");
          }
      }
      else
      {
          result += "Get Mask Option  Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt);
          Debug.WriteLine("Get Mask Option  Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt));
      }

      return result;
    }

    private void GetDeviceInformation()
    {
        if(serializer == null)
        {
            serializer = new ConfigSerializer();
        }

        serializer.ReadConfig();

        // Get Company
        GetCompany();

        // Terminal Info
        string enable_read_terminal_info = System.Configuration.ConfigurationManager.AppSettings["tc_read_terminal_info"] ?? "true";
        bool read_terminal_info;
        bool.TryParse(enable_read_terminal_info, out read_terminal_info);
        if(read_terminal_info)
        {
            GetTerminalInfo();
        }
 
        // Terminal Information
        string enable_read_terminal_data = System.Configuration.ConfigurationManager.AppSettings["tc_read_terminal_data"] ?? "true";
        bool read_terminal_data;
        bool.TryParse(enable_read_terminal_data, out read_terminal_data);
        if(read_terminal_data)
        {
            GetTerminalData();
        }

        // Encryption Control
        string enable_read_encryption = System.Configuration.ConfigurationManager.AppSettings["tc_read_encryption"] ?? "true";
        bool read_encryption;
        bool.TryParse(enable_read_encryption, out read_encryption);
        if(read_encryption)
        {
            GetEncryptionControl();
        }

        // Device Configuration: contact:capk
        string enable_read_capk_settings = System.Configuration.ConfigurationManager.AppSettings["tc_read_capk_settings"] ?? "true";
        bool read_capk_settings;
        bool.TryParse(enable_read_capk_settings, out read_capk_settings);
        if(read_capk_settings)
        {
            GetCapkList();
        }

        // Device Configuration: contact:aid
        string enable_read_aid_settings = System.Configuration.ConfigurationManager.AppSettings["tc_read_aid_settings"] ?? "true";
        bool read_aid_settings;
        bool.TryParse(enable_read_aid_settings, out read_aid_settings);
        if(read_aid_settings)
        {
            GetAidList();
        }

        // MSR Settings
        string enable_read_msr_settings = System.Configuration.ConfigurationManager.AppSettings["tc_read_msr_settings"] ?? "true";
        bool read_msr_settings;
        bool.TryParse(enable_read_msr_settings, out read_msr_settings);
        if(read_msr_settings)
        {
            GetMSRSettings();
        }

        // Update configuration file
        serializer.WriteConfig();

        // Display JSON Config to User
        //DeviceEventArgs args = new DeviceEventArgs();
        //args.payload[0] = serializer.GetFileName();
        //OnShowJsonConfig(args);
        object [] message = { serializer.GetFileName() };
        NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_SHOW_JSON_CONFIG, Message = message });
    }

    private void GetCompany()
    {
        try
        {
            serializer.config_meta.Customer.Company = "TrustCommerce";
        }
        catch(Exception exp)
        {
            Debug.WriteLine("DeviceCfg::GetCompany(): - exception={0}", (object)exp.Message);
        }
    }

    private void GetTerminalInfo()
    {
        try
        {
            serializer.config_meta.Type = "device";
            serializer.hardware.Serial_num = deviceInformation.SerialNumber;
            serializer.config_meta.Terminal_type = deviceInformation.ModelName;
            Version version = typeof(Device_Augusta).Assembly.GetName().Version;
            serializer.config_meta.Version = version.ToString();

            string response = null;
            RETURN_CODE rt = IDT_Augusta.SharedController.device_getFirmwareVersion(ref response);

            if (RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt && !string.IsNullOrWhiteSpace(response))
            {
                serializer.general_configuration.Terminal_info.firmware_ver = response;
            }
            response = "";
            rt = IDT_Augusta.SharedController.emv_getEMVKernelVersion(ref response);
            if(RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt && !string.IsNullOrWhiteSpace(response))
            {
                serializer.general_configuration.Terminal_info.contact_emv_kernel_ver = response;
            }
            response = "";
            rt = IDT_Augusta.SharedController.emv_getEMVKernelCheckValue(ref response);
            if(RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt && !string.IsNullOrWhiteSpace(response))
            {
                serializer.general_configuration.Terminal_info.contact_emv_kernel_checksum = response;
            }
            response = "";
            rt = IDT_Augusta.SharedController.emv_getEMVConfigurationCheckValue(ref response);
            if(RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt && !string.IsNullOrWhiteSpace(response))
            {
                serializer.general_configuration.Terminal_info.contact_emv_kernel_configuration_checksum = response;
            }
        }
        catch(Exception exp)
        {
            Debug.WriteLine("DeviceCfg::GetTerminalInfo(): - exception={0}", (object)exp.Message);
        }
    }

    private void GetTerminalData()
    {
        try
        {
            //int id = IDT_Augusta.SharedController.emv_retrieveTerminalID();

            byte [] tlv = null;
            RETURN_CODE rt = IDT_Augusta.SharedController.emv_retrieveTerminalData(ref tlv);
            
            if(RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt && tlv != null)
            {
                TerminalData td = new TerminalData(tlv);
                string text = td.ConvertTLVToValuePairs();
                serializer.general_configuration.Contact.terminal_data = td.ConvertTLVToString();
                serializer.general_configuration.Contact.tags = td.GetTags();
                // Information From Terminal Data
                string language = td.GetTagValue("DF10");
                language = (language.Length > 1) ? language.Substring(0, 2) : "";
                string merchantName = td.GetTagValue("9F4E");
///                merchantName = CardReader.ConvertHexStringToAscii(merchantName);
                string merchantID = td.GetTagValue("9F16");
///                merchantID = CardReader.ConvertHexStringToAscii(merchantID);
                string terminalID = td.GetTagValue("9F1C");
///                terminalID = CardReader.ConvertHexStringToAscii(terminalID);
                string exp = td.GetTagValue("5F36");
                exponent = Int32.Parse(td.GetTagValue("5F36"));
            }
        }
        catch(Exception exp)
        {
            Debug.WriteLine("DeviceCfg::GetTerminalData(): - exception={0}", (object)exp.Message);
        }
    }

    private void GetEncryptionControl()
    {
        try
        {
            bool msr = false;
            bool icc = false;
            RETURN_CODE rt = IDT_Augusta.SharedController.config_getEncryptionControl(ref msr, ref icc);
            
            if(RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt)
            {
                serializer.general_configuration.Encryption.msr_encryption_enabled = msr;
                serializer.general_configuration.Encryption.icc_encryption_enabled = icc;
                byte format = 0;
                rt = IDT_Augusta.SharedController.icc_getKeyFormatForICCDUKPT(ref format);
                if(RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt)
                {
                    string key_format = "None";
                    switch(format)
                    {
                        case 0x00:
                        {
                            key_format = "TDES";
                            break;
                        }
                        case 0x01:
                        {
                            key_format = "AES";
                            break;
                        }
                    }
                    serializer.general_configuration.Encryption.data_encryption_type = key_format;
                }
            }
        }
        catch(Exception exp)
        {
            Debug.WriteLine("DeviceCfg::GetEncryptionControl(): - exception={0}", (object)exp.Message);
        }
    }

    private void GetCapkList()
    {
        if(deviceInformation.deviceMode == IDTECH_DEVICE_PID.AUGUSTA_HID)
        {
            try
            {
                if(serializer != null)
                {
                    byte [] keys = null;
                    RETURN_CODE rt = IDT_Augusta.SharedController.emv_retrieveCAPKList(ref keys);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        List<Capk> CAPKList = new List<Capk>();

                        foreach(byte[] capk in keys.Split(6))
                        {
                            byte[] key = null;

                            rt = IDT_Augusta.SharedController.emv_retrieveCAPK(capk, ref key);

                            if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                            {
                                Capk apk = new Capk(key);
                                CAPKList.Add(apk);
                            }
                        }

                        // Write to Configuration File
                        if(CAPKList.Count > 0)
                        {
                            serializer.general_configuration.Contact.capk = CAPKList;
                        }
                    }
                }
            }
            catch(Exception exp)
            {
                Debug.WriteLine("DeviceCfg::GetCapkList(): - exception={0}", (object)exp.Message);
            }
        }
    }

    private void GetAidList()
    {
        if(deviceInformation.deviceMode == IDTECH_DEVICE_PID.AUGUSTA_HID)
        {
            try
            {
                if(serializer != null)
                {
                    byte [][] keys = null;
                    RETURN_CODE rt = IDT_Augusta.SharedController.emv_retrieveAIDList(ref keys);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        List<Aid> AidList = new List<Aid>();

                        foreach(byte[] aidName in keys)
                        {
                            byte[] value = null;

                            rt = IDT_Augusta.SharedController.emv_retrieveApplicationData(aidName, ref value);

                            if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                            {
                                Aid aid = new Aid(aidName, value);
                                aid.ConvertTLVToValuePairs();
                                AidList.Add(aid);
                            }
                        }

                        // Write to Configuration File
                        if(AidList.Count > 0)
                        {
                            serializer.general_configuration.Contact.aid = AidList;
                        }
                    }
                }
            }
            catch(Exception exp)
            {
                Debug.WriteLine("DeviceCfg::GetAidList(): - exception={0}", (object)exp.Message);
            }
        }
    }

    private void GetMSRSettings()
    {
        try
        {
            Msr msr = new Msr();
            List<MSRSettings> msr_settings =  new List<MSRSettings>();; 

            foreach(var setting in msr.msr_settings)
            {
                byte value   = 0;
                //RETURN_CODE rt = IDT_Augusta.SharedController.msr_getSetting((byte)setting.function_value, ref value);
                RETURN_CODE rt = IDT_Augusta.SharedController.msr_getSetting(Convert.ToByte(setting.function_id, 16), ref value);

                if(RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt)
                {
                    setting.value = value.ToString("x");
                    msr_settings.Add(setting);
                }
            }

            serializer.general_configuration.msr_settings = msr_settings;
        }
        catch(Exception exp)
        {
            Debug.WriteLine("DeviceCfg::GetMSRSettings(): - exception={0}", (object)exp.Message);
        }
    }

    /********************************************************************************************************/
    // CONFIGURATION SETS
    /********************************************************************************************************/
   private string SetExpirationMask(object payload)
    {
      string result = "";
      bool mask = false;

      List<MsrConfigItem> data = (List<MsrConfigItem>) payload;

      foreach (MsrConfigItem child in data)  
      {  
        switch(child.Id)
        {
          case (int) EXPIRATION_MASK.MASK:
          {
            mask = child.Value.Equals("True", StringComparison.OrdinalIgnoreCase) ? true : false;
            break;
          }
        }
      } 

      RETURN_CODE rt = IDT_Augusta.SharedController.msr_setExpirationMask(mask);

      if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
      {
          return GetExpirationMask();
      }
      else
      {
          result = "Set Expiration Mask Fail Error: " + "0x" + String.Format("{0:X}", (ushort)rt) + ": " + IDTechSDK.errorCode.getErrorString(rt);
      }

      return result;
    }

   private string SetClearPANDigits(object payload)
    {
      string result = "";
      byte val = 0;

      List<MsrConfigItem> data = (List<MsrConfigItem>) payload;

      foreach (MsrConfigItem child in data)  
      {  
        switch(child.Id)
        {
          case (int) PAN_DIGITS.DIGITS:
          {
            val = (byte)Int32.Parse(child.Value.Trim());
            break;
          }
        }
      } 

      RETURN_CODE rt = IDT_Augusta.SharedController.msr_setClearPANID(val);

      if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
      {
        return GetClearPANDigits();
      }
      else
      {
          result = "Get Clear PAN Digits Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt) + ": " + IDTechSDK.errorCode.getErrorString(rt);
          Debug.WriteLine("Get Clear PAN Digits Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt));
      }

      return result;
    }

    private string SetForceSwipeEncryption(object payload)
    {
        string result = "";

        bool track1 = false;
        bool track2 = false; 
        bool track3 = false;
        bool track3card0 = false;

        List<MsrConfigItem> data = (List<MsrConfigItem>) payload;

        foreach (MsrConfigItem child in data)  
        {  
          switch(child.Id)
          {
            case (int) SWIPE_FORCE_ENCRYPTION.TRACK1:
            {
              track1 = child.Value.Equals("True", StringComparison.OrdinalIgnoreCase) ? true : false;
              break;
            }

            case (int) SWIPE_FORCE_ENCRYPTION.TRACK2:
            {
              track2 = child.Value.Equals("True", StringComparison.OrdinalIgnoreCase) ? true : false;
              break;
            }

            case (int) SWIPE_FORCE_ENCRYPTION.TRACK3:
            {
              track3 = child.Value.Equals("True", StringComparison.OrdinalIgnoreCase) ? true : false;
              break;
            }

            case (int) SWIPE_FORCE_ENCRYPTION.TRACK3CARD0:
            {
              track3card0 = child.Value.Equals("True", StringComparison.OrdinalIgnoreCase) ? true : false;
              break;
            }
          }
        } 

        RETURN_CODE rt = IDT_Augusta.SharedController.msr_setSwipeForcedEncryptionOption(track1, track2, track3, track3card0);

        if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
        {
          return GetSwipeForceEncryption();
        }
        else
        {
            result = "Swipe Force Encryption Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt);
            Debug.WriteLine("Set Swipe Force Encryption  Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt));
        }

        return result;
    }

    private string SetSwipeMaskOption(object payload)
    {
      string result = "";

      bool track1 = false;
      bool track2 = false; 
      bool track3 = false;

      List<MsrConfigItem> data = (List<MsrConfigItem>) payload;

      foreach (MsrConfigItem child in data)  
      {  
        switch(child.Id)
        {
          case (int) SWIPE_MASK.TRACK1:
          {
            track1 = child.Value.Equals("True", StringComparison.OrdinalIgnoreCase) ? true : false;
            break;
          }

          case (int) SWIPE_MASK.TRACK2:
          {
            track2 = child.Value.Equals("True", StringComparison.OrdinalIgnoreCase) ? true : false;
            break;
          }

          case (int) SWIPE_MASK.TRACK3:
          {
            track3 = child.Value.Equals("True", StringComparison.OrdinalIgnoreCase) ? true : false;
            break;
          }
        }
      } 

      RETURN_CODE rt = IDT_Augusta.SharedController.msr_setSwipeMaskOption(track1, track2, track3);

      if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
      {
        return GetSwipeMaskOption();
      }
      else
      {
          result += "Get Mask Option  Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt);
          Debug.WriteLine("Get Mask Option  Fail Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt));
      }

      return result;
    }

    #endregion

    /********************************************************************************************************/
    // HIDLIBRARY INTERFACE
    /********************************************************************************************************/
    #region -- hidlibrary interface --
  
    private static byte GetCheckSumValue(byte[] dataBytes)
    {
        return dataBytes.Aggregate<byte, byte>(0x0, (current, t) => (byte)(current ^ t));
    }

    private static byte GetCheckSum(byte[] dataBytes)
    {
        byte result = dataBytes.Aggregate<byte, byte>(0x0, (current, t) => (byte)(current + t));
        return (byte)(result & 0xFF);
    }

    private EntryModeStatus SetupCommand(byte[] command, out byte[] response)
    {
        var status = EntryModeStatus.Success;
        const int bufferLength = 1000;
        var deviceDataBuffer = new byte[bufferLength];
        response = null;

        try
        {
          // Initialize return data buffer
          for (int i = 0; i < bufferLength; i++)
          {
            deviceDataBuffer[i] = 0;
          }

          //TODO: Cannot have init call init, circular calls, recheck logic
          //if (device == null || !device.IsConnected)
          //status = Init();

          if (status == EntryModeStatus.Success)
          {
              int featureReportLen = device.Capabilities.FeatureReportByteLength;
              
              //WriteFeatureData works better if we send the entire feature length array, not just the length of command plus checksum
              var reportBuffer = new byte[featureReportLen];
              
              //Assume featureCommand is not 0 prepended, and contains a checksum.
              var zeroReportIdCommand = new byte[Math.Max(command.Length + 2, featureReportLen)];

              // Prepend 0x00 to command[...] since HidLibrary expects Features to start with reportID, and we use 0.
              zeroReportIdCommand[0] = 0x00;
              Array.Copy(command, 0, zeroReportIdCommand, 1, command.Length);

              // Send COMMAND
              var result = device.WriteFeatureData(zeroReportIdCommand);

              if (result)
              {
                  // Empirical data shows this is a good time to wait.
                  Thread.Sleep(1200); 
                  result = ReadFeatureDataLong(out deviceDataBuffer);
              }

              //as long as we have data in result, we are ok with failed reading later.
              if (result || deviceDataBuffer.Length > 0)
              {
                int dataIndex = 0;

                for (dataIndex = bufferLength - 1; dataIndex > 1; dataIndex--)
                {
                    if (deviceDataBuffer[dataIndex] != 0)
                    {
                        break;
                    }
                }

                response = new byte[dataIndex + 1];

                for (var ind = 0; ind <= dataIndex; ind++)
                {
                    response[ind] += deviceDataBuffer[ind];
                }

                if(dataIndex > 1)
                {
                  Debug.WriteLine("reponse: {0}", Encoding.UTF8.GetString(response, 0, response.Length), null);
                }

                status = EntryModeStatus.Success;
              }
              else
              {
                status = EntryModeStatus.CardNotRead;
              }
          }
      }
      catch (Exception ex)
      {
          status = EntryModeStatus.Error;
      }

      return status;
    }

    public bool ReadFeatureDataLong(out byte[] resBuffer, byte reportId = 0x00)
    {
        bool success = false;
        resBuffer = new byte[1000];
        
        if (device != null && device.IsConnected)
        {
            bool isFirstNonZeroBlock = false;
            int responseLength = 0;
            int reportLength = device.Capabilities.FeatureReportByteLength;
            byte[] reportBuffer = new byte[reportLength];

            try
            {
                // Get response data from HID Device
                success = true;

                for (int k = 0; k < 100 && success; k++)  // 1 second in total
                {
                    for (int indx = 0; indx < reportBuffer.Length; indx++)
                    {
                        reportBuffer[indx] = 0;
                    }

                    success = device.ReadFeatureData(out reportBuffer, reportId);
 
                    if (success)
                    {
                        for (int i = 0; i < reportLength; i++)
                        {
                            if (reportBuffer[i] != 0)
                            {
                                isFirstNonZeroBlock = true;
                                break;
                            }
                        }
                    }

                    // Pack the data after first non zero data block 
                    if (isFirstNonZeroBlock)
                    {
                        Array.Copy(reportBuffer, 1, resBuffer, responseLength, reportLength - 1);
                        responseLength += reportLength - 1;
                    }

                    if (responseLength + reportLength > resBuffer.Length)
                    {
                        success = false;
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception xcp)
            {
                throw xcp;
            }
        }

        return success;
    }

    private EntryModeStatus SetDeviceHidMode()
    {
      byte[] result;

      // Create the command to get config values
      var readConfig = new byte[CommandTokens.SetUSBHIDMode.Length + 1];
      Array.Copy(CommandTokens.SetUSBHIDMode, readConfig, CommandTokens.SetUSBHIDMode.Length);
      readConfig[CommandTokens.SetUSBHIDMode.Length] = 0x00;
      readConfig[readConfig.Length - 1] = GetCheckSumValue(readConfig);

      //execute the command, get the result
      var status = SetupCommand(readConfig, out result);

      return status;
    }

    private EntryModeStatus DeviceSoftReset()
    {
      byte[] result;

      // Create the command to get config values
      var readConfig = new byte[CommandTokens.DeviceReset.Length + 1];
      Array.Copy(CommandTokens.DeviceReset, readConfig, CommandTokens.DeviceReset.Length);
      readConfig[CommandTokens.DeviceReset.Length] = 0x00;
      readConfig[readConfig.Length - 1] = GetCheckSumValue(readConfig);

      //execute the command, get the result
      var status = SetupCommand(readConfig, out result);

      return status;
    }
    #endregion

    /********************************************************************************************************/
    // READER ACTIONS
    /********************************************************************************************************/
    #region -- reader actions --
    public void GetCardData()
    {
      if (!device.IsConnected)
      {
        //DeviceEventArgs args = new DeviceEventArgs();
        //args.payload[0] = "***** REQUEST FAILED: DEVICE IS NOT CONNECTED *****";
        //OnProcessCardDataError(args);
        string [] message = { "" };
        message[0] = "***** REQUEST FAILED: DEVICE IS NOT CONNECTED *****";
        NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_PROCESS_CARDDATA_ERROR, Message = message });
        return;
      }

      int tc_read_msr_timeout = 20000;
      string read_msr_timeout = System.Configuration.ConfigurationManager.AppSettings["tc_read_msr_timeout"] ?? "20000";
      int.TryParse(read_msr_timeout, out tc_read_msr_timeout);
      int msrTimerInterval = tc_read_msr_timeout;

      // Set Read Timeout
      MSRTimer = new System.Timers.Timer(msrTimerInterval);
      MSRTimer.AutoReset = false;
      MSRTimer.Elapsed += (sender, e) => RaiseTimerExpired(new Core.Client.Dal.Models.TimerEventArgs { Timer = TimerType.MSR });
      MSRTimer.Start();

      amount = "1.00";
      
      //if(useUniversalSDK)
      if(deviceInformation.deviceMode == IDTECH_DEVICE_PID.AUGUSTA_HID)
      {
          //RETURN_CODE rt = IDT_Augusta.SharedController.msr_startMSRSwipe(60);
          additionalTags = null;
          RETURN_CODE rt = IDT_Augusta.SharedController.emv_startTransaction(Convert.ToDouble(amount), 0, exponent, 0,30, additionalTags, false);
          if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
          {
              Debug.WriteLine("DeviceCfg::GetCardData(): MSR Turned On successfully; Ready to swipe");
          }
          else
          {
              Debug.WriteLine("DeviceCfg::GetCardData(): start EMV failed Error Code: " + "0x" + String.Format("{0:X}", (ushort)rt));
          }
      }
      else
      {
        // Initialize MSR
        /*Init();

        cardReader.done.Dispose();
        cardReader.done = new EventWaitHandle(false, EventResetMode.AutoReset);
      
        trackData = null;

        // we need to start listening again for more data
        device.ReadReport(OnReport);*/
        Device.CardRead(amount, "HELLO");
      }
    }
/*
    private void ProcessCardData(TrackData cardData)
    {
      string text = "";

        //if (cardData.Event == EVENT_TRANSACTION_DATA_Types.EVENT_TRANSACTION_PIN_DATA)
        //{
        //    Debug.WriteLine("PIN Data received:\r\nKSN: " + cardData.pin_KSN + "\r\nPINBLOCK: " + cardData.pin_pinblock + "\r\nKey Entry: " + cardData.pin_KeyEntry + "\r\n");
        //    return;
        //}

        //if (cardData.Event == EVENT_TRANSACTION_DATA_Types.EVENT_TRANSACTION_DATA_CARD_DATA)
        //{
        //    if (cardData.msr_rawData != null)
        //    {
        //        Debug.WriteLine("Data received: (Length [" + cardData.msr_rawData.Length.ToString() + "])\n" + string.Concat(cardData.msr_rawData.ToArray().Select(b => b.ToString("X2")).ToArray()) + "\r\n");
        //        Debug.WriteLine("Data received: (Length [" + cardData.msr_rawData.Length.ToString() + "])\n" + string.Concat(cardData.msr_rawData.ToArray().Select(b => b.ToString("X2")).ToArray()));
        //    }
        //}

      if (cardData.SerialNumber != null && cardData.SerialNumber.Length > 0)
      {
          text += "Serial Number: " + CardReader.ConvertHexStringToAscii(cardData.SerialNumber) + "\r\n";
      }
        
      if (cardData.Track1.Length > 0)
      {
          text += "Track 1: " + cardData.Track1 + "\r\n";
      }

      if (cardData.T1Crypto != null && cardData.T1Crypto.Length > 0)
      {
          text += "Track 1 Encrypted: " + cardData.T1Crypto + "\r\n";
      }

      if (cardData.T1Hash != null && cardData.T1Hash.Length > 0)
      {
          text += "Track 1 Hash: " + cardData.T1Hash + "\r\n";
      }

      if (cardData.Track2.Length > 0)
      {
        text += "Track 2: " + cardData.Track2 + "\r\n";
      }

      if (cardData.T2Crypto != null && cardData.T2Crypto.Length > 0)
      {
        text += "Track 2 Encrypted: " + cardData.T2Crypto.ToUpper() + "\r\n";
      }

      if (cardData.T2Hash != null && cardData.T2Hash.Length > 0)
      {
        text += "Track 2 Hash: " + cardData.T2Hash + "\r\n";
      }

      if (cardData.Track3.Length > 0)
      {
        text += "Track 3: " + cardData.Track3 + "\r\n";
      }

      if (cardData.T3Crypto != null && cardData.T3Crypto.Length > 0)
      {
        text += "Track 3 Encrypted: " + cardData.T3Crypto + "\r\n";
      }

      if (cardData.T3Hash != null && cardData.T3Hash.Length > 0)
      {
        text += "Track 3 Hash: " + cardData.T3Hash + "\r\n";
      }

      if (cardData.Ksn != null  && cardData.Ksn.Length > 0)
      {
          text += "KSN: " + cardData.Ksn.ToUpper() + "\r\n";
      }

      #region -- msr emv tags --
      //if (cardData.emv_clearingRecord != null)
      //{
      //    if (cardData.emv_clearingRecord.Length > 0)
      //    {
      //        text += "\r\nCTLS Clearing Record: \r\n";
      //        text += Common.getHexStringFromBytes(cardData.emv_clearingRecord) + "\r\n";
      //        Dictionary<string, string> dict = Common.processTLVUnencrypted(cardData.emv_clearingRecord);
      //        foreach (KeyValuePair<string, string> kvp in dict) text += kvp.Key + ": " + kvp.Value + "\r\n";
      //        text += "\r\n\r\n";
      //    }
      //}
      //if (cardData.emv_unencryptedTags != null)
      //{
      //    if (cardData.emv_unencryptedTags.Length > 0)
      //    {
      //        text += "\r\n======================== \r\n";

      //        text += "\r\nUnencrypted Tags: \r\n";
      //        text += Common.getHexStringFromBytes(cardData.emv_unencryptedTags) + "\r\n\r\n";
      //        text += TLV_To_Values(cardData.emv_unencryptedTags);
      //        text += "\r\n======================== \r\n";
      //    }
      //}
      //if (cardData.emv_encryptedTags != null)
      //{
      //    if (cardData.emv_encryptedTags.Length > 0)
      //    {
      //        text += "\r\n======================== \r\n";
      //        text += "\r\nEncrypted Tags: \r\n";
      //        text += Common.getHexStringFromBytes(cardData.emv_encryptedTags) + "\r\n\r\n";
      //        text += TLV_To_Values(cardData.emv_encryptedTags);
      //        text += "\r\n======================== \r\n";
      //    }

      //}
      //if (cardData.emv_maskedTags != null)
      //{
      //    if (cardData.emv_maskedTags.Length > 0)
      //    {
      //        text += "\r\n======================== \r\n";
      //        text += "\r\nMasked Tags: \r\n";
      //        text += Common.getHexStringFromBytes(cardData.emv_maskedTags) + "\r\n\r\n";
      //        text += TLV_To_Values(cardData.emv_maskedTags);
      //        text += "\r\n======================== \r\n";
      //    }
      //}

      //if (cardData.emv_hasAdvise)
      //{
      //  text += "CARD RESPONSE HAS ADVISE" + "\r\n";
      //}

      //if (cardData.emv_hasReversal)
      //{
      //  text += "CARD RESPONSE HAS REFERSAL" + "\r\n";
      //}

      //if (cardData.iccPresent == 1)
      //{
      //  text += "ICC Present: TRUE" + "\r\n";
      //}

      //if (cardData.iccPresent == 2)
      //{
      //  text += "ICC Present: FALSE" + "\r\n";
      //}

      //if (cardData.isCTLS == 1)
      //{
      //  text += "CTLS Capture: TRUE" + "\r\n";
      //}

      //if (cardData.isCTLS == 2)
      //{
      //  text += "CTLS Capture: FALSE" + "\r\n";
      //}

      //if (cardData.msr_extendedField != null && cardData.msr_extendedField.Length > 0)
      //{
      //    text += "Extended Field Bytes: " + Common.getHexStringFromBytes(cardData.msr_extendedField) + "\r\n";
      //}

      //if (cardData.captureEncryptType == CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_TDES)
      //{
      //    text += "Encryption Type: TDES\r\n";
      //}

      //if (cardData.captureEncryptType == CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_AES)
      //{
      //    text += "Encryption Type: AES\r\n";
      //}

      //if (cardData.captureEncryptType == CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_NONE)
      //{
      //    text += "Encryption Type: NONE\r\n";
      //}

      //if (cardData.captureEncryptType != CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_NONE)
      //{
      //  if (cardData.msr_keyVariantType == KEY_VARIANT_TYPE.KEY_VARIANT_TYPE_DATA)
      //  {
      //    text += "Key Type: Data Variant\r\n";
      //  }
      //  else if (cardData.msr_keyVariantType == KEY_VARIANT_TYPE.KEY_VARIANT_TYPE_PIN)
      //  {
      //    text += "Key Type: PIN Variant\r\n";
      //  }
      //}

      //if (cardData.mac != null)
      //{
      //  text += "MAC: " + Common.getHexStringFromBytes(cardData.mac) + "\r\n";
      //}

      //if (cardData.macKSN != null)
      //{
      //  text += "MAC KSN: " + Common.getHexStringFromBytes(cardData.macKSN) + "\r\n";
      //}

      //if (cardData.Event == EVENT_TRANSACTION_DATA_Types.EVENT_TRANSACTION_DATA_EMV_DATA)
      //{
      //    if ((cardData.isCTLS != 1) && (cardData.captureEncryptType != CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_NONE))
      //    {
      //      text += "Capture Encrypt Type: " + ((cardData.captureEncryptType == CAPTURE_ENCRYPT_TYPE.CAPTURE_ENCRYPT_TYPE_TDES) ? "TDES" : "AES") + "\r\n";
      //    }

      //    switch (cardData.emv_resultCode)
      //    {
      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_APPROVED:
      //      {
      //        text += ("RESULT: " + "EMV_RESULT_CODE_APPROVED" + "\r\n");
      //        //Debug.WriteLineLCD("APPROVED");
      //        break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_APPROVED_OFFLINE:
      //      {
      //        //Debug.WriteLineLCD("APPROVED");
      //        text += ("ERESULT: " + "EMV_RESULT_CODE_APPROVED_OFFLINE" + "\r\n");
      //        break; 
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_DECLINED_OFFLINE:
      //      {
      //        text += ("RESULT: " + "EMV_RESULT_CODE_DECLINED_OFFLINE" + "\r\n");
      //        break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_DECLINED:
      //      {
      //        text += ("RESULT: " + "EMV_RESULT_CODE_DECLINED" + "\r\n");
      //        break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_GO_ONLINE:
      //      {
      //        text += ("RESULT: " + "EMV_RESULT_CODE_GO_ONLINE" + "\r\n");
      //        break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_CALL_YOUR_BANK:
      //      {
      //        //SetOutputTextLCD("CALL YOUR BANK");
      //        text += ("RESULT: " + "EMV_RESULT_CODE_CALL_YOUR_BANK" + "\r\n");
      //        break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_NOT_ACCEPTED:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_NOT_ACCEPTED" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_FALLBACK_TO_MSR:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_FALLBACK_TO_MSR" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_TIMEOUT:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_TIMEOUT" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_AUTHENTICATE_TRANSACTION:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_AUTHENTICATE_TRANSACTION" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_SWIPE_NON_ICC:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_SWIPE_NON_ICC" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_CTLS_TWO_CARDS:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_CTLS_TWO_CARDS" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_CTLS_TERMINATE:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_CTLS_TERMINATE" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_CTLS_TERMINATE_TRY_ANOTHER:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_CTLS_TERMINATE_TRY_ANOTHER" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_GO_ONLINE_CTLS:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_GO_ONLINE_CTLS" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_MSR_SWIPE_CAPTURED:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_MSR_SWIPE_CAPTURED" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_REQUEST_ONLINE_PIN:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_REQUEST_ONLINE_PIN" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_REQUEST_SIGNATURE:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_REQUEST_SIGNATURE" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_ADVISE_REQUIRED:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_ADVISE_REQUIRED" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_REVERSAL_REQUIRED:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_REVERSAL_REQUIRED" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_ADVISE_REVERSAL_REQUIRED:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_ADVISE_REVERSAL_REQUIRED" + "\r\n");
      //          break;
      //      }

      //      case EMV_RESULT_CODE.EMV_RESULT_CODE_NO_ADVISE_REVERSAL_REQUIRED:
      //      {
      //          text += ("RESULT: " + "EMV_RESULT_CODE_NO_ADVISE_REVERSAL_REQUIRED" + "\r\n");
      //          break;
      //      }

      //      default:
      //      {
      //        string val = errorCode.getErrorString((RETURN_CODE)cardData.emv_resultCode);
      //        if (val == null || val.Length == 0) val = "EMV_ERROR_ENCOUNTERED";
      //        text += ("RESULT: " + val + "\r\n");
      //        break;
      //      }
      //    }
      //}
      //else
      //{
      //    text += ("RESULT: " + "TRANSACTION OVER" + "\r\n");
      //}

      //if (cardData.emv_transaction_Error_Code > 0)
      //{
      //  text += ("Transaction Error: " + errorCode.getTransError(cardData.emv_transaction_Error_Code) + "\r\n");
      //}

      //if (cardData.emv_RF_State > 0)
      //{
      //  text += ("RF State: " + errorCode.getRFState(cardData.emv_RF_State) + "\r\n");
      //}

      //if (cardData.emv_ESC > 0)
      //{
      //  text += ("Extended Status Code: " + errorCode.getExtendedStatusCode(cardData.emv_ESC) + "\r\n");
      //}

      //if (cardData.emv_appErrorFn > 0)
      //{
      //  text += ("App Error Function: " + errorCode.getEMVAppErrorFn(cardData.emv_appErrorFn) + "\r\n");
      //}

      //if (cardData.emv_appErrorState > 0)
      //{
      //  text += ("App Error State: " + errorCode.getEMVAppErrorState(cardData.emv_appErrorState) + "\r\n");
      //}

      //if (cardData.ctlsApplication > 0)
      //{
      //    text += "Contactless Application: ";

      //    switch (cardData.ctlsApplication)
      //    {
      //      case CTLS_APPLICATION.CTLS_APPLICATION_AMEX:
      //      {
      //          text += ("CTLS_APPLICATION_AMEX" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_DISCOVER:
      //      {
      //          text += ("CTLS_APPLICATION_DISCOVER" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_MASTERCARD:
      //      {
      //          text += ("CTLS_APPLICATION_MASTERCARD" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_VISA:
      //      {
      //          text += ("CTLS_APPLICATION_VISA" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_SPEEDPASS:
      //      {
      //          text += ("CTLS_APPLICATION_SPEEDPASS" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_GIFT_CARD:
      //      {
      //          text += ("CTLS_APPLICATION_GIFT_CARD" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_DINERS_CLUB:
      //      {
      //          text += ("CTLS_APPLICATION_DINERS_CLUB" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_EN_ROUTE:
      //      {
      //          text += ("CTLS_APPLICATION_EN_ROUTE" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_JCB:
      //      {
      //          text += ("CTLS_APPLICATION_JCB" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_VIVO_DIAGNOSTIC:
      //      {
      //          text += ("CTLS_APPLICATION_VIVO_DIAGNOSTIC" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_HID:
      //      {
      //          text += ("CTLS_APPLICATION_HID" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_MSR_SWIPE:
      //      {
      //          text += ("CTLS_APPLICATION_MSR_SWIPE" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_RESERVED:
      //      {
      //          text += ("CTLS_APPLICATION_RESERVED" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_DES_FIRE_TRACK_DATA:
      //      {
      //          text += ("CTLS_APPLICATION_DES_FIRE_TRACK_DATA" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_DES_FIRE_RAW_DATA:
      //      {
      //          text += ("CTLS_APPLICATION_DES_FIRE_RAW_DATA" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_RBS:
      //      {
      //          text += ("CTLS_APPLICATION_RBS" + "\r\n");
      //          break;
      //      }

      //      case CTLS_APPLICATION.CTLS_APPLICATION_VIVO_COMM:
      //      {
      //          text += ("CTLS_APPLICATION_VIVO_COMM" + "\r\n");
      //          break;
      //      }
      //    }
      //}
      #endregion

      Debug.WriteLine(text);

      // Process Card Data
      DeviceEventArgs args = new DeviceEventArgs();
      args.payload[0] = text;
      OnProcessCardData(args);

      trackData = null;
    }
*/
    private void RaiseTimerExpired(Core.Client.Dal.Models.TimerEventArgs e)
    {
      //MSRTimer?.Invoke(null, e);
      MSRTimer?.Stop();

      if(useUniversalSDK)
      {
          RETURN_CODE rt = IDT_Augusta.SharedController.msr_cancelMSRSwipe();
          if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
          {
              Debug.WriteLine("DeviceCfg: MSR Turned Off successfully.");
          }
      }

      // Allow for GUI Recovery
      //DeviceEventArgs args = new DeviceEventArgs();
      //args.payload[0] = "***** TRANSACTION FAILED: TIMEOUT *****";
      //OnProcessCardDataError(args);
      string [] message = { "***** TRANSACTION FAILED: TIMEOUT *****" };
      NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_PROCESS_CARDDATA_ERROR, Message = message });
    }

    #endregion

    /********************************************************************************************************/
    // SETTINGS ACTIONS
    /********************************************************************************************************/
    #region -- settings actions --
    public void GetDeviceConfiguration()
    {
///      if (!device.IsConnected)
      {
        //DeviceEventArgs args = new DeviceEventArgs();
        //args.payload[0] = "***** REQUEST FAILED: DEVICE IS NOT CONNECTED *****";
        //OnProcessCardDataError(args);
        string [] message = { "***** REQUEST FAILED: DEVICE IS NOT CONNECTED *****" };
        NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_PROCESS_CARDDATA_ERROR, Message = message });
        return;
      }

      if(useUniversalSDK)
      {
          // EXPIRATION MASK
          string expMask = GetExpirationMask();
        
          // PAN DIGITS
          string panDigits = GetClearPANDigits();

          // SWIPE FORCE
          string swipeForce = GetSwipeForceEncryption();

          // SWIPE MASK
          string swipeMask = GetSwipeMaskOption();

          // MSR Setting
          string msrSetting = "WIP";

          // Set Configuration
          //DeviceEventArgs args = new DeviceEventArgs();
          //args.payload[0] = expMask;
          //args.payload[1] = panDigits;
          //args.payload[2] = swipeForce;
          //args.payload[3] = swipeMask;
          //args.payload[4] = msrSetting;
          //OnGetDeviceConfiguration(args);
          string [] message = { "" };
          message[0] = expMask;
          message[1] = panDigits;
          message[2] = swipeForce;
          message[3] = swipeMask;
          message[4] = msrSetting;
          NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_GET_DEVICE_CONFIGURATION, Message = message });
      }
      else
      {
        //TO-DO
      }
    }

    #endregion

    /********************************************************************************************************/
    // CONFIGURATION ACTIONS
    /********************************************************************************************************/
    #region -- configuration actions --

    public void SetDeviceConfiguration(object payload)
    {
      try
      {
        Array argArray = new object[4];
        argArray = (Array) payload;

        // EXPIRATION MASK
        object paramset1 = (object) argArray.GetValue(0);
 
        // PAN DIGITS
        object paramset2 = (object) argArray.GetValue(1);

        // SWIPE FORCE
        object paramset3 = (object) argArray.GetValue(2);

        // SWIPE MASK
        object paramset4 = (object) argArray.GetValue(3);

        // DEBUG
        List<MsrConfigItem> item = (List<MsrConfigItem>) paramset3;

        foreach (MsrConfigItem child in item)  
        {  
          Debug.WriteLine("configuration: {0}={1}", child.Name, child.Value);  
        }  

        Debug.WriteLine("main: SetDeviceConfiguration() - track1={0}", (object) item.ElementAt(0).Value);

        if(useUniversalSDK)
        {
            // EXPIRATION MASK
            string expMask = SetExpirationMask(paramset1);

            // PAN DIGITS
            string panDigits = SetClearPANDigits(paramset2);

            // SWIPE FORCE
            object swipeForceEncrypt = SetForceSwipeEncryption(paramset3);
 
            // SWIPE MASK
            string swipeMask = SetSwipeMaskOption(paramset4);

            // Setup Response
            //DeviceEventArgs args = new DeviceEventArgs();
            //args.payload[0] = expMask;
            //args.payload[1] = panDigits;
            //args.payload[2] = swipeForceEncrypt;
            //args.payload[3] = swipeMask;
            //OnSetDeviceConfiguration(args);
            object [] message = { "" };
            message[0] = expMask;
            message[1] = panDigits;
            message[2] = swipeForceEncrypt;
            message[3] = swipeMask;
            NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_SET_DEVICE_CONFIGURATION, Message = message });
        }
        else
        {
          //TO-DO
        }
      }
      catch(Exception exp)
      {
         Debug.WriteLine("DeviceCfg::SetDeviceConfiguration(): - exception={0}", (object)exp.Message);
      }
    }

    public void SetDeviceMode(string mode)
    {
        try
        {
            if(mode.Equals("USB-HID"))
            {
               if(deviceInformation.deviceMode == IDTECH_DEVICE_PID.AUGUSTA_KYB)
               {
                    EntryModeStatus status = SetDeviceHidMode();
                    if(status == EntryModeStatus.Success)
                    {
                        DeviceSoftReset();
                    }
               }
            }
            else if(mode.Equals("USB-KB"))
            {
               if(deviceInformation.deviceMode == IDTECH_DEVICE_PID.AUGUSTA_HID)
               {
                    // TURN ON QUICK CHIP MODE
                    string command = "72 53 01 29 01 31";
                    DeviceCommand(command);
                    // Set Device to KB MODE
                    RETURN_CODE rt = IDT_Augusta.SharedController.msr_switchUSBInterfaceMode(true);

                    // code won't be reached: above function reboot device
                    //if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    //{
                    //    IDT_Augusta.SharedController.device_rebootDevice();
                    //}
               }
            }
        }
        catch(Exception exp)
        {
           Debug.WriteLine("DeviceCfg::SetDeviceMode(): - exception={0}", (object)exp.Message);
        }
    }

    #endregion

    /********************************************************************************************************/
    // DEVICE ACTIONS
    /********************************************************************************************************/
    #region -- device actions --
    public void DeviceCommand(string command)
    {
        if(useUniversalSDK)
        {
//            DeviceEventArgs args = new DeviceEventArgs();
            string [] message = { "" };
            byte[] response = null;
            RETURN_CODE rt = IDT_Augusta.SharedController.device_sendDataCommand(command, true, ref response);
            if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                message[0] = BitConverter.ToString(response).Replace("-", string.Empty);
            }
            else
            {
                if(response != null)
                {
                    message[0] = "COMMAND EXECUTE FAILED - CODE=" + BitConverter.ToString(response).Replace("-", string.Empty);
                }
                else
                {
                    message[0] = "COMMAND EXECUTE FAILED - CODE=0x" + string.Format("{0:X}", rt);
                }
            }

            //OnSetExecuteResult(args);
            NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_SET_EXECUTE_RESULT, Message = message });
         }
        else
        {

        }
    }

    public string GetErrorMessage(string data)
    {
        string message = data;

        if(data.Contains("DFEF61"))
        {
            if(data.Contains("F220"))
            {
                message = "*** TRANSACTION ERROR *** : Insert ICC again / Swipe";
            }
            else if(data.Contains("F221"))
            {
                message = "*** TRANSACTION ERROR *** : Prompt Fallback";
            }
            if(data.Contains("F222"))
            {
                message = "*** TRANSACTION ERROR *** : SWIPE CARD - NO EMV";
            }
        }
        else if(data.StartsWith("9F39"))
        {
            message = "*** TRANSACTION DATA PROCESSED : MSR ***";
        }

        return message;
    }
    #endregion
  }

  #region -- main interface --
 internal class DeviceInformation
 {
    internal string SerialNumber;
    internal string FirmwareVersion;
    internal string ModelName;
    internal string ModelNumber;
    internal string Port;
    internal byte[] ConfigValues;
    internal IDTECH_DEVICE_PID deviceMode;
 }
 #endregion
}
