using IPA.Core.Data.Entity;
using IPA.Core.Data.Entity.Other;
using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Interfaces;
using IPA.DAL.RBADAL.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using HidLibrary;
using IPA.DAL.RBADAL.Services.Devices.IDTech;
using IPA.DAL.RBADAL.Services.Devices.IDTech.Models;
using System.Collections.Generic;
///using IPA.Core.XO.TCCustAttribute;
using System.Threading.Tasks;
using IPA.Core.Shared.Helpers;
using IPA.Core.Shared.Helpers.StatusCode;
using IPA.CommonInterface;
using IPA.CommonInterface.ConfigIDTech;

namespace IPA.DAL.RBADAL.Services
{
    public class Unknown_Device : IDevice
    {
        #region -- member variables --

        public EventWaitHandle waitForReply = new EventWaitHandle(false, EventResetMode.AutoReset);

        public event EventHandler<NotificationEventArgs> OnNotification;

        private static DeviceInfo deviceInfo;

        #endregion

        #region -- public properties --

        bool IDevice.Connected => false;

        private const string unknown = "Unknown";
        private string modelPid = unknown;

        Core.Data.Entity.Device IDevice.DeviceInfo => new Core.Data.Entity.Device
        {
            ManufacturerID = (int)DeviceManufacturer.IDTech,
            SerialNumber = unknown,
            FirmwareVersion = unknown,
            OSVersion = unknown
        };

        Model IDevice.ModelInfo => new Model
        {
            DefaultInterfacePort = unknown,
            ModelNumber = modelPid,
            ManufacturerID = (int)DeviceManufacturer.IDTech
        };

        #endregion

        #region -- public methods --

        public Unknown_Device(IDTECH_DEVICE_PID mode)
        {
            Console.WriteLine($"Unknown Unsupported Device {mode}");
            NotificationRaise(new NotificationEventArgs { NotificationType = NotificationType.DeviceEvent, DeviceEvent = DeviceEvent.DeviceError });
        }

        void IDevice.Init(string[] accepted, string[] available, int baudRate, int dataBits)
        {
            //Create Device info object
            deviceInfo = new DeviceInfo();
        }

        public virtual void Configure(object[] settings)
        {
        }

        DeviceStatus IDevice.Connect()
        {
            //return DeviceStatus.Unsupported;
            return DeviceStatus.Connected;
        }

        bool IDevice.Reset()
        {
            return true;
        }

        bool IDevice.ShowMessage(IDeviceMessage deviceMessage, string message)
        {
            return true;
        }

        void IDevice.Disconnect()
        {
        }

        void IDevice.BadRead()
        {
            NotificationRaise(new NotificationEventArgs { NotificationType = NotificationType.DeviceEvent, DeviceEvent = DeviceEvent.DeviceError });
        }

        void IDevice.Abort(DeviceAbortType abortType)
        {
        }

        void IDevice.ClearBuffer()
        {
        }
        async Task IDevice.CardRead(string paymentAmount, string promptText)
        {
        }

        void IDevice.Process(DeviceProcess process)
        {
        }

        bool IDevice.UpdateDevice(DeviceUpdateType updateType)
        {
            return false;
        }

         public virtual string GetSerialNumber()
        {
            return unknown;
        }
        #endregion

        #region -- private helper methods --

        public void NotificationRaise(NotificationEventArgs e)
        { 
                OnNotification?.Invoke(null, e);
        }

        #endregion

        #region -- device integration methods --


    
       #region -- device send / receive methods --


        #endregion

        #region -- device helper methods --
 
        public virtual string ParseFirmwareVersion(string firmwareInfo)
        {
            return unknown;
        }

        public static int GetDeviceCount()
        {
            return -1;
        }

        public string GetDeviceSerialNumber()
        {
            return unknown;
        }

        public virtual DeviceInfo GetDeviceInfo()
        {
            return new DeviceInfo()
            {
                SerialNumber = GetDeviceSerialNumber(),
                FirmwareVersion = GetFirmwareVersion(),
                ModelName = unknown,
                ModelNumber = modelPid,
                Port = unknown,
            };
        }

        public string GetFirmwareVersion()
        {
            return unknown;
        }
     
        public IDTSetStatus SetConfig(string configCommands, string resetConfigCommands)
        {
            if (configCommands == "PID")
            {
                modelPid = resetConfigCommands;
            }

            return new IDTSetStatus();
        }

        public static TrackData ParseXmlFormat(byte[] bytes)
        {
            return null;
        }

        public static TrackData ParseIdtFormat(byte[] bytes)
        {
            return null;
        }

        #endregion

        #region -- string / array manipulation procedures --

        //TODO: see if these should be extension methods and moved to shared

        #endregion

        #endregion

        #region -- device event procedures --
        public void OnReport(HidReport report)
        {
        }

        private void DeviceRemovedHandler()
        {
            //TODO: When device is rmoved raise an user message?
            System.Diagnostics.Debug.WriteLine("device: removed.");
            NotificationRaise(new NotificationEventArgs { NotificationType = NotificationType.DeviceEvent, DeviceEvent = DeviceEvent.DeviceDisconnected });
        }

        #endregion

        #region -- keyboard mode overrides --
        public bool SetQuickChipMode(bool mode)
        {
            return false;
        }
        public bool SetUSBHIDMode()
        {
            return false;
        }
        public bool SetUSBKeyboardMode()
        {
            return false;
        }
        public void SetVP3000DeviceHidMode()
        {
        }
        public void VP3000PingReport()
        {
        }
        #endregion

        /********************************************************************************************************/
        // DEVICE CONFIGURATION
        /********************************************************************************************************/
        #region -- device configuration --

        public virtual void GetTerminalInfo(ref ConfigSerializer serializer)
        {
        }

        public virtual string [] GetTerminalData(ref ConfigSerializer serializer, ref int exponent)
        {
            return null;
        }
        public virtual void ValidateTerminalData(ref ConfigSerializer serializer)
        {
        }
        public virtual void GetAidList(ref ConfigSerializer serializer)
        {
        }
        public virtual void ValidateAidList(ref ConfigSerializer serializer)
        {
        }
        public virtual void GetCapKList(ref ConfigSerializer serializer)
        {
        }
        public virtual void ValidateCapKList(ref ConfigSerializer serializer)
        {
        }
        public void GetMSRSettings(ref ConfigSerializer serializer)
        {
        }
        public void GetEncryptionControl(ref ConfigSerializer serializer)
        {
        }
        public virtual void CloseDevice()
        {
        }
        public virtual void FactoryReset()
        {
        }
        #endregion

    }
}

