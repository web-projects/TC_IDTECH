using System;
using IPA.Core.Data.Entity.Other;
using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Models;
using System.Collections.Generic;
///using IPA.Core.XO.TCCustAttribute;
using System.Threading;
using System.Threading.Tasks;
using IPA.DAL.RBADAL.Services;

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
        DeviceStatus Connect();
        void Disconnect();
        void Abort(DeviceAbortType abortType);
        void Process(DeviceProcess process);
        void ClearBuffer();

        void BadRead();
        ///Signature Signature();
        bool UpdateDevice(DeviceUpdateType updateType);
        string GetSerialNumber();
        DeviceInfo GetDeviceInfo();
        bool Reset();
        ///Task CardRead(string paymentAmount, string promptText, string availableReaders, List<TCCustAttributeItem> attributes, EntryModeType entryModeType);
        Task CardRead(string paymentAmount, string promptText);

        bool ShowMessage(IDeviceMessage deviceMessage, string message); //only be used when displaying message OUTSIDE of the transaction workflow (like device update)

    }
}
