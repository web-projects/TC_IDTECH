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
using IPA.CommonInterface.Interfaces;
using IPA.CommonInterface.Helpers;
using IPA.CommonInterface.ConfigIDTech;
using IPA.CommonInterface.ConfigIDTech.Configuration;
using IPA.DAL;
using IPA.DAL.RBADAL.Services.Devices.IDTech;
using IPA.Core.Shared.Helpers.StatusCode;
using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Services;
using IPA.DAL.RBADAL.Services.Devices.IDTech.Models;
using IPA.LoggerManager;

namespace IPA.DAL.RBADAL
{
  [Serializable]
  public class DeviceCfg : MarshalByRefObject, IDevicePlugIn
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
    public event EventHandler<DeviceNotificationEventArgs> OnDeviceNotification;

    private bool useUniversalSDK;
    private bool attached;
    private bool connected;
    private bool formClosing;
    private bool firmwareUpdate;

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

    public DeviceCfg()
    {
      deviceType = IDT_DEVICE_Types.IDT_DEVICE_NONE;
///      cardReader = new CardReader();
    }

    public void DeviceInit()
    {
      DevicePluginName = "DeviceCfg";

      // Create Device info object
      deviceInformation = new DeviceInformation();
      deviceInformation.emvConfigSupported = false;

      // Device Discovery
      //useUniversalSDK = DeviceDiscovery();
      connected = false;

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

            // Using the device notifier to detect device removed event
            device.Removed += DeviceRemovedHandler;
            Device.OnNotification += OnNotification;

            Device.Init(SerialPortService.GetAvailablePorts(), ref deviceInformation.deviceMode);

            // Notify Main Form
            SetDeviceMode(deviceInformation.deviceMode);

            // connect to device
            Device.Connect();

            // Set as Attached
            attached = true;

            if(!useUniversalSDK)
            {
                Debug.WriteLine("device information ----------------------------------------------------------------");
                DeviceInfo di = Device.GetDeviceInfo();
                deviceInformation.SerialNumber = di.SerialNumber;
                Debug.WriteLine("device INFO[Serial Number]   : {0}", (object) deviceInformation.SerialNumber);
                deviceInformation.FirmwareVersion = di.FirmwareVersion;
                Debug.WriteLine("device INFO[Firmware Version]: {0}", (object) deviceInformation.FirmwareVersion);
                deviceInformation.ModelNumber = di.ModelNumber;
                Debug.WriteLine("device INFO[Model Number]    : {0}", (object) deviceInformation.ModelNumber);
                deviceInformation.ModelName = di.ModelName;
                Debug.WriteLine("device INFO[Model Name]      : {0}", (object) deviceInformation.ModelName);
                deviceInformation.Port = di.Port;
                Debug.WriteLine("device INFO[Port]            : {0}", (object) deviceInformation.Port);

                // Update Device Configuration
                string [] message = { "COMPLETED" };
                NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_DEVICE_UPDATE_CONFIG, Message = message });
            }

            // DEVICES with USDK Support
            if(useUniversalSDK)
            {
                // Initialize Universal SDK
                IDT_Device.setCallback(MessageCallBack);
                IDT_Device.startUSBMonitoring();
                Logger.debug("DeviceCfg::DeviceInit(): - device TYPE={0}", IDT_Device.getDeviceType());

                string [] message = { "COMPLETED" };
                NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_INITIALIZE_DEVICE, Message = message });
            }
            else if(deviceInformation.deviceMode == IDTECH_DEVICE_PID.VP3000_KYB)
            {
                deviceType = IDT_DEVICE_Types.IDT_DEVICE_VP3300;
                SetDeviceConfig();
                string [] message = { "COMPLETED" };
                NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_INITIALIZE_DEVICE, Message = message });
            }
          }
          else
          {
            throw new Exception("NoDevice");
          }
      }
      catch (Exception xcp)
      {
          throw xcp;
      }
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
      string [] config = { deviceInformation.SerialNumber,
                           deviceInformation.FirmwareVersion,
                           deviceInformation.ModelName,
                           deviceInformation.ModelNumber,
                           deviceInformation.Port
      };

      return config;
    }

    public void SetFormClosing(bool state)
    {
      formClosing = state;
      if(formClosing)
      {
            if(deviceInformation.emvConfigSupported)
            {
                Debug.WriteLine("DeviceCfg::DISCONNECTING FOR device TYPE={0}", IDT_Device.getDeviceType());
                Device.CloseDevice();
            }
      }
    }
    
    protected void OnNotification(object sender, Models.NotificationEventArgs args)
    {
        Debug.WriteLine("device: notification type={0}", args.NotificationType);

        switch (args.NotificationType)
        {
            case NotificationType.DeviceEvent:
            {
                switch(args.DeviceEvent)
                {
                    case DeviceEvent.DeviceDisconnected:
                    {
                        DeviceRemovedHandler();
                        break;
                    }
                    case DeviceEvent.CardReadComplete:
                    {
                        Core.Data.Entity.Other.CreditCard cc = Transaction.PaymentXO.Request.CreditCard;
                        string last4 = cc.EncryptedTracks;
                        NotificationRaise(
                            new DeviceNotificationEventArgs()
                            {
                                NotificationType = NOTIFICATION_TYPE.NT_PROCESS_CARDDATA,
                                Message = new object[] { "Card Read " + last4 },
                            }
                        );
                        break;
                    }
                }
                break;
            }
        }
    }

    public void NotificationRaise(DeviceNotificationEventArgs e)
    {
        OnDeviceNotification?.Invoke(null, e);
    }

    #endregion

    /********************************************************************************************************/
    // DISCOVERY
    /********************************************************************************************************/
    #region -- device discovery ---
    /*private bool DeviceDiscovery()
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

                      object [] message = { "" };

                      switch(pidId)
                      {
                        case (int) IDTECH_DEVICE_PID.AUGUSTA_KYB:
                        {
                          useUniversalSDK = true;
                          deviceInformation.deviceMode = IDTECH_DEVICE_PID.AUGUSTA_KYB;
                          message[0] = USK_DEVICE_MODE.USB_HID;
                          break;
                        }

                        case (int) IDTECH_DEVICE_PID.AUGUSTA_HID:
                        {
                          useUniversalSDK = true;
                          deviceInformation.deviceMode = IDTECH_DEVICE_PID.AUGUSTA_HID;
                          message[0] = USK_DEVICE_MODE.USB_KYB;
                          break;
                        }

                        default:
                        {
                            message[0] = USK_DEVICE_MODE.UNKNOWN;
                            break;
                        }
                      }

                      // Notify Main Form
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
    }*/

    void SetDeviceMode(IDTECH_DEVICE_PID mode)
    {
        object [] message = { "" };

        switch(mode)
        {
            case IDTECH_DEVICE_PID.VP3000_HID:
            case IDTECH_DEVICE_PID.VP5300_HID:
            case IDTECH_DEVICE_PID.AUGUSTA_HID:
            case IDTECH_DEVICE_PID.AUGUSTAS_HID:
            {
                useUniversalSDK = true;
                deviceInformation.deviceMode = mode;
                message[0] = USK_DEVICE_MODE.USB_KYB;
                break;
            }

            case IDTECH_DEVICE_PID.MAGSTRIPE_HID:
            case IDTECH_DEVICE_PID.SECUREKEY_HID:
            case IDTECH_DEVICE_PID.SECUREMAG_HID:
            {
                useUniversalSDK = false;
                deviceInformation.deviceMode = mode;
                message[0] = "OLDIDTECH:" + USK_DEVICE_MODE.USB_KYB;
                break;
            }

            case IDTECH_DEVICE_PID.MAGSTRIPE_KYB:
            case IDTECH_DEVICE_PID.SECUREKEY_KYB:
            case IDTECH_DEVICE_PID.SECUREMAG_KYB:
            {
                useUniversalSDK = false;
                deviceInformation.deviceMode = mode;
                message[0] = "OLDIDTECH:" + USK_DEVICE_MODE.USB_HID;
                break;
            }

            case IDTECH_DEVICE_PID.VP3000_KYB:
            case IDTECH_DEVICE_PID.VP5300_KYB:
            case IDTECH_DEVICE_PID.AUGUSTA_KYB:
            case IDTECH_DEVICE_PID.AUGUSTAS_KYB:
            {
                useUniversalSDK = true;
                deviceInformation.deviceMode = mode;
                message[0] = USK_DEVICE_MODE.USB_HID;
                break;
            }

            default:
            {
                message[0] = USK_DEVICE_MODE.UNKNOWN;
                break;
            }
        }

        // Notify Main Form
        NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_SET_DEVICE_MODE, Message = message });
    }

    #endregion

    /********************************************************************************************************/
    // DEVICE EVENTS INTERFACE
    /********************************************************************************************************/
    #region -- device event interface ---

    private void DeviceRemovedHandler()
    {
      Debug.WriteLine("\ndevice: removed !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n");

      connected = false;

      if(attached)
      {
          attached = false;
          
          // Last device was USDK Type
          if(useUniversalSDK)
          {
              IDT_Device.stopUSBMonitoring();
          }

          // Unload Device Domain
          NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_UNLOAD_DEVICE_CONFIGDOMAIN });
      }
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

    private void SetDeviceConfig()
    {
      if(IDT_DEVICE_Types.IDT_DEVICE_NONE != deviceType)
      {
           object [] settings = { deviceType, deviceConnect };

           Device.Configure(settings);

           // Create Device info object
           if(deviceInformation == null)
           {
              deviceInformation = new DeviceInformation();
           }

           DeviceInfo devInfo = Device.GetDeviceInfo();

           if(devInfo != null)
           {
              deviceInformation.SerialNumber    = devInfo.SerialNumber;
              deviceInformation.FirmwareVersion = devInfo.FirmwareVersion;
              deviceInformation.ModelName       = devInfo.ModelName;
              deviceInformation.ModelNumber     = devInfo.ModelNumber;
              deviceInformation.Port            = devInfo.Port;
           }

           // Update Device Configuration
           object [] message = { "COMPLETED" };
           NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_DEVICE_UPDATE_CONFIG, Message = message });
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
          Debug.WriteLine("DeviceCfg::Callback: device connecting: {0}", (object) deviceType.ToString());
          break;
        }

        case DeviceState.Connected:
        {
          deviceType = type;
          string deviceName = IDTechSDK.Profile.IDT_DEVICE_String(type, deviceConnect);
          Debug.WriteLine("DeviceCfg::Callback: device connected: {0}", (object) deviceName);

          if(!connected && !firmwareUpdate)
          {
              // Populate Device Configuration
              new Thread(() =>
              {
                 Thread.CurrentThread.IsBackground = false;

                 // Disable EMV QC
                 if(deviceName.Contains("HID"))
                 {
                    SetEmvQCMode(false);
                 }

                 // Get Device Configuration
                 SetDeviceConfig();

                 Thread.Sleep(100);
                 connected = true;

                 // Get Device Information
                 GetDeviceInformation();

              }).Start();
          }

          break;
        }

        case DeviceState.Disconnected:
        {
            if(!firmwareUpdate)
            {
                connected = false;
                DeviceRemovedHandler();
            }
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
                Debug.WriteLine("DeviceCfg::Callback: Notification: ICC Card not Seated\n");
            }
            if (cardData.Notification == EVENT_NOTIFICATION_Types.EVENT_NOTIFICATION_Card_Seated)
            {
                Debug.WriteLine("DeviceCfg::Callback: Notification: ICC Card Seated\n");
            }
            if (cardData.Notification == EVENT_NOTIFICATION_Types.EVENT_NOTIFICATION_Swipe_Card)
            {
                Debug.WriteLine("DeviceCfg::Callback: Notification: MSR Swipe Card\n");
            }

            break;
        }

        case DeviceState.EMVCallback:
        {
            if (emvCallback == null) 
            {
                break;
            }

            ProcessEMVCallback(emvCallback);

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
                      Debug.WriteLine("Get MSR Complete!");
                  }
              }

              ClearCallbackData(ref data, ref cardData);

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
            Debug.WriteLine("device: TRANSACTION EMV DATA return code={0}", (object) transactionResultCode.ToString());

            // Data Received Processing
            ProcessCardData(cardData);
          }

          break;
        }

        case DeviceState.DataReceived: 
        {
          Debug.WriteLine("DeviceCfg::Callback: {0} IN : {1}", Utils.GetTimeStamp(), Common.getHexStringFromBytes(data));
          break;
        }

        case DeviceState.DataSent:
        {
          Debug.WriteLine("DeviceCfg::Callback: {0} OUT: {1}", Utils.GetTimeStamp(), Common.getHexStringFromBytes(data));
          break;
        }

        case DeviceState.CommandTimeout:
        {
          Debug.WriteLine("DeviceCfg::Callback: Command Timeout\n");
          break;
        }

        case DeviceState.CardAction:
        {
          if (data != null & data.Length > 0)
          {
              CARD_ACTION action = (CARD_ACTION)data[0];
              StringBuilder sb = new StringBuilder("device: Card Action Request=");

              if ((action & CARD_ACTION.CARD_ACTION_INSERT) == CARD_ACTION.CARD_ACTION_INSERT)
              {
                 sb.Append("INSERT");
              }

              if ((action & CARD_ACTION.CARD_ACTION_REINSERT) == CARD_ACTION.CARD_ACTION_REINSERT)
              {
                 sb.Append("REINSERT");
              }

              if ((action & CARD_ACTION.CARD_ACTION_REMOVE) == CARD_ACTION.CARD_ACTION_REMOVE)
              {  
                 sb.Append("REMOVE");
              }

              if ((action & CARD_ACTION.CARD_ACTION_SWIPE) == CARD_ACTION.CARD_ACTION_SWIPE)
              {  
                sb.Append("SWIPE");
              }

              if ((action & CARD_ACTION.CARD_ACTION_SWIPE_AGAIN) == CARD_ACTION.CARD_ACTION_SWIPE_AGAIN)
              {  
                sb.Append("SWIPE_AGAIN");
              }

              if ((action & CARD_ACTION.CARD_ACTION_TAP) == CARD_ACTION.CARD_ACTION_TAP)
              {  
                sb.Append("TAP");
              }

              if ((action & CARD_ACTION.CARD_ACTION_TAP_AGAIN) == CARD_ACTION.CARD_ACTION_TAP_AGAIN)
              {  
                sb.Append("TAP_AGAIN");
              }

              Debug.WriteLine(sb.ToString());
          }

          break;
        }

        case DeviceState.MSRDecodeError:
        {
          //SoftwareController.MSR_LED_RED_SOLID();
          Debug.WriteLine("DeviceCfg::Callback: MSR Decode Error\n");
          break;
        }

        case DeviceState.SwipeTimeout:
        {
          Debug.WriteLine("DeviceCfg::Callback: Swipe Timeout\n");
          break;
        }

        case DeviceState.TransactionCancelled:
        {
          Debug.WriteLine("DeviceCfg::Callback: TransactionCancelled.");
          //Debug.WriteLine("");
          //Debug.WriteLine(DeviceTerminalInfo.getDisplayMessage(DeviceTerminalInfo.MSC_ID_WELCOME));
          break;
        }

        case DeviceState.DeviceTimeout:
        {
          Debug.WriteLine("DeviceCfg::Callback: Device Timeout\n");
          break;
        }

        case DeviceState.TransactionFailed:
        {
          if ((int)transactionResultCode == 0x8300)
          {
            //SoftwareController.MSR_LED_RED_SOLID();
          }

          string text =  IDTechSDK.errorCode.getErrorString(transactionResultCode);
          Debug.WriteLine("DeviceCfg::Callback: Transaction Failed: {0}\r\n", (object) text);

          // Allow for GUI Recovery
          string [] message = { "" };
          message[0] = "***** TRANSACTION FAILED: " + text + " *****";
          NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_PROCESS_CARDDATA_ERROR, Message = message });
          break;
        }

        case DeviceState.FirmwareUpdate:
        {
            switch (transactionResultCode)
            {
                case RETURN_CODE.RETURN_CODE_FW_STARTING_UPDATE:
                {
                    Logger.debug("device: starting Firmware Update");
                    break;
                }
                case RETURN_CODE.RETURN_CODE_DO_SUCCESS:
                {
                    Logger.debug("device: firmware Update Successful");
                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = false;
                        firmwareUpdate = false;

                        // Get Device Configuration
                        SetDeviceFirmwareVersion();

                    }).Start();

                    break;
                }
                case RETURN_CODE.RETURN_CODE_APPLYING_FIRMWARE_UPDATE:
                {
                    Logger.debug("device: applying Firmware Update....");
                    break;
                }
                case RETURN_CODE.RETURN_CODE_ENTERING_BOOTLOADER_MODE:
                {
                    Logger.debug("device: entering Bootloader Mode....");
                    break;
                }
                case RETURN_CODE.RETURN_CODE_BLOCK_TRANSFER_SUCCESS:
                {
                    int start = data[0];
                    int end = data[1];
                    if (data.Length == 4)
                    {
                        start = data[0] * 0x100 + data[1];
                        end = data[2] * 0x100 + data[3];
                    }
                    Logger.debug("device: sent block {0} of {1}", start.ToString(), end.ToString());
                    break;
                }
                default:
                {
                    Logger.debug("device: firmware Update Error Code: 0x{0:X} : {1}", (ushort)transactionResultCode, IDTechSDK.errorCode.getErrorString(transactionResultCode));
                    break;
                }
            }
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

    private void ProcessEMVCallback(EMV_Callback emvCallback)
    {
        if (emvCallback != null) 
        {
            Debug.WriteLine("ProcessEMVCallback: LCD Display ={0}", (object)Common.getHexStringFromBytes(emvCallback.lcd_messages));
        }
        if (emvCallback.lcd_displayMode != EMV_LCD_DISPLAY_MODE.EMV_LCD_DISPLAY_MODE_CANCEL)
        {
            //IDTechSoftwareDevice.UpdateEMVResponse(emvCallback);
        }
        if (emvCallback.callbackType == EMV_CALLBACK_TYPE.EMV_CALLBACK_TYPE_LCD) //LCD Callback Type
        {
            if (emvCallback.lcd_displayMode == EMV_LCD_DISPLAY_MODE.EMV_LCD_DISPLAY_MODE_CLEAR_SCREEN)
            {
                return;
            }
            else if (emvCallback.lcd_displayMode == EMV_LCD_DISPLAY_MODE.EMV_LCD_DISPLAY_MODE_MESSAGE)
            {
            }
            else
            {
                //Display message with menu/language or prompt, and return result to emv_callbackResponseLCD
                //Kernel will not proceed until this step is complete
            }

        }
        if (emvCallback.callbackType == EMV_CALLBACK_TYPE.EMV_CALLBACK_TYPE_PINPAD) //PIN Callback Type
        {
            //Provide PIN information to emv_callbackResponsePIN
            //Kernel will not proceed until this step is complete
        }
        if (emvCallback.callbackType == EMV_CALLBACK_TYPE.EMV_CALLBACK_MSR) //MSR Callback Type
        {
            //Provide MSR Swipe information to emv_callbackResponseMSR
            //Kernel will not proceed until this step is complete
        }
    }

    private void ProcessMSRData(IDTTransactionData cardData, ref string text)
    {
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
    }
    
    private void ProcessEMVData(IDTTransactionData cardData, ref string text, ref string emvData)
    {
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
                text += "Unencrypted Tags: \r\n";
                text += Common.getHexStringFromBytes(cardData.emv_unencryptedTags) + "\r\n\r\n";
                text += TLV_To_Values(cardData.emv_unencryptedTags);
                emvData = Common.getHexStringFromBytes(cardData.emv_unencryptedTags);
            }
        }

        if (cardData.emv_encryptedTags != null)
        {
            if (cardData.emv_encryptedTags.Length > 0)
            {
                text += "\r\nEncrypted Tags: \r\n";
                text += Common.getHexStringFromBytes(cardData.emv_encryptedTags) + "\r\n\r\n";
                text += TLV_To_Values(cardData.emv_encryptedTags);
            }

        }

        if (cardData.emv_maskedTags != null)
        {
            if (cardData.emv_maskedTags.Length > 0)
            {
                text += "\r\nMasked Tags: \r\n";
                text += Common.getHexStringFromBytes(cardData.emv_maskedTags) + "\r\n\r\n";
                text += TLV_To_Values(cardData.emv_maskedTags);
                emvData += Common.getHexStringFromBytes(cardData.emv_maskedTags);
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
    }

    private string RetrieveAdditionalTags(string tags)
    {
        String text = "";
        IDTTransactionData cardData = null;
        RETURN_CODE rt = IDT_Augusta.SharedController.emv_retrieveTransactionResult(Common.getByteArray(tags), ref cardData);
        if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && cardData != null)
        {
            if (cardData.emv_unencryptedTags != null)
            {
                if (cardData.emv_unencryptedTags.Length > 0)
                {
                    text = Common.getHexStringFromBytes(cardData.emv_unencryptedTags);
                }

            }
            if (cardData.emv_encryptedTags != null)
            {
                if (cardData.emv_encryptedTags.Length > 0)
                {
                    text += Common.getHexStringFromBytes(cardData.emv_encryptedTags);
                }
            }
            if (cardData.emv_maskedTags != null)
            {
                if (cardData.emv_maskedTags.Length > 0)
                {
                    text += Common.getHexStringFromBytes(cardData.emv_maskedTags);
                }
            }
        }
        else
        {
            Debug.WriteLine("device: retrieve Results failed Error Code: {0:X}", (ushort) rt);
        }

        return text;
    }

    private void ProcessCTLSData(IDTTransactionData cardData, ref string text)
    {
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
    }

    private void ProcessCardData(IDTTransactionData cardData)
    {
        // Stop Timer
        MSRTimer?.Stop();

        if (cardData.Event == EVENT_TRANSACTION_DATA_Types.EVENT_TRANSACTION_PIN_DATA)
        {
            Debug.WriteLine("PIN Data received:\r\nKSN: " + cardData.pin_KSN + "\r\nPINBLOCK: " + cardData.pin_pinblock + "\r\nKey Entry: " + cardData.pin_KeyEntry + "\r\n");
            return;
        }

        string text = "";
        string emvData = "";

        // MSR Payload
        ProcessMSRData(cardData, ref text);

        // EMV Payload
        ProcessEMVData(cardData, ref text, ref emvData);

        // CTLS Payload
        ProcessCTLSData(cardData, ref text);

        // Process Card Data
        Debug.WriteLine("device: PROCESS CARD DATA -> CODE={0} - card data=[{1}]", cardData.emv_resultCode, emvData);

        bool updateView = (cardData.emv_resultCode == EMV_RESULT_CODE.EMV_RESULT_CODE_AUTHENTICATE_TRANSACTION) ? true : false;
        bool isHIDMode = (IDT_Device.getProtocolType() != DEVICE_PROTOCOL_Types.DEVICE_PROTOCOL_KB);
        object [] message = { "*** TRANSACTION DATA CAPTURED : EMV ***", emvData, (short) cardData.emv_resultCode, isHIDMode, updateView };
        NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_PROCESS_CARDDATA, Message = message });

        byte[] temp = new byte[0];
        ClearCallbackData(ref temp, ref cardData);
    }

    private RETURN_CODE EMVAuthenticateTransaction(byte [] additionalTags)
    {
        RETURN_CODE rt = IDT_Augusta.SharedController.emv_authenticateTransaction(additionalTags);
        if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
        {
            Debug.WriteLine("device: Authenticate EMV Successful");
        }
        else
        {
            Debug.WriteLine("device: Authenticate EMV failed Error Code: {0:X}", (ushort) rt);
        }

        return rt;
    }

    private RETURN_CODE EMVCompleteTransaction(byte [] additionalTags)
    {
        byte[] iad = null;
        byte[] responseCode = new byte[] { 0x30, 0x30 };
        RETURN_CODE rt = IDT_Augusta.SharedController.emv_completeTransaction(false, responseCode, iad, null, additionalTags);
        if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
        {
            Debug.WriteLine("device: Complete EMV Command Accepted");
        }
        else
        {
            Debug.WriteLine("device: Complete EMV failed Error Code: {0:X}", (ushort) rt);
        }

        return rt;
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
          Debug.WriteLine("Expiration Masking: {0}", (object) ((response == 0x30) ? "Masked" : "Unmasked"));
      }
      else
      {
          result = String.Format("Fail Error Code=0x{0:X4} : {1}.", (ushort)rt, IDTechSDK.errorCode.getErrorString(rt));
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
          result = String.Format("{0:X}", (int)(response));
          Debug.WriteLine("Get Clear PAN Digits Response: " + (int)(response));
      }
      else
      {
          result = String.Format("Fail Error Code=0x{0:X4} : {1}.", (ushort)rt, IDTechSDK.errorCode.getErrorString(rt));
          Debug.WriteLine("Get Clear PAN Digits {0}", (object)result);
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
            result = String.Format("Fail Error Code=0x{0:X4} : {1}.", (ushort)rt, IDTechSDK.errorCode.getErrorString(rt));
            Debug.WriteLine("Swipe Force Encryption {0}", (object)result);
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
          result += String.Format("Fail Error Code=0x{0:X4} : {1}.", (ushort)rt, IDTechSDK.errorCode.getErrorString(rt));
          Debug.WriteLine(String.Format("Get Mask Option Fail Error Code: 0x{0:X4}", (ushort)rt));
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

        // Terminal Configuration
        string configuration = GetTerminalMajorConfiguration();

        // Get Company
        GetCompany();

        // Terminal Info
        string enable_read_terminal_info = System.Configuration.ConfigurationManager.AppSettings["tc_read_terminal_info"] ?? "true";
        bool read_terminal_info;
        bool.TryParse(enable_read_terminal_info, out read_terminal_info);
        if(read_terminal_info)
        {
            serializer.terminalCfg.config_meta.Type = "device";
            serializer.terminalCfg.hardware.Serial_num = deviceInformation.SerialNumber;
            serializer.terminalCfg.config_meta.Terminal_type = deviceInformation.ModelName;
            Version version = typeof(DeviceCfg).Assembly.GetName().Version;
            serializer.terminalCfg.config_meta.Version = version.ToString();

            Device.GetTerminalInfo(ref serializer);
        }
 
        // Terminal Information
        string enable_read_terminal_data = System.Configuration.ConfigurationManager.AppSettings["tc_read_terminal_data"] ?? "true";
        bool read_terminal_data;
        bool.TryParse(enable_read_terminal_data, out read_terminal_data);
        if(read_terminal_data)
        {
            serializer.terminalCfg.general_configuration.Contact.terminal_ics_type = GetTerminalType() ?? null;
            object [] message1 = Device.GetTerminalData(ref serializer, ref exponent);

            // Display Terminal Data to User
            NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_SHOW_TERMINAL_DATA, Message = message1 });
        }

        // Encryption Control
        string enable_read_encryption = System.Configuration.ConfigurationManager.AppSettings["tc_read_encryption"] ?? "true";
        bool read_encryption;
        bool.TryParse(enable_read_encryption, out read_encryption);
        if(read_encryption)
        {
            Device.GetEncryptionControl(ref serializer);
        }

        // Device Configuration: contact:capk
        string enable_read_capk_settings = System.Configuration.ConfigurationManager.AppSettings["tc_read_capk_settings"] ?? "true";
        bool read_capk_settings;
        bool.TryParse(enable_read_capk_settings, out read_capk_settings);
        if(read_capk_settings)
        {
            Device.GetCapKList(ref serializer);
        }

        // Device Configuration: contact:aid
        string enable_read_aid_settings = System.Configuration.ConfigurationManager.AppSettings["tc_read_aid_settings"] ?? "true";
        bool read_aid_settings;
        bool.TryParse(enable_read_aid_settings, out read_aid_settings);
        if(read_aid_settings)
        {
            Device.GetAidList(ref serializer);
        }

        // MSR Settings
        string enable_read_msr_settings = System.Configuration.ConfigurationManager.AppSettings["tc_read_msr_settings"] ?? "true";
        bool read_msr_settings;
        bool.TryParse(enable_read_msr_settings, out read_msr_settings);
        if(read_msr_settings)
        {
            Device.GetMSRSettings(ref serializer);
        }

        // Update configuration file
        serializer.WriteConfig();

        // Display JSON Config to User
        object [] message = { serializer.GetFileName() };
        NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_SHOW_JSON_CONFIG, Message = message });
    }

    private void GetCompany()
    {
        try
        {
            serializer.terminalCfg.config_meta.Customer.Company = "TrustCommerce";
        }
        catch(Exception exp)
        {
            Debug.WriteLine("DeviceCfg::GetCompany(): - exception={0}", (object)exp.Message);
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
          result = String.Format("Fail Error: 0x{0:X4} - {1}", (ushort)rt, IDTechSDK.errorCode.getErrorString(rt));
          Debug.WriteLine("Set Expiration Mask {0}", (object)result);
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
          result = String.Format("Get Clear PAN Digits Fail Error Code: 0x{0:X4} - {1}", (ushort)rt, IDTechSDK.errorCode.getErrorString(rt));
          Debug.WriteLine(result);
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
            result = string.Format("Fail Error Code=0x{0:X4} : {1}.", (ushort)rt, IDTechSDK.errorCode.getErrorString(rt));
            Debug.WriteLine("Swipe Force Encryption {0}", (object)result);
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
          result += string.Format("Get Mask Option  Fail Error Code: 0x{0:X4}", (ushort)rt);
          Debug.WriteLine(string.Format("Get Mask Option  Fail Error Code: 0x{0:X4}", (ushort)rt));
      }

      return result;
    }

    private void SetDeviceFirmwareVersion()
    {
        deviceInformation.FirmwareVersion  = Device.GetFirmwareVersion();
        string [] message = { deviceInformation.FirmwareVersion };
        NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_FIRMWARE_UPDATE_COMPLETE, Message = message });
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

      // CORE's AMOUNT REQUESTED
      amount = "10.00";
      
      if(useUniversalSDK)
      {
          IDT_Device.emv_allowFallback(false);
          IDT_Device.emv_autoAuthenticate(false);

          // The DFEE1A tag is a proprietary ID TECH tag that wrappers other tags: the BYTE after the tag is the length (in bytes of additional tags).
          //additionalTags = Common.getByteArray("DFEE104DFEF5ADFEF5BDFEF5CDFEF5D");
          additionalTags = null;
          RETURN_CODE rt = IDT_Augusta.SharedController.emv_startTransaction(Convert.ToDouble(amount), 0, exponent, 0, 30, additionalTags, false);

          if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
          {
              Debug.WriteLine("DeviceCfg::GetCardData(): MSR Turned On successfully; Ready to swipe");
          }
          else
          {
              Debug.WriteLine("DeviceCfg::GetCardData(): start EMV failed Error Code: 0x{0:X}", (ushort)rt);
              rt = IDT_Augusta.SharedController.emv_cancelTransaction();

              // Disable EMV QC Mode
              SetEmvQCMode(false);
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
       }
    }

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
      string [] message = { "***** TRANSACTION FAILED: TIMEOUT *****" };
      NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_PROCESS_CARDDATA_ERROR, Message = message });
    }

    public string [] ParseCardData(string data)
    {
        byte [] tlv = DALUtils.HexStringToByteArray(data);
        TerminalData td = new TerminalData(tlv);
        string [] text = td.ConvertTLVToValuePairsArray();

        // Retrieve CC-PAN
        //TrackData trackData = Utils.GetTrackData(tlv);

        //if (trackData != null)
        //{
        //}

        return text;
    }

    public void CardReadNextState(object state)
    {
        EMV_RESULT_CODE nextstep = (EMV_RESULT_CODE) Convert.ToInt16(state);
        Debug.WriteLine("DeviceCfg::CardReadNextState(): - TRANSACTION NEXT STEP={0}", nextstep);

        switch (nextstep)
        {
            case EMV_RESULT_CODE.EMV_RESULT_CODE_APPROVED:
            {
                Debug.WriteLine("DeviceCfg::CardReadNextState(): - TRANSACTION APPROVED");
                break;
            }

            case EMV_RESULT_CODE.EMV_RESULT_CODE_GO_ONLINE:
            {
                byte[] additionalTags = null;
                EMVCompleteTransaction(additionalTags);
                break;
            }

            case EMV_RESULT_CODE.EMV_RESULT_CODE_AUTHENTICATE_TRANSACTION:
            {
                byte[] additionalTags = null;
                if(EMVAuthenticateTransaction(additionalTags) != RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                {
                    RETURN_CODE rt = IDT_Augusta.SharedController.emv_cancelTransaction();
                    Debug.WriteLine("DeviceCfg::CardReadNextState(): - status={0}", IDTechSDK.errorCode.getErrorString(rt));
                }
                break;
            }

            case EMV_RESULT_CODE.EMV_RESULT_CODE_TRANSACTION_CANCELED:
            {
                Debug.WriteLine("DeviceCfg::CardReadNextState(): - TRANSACTION CANCELLED");
                break;
            }

            case EMV_RESULT_CODE.EMV_RESULT_CODE_SMARTCARD_FAIL:
            {
                Debug.WriteLine("DeviceCfg::CardReadNextState(): - SMARTCARD FAILED");
                break;
            }
        }
    }

    #endregion

    /********************************************************************************************************/
    // SETTINGS ACTIONS
    /********************************************************************************************************/
    #region -- settings actions --
    public void GetDeviceConfiguration()
    {
      if (!device.IsConnected)
      {
        string [] message = { "***** REQUEST FAILED: DEVICE IS NOT CONNECTED *****" };
        NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_PROCESS_CARDDATA_ERROR, Message = message });
        return;
      }

      if(useUniversalSDK)
      {
          try
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
              string [] message = { expMask, panDigits, swipeForce, swipeMask, msrSetting };
              NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_GET_DEVICE_CONFIGURATION, Message = message });
          }
          catch(Exception exp)
          {
             Debug.WriteLine("DeviceCfg::GetDeviceMode(): - exception={0}", (object)exp.Message);
          }
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
            object [] message = new object[4];
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
            if(mode.Equals(USK_DEVICE_MODE.USB_HID))
            {
               if(deviceInformation.deviceMode == IDTECH_DEVICE_PID.AUGUSTA_KYB   ||
                  deviceInformation.deviceMode == IDTECH_DEVICE_PID.AUGUSTAS_KYB  ||
                  deviceInformation.deviceMode == IDTECH_DEVICE_PID.MAGSTRIPE_KYB ||
                  deviceInformation.deviceMode == IDTECH_DEVICE_PID.SECUREKEY_KYB ||
                  deviceInformation.deviceMode == IDTECH_DEVICE_PID.SECUREMAG_KYB)
               {
                    // Get QC Mode State
                    byte [] response = null;
                    string command = USDK_CONFIGURATION_COMMANDS.GET_QUICK_CHIP_MODE_STATE;
                    RETURN_CODE rt = IDT_Augusta.SharedController.device_sendDataCommand(command, true, ref response);

                    bool disableQC = true;
                    byte result = response[0];
                    if(result == 0x06 && response.Count() > 16)
                    {
                        if(response[17] == 0x01)
                        {
                            // QC-On
                            disableQC = true;
                        }
                        else if(response[17] == 0x00)
                        {
                            // QC-Off
                            disableQC = false;
                        }
                    }
                    // Disable EMV QC
                    if(disableQC)
                    {
                        command  = USDK_CONFIGURATION_COMMANDS.SET_KYB_QC_MODE_DISABLED;
                        //command = USDK_CONFIGURATION_COMMANDS.SET_QUICK_CHIP_MODE_DISABLED;
                        rt = IDT_Augusta.SharedController.device_sendDataCommand_ext(command, false, ref response, 60, false);
                        result = response[0];
                        if(result == 0x06)
                        {
                            Thread.Sleep(1000);
                        }
                        if(attached)
                        { 
                            IDT_Augusta.SharedController.device_sendDataCommand_ext(command, false, ref response, 60 , false);
                            result = response[0];
                            if(result == 0x06)
                            {
                                Thread.Sleep(1000);
                            }
                            // Well, we tried !
                            if(attached)
                            { 
                                // Set Device to HID MODE
                                if(!Device.SetUSBHIDMode())
                                {
                                    DeviceRemovedHandler();
                                }
                                //string [] message = { "Enable" };
                                //NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_ENABLE_MODE_BUTTON, Message = message } );
                            }
                        }

                        // Disable QuickCHIP - device should reboot
                        //Device.SetQuickChipMode(false);
                        //Device.Reset();
                    }
               }
                else if(deviceInformation.deviceMode == IDTECH_DEVICE_PID.VP3000_KYB)
                {
                    //Device.VP3000PingReport();
                    Device.SetVP3000DeviceHidMode();
                }
            }
            else if(mode.Equals(USK_DEVICE_MODE.USB_KYB))
            {
               if(deviceInformation.deviceMode == IDTECH_DEVICE_PID.AUGUSTA_HID  ||
                  deviceInformation.deviceMode == IDTECH_DEVICE_PID.AUGUSTAS_HID)
               {
                    // TURN ON QUICK CHIP MODE
                    byte [] response = null;
                    string command = USDK_CONFIGURATION_COMMANDS.SET_QUICK_CHIP_MODE_ENABLED;
                    RETURN_CODE rt = IDT_Augusta.SharedController.device_sendDataCommand(command, true, ref response);
                    Thread.Sleep(1000);
                    if(attached)
                    { 
                        // Set Device to HID MODE
                        rt = IDT_Augusta.SharedController.msr_switchUSBInterfaceMode(true);
                        Debug.WriteLine("DeviceCfg::SetDeviceMode(): - status={0}", IDTechSDK.errorCode.getErrorString(rt));

                        // Restart device discovery
                        DeviceRemovedHandler();
                    }
               }
               else if(deviceInformation.deviceMode == IDTECH_DEVICE_PID.VP3000_HID)
               {
                  RETURN_CODE rt = IDT_VP3300.SharedController.device_setPollMode(3);
                  if(rt != RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                  {
                    Debug.WriteLine("DeviceCfg::SetDeviceMode(): VP3000 - error={0}", IDTechSDK.errorCode.getErrorString(rt));
                  }
               }
               else
               {
                    // Set Device to Keyboard MODE
                    if(!Device.SetUSBKeyboardMode())
                    {
                        DeviceRemovedHandler();
                    }
               }
            }
        }
        catch(Exception exp)
        {
           Debug.WriteLine("DeviceCfg::SetDeviceMode(): - exception={0}", (object)exp.Message);
        }
    }

    private bool GetEMVQCMode()
    {
        bool isEMVQCEnabled = false;
        byte [] response = null;
        string command = USDK_CONFIGURATION_COMMANDS.GET_QUICK_CHIP_MODE_STATE;
        RETURN_CODE rt = IDT_Augusta.SharedController.device_sendDataCommand(command, true, ref response);
        byte result = (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS) ? response[0] : (byte) 0x00;

        if(result == 0x06)
        {
            if(response.Count() == 6)
            {
                if(response[5] == 0x31)
                {
                    isEMVQCEnabled = true;
                }
                else if(response[5] == 0x30)
                {
                    isEMVQCEnabled = false;
                }
            }
        }

        return isEMVQCEnabled;
    }

    public bool SetEmvQCMode(bool mode)
    {
        if(deviceProtocol == DEVICE_PROTOCOL_Types.DEVICE_PROTOCOL_KB)
        {
            return false;
        }

        bool result = false;

        try
        {
            bool isEMVQCEnabled = GetEMVQCMode();
            byte [] response = null;
            if(mode)
            {
                // Enable QC Mode
                if(!isEMVQCEnabled)
                {
                    string command = USDK_CONFIGURATION_COMMANDS.SET_QUICK_CHIP_MODE_ENABLED;
                    RETURN_CODE rt = IDT_Augusta.SharedController.device_sendDataCommand(command, true, ref response);
                    result = (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS) ? true : false;
                }
                else
                {
                    Debug.WriteLine("DeviceCfg::SetEmvQCMode(): - ALREADY ENABLED.");
                }
            }
            else
            {
                // Disable QC Mode
                if(isEMVQCEnabled)
                {
                    string command = USDK_CONFIGURATION_COMMANDS.SET_QUICK_CHIP_MODE_DISABLED;
                    RETURN_CODE rt = IDT_Augusta.SharedController.device_sendDataCommand(command, true, ref response);
                    result = (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS) ? true : false;
                    // Disable ICC
                    //RETURN_CODE rt = IDT_Augusta.SharedController.icc_disable();
                    // Remove EMV settings
                    //rt = IDT_Augusta.SharedController.emv_removeTerminalData();
                    // Remove All AID
                    //rt = IDT_Augusta.SharedController.emv_removeAllApplicationData();
                    // Remove All CAPK
                    //rt = IDT_Augusta.SharedController.emv_removeAllCAPK();
                    // Set Device to HID MODE
                    //rt = IDT_Augusta.SharedController.msr_switchUSBInterfaceMode(true);
                    //Debug.WriteLine("DeviceCfg::SetEmvQCMode() - status={0}", rt);

                    //string [] message = { "Enable" };
                    //NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_SET_EMV_MODE_BUTTON, Message = message });

                    // Restart device discovery
                    //DeviceRemovedHandler();

                    result = true;
                }
                else
                {
                    Debug.WriteLine("DeviceCfg::SetEmvQCMode(): - ALREADY DISABLED.");
                }
            }
        }
        catch(Exception exp)
        {
           Debug.WriteLine("DeviceCfg::SetEmvQCMode(): - exception={0}", (object)exp.Message);
        }

        return result;
    }

    private string GetTerminalType()
    {
        string command = USDK_CONFIGURATION_COMMANDS.GET_MAJOR_TERMINAL_CFG;
        string response = DeviceCommand(command, false);
        Debug.WriteLine("Terminal Major Configuration: ----------- =[{0}]", (object) response);

        if(response.StartsWith("06"))
        {
            // TerminalConfig-2C: "32" => TerminalConfig-5C: "35"
            string terminal = response.Substring(response.Length -2, 2);

            switch(terminal)
            {
                case TerminalMajorConfiguration.TERMCFG_2:
                {
                    response = TerminalMajorConfiguration.CONFIG_2C;
                    break;
                }
                case TerminalMajorConfiguration.TERMCFG_5:
                {
                    response = TerminalMajorConfiguration.CONFIG_5C;
                    break;
                }
            }
        }
        else
        {
            response = "UNDEFINED";
        }

        return response;
    }

    private string GetTerminalMajorConfiguration()
    {
        string response = GetTerminalType();
        Debug.WriteLine("Terminal Major Configuration: ----------- =[{0}]", (object) response);

        if(response.Equals(TerminalMajorConfiguration.CONFIG_2C) || response.Equals(TerminalMajorConfiguration.CONFIG_5C))
        {
            // WIP: TESTING
            ///SetTerminalData(response);
        }

        return response;
    }

    private string SetTerminalMajorConfiguration(string mode)
    {
        string command = "";
        string response = "";

        switch(mode)
        {
            case TerminalMajorConfiguration.CONFIG_2C:
            {
                // 2C: 0206007253012801323b2103
                command = USDK_CONFIGURATION_COMMANDS.SET_TERMINAL_MAJOR_2C;
                break;
            }
            case TerminalMajorConfiguration.CONFIG_5C:
            {
                // 5C: 0206007253012801353c2403
                command = USDK_CONFIGURATION_COMMANDS.SET_TERMINAL_MAJOR_5C;
                break;
            }
        }

        if(command.Length > 0)
        {
            //response = DeviceCommand(command, false);
            //Debug.WriteLine("device::SetTerminalMajorConfiguration() reply=[{0}]", (object) response);
            int configuration = Convert.ToInt32(command.Substring(0, 1));
            RETURN_CODE rt = IDT_Augusta.SharedController.emv_setTerminalMajorConfiguration(configuration);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                Debug.WriteLine("DeviceCfg::SetTerminalMajorConfiguration(): success.");
            }
            else
            {
                Debug.WriteLine("DeviceCfg::SetTerminalMajorConfiguration(): failed Error Code: 0x{0:X}", (ushort)rt);
            }
        }

        return response;
    }

    private byte [] GetEMVTerminalData()
    {
        byte [] response = null;
        string command = USDK_CONFIGURATION_COMMANDS.GET_EMV_TERMINAL_DATA;
        RETURN_CODE rt = IDT_Augusta.SharedController.device_sendDataCommand(command, true, ref response);
        if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && response != null)
        {
            byte [] tags = response.Skip(3).Take(response.Length - 3).ToArray();
            TerminalData td = new TerminalData(tags);
            string text = td.ConvertTLVToValuePairs();
        }

        return response;
    }

    private void SetTerminalData(string configuration)
    {
        string response = SetTerminalMajorConfiguration(configuration);

        // Retrieve all EMV L2 Terminal Data from the reader
        byte[] data =  GetEMVTerminalData();

        if(data != null)
        {
            // Save this to a backup file prior to any other operation
            serializer.WriteTerminalDataConfig();

            byte [] tags = data.Skip(3).Take(data.Length - 3).ToArray();

            // Set Terminal Data) command : "72 46 02 03"
            RETURN_CODE rt = IDT_Augusta.SharedController.emv_setTerminalData(tags);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS) 
            {
                Debug.WriteLine("device: SetTerminalData() Successful for configuration: ", (object) configuration);
            }
            else 
            {
                Debug.WriteLine("device: SetTerminalData() failed error=0x{0:X}: {1}", (ushort)rt, IDTechSDK.errorCode.getErrorString(rt));
            }
        }
    }
    #endregion

    /********************************************************************************************************/
    // DEVICE ACTIONS
    /********************************************************************************************************/
    #region -- device actions --
    public string DeviceCommand(string command, bool notify)
    {
        string [] message = { "" };

        if(useUniversalSDK)
        {
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
                    message[0] =  string.Format("COMMAND EXECUTE FAILED - CODE=0x{0:X4}", (ushort)rt);
                }
            }

            if(notify)
            {
                NotificationRaise(new DeviceNotificationEventArgs { NotificationType = NOTIFICATION_TYPE.NT_SET_EXECUTE_RESULT, Message = message });
            }
         }
        else
        {
            //TODO
        }

        return message[0];
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
            message = "*** TRANSACTION DATA CAPTURED : MSR ***";
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
    internal IDTECH_DEVICE_PID deviceMode;
    internal bool emvConfigSupported;
 }
 public static class USK_DEVICE_MODE
 {
    public const string USB_HID = "USB-HID";
    public const string USB_KYB = "USB-KB";
    public const string UNKNOWN = "UNKNOWN";
    public const string OLDIDTECHHID = "OLDIDTECHHID";
    public const string OLDIDTECHKYB = "OLDIDTECHKB";
    }

 internal static class TerminalMajorConfiguration
 {
    internal const string CONFIG_2C = "2C";
    internal const string CONFIG_5C = "5C";
    internal const string TERMCFG_2 = "32";
    internal const string TERMCFG_5 = "35";
 }

 internal static class USDK_CONFIGURATION_COMMANDS
 {
    internal const string GET_MAJOR_TERMINAL_CFG       = "72 52 01 28";
    internal const string SET_TERMINAL_MAJOR_2C        = "72 53 01 28 01 32";
    internal const string SET_TERMINAL_MAJOR_5C        = "72 53 01 28 01 35";
    internal const string GET_EMV_TERMINAL_DATA        = "72 46 02 01";
    internal const string GET_QUICK_CHIP_MODE_STATE    = "72 52 01 29";
    internal const string SET_QUICK_CHIP_MODE_DISABLED = "72 53 01 29 01 30";
    internal const string SET_KYB_QC_MODE_DISABLED     = "02 06 00 72 53 01 29 01 30 38 20 03";
    internal const string SET_QUICK_CHIP_MODE_ENABLED  = "72 53 01 29 01 31";
 }

 #endregion
}
