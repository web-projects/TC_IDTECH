using System;
using IPA.Core.Data.Entity.Other;
using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Models;
using System.Collections.Generic;
///using IPA.Core.XO.TCCustAttribute;
using System.Threading;
using System.Threading.Tasks;
using IPA.DAL.RBADAL.Services;
using IPA.CommonInterface;
using IPA.CommonInterface.ConfigIDTech;

namespace IPA.DAL.RBADAL.Interfaces
{
    public enum IDeviceMessage
    {
        DeviceBusy = 1,
        Offline    = 2
    }

    interface IDevice
    {
        event EventHandler<NotificationEventArgs> OnNotification;
        
        // Readonly Properties
        bool Connected { get; }
        Core.Data.Entity.Device DeviceInfo { get; }
        Core.Data.Entity.Model ModelInfo { get; }
        
        //Public methods
        void Init(string[] accepted, string[] available, int baudRate, int dataBits);
        void Configure(object[] settings);
        DeviceStatus Connect();
        void Disconnect();
        void Abort(DeviceAbortType abortType);
        void Process(DeviceProcess process);
        void ClearBuffer();

        void BadRead();
        ///Signature Signature();
        bool UpdateDevice(DeviceUpdateType updateType);
        string GetSerialNumber();
        string GetFirmwareVersion();
        DeviceInfo GetDeviceInfo();
        bool Reset();
        ///Task CardRead(string paymentAmount, string promptText, string availableReaders, List<TCCustAttributeItem> attributes, EntryModeType entryModeType);
        Task CardRead(string paymentAmount, string promptText);

        bool ShowMessage(IDeviceMessage deviceMessage, string message); //only be used when displaying message OUTSIDE of the transaction workflow (like device update)

        #region -- keyboard mode overrides --
        // keyboard mode overrides
        bool SetQuickChipMode(bool mode);
        bool SetUSBHIDMode();
        bool SetUSBKeyboardMode();

        void SetVP3000DeviceHidMode();
        void VP3000PingReport();
        #endregion

        /********************************************************************************************************/
        // DEVICE CONFIGURATION
        /********************************************************************************************************/
        #region -- device configuration --

        void GetTerminalInfo(ref ConfigSerializer serializer);
        string [] GetTerminalData(ref ConfigSerializer serializer, ref int exponent);
        void ValidateTerminalData(ref ConfigSerializer serializer);
        void GetAidList(ref ConfigSerializer serializer);
        void ValidateAidList(ref ConfigSerializer serializer);
        void GetCapKList(ref ConfigSerializer serializer);
        void ValidateCapKList(ref ConfigSerializer serializer);
        void GetMSRSettings(ref ConfigSerializer serializer);
        void GetEncryptionControl(ref ConfigSerializer serializer);
        void CloseDevice();
        void FactoryReset();
        #endregion
    }
}
