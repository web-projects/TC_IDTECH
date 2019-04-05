using System;
using IPA.Core.Data.Entity.Other;
using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Interfaces;
using IPA.DAL.RBADAL.Models;
using System.Management;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using IPA.CommonInterface.Interfaces;
using IPA.CommonInterface.ConfigIDTech;

namespace IPA.DAL.RBADAL.Services
{
    public class Device
    {
        #region -- member variables --

        private IDevice deviceInterface;
        private IDTECH_DEVICE_PID deviceMode;

        public delegate void EventHandler(object sender, NotificationEventArgs args);
        public event EventHandler<NotificationEventArgs> OnNotification = delegate { };
        public static DeviceStatus deviceStatus;
        public static int retryTimes = 2;
        #endregion

        #region -- public methods --

        public void Init(string[] available, ref IDTECH_DEVICE_PID mode)
        {
            BaudRate = int.Parse(ConfigurationManager.AppSettings["IPA.DAL.Device.COMBaudRate"]);
            DataBits = int.Parse(ConfigurationManager.AppSettings["IPA.DAL.Device.COMDataBits"]);
            AcceptedPorts = ConfigurationManager.AppSettings["IPA.DAL.Device.AcceptedComPorts"].Split(',');

            DeviceFolder = ConfigurationManager.AppSettings["IPA.DAL.Application.Folders.Devices"];
            LoggingLevel = ConfigurationManager.AppSettings["IPA.DAL.Device.LoggingLevel"];
            
            deviceStatus = DeviceStatus.NoDevice;
            var devices = GetUSBDevices();
            
            if (devices.Count == 1)
            {
                var vendor = devices[0].Vendor;
                Device.Manufacturer = devices[0].Vendor;

                switch (devices[0].Vendor)
                {
                    case DeviceManufacturer.IDTech:
                    {
                        var deviceID = devices[0].DeviceID;
                        string [] worker = deviceID.Split('&');

                            int pidId = 0;
                        // should contain PID_XXXX...
                        if(System.Text.RegularExpressions.Regex.IsMatch(worker[1], "PID_"))
                        {
                          string [] worker2 = System.Text.RegularExpressions.Regex.Split(worker[1], @"PID_");
                          string pid = worker2[1].Substring(0, 4);

                          // See if device matches
                          pidId = Int32.Parse(pid);

                          switch(pidId)
                          {
                            case (int) IDTECH_DEVICE_PID.MAGSTRIPE_HID:
                            case (int) IDTECH_DEVICE_PID.MAGSTRIPE_KYB:
                            case (int) IDTECH_DEVICE_PID.SECUREKEY_HID:
                            case (int) IDTECH_DEVICE_PID.SECUREKEY_KYB:
                            case (int) IDTECH_DEVICE_PID.VP3000_HID:
                            case (int) IDTECH_DEVICE_PID.VP3000_KYB:
                            case (int) IDTECH_DEVICE_PID.AUGUSTA_KYB:
                            case (int) IDTECH_DEVICE_PID.AUGUSTAS_KYB:
                            case (int) IDTECH_DEVICE_PID.AUGUSTA_HID:
                            case (int) IDTECH_DEVICE_PID.AUGUSTAS_HID:
                            case (int) IDTECH_DEVICE_PID.VP5300_HID:
                            case (int) IDTECH_DEVICE_PID.VP5300_KYB:
                            {
                              mode = (IDTECH_DEVICE_PID)pidId;
                              break;
                            }

                            default:
                            {
                              mode = IDTECH_DEVICE_PID.UNKNOWN;
                              break;
                            }
                          }
                        }

                        deviceMode = mode;

                        if (mode == IDTECH_DEVICE_PID.AUGUSTA_HID  || mode == IDTECH_DEVICE_PID.AUGUSTA_KYB  ||
                            mode == IDTECH_DEVICE_PID.AUGUSTAS_HID || mode == IDTECH_DEVICE_PID.AUGUSTAS_KYB)
                        {
                            deviceInterface = new Device_Augusta(deviceMode);
                        }
                        else if (mode == IDTECH_DEVICE_PID.VP3000_HID || mode == IDTECH_DEVICE_PID.VP3000_KYB)
                        {
                            deviceInterface = new Device_VP3000(deviceMode);
                        }
                        else if (mode == IDTECH_DEVICE_PID.VP5300_HID || mode == IDTECH_DEVICE_PID.VP5300_KYB)
                        {
                            deviceInterface = new Device_VP5300(deviceMode);
                        }
                        else if (mode == IDTECH_DEVICE_PID.SECUREKEY_HID ||
                                 mode == IDTECH_DEVICE_PID.SECUREKEY_KYB ||
                                 mode == IDTECH_DEVICE_PID.MAGSTRIPE_KYB ||
                                 mode == IDTECH_DEVICE_PID.MAGSTRIPE_HID)
                        {
                            deviceInterface = new Device_IDTech(deviceMode);
                        }
                        else
                        {
                            Unknown_Device ukd = new Unknown_Device(mode);
                            ukd.SetConfig("PID", pidId.ToString());
                            deviceInterface = ukd;
                        }
                        deviceInterface.OnNotification += DeviceOnNotification;
                        break;
                    }
                    case DeviceManufacturer.Ingenico:
                    {
///                        deviceInterface = new Device_Ingenico();
///                        deviceInterface.OnNotification += DeviceOnNotification;
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(vendor), vendor, null);
                    }
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
            deviceInterface?.Init(Device.AcceptedPorts, available, Device.BaudRate, Device.DataBits);            
        }
        
        public void Configure(object[] settings)
        {
            deviceInterface.Configure(settings);
        }

        //TODO: map response codes to something meaningful
///        public async Task CardRead(string paymentAmount, string promptText, string availableReaders, List<TCCustAttributeItem> attributes, EntryModeType entryModeType, int attemptedReads, CreditCard cc)
        public async Task CardRead(string paymentAmount, string promptText)
        {
///            if (Transaction.PaymentXO.Request.CreditCard == null)
///                Transaction.PaymentXO.Request.CreditCard = new CreditCard();

            //Clear the IDTech Buffers before we take a card read of any type
            if (Manufacturer == DeviceManufacturer.IDTech)
                ClearBuffer();
 
///            await deviceInterface?.CardRead(paymentAmount, promptText, availableReaders, attributes, entryModeType);
            await deviceInterface?.CardRead(paymentAmount, promptText);

            return ;
        }

        //necessary because Device.Init happens before Company Configs are read.
        public void SetRetryAttempts(int retries)
        {
            retryTimes = retries;
        }

        public void Connect()
        {
            deviceStatus = (DeviceStatus)deviceInterface?.Connect();
        }
 
        public void Disconnect()
        {
            deviceInterface?.Disconnect();
        }

        public void BadRead()
        {
///            Transaction.PaymentXO.Request.ReadAttempts += 1;
///            Transaction.PaymentXO.Request.CreditCard.AbortType = DeviceAbortType.BadRead;
            DeviceOnNotification(null, new NotificationEventArgs { NotificationType = NotificationType.DeviceEvent, DeviceEvent = DeviceEvent.CardReadComplete });
        }
  
        public void Abort(object abortType)
        {
            deviceInterface?.Abort((DeviceAbortType) abortType);
        }

        public void ClearBuffer()
        {
            deviceInterface?.ClearBuffer();
        }

        public void Process(DeviceProcess process)
        {
            deviceInterface.Process(process);
        }

        //public Signature Signature()
        //{
        //    return deviceInterface.Signature();
        //}

        public bool Reset()
        {
            return deviceInterface.Reset();
        }

        public bool SetQuickChipMode(bool mode)
        {
            return deviceInterface.SetQuickChipMode(mode);
        }

        public bool SetUSBHIDMode()
        {
            return deviceInterface.SetUSBHIDMode();
        }

        public bool SetUSBKeyboardMode()
        {
            return deviceInterface.SetUSBKeyboardMode();
        }

        public bool UpdateDevice(DeviceUpdateType updateType)
        {
            return deviceInterface.UpdateDevice(updateType);
        }

        //only be used when displaying message OUTSIDE of the transaction workflow (like device update)
        public bool ShowMessage(IDeviceMessage deviceMessage, string message)
        {
            return deviceInterface?.ShowMessage(deviceMessage, message) ?? false;
        }

        public List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            ManagementObjectCollection collection;

            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity"))
                collection = searcher.Get();

            foreach (var device in collection)
            {
                var deviceID = (string)device.GetPropertyValue("DeviceID");
///                if (deviceID.ToLower().Contains("usb\\") && ((deviceID.Contains($"VID_{IDTECH}") && !Configuration.General.IDTechDisable )|| deviceID.Contains($"VID_{INGNAR}")))
if (deviceID.ToLower().Contains("usb\\") && ((deviceID.Contains($"VID_{IDTECH}"))|| deviceID.Contains($"VID_{INGNAR}"))) 
                {
                    DeviceManufacturer vendor = deviceID.Contains($"VID_{IDTECH}") ? DeviceManufacturer.IDTech : DeviceManufacturer.Ingenico;
                    devices.Add(new USBDeviceInfo(
                    (string)device.GetPropertyValue("DeviceID"),
                    (string)device.GetPropertyValue("PNPDeviceID"),
                    (string)device.GetPropertyValue("Description"),
                    vendor
                    ));
                }
            }

            collection.Dispose();
            return devices;
        }
        public string GetSerialNumber()
        {
            return deviceInterface.GetSerialNumber();
        }
        public string GetFirmwareVersion()
        {
            return deviceInterface.GetFirmwareVersion();
        }

        public DeviceInfo GetDeviceInfo()
        {
            return deviceInterface.GetDeviceInfo();
        }

         #region -- keyboard mode overrides --
        public void SetVP3000DeviceHidMode()
        {
            deviceInterface.SetVP3000DeviceHidMode();
        }
        public void VP3000PingReport()
        {
            deviceInterface.VP3000PingReport();
        }
        #endregion

        /********************************************************************************************************/
        // DEVICE CONFIGURATION
        /********************************************************************************************************/
        #region -- device configuration --

        public void GetTerminalInfo(ref ConfigSerializer serializer)
        {
        }

        public string [] GetTerminalData(ref ConfigSerializer serializer, ref int exponent)
        {
            return deviceInterface?.GetTerminalData(ref serializer, ref exponent);
        }
        public void ValidateTerminalData(ref ConfigSerializer serializer)
        {
            deviceInterface?.ValidateTerminalData(ref serializer);
        }
        public void GetAidList(ref ConfigSerializer serializer)
        {
            deviceInterface?.GetAidList(ref serializer);
        }
        public void ValidateAidList(ref ConfigSerializer serializer)
        {
            deviceInterface?.ValidateAidList(ref serializer);
        }
        public void GetCapKList(ref ConfigSerializer serializer)
        {
            deviceInterface?.GetCapKList(ref serializer);
        }
        public void ValidateCapKList(ref ConfigSerializer serializer)
        {
            deviceInterface?.ValidateCapKList(ref serializer);
        }
        public void GetMSRSettings(ref ConfigSerializer serializer)
        {
            deviceInterface?.GetMSRSettings(ref serializer);
        }
        public void GetEncryptionControl(ref ConfigSerializer serializer)
        {
            deviceInterface?.GetEncryptionControl(ref serializer);
        }
        public void CloseDevice()
        {
            deviceInterface?.CloseDevice();
        }
        public void FactoryReset()
        {
            deviceInterface?.FactoryReset();
        }
        #endregion

        public class USBDeviceInfo
        {
            public USBDeviceInfo(string deviceID, string pnpDeviceID, string description, DeviceManufacturer vendor)
            {
                this.DeviceID = deviceID;
                this.PnpDeviceID = pnpDeviceID;
                this.Description = description;
                this.Vendor = vendor;
            }
            public string DeviceID { get; private set; }
            public string PnpDeviceID { get; private set; }
            public string Description { get; private set; }
            public DeviceManufacturer Vendor { get; private set; }
        }
        #endregion

        #region event handlers --

        public void DeviceOnNotification(object sender, NotificationEventArgs e)
        {
            OnNotification?.Invoke(null, e);
        }

        #endregion

        //TODO: vette pattern of where class declaritions are located (top/bottom) - Mark
        #region -- public properties --   

        public  Core.Data.Entity.Device DeviceInfo => deviceInterface?.DeviceInfo;

        public  Core.Data.Entity.Model ModelInfo => deviceInterface?.ModelInfo;
        
        public bool Connected => deviceInterface?.Connected ?? false;
        public static int BaudRate;
        public static int DataBits;
        public static string[] AcceptedPorts;
        public static DeviceManufacturer Manufacturer;
        public string DeviceFolder;
        public string LoggingLevel;

        public const string IDTECH = "0ACD";
        public const string INGNAR = "0B00";      
        #endregion 
    }
}
