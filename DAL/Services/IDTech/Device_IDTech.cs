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
    public class Device_IDTech : IDevice
    {
        const int StxValid = 2;
        const int EtxValid = 3;
        
        const int osStx = 0;
        const int osLenDataL = 1;
        const int osLenDataH = 2;
        const int osCardEncodeType = 3;
        const int osTrackStatus = 4;
        const int osLenT1 = 5;
        const int osLenT2 = 6;     //LenPanData
        const int osLenT3 = 7;     //LenAddrZip
        const int osMaskStatus = 8;
        const int osDataStatus = 9;

        const int lenHash = 20;
        const int lenKsn = 10;
        const int lenSerialNumber = 10;

        #region -- member variables --

        //IDTech variables
        private HidDevice device;

        public const int IDTechVendorID = 0x0ACD;

        //Not sure how this is used, set in Connect
        private static DeviceInfo deviceInfo;

//        private static string attachedPort;
        private static string[] acceptedPorts;
        private static string[] availablePorts;

        //Transaction Processing Variables, track state and status through device workflow
        
//        private Signature signature;
        private string currentPaymentAmount;
//        private bool IsDebit;
//        private bool IsEMV;
        private IDTECH_DEVICE_PID deviceMode;

        public EventWaitHandle waitForReply = new EventWaitHandle(false, EventResetMode.AutoReset);

        public event EventHandler<NotificationEventArgs> OnNotification;

        #endregion

        #region -- public properties --

        bool IDevice.Connected => device?.IsConnected != null && (bool) device?.IsConnected;

        Core.Data.Entity.Device IDevice.DeviceInfo => new Core.Data.Entity.Device
        {
            ManufacturerID = (int)DeviceManufacturer.IDTech,
            SerialNumber = deviceInfo.SerialNumber,
            FirmwareVersion = deviceInfo.FirmwareVersion,
            OSVersion = device.Attributes.Version.ToString()
        };

        Model IDevice.ModelInfo => new Model
        {
            DefaultInterfacePort = deviceInfo.Port,
            ModelNumber = deviceInfo.ModelNumber,
            ManufacturerID = (int)DeviceManufacturer.IDTech
        };

        #endregion

        #region -- public methods --

        public Device_IDTech(IDTECH_DEVICE_PID mode)
        {
            deviceMode = mode;
        }

        void IDevice.Init(string[] accepted, string[] available, int baudRate, int dataBits)
        {
            acceptedPorts = accepted;
            availablePorts = available;

            //Create Device info object
            deviceInfo = new DeviceInfo();

            //TODO: Get IDTech replacement
            //Add event handlers
            //ingenicoDevice.DeviceInputReceived += (sender3, deviceArgs) => DeviceInputReceived(deviceArgs.MessageID, deviceArgs.DeviceForm, deviceArgs.KeyPressID);
            //ingenicoDevice.DeviceConnectionChanged += (sender4, deviceConnectionArgs) => UpdateDeviceIngenico(deviceConnectionArgs.ConnectionStatus);
        }

        DeviceStatus IDevice.Connect()
        {
            //initialize attached device because application detected USB device change
//            attachedPort = null;

            if (Convert.ToInt32(ConnectToDevice()) == (int)EntryModeStatus.Success)
            {
                DeviceReset();

                InitializeDeviceXORequest();

                SetDeviceXORequest();
                if(!device.IsConnected )
                    ConnectToDevice();
                return DeviceStatus.Connected;
            }
            else
            {
                //todo: determine what to do on connection failure
                if (deviceInfo.SecurityLevel == SecurityLevelNumber.NoEncryption)
                    return DeviceStatus.NoEncryption;
                else
                    return DeviceStatus.NoDevice;
            }
        }

        bool IDevice.ShowMessage(IDeviceMessage deviceMessage, string message)
        {
            return true;
        }

        void IDevice.Disconnect()
        {
            device?.CloseDevice();
        }

        void IDevice.BadRead()
        {
///            Transaction.PaymentXO.Request.ReadAttempts += 1;
///            Transaction.PaymentXO.Request.CreditCard.AbortType = DeviceAbortType.BadRead;
            NotificationRaise(new NotificationEventArgs { NotificationType = NotificationType.DeviceEvent, DeviceEvent = DeviceEvent.CardReadComplete });
        }

        void IDevice.Abort(DeviceAbortType abortType)
        {
            //Transaction.PaymentXO.Request.CreditCard = new CreditCard
            //{
            //    AbortType = abortType,
                
            //};
            //Transaction.PaymentXO.Request.CustomFields = Transaction.PaymentXO.Request.CustomFields ?? string.Empty;
            waitForReply.Set();
            device?.CloseDevice();
            device?.OpenDevice(DeviceMode.Overlapped, DeviceMode.NonOverlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
        }

        void IDevice.ClearBuffer()
        {
            waitForReply.Set();
            if (device?.IsOpen == true)
            {
                device?.CloseDevice();
                device?.OpenDevice(DeviceMode.Overlapped, DeviceMode.NonOverlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
            }
        }
/*
        async Task IDevice.CardRead(string paymentAmount, string promptText, string availableReaders, List<TCCustAttributeItem> tcCustAttributes, Core.Shared.Enums.EntryModeType entryModeType)
        {
            Transaction.PaymentXO.Request.CustomFields = Transaction.PaymentXO.Request.CustomFields ?? string.Empty;

            if (!device.IsConnected)
                return ;

            currentPaymentAmount = paymentAmount;
            //waitForReply.Reset();
            device.ReadReport(OnReport, int.Parse(Transaction.MSRTimer.Interval.ToString()));
            //Wait for the Event Procedure to notify it has the data
            //waitForReply.WaitOne();
            //device.CloseDevice();
            //NotificationRaise(new NotificationEventArgs { NotificationType = NotificationType.DeviceEvent, DeviceEvent = DeviceEvent.CardReadComplete });

            //Now that the card info was captured, return it
            return;
        }
*/
        async Task IDevice.CardRead(string paymentAmount, string promptText)
        {
            if (!device.IsConnected)
                return;

            currentPaymentAmount = paymentAmount;
            //waitForReply.Reset();
            device.ReadReport(OnReport, 10000);//int.Parse(Transaction.MSRTimer.Interval.ToString()));
            //Wait for the Event Procedure to notify it has the data
            waitForReply.WaitOne();
            //device.CloseDevice();
            //NotificationRaise(new NotificationEventArgs { NotificationType = NotificationType.DeviceEvent, DeviceEvent = DeviceEvent.CardReadComplete });
            await Task.FromResult(0);
        }

        void IDevice.Process(DeviceProcess process)
        {

            //TODO: Confirm Process function : can device show messages? are there different reset modes?
            switch (process)
            {
                case DeviceProcess.Approved:

                    DeviceReset();
                    break;

                case DeviceProcess.Declined:

                    DeviceReset();
                    break;

                case DeviceProcess.Reset:

                    DeviceReset();
                    InitializeDeviceXORequest();
                    SetDeviceXORequest();
                    break;

                case DeviceProcess.Canceled:

                    DeviceReset();
                    break;
            }
        }
/*
        Signature IDevice.Signature()
        {
            throw new NotImplementedException();
        }
*/
        bool IDevice.UpdateDevice(DeviceUpdateType updateType)
        {
            if (updateType == DeviceUpdateType.Forms)   //TODO: need to define parameters to an update type for IDTech devices
            {
                var rtn = SetUSBKBMode();
                return rtn.Success;
            }
            else
                return false;
        }

        private IDTSetStatus SetUSBKBMode()
        {
            string SetKBCommand = string.Empty;
            string resetConfigCommand = string.Empty;

            switch (deviceInfo.ModelNumber)
            {
                case DeviceModelNumber.SecureMag:
                    SetKBCommand = SetKBCommandTokens.DTSecureMag;
                    resetConfigCommand = ResetConfigCommand.DTSecureMag;
                    break;
                case DeviceModelNumber.SRedKey:
                case DeviceModelNumber.SecuRED:
                    SetKBCommand = SetKBCommandTokens.DTSRedKey;
                    resetConfigCommand = ResetConfigCommand.DTSRedKey;
                    break;
                default:
                    SetKBCommand = SetKBCommandTokens.DTSecureKey;
                    resetConfigCommand = ResetConfigCommand.DTSecureKey;
                    break;
            }
            var configStatus = SetConfig(SetKBCommand, resetConfigCommand);
            
            return configStatus;
        }
        public virtual string GetSerialNumber()
        {
            string serial = string.Empty;
            //declare variables
            var status = GetCurrentConfig();

            //if it was a successful retrieval, get the security level and Serial Number
            if (status == EntryModeStatus.Success && deviceInfo.ConfigValues[0] == (byte)Token.ACK)
            {
                for (var index = 0; index < deviceInfo.ConfigValues.Length; index++)
                {
                    switch (deviceInfo.ConfigValues[index])
                    {
                        case (byte)FuncID.SerialNumber:
                            // Device serial number starts at 0x4E and followed by a 12-bytes value
                            if (index + 12 < deviceInfo.ConfigValues.Length)
                            {
                                serial = new ASCIIEncoding().GetString(deviceInfo.ConfigValues, index + 3, 10);
                            }
                            break;
                    }
                }
            }
            if(string.IsNullOrEmpty(serial ))
                serial = GetDeviceSerialNumber();
            return serial;
        }
        #endregion

        #region -- private helper methods --

        public void NotificationRaise(NotificationEventArgs e)
        {
///            if (Notifications.LoggingOn && e?.Message != null)
///                IPA.Core.Client.DataAccess.Shared.Logging.LogToFile(new Exception(e.Message), EntryModeStatus.Error, skipElmahLogging: true);

            if (e.NotificationType == NotificationType.DeviceEvent)
            {
                OnNotification?.Invoke(null, e);
            }
        }

        #endregion

        #region -- device integration methods --

        private Enum ConnectToDevice()
        {
            var status = IPAErrorType.NoDevice;

            //Get a count of devices
            var deviceCount = GetDeviceCount();

            if (deviceCount > 0)
            {
                //TODO: Add messaging if there are multiple devices? what is the user action?

                //Select the device for use (currently first device found)
                device = HidDevices.Enumerate(IDTechVendorID).FirstOrDefault();

                if (device != null)
                {
                    bool? isHid = null;
                    try
                    {
                        byte[] resBuffer;
                        var eStatus = PrepareGetCommand(0x23, out resBuffer);
                        if (byteCompare(resBuffer, FeatureResponses.USBHIDResponse, FeatureResponses.USBHIDResponse.Length))
                            isHid = true;
                        else if (byteCompare(resBuffer, FeatureResponses.USBKBResponse, FeatureResponses.USBKBResponse.Length))
                            isHid = false;
                        else
                        {
                            //TODO handle failure to get command...
                        }
                    }
                    catch (Exception xcp)
                    {
                        //TODO Handle Exception
                        throw xcp;
                    }
                    if (isHid != null && isHid != true)
                    {
                        PopulateDeviceInfo();
                        //DeviceReset requires the device be Populated first...
                        DeviceReset();
                        //Reseting device changes the device.DevicePath, so to be safe, just get the entire object as new...
                        device = HidDevices.Enumerate(IDTechVendorID).FirstOrDefault();
                    }
                }
                if (device != null)
                {
                    //Open the device
                    //TODO: vette wheather DeviceMode.NonOverlapped approriate for write
                    device.OpenDevice(DeviceMode.Overlapped, DeviceMode.NonOverlapped, ShareMode.ShareRead | ShareMode.ShareWrite);

                    //Add an event handler
                    device.Removed += DeviceRemovedHandler;

                    //Tell the device to raise events 
                    device.MonitorDeviceEvents = true;

                    //TODO: what is Security level used for?
                    //Default the Security Level ???
                    deviceInfo.SecurityLevel = SecurityLevelNumber.NotChecked;

                    /////////////////////////////////////////////////////////////////////////////////
                    // The order of the following function calls is critical. Please don't alter it.
                    /////////////////////////////////////////////////////////////////////////////////

                    //Get the Device Config Values
                    if (PopulateDeviceInfo())
                    {
                        if (deviceInfo.SecurityLevel == SecurityLevelNumber.NoEncryption)
                        {

                            return EntryModeStatus.Unsupported;
                        }
                        else
                        {
                            return EntryModeStatus.Success;
                        }
                    }
                }
                else
                {
                    //TODO: Raise a message, cannot connect?

                }
            }

            return status;
        }

        private static void SetDeviceXORequest()
        {
            //Data.DeviceXO.Request.Device.AppID = Data.App.AppID;
            //Data.DeviceXO.Request.Device.CompanyID = Data.App.CompanyID;
            //Data.DeviceXO.Request.Device.Debit = false;
            //Data.DeviceXO.Request.Device.P2PEEnabled = false;
            //Data.DeviceXO.Request.Device.FirmwareVersion = deviceInfo.FirmwareVersion ;
            //Data.DeviceXO.Request.Device.Manufacturer.MfgCode = DeviceManufacturer.IDTech.ToString();
            //Data.DeviceXO.Request.Device.Model.ModelNumber = deviceInfo.ModelNumber;
            //Data.DeviceXO.Request.Device.Model.DefaultInterfacePort = deviceInfo.Port;
            //Data.DeviceXO.Request.Device.SerialNumber = deviceInfo.SerialNumber;
            //Data.DeviceXO.Request.Device.CreatedBy = Environment.UserName;
            //Data.DeviceXO.Request.Device.CreatedDate = DateTime.UtcNow;
            //Data.DeviceXO.Request.Device.UpdatedBy = Environment.UserName;
            //Data.DeviceXO.Request.Device.UpdatedDate = DateTime.UtcNow;
        }

        private static void InitializeDeviceXORequest()
        {
            //if (Data.DeviceXO.Request == null)
            //    Data.DeviceXO.Request = new Core.XO.Request();

            //if (Data.DeviceXO.Request.Device == null)
            //    Data.DeviceXO.Request.Device = new Core.Data.Entity.Device();

            //if (Data.DeviceXO.Request.Device.Manufacturer == null)
            //    Data.DeviceXO.Request.Device.Manufacturer = new Manufacturer();

            //if (Data.DeviceXO.Request.Device.Model == null)
            //    Data.DeviceXO.Request.Device.Model = new Model();
        }

        private EntryModeStatus GetCurrentConfig()
        {
            byte[] result;

            //Create the command to get config values
            var readConfig = new byte[CommandTokens.ReadConfiguration.Length + 1];
            Array.Copy(CommandTokens.ReadConfiguration, readConfig, CommandTokens.ReadConfiguration.Length);
            readConfig[CommandTokens.ReadConfiguration.Length] = 0x00;
            readConfig[readConfig.Length - 1] = GetCheckSumValue(readConfig);

            //execute the command, get the result
            var status = SetupCommand(readConfig, out result);
            deviceInfo.ConfigValues = result;

            return status;
        }

        private bool PopulateDeviceInfo()
        {
            //declare variables
            var status = GetCurrentConfig();

            //if it was a successful retrieval, get the security level and Serial Number
            if (status == EntryModeStatus.Success && deviceInfo.ConfigValues[0] == (byte)Token.ACK)
            {
                for (var index = 0; index < deviceInfo.ConfigValues.Length; index++)
                {
                    switch (deviceInfo.ConfigValues[index])
                    {
                        case (byte)FuncID.SecurityLevel:
                            deviceInfo.SecurityLevel = GetSecurityLevel(deviceInfo.ConfigValues, index);
                            break;

                        case (byte)FuncID.SerialNumber:
                            // Device serial number starts at 0x4E and followed by a 12-bytes value
                            if (index + 12 < deviceInfo.ConfigValues.Length)
                            {
                                // Find out the end of Serial Number indicator, which is either ETK or a FuncID 
                                int endIndex = 0;
                                while (endIndex <= 12)
                                {
                                    if (deviceInfo.ConfigValues[index + endIndex] == (byte)Token.ETK || deviceInfo.ConfigValues[index + endIndex] == (byte)FuncID.DeviceFormat)
                                        break;
                                    else
                                        endIndex++;
                                }

                                deviceInfo.SerialNumber = new ASCIIEncoding().GetString(deviceInfo.ConfigValues, index + 3, endIndex - 3);
                            }
                            break;
                    }
                }

                //Get the Device Serial Number
                if (string.IsNullOrEmpty(deviceInfo.SerialNumber))
                {
                    deviceInfo.SerialNumber = GetDeviceSerialNumber();
                }

                //Get the Device Firmware Version / Model
                var firmwareModelInfo = GetFirmwareVersion();

                if (!String.IsNullOrWhiteSpace(firmwareModelInfo))
                {
                    deviceInfo.FirmwareVersion = ParseFirmwareVersion(firmwareModelInfo);
                    deviceInfo.ModelName = firmwareModelInfo.Substring(2, firmwareModelInfo.IndexOf("USB", StringComparison.Ordinal) - 3);
                    deviceInfo.Port = firmwareModelInfo.Substring(firmwareModelInfo.IndexOf("USB", StringComparison.Ordinal), 7);
                    // MAGSTRIP DEVICE EMBBEDS THE TYPE AS: "USB HID KB Reader"
                    if(firmwareModelInfo.Contains("USB HID KB Reader"))
                    {
                        deviceInfo.Port = "USB KB";
                    }

                    //Get the device model #
                    deviceInfo.ModelNumber = GetModelNumber(deviceInfo.ModelName, deviceInfo.ConfigValues, double.Parse(deviceInfo.FirmwareVersion));

                    return true;
                }
                return false;
            }
            else
                return false;
        }

        private IDTSetStatus DeviceReset()
        {
            var configStatus = new IDTSetStatus { Success = true };
            if(deviceMode == IDTECH_DEVICE_PID.AUGUSTA_KYB   ||
               deviceMode == IDTECH_DEVICE_PID.MAGSTRIPE_KYB ||
               deviceMode == IDTECH_DEVICE_PID.SECUREKEY_KYB ||
               deviceMode == IDTECH_DEVICE_PID.SECUREMAG_KYB)
            {
                // WIP: no resets for these device types
                return configStatus;
            }
            string resetConfigCommand;
            string mustRunCommand;

            //TODO: figure what this value is, how it is set, comes from clientconfig.json?
            // sent a team email on 7/25/2017 to identify the use of this field
            var configCommand = string.Empty;

            //run command from config
            switch (deviceInfo.ModelNumber)
            {

                case DeviceModelNumber.SecureMag:
                    resetConfigCommand = ResetConfigCommand.DTSecureMag;
                    mustRunCommand = MustRunCommandTokens.DTSecureMag;
                    //configCommand = _config.DeviceSettingsIDTSecureMag;

                    break;
                case DeviceModelNumber.SRedKey:
                case DeviceModelNumber.SecuRED:
                    resetConfigCommand = ResetConfigCommand.DTSRedKey;
                    mustRunCommand = MustRunCommandTokens.DTSRedKey;
                    //configCommand = _config.DeviceSettingsIDTSRedKey;
                    break;
                default:
                    resetConfigCommand = ResetConfigCommand.DTSecureKey;
                    mustRunCommand = MustRunCommandTokens.DTSecureKey;
                    //configCommand = _config.DeviceSettingsIDTSecureKey;
                    break;
            }

            if (!string.IsNullOrEmpty(mustRunCommand))
                configStatus = SetConfig(mustRunCommand, resetConfigCommand);

            if (configStatus.Success && !string.IsNullOrEmpty(configCommand))
            {
                configStatus = SetConfig(configCommand, resetConfigCommand);
            }

            return configStatus;
        }

        #region -- device send / receive methods --

        private EntryModeStatus PrepareGetCommand(byte inputToken, out byte[] output)
        {
            var commandLine = new byte[5];
            commandLine[0] = (byte)Token.STK;
            commandLine[1] = (byte)Token.R;
            commandLine[2] = inputToken;
            commandLine[3] = (byte)Token.ETK;
            commandLine[4] = 0x00;
            commandLine[4] = GetCheckSumValue(commandLine);

            return SetupCommand(commandLine, out output);
        }

        private EntryModeStatus SetupCommand(byte[] command, out byte[] response)
        {
            var status = EntryModeStatus.Success;
            const int bufferLength = 1000;
            var deviceDataBuffer = new byte[bufferLength];
            response = null;

            try
            {
                for (int i = 0; i < bufferLength; i++)
                {
                    deviceDataBuffer[i] = 0;
                }

                if (status == EntryModeStatus.Success)
                {
                    int featureReportLen = device.Capabilities.FeatureReportByteLength;
                    //WriteFeatureData works better if we send the entire feature length array, not just the length of command plus checksum
                    var reportBuffer = new byte[featureReportLen];
                    //Assume featureCommand is not 0 prepended, and contains a checksum.
                    var zeroReportIdCommand = new byte[Math.Max(command.Length + 2, featureReportLen)];
                    //Prepend 0x00 to command[...] since HidLibrary expects Features to start with reportID, and we use 0.
                    zeroReportIdCommand[0] = 0x00;
                    Array.Copy(command, 0, zeroReportIdCommand, 1, command.Length);

                    var result = false;
                    if (device.WriteFeatureData(zeroReportIdCommand))
                    {
                        Thread.Sleep(1200); //Emperical data shows this is a good time to wait.
                        result = ReadFeatureDataLong(out deviceDataBuffer);
                    }


                    if (result || deviceDataBuffer.Length > 0)//as long as we have data in result, we are ok with failed reading later.
                    {
                        int dataIndex;
                        for (dataIndex = bufferLength - 1; dataIndex > 1; dataIndex--)
                        {
                            if (deviceDataBuffer[dataIndex] != 0)
                                break;
                        }

                        response = new byte[dataIndex + 1];

                        for (var ind = 0; ind <= dataIndex; ind++)
                        {
                            response[ind] += deviceDataBuffer[ind];
                        }

                        status = EntryModeStatus.Success;
                    }
                    else
                        status = EntryModeStatus.CardNotRead;
                }
            }
            catch (Exception)
            {
                status = EntryModeStatus.Error;
            }

            return status;
        }

        #endregion

        #region -- device helper methods --

        private static bool byteCompare(byte[] left, byte[] right, int length)
        {
            bool same = true;
            if (left.Length < length || right.Length < length)
                same = false;
            else
            {
                for (int i = 0; i < length; i++)
                {
                    if (left[i] != right[i])
                    {
                        same = false;
                        break;
                    }
                }
            }
            return same;
        }

        //It is not uncommon for Features to be longer than Feature Report on IDTech Devices.
        //The following loops through reads to acquire the entire Feature output.
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
                            reportBuffer[indx] = 0;

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

        public virtual string ParseFirmwareVersion(string firmwareInfo)
        {
            bool AUGUSTA_DEVICE = (deviceMode == IDTECH_DEVICE_PID.AUGUSTA_HID || deviceMode == IDTECH_DEVICE_PID.AUGUSTA_KYB);
            // Validate the format firmwareInfo see if the version # exists
            var version = firmwareInfo.Substring(firmwareInfo.IndexOf('V') + ((AUGUSTA_DEVICE) ? 1 : 2),
                firmwareInfo.Length - firmwareInfo.IndexOf('V') - 3);
            var mReg = Regex.Match(version, @"[0-9]+\.[0-9]+");

            //If the pars succeeded 
            if (mReg.Success)
            {
                version = mReg.Value;
            }

            return version;
        }

        public static int GetDeviceCount()
        {
            var devices = HidDevices.Enumerate(IDTechVendorID);
            return devices.Count();
        }

        public string GetDeviceSerialNumber()
        {
            //declare variables
            string serialNumber = null;
            byte[] result;

            //setup command to get the serial number
            var getSerialNumber = new byte[CommandTokens.GetSerialNumber.Length + 1];
            Array.Copy(CommandTokens.GetSerialNumber, getSerialNumber, CommandTokens.GetSerialNumber.Length);
            getSerialNumber[CommandTokens.ReadConfiguration.Length] = 0x00;
            getSerialNumber[getSerialNumber.Length - 1] = GetCheckSumValue(getSerialNumber);

            //issue the call to run the command
            var status = SetupCommand(getSerialNumber, out result);

            //if the call was successful, get the serial number
            if (status == EntryModeStatus.Success && result[0] == (byte)Token.ACK)
            {
                // Find out the end of Serial Number indicator - ETK
                int endIndex = 0;
                for (var index = 0; index < result.Length - 1; index++)
                {
                    if (result[index] == (byte)Token.ETK)
                        endIndex = index;
                }
       
                serialNumber = new ASCIIEncoding().GetString(result, 5, endIndex - 5);
            }

            return serialNumber;
        }

        public virtual DeviceInfo GetDeviceInfo()
        {
            return deviceInfo;
        }

        public virtual string GetFirmwareVersion()
        {
            // declare variables
            string firmwareVersion = null;
            byte[] result;

            // setup the command to get the firmware version
            var getFirmware = new byte[CommandTokens.ReadFirmwareVersion.Length + 1];
            Array.Copy(CommandTokens.ReadFirmwareVersion, getFirmware, CommandTokens.ReadFirmwareVersion.Length);
            getFirmware[CommandTokens.ReadFirmwareVersion.Length] = 0x00;
            getFirmware[getFirmware.Length - 1] = GetCheckSumValue(getFirmware);

            //execute the command
            var status = SetupCommand(getFirmware, out result);
            if (status == EntryModeStatus.Success && result[0] == (byte)Token.ACK)
            {
                firmwareVersion = Encoding.ASCII.GetString(result);
            }

            return firmwareVersion;
        }

        private string GetModelNumber(string modelType, byte[] configValues, double versionNum)
        {
            // declare variables
            string model = null;

            switch (modelType.Trim())
            {
                default:
                case DeviceModelType.SecureKey:
                    byte configFormat = 0;

                    var currentConfigDeviceFormatIndex = Array.IndexOf(configValues, (byte)FuncID.DeviceFormat);
                    if (currentConfigDeviceFormatIndex > -1)
                        configFormat = configValues[currentConfigDeviceFormatIndex + 2];
                    else
                    {
                        byte[] buffer;
                        var status = PrepareGetCommand((byte)FuncID.DeviceFormat, out buffer);
                        if (status == EntryModeStatus.Success && buffer[0] == (byte)Token.ACK)
                            configFormat = buffer[4];
                    }

                    switch (configFormat)
                    {
                        case (byte)SecureKeyModelFormat.M100IDT:
                            model = DeviceModelNumber.SecureKeyM100Enhanced;
                            break;
                        case (byte)SecureKeyModelFormat.M100XML:
                            model = DeviceModelNumber.SecureKeyM100Xml;
                            break;
                        case (byte)SecureKeyModelFormat.M130IDT:
                            model = versionNum >= DeviceVersion.V130 ? DeviceModelNumber.SecureKeyM130NewFormat : DeviceModelNumber.SecureKeyM130Enhanced;
                            break;
                        case (byte)SecureKeyModelFormat.M130XML:
                            model = DeviceModelNumber.SecureKeyM130Xml;
                            break;
                        default:
                            model = DeviceModelNumber.SecureKeyM130Enhanced;
                            break;
                    }
                    break;
                case DeviceModelType.SecureMag:
                    model = DeviceModelNumber.SecureMag;
                    break;
                case DeviceModelType.SRedKey:
                    model = DeviceModelNumber.SRedKey;
                    break;
                case DeviceModelType.SecuRED:
                    model = DeviceModelNumber.SecuRED;
                    break;
                case DeviceModelType.Augusta:
                    model = DeviceModelNumber.AugustKYB;
                    break;
            }

            return model;
        }

        private static byte GetCheckSumValue(byte[] dataBytes)
        {
            return dataBytes.Aggregate<byte, byte>(0x0, (current, t) => (byte)(current ^ t));
        }

        private SecurityLevelNumber GetSecurityLevel(byte[] configValues, int index)
        {
            //Declare variables
            byte securityID = 0;
            var securityLevelNumber = SecurityLevelNumber.NotChecked;

            //check the config values for a securityid
            if (index + 2 <= configValues.Length)
                securityID = configValues[index + 2];
            else
            {
                //if it is not in the config values, get it directly from the device
                byte[] securityResult = null;
                var status = PrepareGetCommand((byte)FuncID.SecurityLevel, out securityResult);

                if (status == EntryModeStatus.Success && securityResult[0] == (byte)Token.ACK)
                    securityID = securityResult[4];
            }

            //check the securityid to see what kind of security it is
            switch (securityID)
            {
                case (byte)SecurityLevelID.AuthenticationRequired:
                    securityLevelNumber = SecurityLevelNumber.AuthenticationRequired;
                    break;
                case (byte)SecurityLevelID.DUKPTExhausted:
                    securityLevelNumber = SecurityLevelNumber.DUKPTExhausted;
                    break;
                case (byte)SecurityLevelID.EncryptedReader:
                    securityLevelNumber = SecurityLevelNumber.EncryptedReader;
                    break;
                case (byte)SecurityLevelID.KeyLoaded:
                    securityLevelNumber = SecurityLevelNumber.KeyLoaded;
                    break;
                case (byte)SecurityLevelID.NoEncryption:
                default:
                    securityLevelNumber = SecurityLevelNumber.NoEncryption;
                    break;
            }

            return securityLevelNumber;
        }
        
        public IDTSetStatus SetConfig(string configCommands, string resetConfigCommands)
        {
            EntryModeStatus status = EntryModeStatus.Unsupported;

            IDTSetStatus setStatus = new IDTSetStatus();
            //byte[] currentConfig = deviceInfo.ConfigValues;
            try
            {
                if (String.IsNullOrEmpty(configCommands))
                {
                    setStatus.ErrorMsg = "Config command is an empty string";
                    return setStatus;
                }
                
                string[] commandTypes = configCommands.Split('|');
                for (int indx = 0; indx < commandTypes.Length; indx++)
                {
                    string[] commands = commandTypes[indx].Split(',');
                    switch (indx)
                    {
                        case 0:
                        case 2:
                            // First part of the commands are required - unrecoverable error, i.e. stop the process if any error returned

                            foreach (string comm in commands)
                            {
                                setStatus = new IDTSetStatus();                               
                               var config = ByteArrayToHexString(deviceInfo.ConfigValues);
                                if (!config.Contains(comm) && !String.IsNullOrEmpty(comm))//the device need to set this config after this command ran.
                                {

                                    byte result = PrepareSetCommand(comm);
                                    if (status == EntryModeStatus.Success && result == (byte)Token.ACK)
                                    {
                                        setStatus.Success = true;
                                        if (resetConfigCommands.Contains(comm))//This command reset other configs. so, we need to get the config from the device after this command
                                        {
                                            //TODO: Refresh Config Values from the device? it is already loaded
                                            setStatus.CurrentConfig = deviceInfo.ConfigValues;
                                        }
                                    }
                                    else
                                    {
                                        setStatus.Success = result == (byte)Token.ACK ? true : false;
                                        setStatus.ErrorMsg = $"Return code on Command {comm} is " + (result > 0 ? result.ToString("X") : "null");
                                        setStatus.CurrentConfig = deviceInfo.ConfigValues;
                                        setStatus.RequestedConfig = configCommands;
                                        break;
                                    }
                                    if (indx == 2)
                                    {
                                        //TODO: CVarry over comment from 4.2.x : Create method to detect if the device ready after reboot, instead of sleep 10 seconds.
                                        Thread.Sleep(10000);//we need to wait for device to reboot.

                                        //TODO: what case gets the code here? What should happen? re-init device?
                                        //Init();
                                        Thread.Sleep(5000);//we need to wait for device to reboot.
                                    }
                                }
                                else
                                {
                                    setStatus.Success = true;
                                }
                            }
                            if (!setStatus.Success)
                                return setStatus;
                            break;
                        case 1:
                            // Second part of the commands are not required so errors returned will be ignored
                            setStatus = new IDTSetStatus();

                            foreach (string comm in commands)
                            {
                                var config = ByteArrayToHexString(deviceInfo.ConfigValues);
                                if (!config.Contains(comm) && !String.IsNullOrEmpty(comm))//the device need to set this config
                                {
                                    byte result = PrepareSetCommand(comm);
                                    if (resetConfigCommands.Contains(comm))//This command reset other configs. so, we need to get the config from the device after this command
                                    {
                                        status = GetCurrentConfig();
                                    }
                                }
                                else
                                {
                                    setStatus = new IDTSetStatus();
                                    setStatus.Success = true;
                                }
                            }
                            status = EntryModeStatus.Success;
                            setStatus.Success = status == EntryModeStatus.Success ? true : false;
                            break;

                    }
                }

                if (deviceInfo.ConfigValues == null)
                    status = GetCurrentConfig();

                setStatus.CurrentConfig = deviceInfo.ConfigValues;
            }
            catch (Exception exc)
            {
                setStatus.Success = false;
                setStatus.ErrorMsg = exc.StackTrace + exc.Message;
                setStatus.CurrentConfig = deviceInfo.ConfigValues;
                setStatus.RequestedConfig = configCommands;
            }
            return setStatus;
        }

        private byte PrepareSetCommand(string inputTokens)
        {
            byte[] commandLine = new byte[inputTokens.Length / 2 + 4];
            commandLine[0] = (byte)Token.STK;
            commandLine[1] = (byte)Token.S;
            int indx = 2;
            for (int i = 0; i < inputTokens.Length; i += 2)
            {
                commandLine[indx] = Convert.ToByte(inputTokens.Substring(i, 2), 16);
                indx++;
            }
            commandLine[indx] = (byte)Token.ETK;
            commandLine[indx + 1] = 0x00;
            commandLine[indx + 1] = GetCheckSumValue(commandLine);

            byte[] result;
            var status = SetupCommand(commandLine, out result);
            if (result != null)
                return result[0];
            else
                return 0;
        }

        public static TrackData ParseXmlFormat(byte[] bytes)
        {
            var test = Encoding.ASCII.GetString(bytes);
            var testdata = Encoding.ASCII.GetBytes(test);

            var osDvcMsgEnd = search(bytes, Encoding.ASCII.GetBytes("</DvcMsg>"), 0);
            var osCard = search(bytes, Encoding.ASCII.GetBytes("<Card "), 0);
            var osCardEnd = search(bytes, Encoding.ASCII.GetBytes("></Card"), osCard);
            bool osIsSwipe = true;
            int osETrk1 = 0;
            int osETrk1End = 0;
            int osETrk2 = 0;
            int osETrk2End = 0;
            int osECData = 0;
            int osECDataEnd = 0;
            int cardEntry = search(bytes, Encoding.ASCII.GetBytes("Entry=\"MANUAL\""), 0);
            if (cardEntry > 0)//manual
            {
                osIsSwipe = false;
                osECData = search(bytes, Encoding.ASCII.GetBytes("ECData=\""), osCard);
                if (osECData < 0)
                    osECData = 0;
                osECDataEnd = search(bytes, Encoding.ASCII.GetBytes("\" CDataKSN=\""), osECData);
            }
            else
            {
                try
                {
                    osETrk1 = search(bytes, Encoding.ASCII.GetBytes("ETrk1=\""), osCard);
                    if (osETrk1 < 0)
                        osETrk1 = 0;
                    osETrk1End = search(bytes, Encoding.ASCII.GetBytes("\" ETrk2=\""), osETrk1);
                }
                catch (Exception)
                {

                }
                osETrk2 = search(bytes, Encoding.ASCII.GetBytes("ETrk2=\""), osETrk1End);
                osETrk2End = search(bytes, Encoding.ASCII.GetBytes("\" CDataKSN=\""), osETrk2);

            }


            var osKsn = search(bytes, Encoding.ASCII.GetBytes("CDataKSN=\""), osETrk2);
            var osKsnEnd = search(bytes, Encoding.ASCII.GetBytes("\" Exp=\""), osKsn);

            const int MaxResponseLength = 666;
            var data = new StringBuilder(MaxResponseLength);
            if (osETrk1 > 0)
            {
                data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, 0, osETrk1 + 7)));
                data.Append(ByteArrayToHexString(((SubArray<byte>(bytes, osETrk1 + 7, osETrk1End - osETrk1 - 7)))));
                data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, osETrk1End, osETrk2 - osETrk1End + 7)));
            }
            if (osETrk2 > 0)
            {
                data.Append(ByteArrayToHexString(SubArray<byte>(bytes, osETrk2 + 7, osETrk2End - osETrk2 - 7)));
                data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, osETrk2End, osKsn - osETrk2End + 10)));
            }
            if (osECData > 0) //manual input
            {
                data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, 0, osECData + 8)));
                data.Append(ByteArrayToHexString(((SubArray<byte>(bytes, osECData + 8, osECDataEnd - osECData - 8)))));
                data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, osECDataEnd, osKsn - osECDataEnd + 10)));
            }

            data.Append(ByteArrayToHexString(SubArray<byte>(bytes, osKsn + 10, osKsnEnd - osKsn - 10)));
            data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, osKsnEnd, osDvcMsgEnd - osKsnEnd + 9)));
            var dvcMsg = data.ToString();
            dvcMsg = dvcMsg.Replace("\0", " ");//remove the "\0" null value.
            XmlSerializer serializer = new XmlSerializer(typeof(DvcMsg));
            StringReader rdr = new StringReader(dvcMsg);
            var obj = (DvcMsg)serializer.Deserialize(rdr);

            var trackData = new TrackData()
            {
                IsDebit = false,
                T1Data = obj.Card.ETrk1,
                T2Data = obj.Card.ETrk2,
                Name = obj.Card.CHolder ?? string.Empty,
                ExpDate = obj.Card.Exp.ToString(),
                PAN = obj.Card.MskPAN,
                EncryptedTracks = "",
                T1Crypto = obj.Card.ETrk1,
                T2Crypto = obj.Card.ETrk2,
                Ksn = obj.Card.CDataKSN,
                IsSwipe = osIsSwipe
            };

            //trackData.SerialNumber = obj.Dvc.DvcSN.ToString();

            //trackData.IsSwipe = true;

            //Matthew: use ingenigo format for IDT data 
            if (String.IsNullOrWhiteSpace(trackData.T1Crypto) && String.IsNullOrWhiteSpace(trackData.T2Crypto))
            {
                trackData.IsSwipe = false;
                trackData.PAN = trackData.PAN.Replace('*', '0');
                trackData.T1Data = $"";
                trackData.T2Data = $"";
                trackData.T3Data = $"";
            }
            else if (String.IsNullOrWhiteSpace(trackData.T2Data))
            {
                trackData.T3Data = $"{trackData.Ksn}:1:{trackData.T1Crypto.Length / 2:D4}:{trackData.T1Crypto}";
            }
            else
            {
                trackData.T3Data = $"{trackData.Ksn}:2:{trackData.T2Crypto.Length / 2:D4}:{trackData.T2Crypto}";
                trackData.T1Data = string.Empty;
            }
            if (trackData.IsSwipe)
                trackData.EncryptedTracks = $"{trackData.T1Data}|{trackData.T2Data}|{trackData.T3Data}";
            else
            {
                trackData.EncryptedTracks = dvcMsg;
            }

            return trackData;
        }

        public static TrackData ParseIdtFormat(byte[] bytes)
        {
            var test = ByteArrayToHexString(bytes);

            var testdata = HexStringToByteArray(test);
            //get length data
            var lenData = BitConverter.ToInt16(new byte[] { bytes[osLenDataL], bytes[osLenDataH] }, 0);
            var hexlenData = bytes.Length - 6;
            //validate data

            var data = SubArray<byte>(bytes, 3, lenData);
            if (bytes[lenData + 3] != GetLrc(data) ||
                bytes[lenData + 4] != GetChecksumAdd(data) ||
                bytes[osStx] != StxValid ||
                bytes[lenData + 5] != EtxValid)
                throw new Exception("invalid card read");

            // get the actual idtech data
            string idtData = test.Substring(0, (lenData + 6) * 2);

            //// get the actual idtech data
            //string idtData = test.Substring(0, (lenData +6)*2);
            //get card encode type and manual
            var cardEncodeType = (CardEncodeType)bytes[osCardEncodeType];
            var isEnhancedFormat = cardEncodeType == CardEncodeType.AAMVA_Enhanced || cardEncodeType == CardEncodeType.ISOABA_Enhanced || cardEncodeType == CardEncodeType.manual_Enhanced || cardEncodeType == CardEncodeType.Other_Enhanced || cardEncodeType == CardEncodeType.Raw_Enhanced;  //isEnhancedFormat==true
            var isManual = cardEncodeType == CardEncodeType.manual || cardEncodeType == CardEncodeType.manual_Enhanced;

            TrackData trackData;
            if (cardEncodeType == CardEncodeType.manual)
            {
                var trackStatus = bytes[8];//should be 04
                var lenUnEncryptData = bytes[9];//length of unencrypted data (PAN=EXP= from the status bits)  - 16 (22 dec)                                               
                int lenEncryptData = lenUnEncryptData % 8 > 0 ? (lenUnEncryptData / 8 + 1) * 8 : lenUnEncryptData; //Note: encrypted data length is len of unencrypted data rounded up to a multiple of 8 bytes(64 - bits) – I think (please test with other lengths of PAN data)
                var encryptedData = ByteArrayToHexString(SubArray<byte>(bytes, 10, lenEncryptData));
                var hashData = ByteArrayToHexString(SubArray<byte>(bytes, 10 + lenEncryptData, 20));
                var expLenStr = ByteArrayToHexString(SubArray<byte>(bytes, 10 + lenEncryptData + 20, 1));
                int expLen = Int32.Parse(expLenStr.TrimStart('0'));
                var exp = Encoding.ASCII.GetString(SubArray<byte>(bytes, 10 + lenEncryptData + 20 + 1, expLen));
                var ksn = ByteArrayToHexString(SubArray<byte>(bytes, 10 + lenEncryptData + 20 + 1 + expLen, 10));
                string track2Data = new string('*', lenUnEncryptData - expLen - 3);
                track2Data = $";{track2Data}={exp}=";

                trackData = new TrackData()
                {
                    T1Data = "",
                    T2Data = "",
                    T3Data = "",

                    T1Crypto = "",
                    T2Crypto = "",
                    T3Crypto = "",

                    T1Hash = "",
                    T2Hash = "",
                    T3Hash = "",

                    SerialNumber = "",
                    Ksn = ksn,

                    //EncryptedTracks = ConvertIDTByteArrayToString(bytes),
                    EncryptedTracks = idtData,
                    IsSwipe = !isManual
                };

                trackData.PAN = "";
                trackData.Name = "";
                trackData.ExpDate = exp;
                trackData.T3Data = encryptedData;

            }
            else if (isEnhancedFormat)
            {
                var trackStatus = (TrackStatus)bytes[osTrackStatus];
                var maskStatus = (MaskStatus)bytes[osMaskStatus];
                var dataStatus = (CryptoStatus)bytes[osDataStatus];
                var isAesCrypto = maskStatus.HasFlag(MaskStatus.AesEncryption);
                var cryptoMultiple = (isAesCrypto ? 16 : 8);

                var serialNumberLen = maskStatus.HasFlag(MaskStatus.SerialNumberPresent) ? lenSerialNumber : 0;
                var ksnLen = dataStatus.HasFlag(CryptoStatus.KsnPresent) ? lenKsn : 0;

                var lenT1 = bytes[osLenT1];
                var lenT2 = bytes[osLenT2];
                var lenT3 = bytes[osLenT3];
                var t1DataLen = trackStatus.HasFlag(TrackStatus.T1Sampling) ? lenT1 : 0;
                var t2DataLen = trackStatus.HasFlag(TrackStatus.T2Sampling) ? lenT2 : 0;
                var t3DataLen = trackStatus.HasFlag(TrackStatus.T3Sampling) ? lenT3 : 0;

                var lenT1CryptoPad = ((lenT1 % cryptoMultiple) > 0 ? cryptoMultiple : 0) - (lenT1 % cryptoMultiple);
                var lenT2CryptoPad = ((lenT2 % cryptoMultiple) > 0 ? cryptoMultiple : 0) - (lenT2 % cryptoMultiple);
                var lenT3CryptoPad = ((lenT3 % cryptoMultiple) > 0 ? cryptoMultiple : 0) - -(lenT3 % cryptoMultiple);
                var t1CryptoLen = dataStatus.HasFlag(CryptoStatus.T1Encrypted) ? lenT1 + lenT1CryptoPad : 0;
                var t2CryptoLen = dataStatus.HasFlag(CryptoStatus.T2Encrypted) ? lenT2 + lenT2CryptoPad : 0;
                var t3CryptoLen = dataStatus.HasFlag(CryptoStatus.T3Encrypted) ? lenT3 + lenT3CryptoPad : 0;
                var t1HashLen = dataStatus.HasFlag(CryptoStatus.T1Hash) ? lenHash : 0;
                var t2HashLen = dataStatus.HasFlag(CryptoStatus.T2Hash) ? lenHash : 0;
                var t3HashLen = dataStatus.HasFlag(CryptoStatus.T3Hash) ? lenHash : 0;


                var osT1Data = 10;
                var osT2Data = osT1Data + t1DataLen;
                var osT3Data = osT2Data + t2DataLen;
                var osT1Crypto = osT3Data + t3DataLen;
                var osT2Crypto = osT1Crypto + t1CryptoLen;
                var osT3Crypto = osT2Crypto + t2CryptoLen;
                var osT1Hash = osT3Crypto + t3CryptoLen;
                var osT2Hash = osT1Hash + t1HashLen;
                var osT3Hash = osT2Hash + t2HashLen;

                var osSerialNumber = osT3Hash + t3HashLen;
                var osKsn = osSerialNumber + serialNumberLen;
                var osLrc = osKsn + ksnLen;
                var osCheckSum = osLrc + 1;
                var osEtx = osCheckSum + 1;

                trackData = new TrackData()
                {
                    T1Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, osT1Data, t1DataLen)),
                    T2Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, osT2Data, t2DataLen)),
                    T3Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, osT3Data, t3DataLen)),

                    T1Crypto = ByteArrayToHexString(SubArray<byte>(bytes, osT1Crypto, t1CryptoLen)),
                    T2Crypto = ByteArrayToHexString(SubArray<byte>(bytes, osT2Crypto, t2CryptoLen)),
                    T3Crypto = ByteArrayToHexString(SubArray<byte>(bytes, osT3Crypto, t3CryptoLen)),

                    T1Hash = ByteArrayToHexString(SubArray<byte>(bytes, osT1Hash, t1HashLen)),
                    T2Hash = ByteArrayToHexString(SubArray<byte>(bytes, osT2Hash, t2HashLen)),
                    T3Hash = ByteArrayToHexString(SubArray<byte>(bytes, osT3Hash, t3HashLen)),

                    SerialNumber = ByteArrayToHexString(SubArray<byte>(bytes, osSerialNumber, serialNumberLen)),
                    Ksn = ByteArrayToHexString(SubArray<byte>(bytes, osKsn, ksnLen)),

                    EncryptedTracks = ConvertIDTByteArrayToString(bytes),
                    IsSwipe = !isManual
                };

                var track1Values = GetTrack1(trackData.T1Data);
                trackData.PAN = track1Values.PAN;
                trackData.Name = track1Values.Name ?? track1Values.Name;
                trackData.ExpDate = track1Values.ExpDate;

                //debit card has not track1 data
                if (trackData.T1Data == string.Empty)
                {
                    string track1 = trackData.T2Data;
                    track1 = track1.Replace(";", "%%");
                    track1 = track1.Replace('*', '0');
                    track1 = track1.Replace('?', '0');
                    track1 = track1.Replace("=", "^MANUALLY/ENTERED^");
                    track1Values = GetTrack1(track1);
                    trackData.PAN = track1Values.PAN;
                    trackData.Name = "";
                    trackData.ExpDate = track1Values.ExpDate;
                }

                //Matthew: use ingenigo format for IDT data 
                if (String.IsNullOrWhiteSpace(trackData.T2Data) && String.IsNullOrWhiteSpace(trackData.T3Data))
                {
                    trackData.T3Data = $"{trackData.Ksn}:1:{trackData.T1Crypto.Length / 2:D4}:{trackData.T1Crypto}";
                }
                else if (String.IsNullOrWhiteSpace(trackData.T3Data) && !String.IsNullOrWhiteSpace(trackData.T2Crypto))
                {
                    trackData.T3Data = $"{trackData.Ksn}:2:{trackData.T2Crypto.Length / 2:D4}:{trackData.T2Crypto}";
                    trackData.T1Data = string.Empty;
                }
                if (isManual)
                {
                    string firstHalf = ByteArrayToHexString(SubArray<byte>(bytes, 3, osT2Data - 3)) + Encoding.ASCII.GetString(SubArray<byte>(bytes, osT2Data, t2DataLen));
                    string newData = firstHalf + idtData.Substring(firstHalf.Length + t2DataLen + 6);
                    int dataLength = newData.Length - 6;
                    string dataLengthHex = dataLength.ToString("X");

                    newData = newData.Substring(0, dataLength);

                    //var newDataHex =HexStringToByteArray(newData);
                    long LRC = 0;
                    long ChkSum = 0;
                    for (int i = 0; i < newData.Length; i++)
                    {
                        long l = Convert.ToInt64(newData[i]);
                        LRC ^= l;
                        ChkSum += l;
                    }
                    byte[] LRCArray = BitConverter.GetBytes(LRC);
                    byte[] ChkSumArray = BitConverter.GetBytes(ChkSum);
                    byte[] ending = new byte[3];
                    ending[0] = LRCArray[0];
                    ending[1] = ChkSumArray[0];
                    ending[2] = EtxValid;
                    newData = "02" + dataLengthHex + "00" + newData + ByteArrayToHexString(ending);
                    trackData.EncryptedTracks = newData;
                }
                else
                {
                    trackData.EncryptedTracks = $"{trackData.T1Data}|{trackData.T2Data}|{trackData.T3Data}";
                }


            }
            else if (cardEncodeType == CardEncodeType.ISOABA)//original format
            {
                var trackStatus = bytes[4];//should be 04
                var lenUnEncryptTrack1 = bytes[5];//length of unencrypted data (PAN=EXP= from the status bits)  - 16 (22 dec)    
                var lenUnEncryptTrack2 = bytes[6];
                var lenUnEncryptTrack3 = bytes[7];
                int indexUnEcryptTrack1 = 8;
                int indexUnEcryptTrack2 = indexUnEcryptTrack1 + lenUnEncryptTrack1;
                int indexUnEcryptTrack3 = indexUnEcryptTrack2 + lenUnEncryptTrack2;

                int lenEncryptTrack = (lenUnEncryptTrack1 + lenUnEncryptTrack2 + lenUnEncryptTrack3) % 8 > 0 ?
                    ((lenUnEncryptTrack1 + lenUnEncryptTrack2 + lenUnEncryptTrack3) / 8 + 1) * 8 : (lenUnEncryptTrack1 + lenUnEncryptTrack2 + lenUnEncryptTrack3);
                int indexEncryptTrack = indexUnEcryptTrack3 + lenUnEncryptTrack3;

                int lenHash = 20;
                int lenHashTrack1 = 0;
                int lenHashTrack2 = 0;
                int indexHashTrack1 = indexEncryptTrack + lenEncryptTrack;

                if (lenUnEncryptTrack1 > 0)//we have track1, we have track1 hash.
                {
                    lenHashTrack1 = lenHash;
                }
                int indexHashTrack2 = indexHashTrack1 + lenHashTrack1;

                if (lenUnEncryptTrack2 > 0)//we have track2, we have track2 hash.
                {
                    lenHashTrack2 = lenHash;
                }
                int indexKsn = indexHashTrack2 + lenHashTrack2;
                int lenKsn = 10;

                trackData = new TrackData()
                {
                    T1Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, indexUnEcryptTrack1, lenUnEncryptTrack1)),
                    T2Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, indexUnEcryptTrack2, lenUnEncryptTrack2)),
                    T3Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, indexUnEcryptTrack3, lenUnEncryptTrack3)),

                    T1Crypto = ByteArrayToHexString(SubArray<byte>(bytes, indexEncryptTrack, lenEncryptTrack)),
                    T2Crypto = "",
                    T3Crypto = "",

                    T1Hash = ByteArrayToHexString(SubArray<byte>(bytes, indexHashTrack1, lenHashTrack1)),
                    T2Hash = ByteArrayToHexString(SubArray<byte>(bytes, indexHashTrack2, lenHashTrack2)),
                    T3Hash = "",

                    SerialNumber = "",
                    Ksn = ByteArrayToHexString(SubArray<byte>(bytes, indexKsn, lenKsn)),

                    EncryptedTracks = "",
                    IsSwipe = !isManual
                };

                var track1Values = GetTrack1(trackData.T1Data);
                trackData.PAN = track1Values.PAN;
                trackData.Name = track1Values.Name ?? track1Values.Name;
                trackData.ExpDate = track1Values.ExpDate;

                //debit card has not track1 data
                if (trackData.T1Data == string.Empty)
                {
                    string track1 = trackData.T2Data;
                    track1 = track1.Replace(";", "%%");
                    track1 = track1.Replace('*', '0');
                    track1 = track1.Replace('?', '0');
                    track1 = track1.Replace("=", "^MANUALLY/ENTERED^");
                    track1Values = GetTrack1(track1);
                    trackData.PAN = track1Values.PAN;
                    trackData.Name = "";
                    trackData.ExpDate = track1Values.ExpDate;
                }
                if (lenUnEncryptTrack2 > 0)//if we do not have track2, it means it is an invalid swipe or card.
                    trackData.T3Data = $"{trackData.Ksn}:4:{(trackData.T1Crypto.Length) / 2:D4}:{trackData.T1Crypto}";
                else
                    trackData.T3Data = string.Empty;

                trackData.EncryptedTracks = $"{trackData.T1Data}|{trackData.T2Data}|{trackData.T3Data}";
            }
            else
            {
                trackData = null;
            }
            if (String.IsNullOrWhiteSpace(trackData?.Ksn) || String.IsNullOrWhiteSpace(trackData?.T3Data))
                trackData = null;

            return trackData;
        }

        private static Track1 GetTrack1(string t1Data)
        {
            try
            {
                const int lenExpDate = 4;

                var startSentinel = t1Data.IndexOf('%');
                if (startSentinel != 0)
                    throw new Exception("Track1|StartSentinel Missing");

                var panSeparator = t1Data.IndexOf('^', startSentinel + 1);
                if (panSeparator == 0)
                    throw new Exception("Track1|PanSeparator Missing");

                var nameSeparator = t1Data.IndexOf('^', panSeparator + 1);
                if (nameSeparator == 0)
                    throw new Exception("Track1|NameSeparator Missing");

                var endSentinel = t1Data.IndexOf('?', nameSeparator + 1);
                if (endSentinel == 0)
                    throw new Exception("Track1|EndSeparator Missing");

                var lenPan = panSeparator - startSentinel - 2;
                if (lenPan > 19)   //TODO: regex validation
                    throw new Exception("Track1|Invalid PAN");

                var lenName = nameSeparator - panSeparator - 1;
                if (lenName > 26 || lenName < 2)  //TODO: regex validation
                    throw new Exception("Track1|Invalid Name");

                var ExpData = t1Data.Substring(nameSeparator + 1, 4);
                if (ExpData.Length != 4)  //TODO: regex validation
                    throw new Exception("Track1|Invalid ExpDate");

                return new Track1()
                {
                    PAN = t1Data.Substring(startSentinel + 2, lenPan),
                    Name = t1Data.Substring(panSeparator + 1, lenName),
                    ExpDate = t1Data.Substring(nameSeparator + 1, lenExpDate)
                };
            }
            catch (Exception ex)
            {
                var t = ex;
                //LOG error
                return new Track1();
            }
        }

        #endregion

        #region -- string / array manipulation procedures --

        //TODO: see if these should be extension methods and moved to shared

        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static string ByteArrayToHexString(byte[] values)
        {
            return BitConverter.ToString(values).Replace("-", "");
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        static int search(byte[] haystack, byte[] needle, int start)
        {
            for (int i = start; i <= haystack.Length - needle.Length; i++)
            {
                if (match(haystack, needle, i))
                {
                    return i;
                }
            }
            return -1;
        }
        static bool match(byte[] haystack, byte[] needle, int start)
        {
            if (needle.Length + start > haystack.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < needle.Length; i++)
                {
                    if (needle[i] != haystack[i + start])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public static string ConvertIDTByteArrayToString(byte[] values)
        {
            var hex1 = ByteArrayToHexString(SubArray<byte>(values, osStx, osDataStatus + 1));
            var asc1 = Encoding.ASCII.GetString(SubArray<byte>(values, osDataStatus + 1, values[osLenT1] + values[osLenT2] + values[osLenT3]));
            var hex2 = ByteArrayToHexString(SubArray<byte>(values, hex1.Length / 2 + asc1.Length, values.Length - hex1.Length / 2 - asc1.Length));

            var data = new StringBuilder(hex1.Length + asc1.Length + hex2.Length);
            data.Append(hex1).Append(asc1).Append(hex2);

            data.Remove(2, 4).Insert(2, ByteArrayToHexString(BitConverter.GetBytes(Convert.ToInt16(data.Capacity - 12))));
            //note: ITC checksum/lrc unmodified

            var d = data.ToString();       //IDT HEX_ASCII_HEX mixedmode string
            var t = GetLrc(ASCIIEncoding.ASCII.GetBytes(d.Substring(6, d.Length - 12)));
            var r = GetChecksumAdd(ASCIIEncoding.ASCII.GetBytes(d.Substring(6, d.Length - 12)));

            return d;
        }
        public static byte GetChecksumAdd(byte[] data)
        {
            long longSum = data.Sum(x => (long)x);
            return unchecked((byte)longSum);
        }

        public static byte GetLrc(byte[] bytes)
        {
            byte LRC = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                LRC ^= bytes[i];
            }
            return LRC;
        }

        #endregion

        #endregion

        #region -- device event procedures --
        public void OnReport(HidReport report)
        {
///            Transaction.PaymentXO.Request.CustomFields = Transaction.PaymentXO.Request?.CustomFields ?? string.Empty;
            if (!device.IsConnected)
            {
///                Transaction.PaymentXO.Request.CreditCard = new Core.Data.Entity.Other.CreditCard
///                {
///                    AbortType = DeviceAbortType.BadRead,
///                };                
                ((IDevice)this).BadRead();
            }

            TrackData trackData = null;

            //HACK - setting this to make sure the Payment is incorporated into the idiotic PaymentInfo object
///            Transaction.PaymentXO.Request.PaymentRequest.Payment = Transaction.PaymentXO.Request.Payment;
///            Transaction.PaymentXO.Request.Payment.IsEMV = false;

            try
            {
                //if the content empty, it means it is timed out.
                bool empty = true;
                foreach(var dataByte in report.Data )
                {
                    if (dataByte != '\0')
                    {
                        empty = false;
                        break;
                    }
                }
///                if (empty && !Transaction.MSRTimer.Enabled )//the transaction timed out.
///                {
///                    return;
///                }
///                else if(empty)
if(empty)                
                {
///                    Transaction.PaymentXO.Request.CreditCard = new Core.Data.Entity.Other.CreditCard
///                    {
///                        AbortType = DeviceAbortType.BadRead,
///                    };                    
                    ((IDevice)this).BadRead();
                }
                var isXmlFormat = Encoding.ASCII.GetString(SubArray<byte>(report.Data, 0, 7)) == "<DvcMsg";
                trackData = isXmlFormat ? ParseXmlFormat(report.Data) : ParseIdtFormat(report.Data);

                if (trackData != null)
                {
                    if (Transaction.PaymentXO == null)
                        Transaction.Init();
                    Transaction.PaymentXO.Request.CreditCard = new Core.Data.Entity.Other.CreditCard();
                    {
                        Transaction.PaymentXO.Request.CreditCard.AbortType = DeviceAbortType.NoAbort;
                        Transaction.PaymentXO.Request.CreditCard.CardHolderName = trackData.Name;
                        Transaction.PaymentXO.Request.CreditCard.CreditCardNumber = trackData.PAN;
                        Transaction.PaymentXO.Request.CreditCard.EncryptedTracks = trackData.EncryptedTracks;
                        Transaction.PaymentXO.Request.CreditCard.CardExpirationMonth = trackData.ExpDate.Substring(2, 2);
                        Transaction.PaymentXO.Request.CreditCard.CardExpirationYear = trackData.ExpDate.Substring(0, 2);
                        Transaction.PaymentXO.Request.CreditCard.Track1 = trackData.Track1;
                        Transaction.PaymentXO.Request.CreditCard.Track2 = trackData.Track2;
                        Transaction.PaymentXO.Request.CreditCard.Track3 = trackData.Track3;
                        Transaction.PaymentXO.Request.CreditCard.EMVCardEntryMode =
                            trackData.IsSwipe ? Core.Shared.Enums.EntryModeType.Swiped.ToString() : Core.Shared.Enums.EntryModeType.Keyed.ToString();
                    };
///                        AbortType = DeviceAbortType.NoAbort,
///                        CardHolderName = trackData.Name,
///                        CreditCardNumber = trackData.PAN,

///                        EncryptedTracks = trackData.EncryptedTracks,
///                        CardExpirationMonth = trackData.ExpDate.Substring(2, 2),
///                        CardExpirationYear = trackData.ExpDate.Substring(0, 2),
///                        Track1 = trackData.Track1,
///                        Track2 = trackData.Track2,
///                        Track3 = trackData.Track3,
///                        EMVCardEntryMode = trackData.IsSwipe ? Core.Shared.Enums.EntryModeType.Swiped.ToString() : Core.Shared.Enums.EntryModeType.Keyed.ToString(),

                        //TODO: where does the PIN come from on DebitCards?
                        //EncryptedPIN = IsDebit ? trackData. : null,

///                    Transaction.IsManual = !trackData.IsSwipe;
///                    Transaction.PaymentXO.Request.PaymentTender.EntryModeTypeID = trackData.IsSwipe? (int)Core.Shared.Enums.EntryModeType.Swiped : (int)Core.Shared.Enums.EntryModeType.Keyed;                  
                    NotificationRaise(new NotificationEventArgs { NotificationType = NotificationType.DeviceEvent, DeviceEvent = DeviceEvent.CardReadComplete });
                }
                else
                {
///                    Transaction.PaymentXO.Request.CreditCard = new Core.Data.Entity.Other.CreditCard
///                    {
///                        AbortType = DeviceAbortType.BadRead,
///                    };                    
                    ((IDevice)this).BadRead();
                }
            }
            catch (Exception)
            {
///                Transaction.PaymentXO.Request.CreditCard = new Core.Data.Entity.Other.CreditCard
///                {
///                    AbortType = DeviceAbortType.BadRead,
///
///                };                
                ((IDevice)this).BadRead();
            }
            
            //Release the wait handle
            //waitForReply.Set();
        }

        private void DeviceRemovedHandler()
        {
            //TODO: When device is rmoved raise an user message?
            System.Diagnostics.Debug.WriteLine("device: removed.");
            NotificationRaise(new NotificationEventArgs { NotificationType = NotificationType.DeviceEvent, DeviceEvent = DeviceEvent.DeviceDisconnected });
        }

        #endregion

        public bool Reset()
        {
           byte[] result;

           // Create the command to get config values
           var readConfig = new byte[CommandTokens.DeviceReset.Length + 1];
           Array.Copy(CommandTokens.DeviceReset, readConfig, CommandTokens.DeviceReset.Length);
           readConfig[CommandTokens.DeviceReset.Length] = 0x00;
           readConfig[readConfig.Length - 1] = GetCheckSumValue(readConfig);

           // execute the command, get the result
           var status = SetupCommand(readConfig, out result);
           return (status == EntryModeStatus.Success) ? true : false;
        }

        public bool SetQuickChipMode(bool mode)
        {
           byte[] result;

           // Create the command to get config values
           var readConfig = new byte[CommandTokens.QuickChipModeOn.Length + 1];
           if(mode)
           {
              Array.Copy(CommandTokens.QuickChipModeOn, readConfig, CommandTokens.QuickChipModeOn.Length);
           }
           else
           {
              Array.Copy(CommandTokens.QuickChipModeOff, readConfig, CommandTokens.QuickChipModeOff.Length);
           }
           readConfig[CommandTokens.QuickChipModeOn.Length] = 0x00;
           readConfig[readConfig.Length - 1] = GetCheckSumValue(readConfig);

           // execute the command, get the result
           var status = SetupCommand(readConfig, out result);
           return (status == EntryModeStatus.Success) ? true : false;
        }

        public bool SetUSBHIDMode()
        {
           byte[] result;

           // Create the command to get config values
           var readConfig = new byte[CommandTokens.SetUSBHIDMode.Length + 1];
           Array.Copy(CommandTokens.SetUSBHIDMode, readConfig, CommandTokens.SetUSBHIDMode.Length);
           readConfig[CommandTokens.SetUSBHIDMode.Length] = 0x00;
           readConfig[readConfig.Length - 1] = GetCheckSumValue(readConfig);

           // execute the command, get the result
           var status = SetupCommand(readConfig, out result);
           return (status == EntryModeStatus.Success) ? true : false;
        }

        public bool SetUSBKeyboardMode()
        {
            var rtn = SetUSBKBMode();
            return rtn.Success;
        }

        public virtual void Configure(object[] settings)
        {
        }

        #region -- keyboard mode overrides --
        public virtual void SetVP3000DeviceHidMode()
        {
        }

        public virtual void VP3000PingReport()
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
        public virtual void GetMSRSettings(ref ConfigSerializer serializer)
        {
        }
        public virtual void GetEncryptionControl(ref ConfigSerializer serializer)
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

    ///internal class DeviceInfo
    public class DeviceInfo
    {
        public string SerialNumber;
        public string FirmwareVersion;
        public string EMVKernelVersion;
        public string ModelName;
        public string ModelNumber;
        public string Port;
        public byte[] ConfigValues;

        public SecurityLevelNumber SecurityLevel;
    }
}

