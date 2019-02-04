using System;
using System.ComponentModel;

namespace IPA.Core.Shared.Enums
{
    #region -- Enumerations ---

    public enum DeviceManufacturer
    {
        Unknown = 0,
        IDTech = 1,
        Ingenico = 2
    }
    public enum IDTECH_DEVICE_PID
    {
        MAGSTRIPE_HID = 2010,
        MAGSTRIPE_KYB = 2030,
        SECUREKEY_HID = 2610,
        SECUREKEY_KYB = 2620,
        SECUREMAG_HID = 2810,
        SECUREMAG_KYB = 2820,
        VP3000_HID    = 3530,
        VP3000_KYB    = 3531,
        AUGUSTA_KYB   = 3810,
        AUGUSTA_HID   = 3820,
        AUGUSTAS_HID  = 3920,
        AUGUSTAS_KYB  = 3910,
        VP5300_HID    = 4450,
        VP5300_KYB    = 4650,
        // ^^^ ADDITIONAL PID's HERE ^^^
        UNKNOWN       = 9999
    }
    public enum DeviceStatus
    {
        NoEncryption = 1,
        Connected = 2,
        NoDevice = 3,
        MultipleDevice = 4,
        Unsupported = 5,
        EncyptionDisabled = 6,
        WrongComPort = 7

    }
    public enum DeviceProcess
    {
        Approved,
        Declined,
        Canceled,
        Reset,
        Reswipe
    }
    public enum DeviceUpdateType
    {
        [Description("Firmware")]
        Firmware,
        [Description("Forms")]
        Forms,
        [Description("DAL")]
        DAL,
        [Description("HIDMode")]
        HIDMode,
        [Description("KBMode")]
        KBMode
    }

    public enum NotificationType
    {
        UI,
        Systray,
        Log,
        DeviceEvent,
        ACHWorkflow
    }

    public enum DeviceAbortType
    {
        NoAbort = 0,
        UserCancel,
        MsrTimeout,
        TransactionTimeout,
        BadRead,
        SignatureTimeout,
        ExpiredCard,
        EMVFallback
    }

    public enum DeviceEvent
    {
        CardReadInit = 0,
        CardReadComplete,
        SignatureComplete,
        ManualInputComplete,
        DeviceUpdating,
        DeviceUpdated,
        DeviceUpdateError,
        DeviceError,
        ChipCardDetected,
        DeviceDisconnected
    }

    public enum TimerType
    {
        [Description("MsrTimeout")]
        MSR = ConfigTypeEnum.MsrTimeout,
        [Description("EMVTimeout")]
        EMV,                                        // ??
        [Description("SignatureCaptureTimeout")]
        Signature = ConfigTypeEnum.SignatureCaptureTimeout,
        [Description("TransactionTimeout")]
        Transaction,                                // Computed value, not Config
        [Description("AutoSelectTenderTimeout")]
        AutoClose = ConfigTypeEnum.AutoSelectTenderTimeout,
        [Description("TimeoutBridge")]
        ServiceCaller = ConfigTypeEnum.TimeoutBridge,
        [Description("AchTimeout")]
        ACH = ConfigTypeEnum.AchTimeout,
        [Description("ManualCardTimeout")]
        Manual = ConfigTypeEnum.ManualCardTimeout,
        [Description("ServicePollingInterval")]
        ServicePolling = ConfigTypeEnum.ServicePollingInterval,
        [Description("ServiceMaxLatency")]
        ServiceMaxLatency = ConfigTypeEnum.ServiceMaxLatency,
        [Description("IpaLinkPollingTimeout")]
        IPALinkPollingTimer = ConfigTypeEnum.IpaLinkPollingTimeout
    }

    public enum UserControls
    {
        [Description("ACHAccount")]
        ACH,
        [Description("ACHAccountHolderName")]
        ACHAccountHolder,
        [Description("CardHolderName")]
        CardHolder,
        [Description("Manual")]
        Manual,
        [Description("Message")]
        Message,
        [Description("TenderSelector")]
        TenderSelector,
        [Description("SetKBMode")]
        SetKBMode
    }

    public enum ACHWorkflow
    {
        ACHNameCancel,
        ACHNameSubmit,
        ACHCancel,
        ACHSubmit,
        ACHStart
    }

    public enum AppType
    {
        [Description("Bridge")]
        Bridge = 100,

        [Description("Pedal")]
        Pedal = 200,

        [Description("IPALink")]
        IPALink = 300,

        [Description("PorterDev")]
        PorterDev = 400,

        [Description("PorterTest")]
        PorterTest = 410,

        [Description("PorterUAT")]
        PorterUAT = 420,

        [Description("PorterProd")]
        PorterProd = 430,

        [Description("PorterStage")]
        PorterStage = 440,

        [Description("POS")]
        POS = 500,

        [Description("Hub")]
        Hub = 600,

        [Description("Update")]
        Update = 700,
        [Description("AppManager")]
        AppManager = 800
    }

    public enum TenderType
    {
        Invalid = 0,

        [Description("Visa")]
        Visa = 1,

        [Description("MasterCard")]
        MasterCard = 2,

        [Description("American Express")]
        AMEX = 3,

        [Description("Diners Club")]
        DinersClub = 4,

        [Description("enRoute")]
        enRoute = 5,

        [Description("Japan Credit Bureau")]
        JCB = 6,

        Discover = 7,
        ACH = 8,
        Debit = 9,

        [Description("PINless Debit")]
        DebitPINLess = 10,

        Cash = 17,
        Check = 18,

        [Description("FSA/HSA")]
        FSA_HSA = 20,

        [Description("Maestro")]
        Maestro = 21,

        // The rest of the codes are in TCLink, but James Removed them from the database. We are not using them at them moment. 2/4/2016
        // THis is used when we do not have a Credit Card Number and prevents the string "INVALID" from showing up in response, etc.
        //[Description("")]
        //Refund         = 100,

        //Invalid_D      = 128,
        //Visa_D         = 129,

        //[Description("MasterCard-D")]
        //MasterCard_D   = 130,

        //[Description("American Express-D")]
        //AMEX_D         = 131,

        //[Description("Diners Club-D")]
        //DinersClub_D   = 132,

        //[Description("enRoute-D")]
        //enRoute_D      = 133,

        //[Description("JCB-D")]
        //JCB_D          = 134,

        //[Description("Discover-D")]
        //Discover_D     = 135,

        //[Description("ACH-D")]
        //ACH_D          = 136,

        //[Description("Debit Card")]
        //Debit_D        = 137,

        //[Description("PINless Debit-D")]
        //PINlessDebit_D = 138
    }

    public enum TCCustAttributeSourceEnum
    {
        Cdb = 0,
        Ipa = 1
    }

    public enum TCCustAttributeNameEnum  //TODO: move to t4 codegen location
    {
        //Source=IPA
        SignatureEnabled,
        VerifyAmountEnabled,
        CVVEnabled,
        PosEnabled,
        CardExpEnabled,

        ACHEnabled,

        //Source=CDB
        DebitEnabled,
        ACHPlatform,
        EMVEnabled,
        BlockPartialAuth
    }

    //TCLink possible error
    //public enum ErrorField
    //{
    //    MissingFields = 300,
    //    ExtraFields = 301,
    //    BadFormat = 302,
    //    BadLength = 303,
    //    MerchantCantAccept = 304,
    //    Mismatch = 305
    //}

    //public enum DeclineType
    //{
    //    Decline = 200,
    //    AVS = 201,
    //    CVV = 202,
    //    Call = 203,
    //    ExpiredCard = 204,
    //    CardError = 205,
    //    AuthExpired = 206,
    //    Fraud = 207,
    //    BlackList = 208,
    //    Velocity = 209,
    //    Swiper = 210,
    //    OfflineDecline = 211 //This is offline decline sent from the device. 
    //}

    public enum EMVTagGroupType
    {
        [Description("ARQC from device")]
        Auth = 10,

        [Description("ARQC Response from TCLink")]
        AuthResponse = 20,

        [Description("TC or AC from device")]
        AuthConfirm = 30,

        [Description("TC or AC Response from TCLink")]
        AuthConfirmResponse = 40
    }

    //public enum ErrorType
    //{
    //    [Description("Internet connection failed, call your local administrator for assistance")]
    //    CantConnect = 400,

    //    [Description("Unable to resolve DNS Host Name")]
    //    DNSValue = 401,

    //    [Description("Communication lost with processor during the payment.")]
    //    LinkFailure = 402,

    //    [Description("Communication with the card processor cannot be established, call your local administrator for assistance")]
    //    FailToProcess = 403,//3607 to distinguish it from other connectivity error.

    //    [Description("Not authorized to process this type of payment")]
    //    NotAllowed = 404,

    //    [Description("There was a problem processing the encrypted swipe data. Please re-swipe card and try again")]
    //    Corrupted_TrackData = 405,

    //    [Description("TC Link was not able to connect to the external processor to complete the transaction")]
    //    FailedAtProcessor = 406,//3607 to distinguish it from other connectivity error.

    //    [Description("Device update in progress.Do not restart.Please wait until the process has completed.")]
    //    DeviceUpdate = 407,

    //    [Description("Transmission errors were encountered in TrustCommerce’s connection to TCLink.")]
    //    FailAtTCLink = 408,

    //    [Description("A new device has been detected. Please restart TC IPADAL to initialize the device.")]
    //    DeviceSwitched = 409,

    //    [Description("'Unable to decrypt request received.  If this error persists please contact your TrustCommerce representative for assistance.")]
    //    DecryptError = 450,

    //    [Description("''TC IPA validation could not be completed.  Please contact your TrustCommerce representative for assistance.")]
    //    PKIDecryptError = 451,

    //    MismatchedLinkDALIP = 452,
    //    ConnectToDALFailed = 453,
    //    TransactionTimeOut = 454,
    //    InputInvalid = 455,
    //    [Description("Device Encryption Key Not Found")]
    //    NoKeyFound = 1600,

    //    [Description("Device does not have encryption enabled")]
    //    EncryptionNotEnabled = 1601,

    //    [Description("The payment entry device was not able to connect.")]
    //    DeviceNotConnected = 1602
    //}

    public enum StatusBadData
    {
        MissingFields = 700,
        ExtraFields = 701,
        BadLength = 702,
        BadFormat = 703,
        MerchantCantAccept = 704,
        Mismatch = 705,
        InvalidCustID = 901,
        InvalidAchRouteNo = 902,
    }

    //public enum TransactionStatus
    //{
    //    Unknown = 0,

    //    [Description("The transaction was successfully authorized")]
    //    Approved = 100,

    //    [Description("The transaction has been successfully accepted into the system.")]
    //    Accepted = 101,

    //    [Description("The transaction was declined; see Decline Type.")]
    //    Decline = 102,

    //    [Description("Invalid fields were passed; see Error Type.")]
    //    BadData = 103,

    //    [Description("Cannot communicate with Processor")]
    //    Error = 104,

    //    [Description("The transaction was declined; see Decline Type.")]
    //    Declined = 105,

    //    [Description("Card Blocked, please call card issuer for assistance. Swipe another card.")]
    //    TCLinkCardBlocked = 106,

    //    [Description("Rejected on a transaction that may have been accepted but not yet approved")]
    //    Rejected = 107,

    //    [Description("TCTransactionID referenced not found.")]
    //    TCTransIDNotFound = 110,

    //    [Description("TCTransactionID referenced has been previously voided or reversed.")]
    //    TCTransIDCxlNoAmount = 112,

    //    [Description("Transaction request included data not allowed for transaction type")]
    //    TransTypeOffender = 120,

    //    [Description("TCTransactionID CompanyID or CustID Mismatch from originating Sale/PreAuth")]
    //    TCTransIDNoMatch = 125,

    //    [Description("TCToken update request sent could not find an exact match of original BillingID and CustID combination.")]
    //    TokenUpdateIDMisMatch = 126,

    //    [Description("TCToken attribute missing from request.")]
    //    TokenAttributeNotFound = 127,

    //    [Description("Transaction request referenced a declined TransactionID.")]
    //    TransTypeCxlDecline = 130,

    //    [Description("TCTransactionID referenced has already been refunded.")]
    //    TCTransIDCreditNoAmount = 140,


    //    [Description("Card Blocked, please call card issuer for assistance. Swipe another card.")]
    //    CardDeviceCardBlocked = 166,

    //    [Description("Payment has been processed successfully. Card information was not stored. Please contact your TrustCommerce representative.")]
    //    TCTransTokenNotStored = 706,
    //    //TODO: Refactor duplicate 901 reference.
    //    [Description("Not able to process request. The Cust ID or password is invalid, please contact your Administrator.")]
    //    InvalidCustID = 901,

    //    [Description("Not able to process request. The Cust ID used has no access to the ServiceURL reached, please contact your Administrator.")]
    //    InvalidServiceURL = 910,
    //}

    public enum PaymentMethod
    {
        credit = 0,
        checking = 1
    }

    //public enum EntryModeStatus
    //{
    //    OK = 0,

    //    [Description("Payment canceled by user.")]
    //    Canceled = 150,

    //    [Description("Services encounter an error, original entry mode not known.")]
    //    EntryModeUnknown = 152,

    //    [Description("Error reading card. Maximum number of swipes or inserts has been reached. Transaction canceled.")]
    //    CardNotRead = 155,

    //    [Description("Device has timed out due to inactivity.")]
    //    Timeout = 160,

    //    [Description("Device Error.")]  //Not used atm. 12/22/2015 - JamesB, per Jana
    //    Error = 165,

    //    [Description("Card Blocked.")]
    //    Blocked = 166,

    //    [Description("This card is not supported, please provide another card.")]
    //    Unsupported = 170,

    //    [Description("Device update in progress.")]
    //    DeviceUpdate = 171,

    //    [Description("No DAL or DAL failure.")]
    //    NoDal = 175,

    //    [Description("IPALink cannot find bridge.")]
    //    NoBridge = 176,

    //    [Description("Device not found.")]
    //    NoDevice = 177,

    //    [Description("Error encountered during pin entry.")]
    //    ErrorPinEntry = 178,

    //    [Description("Pin try limit exceeded.")]
    //    PinEntryExceed = 179,

    //    [Description("Successful")]
    //    Success = 180,

    //    [Description("Device Configuration Mismatch")]
    //    DeviceConfigMismatch = 185,

    //    [Description("Not able to process request. The Cust ID or password is invalid, please contact your Administrator.")]
    //    InvalidCustID = 901,

    //    [Description("User retry the card.")]
    //    Retry = 1000,

    //    [Description("Not able to process request. The Cust ID used has no access to the ServiceURL reached, please contact your Administrator.")]
    //    InvalidServiceURL,

    //    [Description("Multiple devices connected.")]
    //    MultipleDevice,
    //    [Description("DAL not ready.")]
    //    NotReady = 1003
    //}

    //public enum InitializationFlow
    //{
    //    [Description("IPALink sent an initialization request")]
    //    InitIPALink = 1500,

    //    [Description("Hub receieved an Init request from an application")]
    //    InitHub = 1501,

    //    [Description("DAL recevied an Init Request from an application")]
    //    InitDAL = 1502,

    //    [Description("Service received an Init response from an application")]
    //    InitService = 1503,

    //    [Description("Initialization completed - send response to an application")]
    //    InitComplete = 1504
    //}

    public enum PackageType
    {
        Forms = 1,
        Firmware = 2,
        EMVKernelConfig = 3,
        MSI = 4,
        Files = 5,
        Porter_InitialCommunication = 8,
        Porter_InitialCommunicationConfig = 9,
        Porter_Communication = 10,
        Porter_CommunicationConfig = 11,
        Porter_Libraries = 12,
        Porter_Base = 13,
        Porter_BaseConfig = 14,
        Porter_Application = 15,
        Porter_ApplicationConfig = 16
    }

    public enum PackageDeployType
    {
        Auto = 1,
        Manual = 2,
        Scheduled = 3
    }

    public enum PackageDeployStatus
    {
        Pending = 800,
        Success = 801,
        Error = 802,
        SecurityDenied = 803,
        NoReplyYet = 804,
        FileOnClient = 805,
        FileOnDevice = 806,
        FileCleaned = 807,
        ErrorOnPort = 808,
        DeviceTimeOut = 809,
        CheckUpdate = 810,
        DownloadPending = 811,
        PackageUpdateSuccess = 812,
        PackageUpdateFailed = 813,
        FormsDeviceMismatch = 814,
        FirmwareDeviceMismatch = 815,
        PackageDownloadFailed = 816
    }

    public enum PaymentType
    {
        [Description("sale")]
        Sale = 1,
        [Description("refund")]
        Refund = 2,
        [Description("preauth")]
        PreAuth = 3,
        [Description("postauth")]
        PostAuth = 4,
        [Description("void")]
        Void = 5,
        [Description("store")]
        Store = 6,
        [Description("reversal")]
        Reversal = 7,
        [Description("unstore")]
        Unstore = 8,
        [Description("credit2")]
        Credit2 = 9,
        [Description("chargeback")]
        ChargeBack = 10,
        [Description("verify")]
        Verify = 11,
        [Description("tokenupdate")]
        TokenUpdate = 12,
        [Description("credit")]
        Credit = 13,
        [Description("signature")]
        SaveSignature = 14,
        [Description("emv_auth")]
        EMVAuthorize = 15,
        [Description("emv_refund")]
        EMVRefund = 16,
        [Description("emv_auth_confirmation")]
        EMVAuthConfirmation = 17,
        [Description("emv_refund_confirmation")]
        EMVRefundConfirmation = 18,
        [Description("chargeupdate")]   // For Parking (IPALink All)
        ChargeUpdate = 19,
        [Description("chargecancel")]   // For Parking (IPALink All)
        ChargeCancel = 20
    }

    public enum PaymentSystemType
    {
        Legacy = 1,
        IPAService  = 3,
        Web = 4,

        Epic = 6,
        FBTN = 7,
        IPAPOS = 8
    }

    public enum EntryModeType
    {
        [Description("")]
        Unknown = 0,

        [Description("Swiped")]
        Swiped = 1,

        [Description("EMV Chip Read")]
        EMVChipRead = 2,

        [Description("Keyed/Manual")]
        Keyed = 3,

        [Description("ACH Keyed")]
        ACHKeyed = 4
    }

    //public enum TCLinkSaleType
    //{
    //    [Description("sale")]
    //    Sale = 1,

    //    [Description("preauth")]
    //    PreAuth = 3,

    //    [Description("postauth")]
    //    PostAuth = 4,

    //    [Description("void")]
    //    Void = 5,

    //    [Description("store")]
    //    Store = 6,

    //    [Description("reversal")]
    //    Reversal = 7,

    //    [Description("credit")]
    //    Credit = 8,

    //    [Description("unstore")]
    //    UnStore = 10,

    //    [Description("signature")]
    //    SaveSignature = 11,

    //    [Description("emv_auth")]
    //    EMVAuthorize = 12,

    //    [Description("emv_refund")]
    //    EMVRefund = 13,

    //    [Description("emv_auth_confirmation")]
    //    EMVAuthConfirmation = 14,

    //    [Description("emv_refund_confirmation")]
    //    EMVRefundConfirmation = 15,

    //    [Description("chargeback")]
    //    ChargeBack = 16,

    //    [Description("verify")]
    //    Verify = 18
    //}

    /// <summary>
    /// Transaction Types supported.
    /// </summary>
    public enum TransactionType
    {
        [Description("Card Present")]
        CP = 0, // Card present - Do not do anything

        [Description("Mail Order / Telephone Order")]
        MOTO = 1
    }

    public enum StatusType
    {
        [Description("IPALink Initialization")]
        IPALinkInit = 50,
        [Description("Start Initialization Process")]
        InitializeStart = 60,
        [Description("IPALink Payment requested")]
        Payment = 100,
        [Description("Payment linked to IPA")]
        IPALink = 200,
        [Description("End of the Initialization Process")]
        InitializeEnd = 65,
        [Description("Payment bridged to Pedal")]
        Bridged = 300,
        [Description("Start Payment Complete")]
        PaymentStarted = 350,
        [Description("Payment Pedalled to PED")]
        Pedalled = 400,
        [Description("The Pedal received back from the DAL a response.")]
        DALBBack2Pedal = 410,
        [Description("The Pedal has recorded the swipe and questions. The CC data is being sent back to the bridge.")]
        PedalSendToBridge = 425,
        [Description("Payment collected and sent from DAL back to Bridge")]
        PaymentResponseDAL = 440,
        [Description("PaymentApproved")]
        PaymentApproved = 470,
        [Description("Signature request and Approve/Decline arrived in DAL")]
        SignatureRequestInDAL  = 485,
        [Description("Signature arrived in DAL")]
        SignatureInDAL = 487,
        [Description("DALTimeOuts")]
        DALTimeOuts = 419,
        [Description("signature request send from service to dal.")]
        SignatureRequestSent = 431,
        [Description("Payment data from Hub received in Service WebAPI.")]
        PaymentAuthHubToWebAPI = 444,
        [Description("Signature Saved to Vault")]
        SignatureSaved = 493,
        [Description("Start ChargeBack Process.")]
        ChargeBackStarted = 530,
        [Description("Finished ChargeBack Process.")]
        ChargeBackFinished = 540,
        [Description("Start Verify Process.")]
        VerifyStarted = 550,
        [Description("Finished Verify Process.")]
        VerifyFinished = 560,
        [Description("Payment Sent to TCLINK")]
        SentToTCLink = 700,
        [Description("Requests sent to TCLink over a Socket")]
        TCLinkInterfaceDirect = 710,
        [Description("Response sent from TCLink over a Socket")]
        TCLinkInterfaceDirectResponse = 730,
        [Description("Payment status sent to IPALink")]
        Back2IPALink = 800,
        [Description("Payment complete")]
        Complete = 900,
        [Description("Transform the PaymentDetail.Response object to epic xml")]
        TransfromObjectToEpic = 950,
        [Description("Void Transaction By The User From POS")]
        VoidTransPOS = 971,
        [Description("The application status for an individual Porter unit")]
        ApplicationStatus = 1000,
        [Description("Request Porter Reboot")]
        RebootRequest = 1150,
        [Description("Request Porter restart its applications")]
        RestartRequest = 1155,
        [Description("Request Porter Regregister")]
        ReregisterRequest = 1160,
        [Description("Request Porter reload its applications")]
        ReloadRequest = 1165,
        [Description("Set Porter clock to server time")]
        SetClock = 1170,
        [Description("Adjust Porter clock to server time")]
        AdjustClock = 1175,
        [Description("Validation Requested from Communication Module")]
        CommValidate = 1250,
        [Description("Startup of Application module acknowledge")]
        DALStartupAck = 1301,
        [Description("Status associated with the Message request")]
        ApplicationPollingStart = 3031,
        [Description("Status associated with the Message response")]
        ApplicationPollingEnd = 3032,
        [Description("Regenerated TCKey from token")]
        TCKeyFromToken = 3040,
        [Description("Regenerated AppKey from token")]
        AppKeyFromToken = 3041,
        [Description("TCIPADAL recevied an Init Request from an application.")]
        InitTCIPADAL = 3113,
        [Description("DAL start up")]
        DALStartup = 3119,
        [Description("Contract Request")]
        ContractRequest = 3120,
        [Description("IPALink response to Contract")]
        ContractResponse = 3121
    }

    /// <summary>
    /// Commented items are not yet supported
    /// </summary>
    public enum TenderFamilyType
    {
        Unknown = 0,
        Card = 1,
        Check = 2,
        //Cash = 3,
        //MoneyOrder = 4,
        //TravelersCheck = 5,
        //Voucher = 6,
        //AccountsPayable = 7
    }   

    public enum ACHAccountType
    {
        Savings = 1,
        Checking = 2
    }

    public enum MessageType
    {
        [Description("Unknown")]
        Unknown = 0,
        [Description("Init")]
        Init = 1,
        [Description("Transaction")]
        Transaction = 2,
    }
    public enum LoggingOption
    {
        File,
        EventLog
    }

    public enum CommunicationRoute
    {
        File = 1,
        Service = 2
    };

    [Obsolete]
    public enum ConfigTypeID
    {
        SuppressSignature = 730,
        PosEnabled = 733,
        DalLookupHint = 734
    }
    //TODO: refactor to t4codegen assembly
    public enum ConfigTypeEnum
    {
        AllowPartialPayment = 100,
        AllowSplitTender = 110,
        AllowPartialRefund = 120,
        ForceRefundDays = 130,
        MaxiumumRefundDays = 140,
        RefundTenderTypeID = 150,
        SettlmentTime = 160,
        SettlmentTimeZoneID = 170,
        RequireWorkstationConfig = 180,
        AutomaticDeviceIntake = 190,
        MasterTCCustID = 200,
        CreditCardReceiptMinimum = 210,
        PaymentReceiptMinimum = 220,
        DefaultCCReceiptID = 230,
        CreditCardSignatureMinimum = 270,
        DefaultPaymentReceiptID = 280,
        DefaultLogoID = 290,
        DefaultSourceSystemID = 300,
        AllowPartialAuthorization = 310,
        StoreToken = 320,
        BridgeDNS = 495,
        BridgePort = 496,
        //IsCitrix = 505,
        //32BitOr64Bit = 550,
        TimeoutPedal = 551,
        MsrTimeout = 552,
        JavaAppPath = 553,
        LoggingVerbose = 554,
        LoggingPath = 555,
        JavaTestAppPath = 556,
        TimeoutHttpClient = 557,
        FormsVersionIpp350 = 600,
        FormsVersionIsc250 = 601,
        FormsVersionIsc480 = 602,
        PaymentPort = 603,
        SignaturePort = 604,
        PaymentStatusPort = 605,
        RequireHTTPS = 606,
        PaymentPort2 = 607,
        PaymentUDPIP = 608,
        ShowCvvForm = 675,
        StartMode = 676,
        EnvironmentType = 677,
        SignatureCaptureTimeout = 678,
        JDALConfigPath = 679,
        JDALConfigFileName = 680,
        VerifySignature = 681,
        OSVersion = 682,
        PackageDeployFolder = 683,
        WaitForPayment = 684,
        WaitForSignature = 685,
        SignalRObjectGarbageCollection = 686,
        TimeoutBridge = 687,
        SignalRServerReconnectAttempts = 688,
        SignalRServerReconnectDelay = 689,
        SignalRServerKeepAliveInterval = 690,
        SignalRDeadlockErrorTimeout = 691,
        DALCloseOnUserLock = 692,
        EmvEnabled = 693,
        EmvRetryAttempts = 694,
        ContactlessEnabled = 695,
        LoadTestMode = 696,
        LoadTestModeIterations = 697,
        IPAServiceURL = 698,
        IPASFTP = 699,
        JDALSocketBufferSize = 700,
        EMVMinFirmwareVersion = 701,
        AllowVoid = 702,
        WhiteList = 703,
        DevicePreference = 704,
        DisplayPaymentUI = 705,
        DeviceFirmwareUploadTimeout = 710,
        DeviceFormsUploadTimeout = 711,
        DeviceFirmwareResetTimeout = 712,
        DeviceFormsResetTimeout = 713,
        DeviceFirmwareUpdateTimeout = 714,
        ProcessingPlatform = 715,
        DeviceFormsUpdateTimeout = 716,
        DeviceSettingsIDTSecureKey = 722,
        DeviceSettingsIDTSecureMag = 723,
        DeviceSettingsIDTSRedKey = 724,
        PaymentReceiptFormat = 725,
        TCLinkSendTimeout = 726,
        TCLinkReceiveTimeout = 727,
        ClientLookupHint = 734,
        //TODO: configtypes to be approved by Jana
        DisplayTransactionTime = 735,
        DisplayFailuresTime = 736,
        EpicLookupHint = 737,
        AllLookupHint = 738,
        AchTimeout = 739,
        ManualCardTimeout = 740,
        AutoSelectTenderTimeout = 741,
        ServicePollingInterval = 742,
        ServiceMaxLatency = 743,
        IpaLinkPollingTimeout = 744,
        EnableFirmware = 745,
        EnableForms = 746,
        DownLoadFileBufferSize = 747,
        AdminUserLogin = 748,
        ShowSysTrayDeployStatus = 749,
        IngenicoLoggingLevel = 750,
        LoggingNumDays = 751,
        DeviceSelfCheck = 752,
        PackageCheckPollRate = 754,
        IPALinkInitTime = 759,
        IDTechDisable = 760,
        PorterRestartTime = 762,
        ClockSetThreshold = 763,
        ClockAdjustThreshold = 764,
        AliveReportingPeriod = 765,
        HttpTimeout = 767,
        AppTopMostInterval = 768,
        AppTopMost = 769
    }
    public enum FunctionType
    {
        INITIALIZE,
        TRANSACTION,
        LICENSEISVALID,
        REGISTERWORKSTATION,
        STATUSCODELIST,
        CONFIG,
        SHUTDOWN,
        UNKNOWN
    };
    public enum IngenicoLoggingLevel
    {
        NONE = -1,
        ERROR = 0,
        WARNING = 1,
        INFO = 2,
        TRACE = 3,
        DEBUG = 4
    };
    public enum POSClientSystem
    {
        Undefined,
        Epic,
        IPALinkAll
    };

    public enum DataExchangeFormat
    {
        XML,
        JSON
    };

    public enum PorterBOMethodName
    {
        [Description("IPA.Core.BusinessObjects.Porter.Register")]
        Register,
        [Description("IPA.Core.BusinessObjects.Porter.Preregister")]
        Preregister,
        [Description("IPA.Core.BusinessObjects.Porter.Authenticate")]
        Authenticate
    }

    public enum PorterServiceErrorType
    {
        [Description("Invalid software identifers")]
        InvalidRegisterKeyValue,

        [Description("Missing serial number")]
        MissingSerialNumber,

        [Description("Invalid unit identifiers")]
        InvalidPackageIdentification,

        [Description("Unknown unit identifier")]
        UnknownUnitIdentifier,

        [Description("Incorrect status")]
        IncorrectUnitStatus,

        [Description("Unable to calculate package HMAC")]
        PackageHMACFailure,

        [Description("Package HMAC validation failure")]
        PackageHMACMismatch,

        [Description("Serial number validation failure")]
        SerialNumberValidationFailure,

        [Description("ETH MAC validation failure")]
        EthMACValidationFailure,

        [Description("ConnectionKey signing failure")]
        ConnectionKeySigningFailure,

        [Description("Registration certificate lookup failure")]
        RegistrationCertificateLookupFailure,

        [Description("Response pod signing failure")]
        ResponsePodSigningFailure,

        [Description("Validation Package file access failure")]
        ValidationPackageAccessFailure
    };

    //public enum ActionType
    //{
    //    Sale = 1,
    //    //Credit = 2  // Another new contact ach:credit will be implemented later.
    //}

    public enum LoginUserType
    {
        AsyncUser = 1,
        SyncUser = 2,
        AdminUser = 3
    }
    #endregion
}
