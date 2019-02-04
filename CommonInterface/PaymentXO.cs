using IPA.Core.Shared.Enums;
using IPA.Core.Shared.Helpers;
//using IPA.Core.Shared.Helpers.Extensions;
//using IPA.Core.Shared.Helpers.StatusCode;
using IPA.Core.Data.Entity.Other;
using IPA.Core.Shared.Helpers.StatusCode;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace IPA.Core.XO.Payment
{
    public static class DecimalHelper
    {
        public static int NumberOfDecimals(this decimal input)
        {
            return BitConverter.GetBytes(decimal.GetBits(input)[3])[2];
        }
    }
    public class PaymentXO : XOBase
    {
        public XO.Payment.Request Request { get; set; }
        public XO.Payment.Response Response { get; set; }

        public override void Validate()
        {
            if (this.Request?.Payment?.AppID <= 0)
                this.ValidationMessageAdd((int)ErrorType.notallowed, (int)ErrorField.missingfields, "The AppID is required for the processing of a Payment.");

            if (this.Request?.Payment?.CompanyID <= 0)
                this.ValidationMessageAdd((int)ErrorType.notallowed, (int)ErrorField.missingfields, "The CompanyID is required for the processing of a Payment.");

            if (this.Request?.Payment?.TCCustID <= 0 || this.Request?.PaymentRequest?.TCCustID <= 0 || this.Request?.Payment?.TCCustID != this.Request?.PaymentRequest?.TCCustID)
                this.ValidationMessageAdd((int)ErrorType.notallowed, (int)ErrorField.InvalidCustID, Constants.IPA_Service_CannotValidateCustID);

            if (this.Request.Payment.PaymentTypeID != (int)Shared.Enums.PaymentType.Verify)
            {
                if (this.Request?.Payment?.PaymentAmount.NumberOfDecimals() > Constants.MaxNumberOfDecimals)
                    this.ValidationMessageAdd((int)TransactionStatus.ProcessPaymentError, (int)IPAErrorField.InvalidAmount, $"Invalid format. Expected US Dollar format X,XXX.XX - not an amount of {this.Request?.Payment?.PaymentAmount}");
            }

            if (!Enum.IsDefined(typeof(Shared.Enums.PaymentSystemType), this.Request?.Payment?.PaymentSystemTypeID))
                this.ValidationMessageAdd((int)ErrorType.notallowed, (int)ErrorField.missingfields, "A Valid Payment System Type ID is required for the processing a Payment.");

            //TODO: refactor paymentType to switch...case... syntax for the following validation
            //processing check
            if (this.Request?.PaymentTender?.ACH == true)
            {
                if (String.IsNullOrEmpty(this.Request?.PaymentTender?.BankAccountNumber) || String.IsNullOrEmpty(this.Request?.PaymentTender?.BankRoutingNumber))
                    this.ValidationMessageAdd((int)ErrorType.failtoprocess, (int)ErrorField.missingfields, $"BankRoutingNumber and BankAccountNumber: {Constants.IsRequired} when payment is checking");
                else if (this.Request?.PaymentTender?.BankAccountNumber?.Length > 17)
                    this.ValidationMessageAdd((int)ErrorType.failtoprocess, (int)StatusCodeEnum.IPAErrorField_InvalidAcctNo, $"BankAccountNumber must not exceed 17 digits.");
                else if (this.Request?.PaymentTender?.BankRoutingNumber?.Length > 9)
                    this.ValidationMessageAdd((int)ErrorType.failtoprocess, (int)StatusCodeEnum.InvalidAchRouteNo, $"BankRoutingNumber must be 9 digits.");
            }
            else if (this.Request?.Payment?.PaymentTypeID == (int)Shared.Enums.PaymentType.Void ||
                     this.Request?.Payment?.PaymentTypeID == (int)Shared.Enums.PaymentType.ChargeBack ||
                     this.Request?.Payment?.PaymentTypeID == (int)Shared.Enums.PaymentType.Refund)
            {
                if (String.IsNullOrEmpty(this.Request?.TCTransactionID))
                    this.ValidationMessageAdd((int)ErrorType.notallowed, (int)ErrorField.missingfields, "The TransactionID is required to process a Void Transaction");
            }
            else //processing card
            {
                //Validate input 
                // This checks for the length of the amount, the length of the precision is checked in the ProcessPaymentRequest method.
                // Restricting the processing of payments over $1 million (6 digits or less is allowed).
                // When processing a Refund, the amount is optional. When the Amount is not supplied, the whole amount paid will get refunded.
                if (this.Request?.Payment?.PaymentTypeID != (int)Shared.Enums.PaymentType.Refund &&
                    this.Request?.Payment?.PaymentTypeID != (int)Shared.Enums.PaymentType.Verify)
                {
                    if (this.Request.Payment.PaymentAmount > Constants.MaxPaymentAmountAllowed)
                    {
                        this.ValidationMessageAdd((int)ErrorType.failtoprocess, (int)IPAErrorField.InvalidAmount, $"Amount: {this.Request.Payment.PaymentAmount} - {Constants.ValueLengthInvalid}");
                        this.Request.Payment.PaymentAmount = 0;
                    }
                }
                else
                {
                    if (this.Request?.Payment?.PaymentTypeID != (int)Shared.Enums.PaymentType.Verify)
                    {
                        if (String.IsNullOrEmpty(this.Request?.CreditCard?.OriginalTCTransactionID))
                            this.ValidationMessageAdd((int)ErrorType.failtoprocess, (int)ErrorField.missingfields, $"TransactionID: {Constants.IsRequired} to process a Refund");
                    }
                }

                if (this.Request?.PaymentTender?.EntryModeTypeID == (int)EntryModeType.Swiped)
                {
                    if (String.IsNullOrEmpty(this.Request?.CreditCard?.EncryptedTracks))
                        this.ValidationMessageAdd((int)ErrorType.failtoprocess, (int)ErrorField.missingfields, $"EncryptedTracks: {Constants.IsRequired} when the card is swiped");
                }

                if (this.Request?.PaymentTender?.EntryModeTypeID == (int)EntryModeType.Keyed)
                {
                    if (String.IsNullOrEmpty(this.Request?.CreditCard?.EncryptedTracks))
                        this.ValidationMessageAdd((int)ErrorType.failtoprocess, (int)ErrorField.missingfields, $"EncryptedTracks: {Constants.IsRequired} when the card is input");
                }
            }

            if (string.IsNullOrEmpty(this.Request?.PaymentRequest?.MessageID))
                this.ValidationMessageAdd((int)ErrorType.failtoprocess, (int)ErrorField.missingfields, "The PaymentRequest messageid is required for the processing of a Payment.");

            switch ((PaymentType)this.Request?.Payment?.PaymentTypeID)
            {
                default:
                    if (string.IsNullOrEmpty(this.Request?.PaymentTender?.CreatedBy) || string.IsNullOrEmpty(this.Request?.PaymentTender?.UpdatedBy) || this.Request?.PaymentTender?.CreatedDate == default(DateTimeOffset) || this.Request?.PaymentTender?.UpdatedDate == default(DateTimeOffset))
                        this.ValidationMessageAdd((int)ErrorType.notallowed, (int)ErrorField.missingfields, "The PaymentTender CreatedBy, UpdatedBy, CreatedDate and UpdatedDate are required for the processing of a Payment.");
                    break;

                case PaymentType.TokenUpdate:
                    if (this.Request.TenderUpdatePayment && (string.IsNullOrEmpty(this.Request?.PaymentTender?.CreatedBy) || string.IsNullOrEmpty(this.Request?.PaymentTender?.UpdatedBy) || this.Request?.PaymentTender?.CreatedDate == default(DateTimeOffset) || this.Request?.PaymentTender?.UpdatedDate == default(DateTimeOffset)))
                        this.ValidationMessageAdd((int)ErrorType.notallowed, (int)ErrorField.missingfields, "The PaymentTender CreatedBy, UpdatedBy, CreatedDate and UpdatedDate are required for the processing of a Payment.");
                    break;

                case PaymentType.Void:
                case PaymentType.ChargeBack:
                case PaymentType.Refund:
                case PaymentType.Unstore:
                case PaymentType.PostAuth:
                    break;
            }
        }
    }

    public class Request
    {
        public string TCCustPassword { get; set; }
        public string MasterTCCustPassword { get; set; }
        public string DeviceSerial { get; set; }
        public string TCTransactionID { get; set; }
        public string TCToken { get; set; }
        public string TimeZoneString { get; set; }
        public bool TenderUpdatePayment { get; set; }
        public TenderFamilyType PaymentMethod { get; set; }
        public EntryModeType PaymentEntryMode { get; set; }
        public int EntryModeStatusID { get; set; }
        public bool DemoMode { get; set; }
        public bool P2PEEnabled { get; set; }
        //public ActionType Action { get; set; }
        public string CheckNumber { get; set; }
        public string BankAccountType { get; set; }
        public string AccountNumber { get; set; }
        public string RoutingNumber { get; set; }
        public List<string> PartnerRegistryKeys { get; set; }
        public Data.Entity.Other.CreditCard CreditCard { get; set; }
        public string CustomFields { get; set; }
        public Data.Entity.Payment Payment { get; set; }
        public Data.Entity.PaymentRequest PaymentRequest { get; set; }
        public Data.Entity.PaymentTender PaymentTender { get; set; }
        public int ReadAttempts { get; set; }
        public bool EMVFallback { get; set; }
        public bool DeviceEMVCapable { get; set; }
    }

    public class Response
    {
        public Response()
        {
            TransactionStatus = PaymentResultStatus.error;
        }

        public List<string> ErrorMessages { get; set; }
        public int? ErrorCode { get; set; }
        public long? TCStatusCode { get; set; }
        public string Result { get; set; }
        public string EntryModeStatusDesc { get; set; }
        public PaymentResultStatus TransactionStatus { get; set; }
        public bool SignatureRequired { get; set; }
        public string MessageID { get; set; }

        public Data.Entity.Other.CreditCard CreditCard { get; set; }
        public Data.Entity.Payment Payment { get; set; }
        public Data.Entity.PaymentRequest PaymentRequest { get; set; }
        public Data.Entity.PaymentTender PaymentTender { get; set; }
        public Data.Entity.PaymentResponse PaymentResponse { get; set; }

        public Data.Entity.PaymentResponseEMV PaymentResponseEMV { get; set; }

        public bool HasErrors()
        {
            return ErrorMessages?.Count > 0 || PaymentResponse?.ErrorTypeID > 0;
        }
    }
}
namespace IPA.Core.XO
{
    [DataContract]
    public class XOBase
    {
        public XOBase()
        {
            ValidationMessages = new List<ValidationMessage>();
        }

        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public Exception Error { get; set; }

        [DataMember]
        public List<ValidationMessage> ValidationMessages { get; set; }

        [DataMember]
        public int BridgeAppID { get; set; }

        public void ValidationMessageAdd(int validationType, int validationCode, string validationText)
        {
            ValidationMessages.Add(new ValidationMessage()
            {
                ValidationCode = validationCode,
                ValidationType = validationType,
                ValidationText = validationText
            });
        }

        public virtual void Validate()
        {
        }
    }
}

namespace IPA.Core.Data.Entity.Other
{
    public class ValidationMessage
    {
        public ValidationMessage()
        {
        }

        public ValidationMessage(int validationCode, int validationType, string validationText)
        {
            this.ValidationCode = validationCode;
            this.ValidationType = validationType;
            this.ValidationText = validationText;
        }

        public int ValidationCode { get; set; }
        public int ValidationType { get; set; }
        public string ValidationText { get; set; }
    }
}

namespace IPA.Core.Shared.Helpers
{
    public static class Constants
    {
        public const Decimal MaxPaymentAmountAllowed = 999999.99M;

        public const int MaxNumberOfDecimals = 2;

        public const string TCLinkSendTimeout = "TCLinkSendTimeout";
        public const string TCLinkReceiveTimeout = "TCLinkReceiveTimeout";

        public const string IsRequired = "is required.";
        public const string ValueLengthInvalid = "Value received does not meet the length requirements.";
        public const string IPA_Service_CannotValidateCustID = "Not able to process request. Failed to validate Cust ID or Password, please contact your Administrator.";
    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IPA.Core.Data.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class Payment
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Payment()
        {
            this.Addresses = new HashSet<Address>();
            this.PaymentRequests = new HashSet<PaymentRequest>();
            this.PaymentResponses = new HashSet<PaymentResponse>();
            this.PaymentResponseEMVs = new HashSet<PaymentResponseEMV>();
            this.PaymentSignatures = new HashSet<PaymentSignature>();
            this.PaymentTenders = new HashSet<PaymentTender>();
            this.Status = new HashSet<Status>();
            this.UserDefinedFields = new HashSet<UserDefinedField>();
        }
        [Required(ErrorMessage = "PaymentID is Required.")]
        public long PaymentID { get; set; }
        [Required(ErrorMessage = "CompanyID is Required.")]
        public int CompanyID { get; set; }
        [Required(ErrorMessage = "PaymentSystemTypeID is Required.")]
        public int PaymentSystemTypeID { get; set; }
        public Nullable<System.DateTimeOffset> PaymentDate { get; set; }
        [Required(ErrorMessage = "PaymentAmount is Required.")]
        public decimal PaymentAmount { get; set; }
        [Required(ErrorMessage = "PaymentTypeID is Required.")]
        public int PaymentTypeID { get; set; }
        public Nullable<long> UID { get; set; }
        [Required(ErrorMessage = "AppID is Required.")]
        public int AppID { get; set; }
        public Nullable<long> OriginalPaymentID { get; set; }
        [Required(ErrorMessage = "TCCustID is Required.")]
        public long TCCustID { get; set; }
        public Nullable<long> CustomerID { get; set; }
        public Nullable<long> ChainID { get; set; }
        public Nullable<long> BatchID { get; set; }
        [MaxLength(30)]
        public string PONumber { get; set; }
        public Nullable<bool> Completed { get; set; }
        [Required(ErrorMessage = "Active is Required.")]
        public bool Active { get; set; }
        [Required(ErrorMessage = "CreatedDate is Required.")]
        public System.DateTimeOffset CreatedDate { get; set; }
        [Required(ErrorMessage = "CreatedBy is Required.")]
        [MaxLength(100)]
        public string CreatedBy { get; set; }
        [Required(ErrorMessage = "UpdatedDate is Required.")]
        public System.DateTimeOffset UpdatedDate { get; set; }
        [Required(ErrorMessage = "UpdatedBy is Required.")]
        [MaxLength(100)]
        public string UpdatedBy { get; set; }
        [Required(ErrorMessage = "IsEMV is Required.")]
        public bool IsEMV { get; set; }
        [Required(ErrorMessage = "DALAppID is Required.")]
        public int DALAppID { get; set; }
        public Nullable<long> MasterTCCustID { get; set; }
        public Nullable<System.Guid> RequestID { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Address> Addresses { get; set; }
        ////public virtual App App { get; set; }
        ////public virtual Company Company { get; set; }
        ////public virtual Customer Customer { get; set; }
        public virtual PaymentType PaymentType { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PaymentRequest> PaymentRequests { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PaymentResponse> PaymentResponses { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PaymentResponseEMV> PaymentResponseEMVs { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PaymentSignature> PaymentSignatures { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PaymentTender> PaymentTenders { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Status> Status { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<UserDefinedField> UserDefinedFields { get; set; }
        public virtual PaymentSystemType PaymentSystemType { get; set; }

    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IPA.Core.Data.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class Address
    {
        [Required(ErrorMessage = "AddressID is Required.")]
        public int AddressID { get; set; }
        [Required(ErrorMessage = "CompanyID is Required.")]
        public int CompanyID { get; set; }
        [Required(ErrorMessage = "PaymentID is Required.")]
        public long PaymentID { get; set; }
        [Required(ErrorMessage = "AddressTypeID is Required.")]
        public int AddressTypeID { get; set; }
        public Nullable<System.DateTimeOffset> StartDate { get; set; }
        public Nullable<System.DateTimeOffset> EndDate { get; set; }
        [Required(ErrorMessage = "Address1 is Required.")]
        [MaxLength(100)]
        public string Address1 { get; set; }
        [Required(ErrorMessage = "Address2 is Required.")]
        [MaxLength(100)]
        public string Address2 { get; set; }
        [Required(ErrorMessage = "Address3 is Required.")]
        [MaxLength(100)]
        public string Address3 { get; set; }
        [Required(ErrorMessage = "StateName is Required.")]
        [MaxLength(50)]
        public string StateName { get; set; }
        [Required(ErrorMessage = "City is Required.")]
        [MaxLength(100)]
        public string City { get; set; }
        [Required(ErrorMessage = "Zip is Required.")]
        [MaxLength(30)]
        public string Zip { get; set; }
        [Required(ErrorMessage = "PhoneWork is Required.")]
        [MaxLength(40)]
        public string PhoneWork { get; set; }
        [Required(ErrorMessage = "PhoneWorkExtension is Required.")]
        [MaxLength(40)]
        public string PhoneWorkExtension { get; set; }
        [Required(ErrorMessage = "PhoneFax is Required.")]
        [MaxLength(40)]
        public string PhoneFax { get; set; }
        [Required(ErrorMessage = "Ordinal is Required.")]
        public byte Ordinal { get; set; }
        [Required(ErrorMessage = "Active is Required.")]
        public bool Active { get; set; }
        [Required(ErrorMessage = "CreatedDate is Required.")]
        public System.DateTime CreatedDate { get; set; }
        [Required(ErrorMessage = "CreatedBy is Required.")]
        [MaxLength(100)]
        public string CreatedBy { get; set; }
        [Required(ErrorMessage = "UpdatedDate is Required.")]
        public System.DateTime UpdatedDate { get; set; }
        [Required(ErrorMessage = "UpdatedBy is Required.")]
        [MaxLength(100)]
        public string UpdatedBy { get; set; }
        [Required(ErrorMessage = "Enabled is Required.")]
        public bool Enabled { get; set; }

        ////public virtual AddressType AddressType { get; set; }
        ////public virtual Company Company { get; set; }
        public virtual Payment Payment { get; set; }

    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IPA.Core.Data.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class PaymentRequest
    {
        [Required(ErrorMessage = "PaymentRequestID is Required.")]
        public long PaymentRequestID { get; set; }
        [Required(ErrorMessage = "CompanyID is Required.")]
        public int CompanyID { get; set; }
        [Required(ErrorMessage = "PaymentID is Required.")]
        public long PaymentID { get; set; }
        [Required(ErrorMessage = "PaymentTenderID is Required.")]
        public long PaymentTenderID { get; set; }
        [Required(ErrorMessage = "MessageID is Required.")]
        [MaxLength(50)]
        public string MessageID { get; set; }
        [Required(ErrorMessage = "TCCustID is Required.")]
        public long TCCustID { get; set; }
        [Required(ErrorMessage = "StatusCodeID is Required.")]
        public int StatusCodeID { get; set; }
        [MaxLength(30)]
        public string Configuration { get; set; }
        [Required(ErrorMessage = "AmountRequested is Required.")]
        public decimal AmountRequested { get; set; }
        [Required(ErrorMessage = "PartialPayment is Required.")]
        public bool PartialPayment { get; set; }
        [Required(ErrorMessage = "CurrencyCode is Required.")]
        [MaxLength(30)]
        public string CurrencyCode { get; set; }
        [Required(ErrorMessage = "RequestToken is Required.")]
        public bool RequestToken { get; set; }
        public Nullable<int> LineItemNo { get; set; }
        [MaxLength(30)]
        public string LineItemDesc { get; set; }
        public Nullable<decimal> AmountLineItem { get; set; }
        [MaxLength(300)]
        public string DisplayMessage { get; set; }
        public Nullable<System.DateTimeOffset> ProcessedDate { get; set; }
        [Required(ErrorMessage = "Active is Required.")]
        public bool Active { get; set; }
        [Required(ErrorMessage = "CreatedDate is Required.")]
        public System.DateTimeOffset CreatedDate { get; set; }
        [Required(ErrorMessage = "CreatedBy is Required.")]
        [MaxLength(100)]
        public string CreatedBy { get; set; }
        [Required(ErrorMessage = "PedalDNS is Required.")]
        [MaxLength(50)]
        public string PedalDNS { get; set; }
        [Required(ErrorMessage = "PedalIPv4 is Required.")]
        [MaxLength(50)]
        public string PedalIPv4 { get; set; }
        [Required(ErrorMessage = "PedalIPv6 is Required.")]
        [MaxLength(50)]
        public string PedalIPv6 { get; set; }
        public Nullable<long> MasterTCCustID { get; set; }

        public virtual Payment Payment { get; set; }
        public virtual PaymentTender PaymentTender { get; set; }
        ////public virtual Company Company { get; set; }
        ////public virtual StatusCode StatusCode { get; set; }

    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IPA.Core.Data.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class PaymentTender
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PaymentTender()
        {
            this.PaymentRequests = new HashSet<PaymentRequest>();
            this.PaymentResponses = new HashSet<PaymentResponse>();
            this.PaymentResponseEMVs = new HashSet<PaymentResponseEMV>();
            this.PaymentSignatures = new HashSet<PaymentSignature>();
            this.Status = new HashSet<Status>();
            this.UserDefinedFields = new HashSet<UserDefinedField>();
        }
        [Required(ErrorMessage = "PaymentTenderID is Required.")]
        public long PaymentTenderID { get; set; }
        [Required(ErrorMessage = "PaymentID is Required.")]
        public long PaymentID { get; set; }
        [Required(ErrorMessage = "TenderTypeID is Required.")]
        public int TenderTypeID { get; set; }
        public Nullable<int> OriginalTenderTypeID { get; set; }
        [Required(ErrorMessage = "CompanyID is Required.")]
        public int CompanyID { get; set; }
        public Nullable<long> DeviceID { get; set; }
        public Nullable<short> Ordinal { get; set; }
        public Nullable<short> OriginalOrdinal { get; set; }
        [MaxLength(20)]
        public string Reference { get; set; }
        [MaxLength(20)]
        public string OriginalReference { get; set; }
        [Required(ErrorMessage = "AmountRequested is Required.")]
        public decimal AmountRequested { get; set; }
        [Required(ErrorMessage = "AmountAuthorized is Required.")]
        public decimal AmountAuthorized { get; set; }
        public Nullable<decimal> AmountCashBack { get; set; }
        public Nullable<decimal> OriginalAmountAuthorized { get; set; }
        public Nullable<bool> DebitCard { get; set; }
        [MaxLength(6)]
        public string CardFirstSix { get; set; }
        [MaxLength(4)]
        public string CardLastFour { get; set; }
        [MaxLength(4)]
        public string CardExpirationYear { get; set; }
        [MaxLength(2)]
        public string CardExpirationMonth { get; set; }
        [MaxLength(150)]
        public string CardHolderName { get; set; }
        [MaxLength(60)]
        public string CardHolderAddress { get; set; }
        [MaxLength(10)]
        public string CardHolderZip { get; set; }
        [Required(ErrorMessage = "EntryModeTypeID is Required.")]
        public int EntryModeTypeID { get; set; }
        public Nullable<bool> CardPresent { get; set; }
        public Nullable<bool> ACH { get; set; }
        [MaxLength(12)]
        public string BankRoutingNumber { get; set; }
        [MaxLength(17)]
        public string BankAccountNumber { get; set; }
        public Nullable<bool> AVS { get; set; }
        [Required(ErrorMessage = "HSA is Required.")]
        public bool HSA { get; set; }
        [Required(ErrorMessage = "FSA is Required.")]
        public bool FSA { get; set; }
        public Nullable<bool> CVVVerified { get; set; }
        public Nullable<bool> TokenRequested { get; set; }
        public Nullable<bool> SignatureCaptured { get; set; }
        [MaxLength(10)]
        public string HSAType { get; set; }
        public Nullable<bool> EMVTransaction { get; set; }
        [MaxLength(255)]
        public string EMVAID { get; set; }
        [MaxLength(16)]
        public string EMVCardHolderVerification { get; set; }
        [MaxLength(16)]
        public string EMVAuthorizationMode { get; set; }
        [MaxLength(128)]
        public string EMVTerminalVerificationResults { get; set; }
        [MaxLength(255)]
        public string EMVIssuerApplicationData { get; set; }
        [Required(ErrorMessage = "Active is Required.")]
        public bool Active { get; set; }
        [Required(ErrorMessage = "CreatedDate is Required.")]
        public System.DateTimeOffset CreatedDate { get; set; }
        [Required(ErrorMessage = "CreatedBy is Required.")]
        [MaxLength(100)]
        public string CreatedBy { get; set; }
        [Required(ErrorMessage = "UpdatedDate is Required.")]
        public System.DateTimeOffset UpdatedDate { get; set; }
        [Required(ErrorMessage = "UpdatedBy is Required.")]
        [MaxLength(100)]
        public string UpdatedBy { get; set; }
        [Required(ErrorMessage = "IsEMV is Required.")]
        public bool IsEMV { get; set; }
        [MaxLength(20)]
        public string CheckNumber { get; set; }
        public Nullable<bool> Savings { get; set; }

        ////public virtual Company Company { get; set; }
        public virtual EntryModeType EntryModeType { get; set; }
        public virtual Payment Payment { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PaymentRequest> PaymentRequests { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PaymentResponse> PaymentResponses { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PaymentResponseEMV> PaymentResponseEMVs { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PaymentSignature> PaymentSignatures { get; set; }
        public virtual TenderType TenderType { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Status> Status { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<UserDefinedField> UserDefinedFields { get; set; }
        public virtual Device Device { get; set; }

    }
}
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IPA.Core.Data.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class PaymentResponse
    {
        [Required(ErrorMessage = "PaymentResponseID is Required.")]
        public long PaymentResponseID { get; set; }
        [Required(ErrorMessage = "CompanyID is Required.")]
        public int CompanyID { get; set; }
        [Required(ErrorMessage = "PaymentID is Required.")]
        public long PaymentID { get; set; }
        [Required(ErrorMessage = "PaymentTenderID is Required.")]
        public long PaymentTenderID { get; set; }
        [Required(ErrorMessage = "EntryModeStatusID is Required.")]
        public int EntryModeStatusID { get; set; }
        [Required(ErrorMessage = "DeclineTypeID is Required.")]
        public int DeclineTypeID { get; set; }
        [Required(ErrorMessage = "TransactionStatusID is Required.")]
        public int TransactionStatusID { get; set; }
        [Required(ErrorMessage = "AmountActual is Required.")]
        public decimal AmountActual { get; set; }
        [Required(ErrorMessage = "TenderTypeID is Required.")]
        public int TenderTypeID { get; set; }
        [MaxLength(100)]
        public string AVSResult { get; set; }
        public Nullable<int> ErrorFieldID { get; set; }
        public Nullable<int> ErrorTypeID { get; set; }
        [MaxLength(500)]
        public string Offenders { get; set; }
        public Nullable<int> CVVResponseCodeID { get; set; }
        public Nullable<int> AddressVerificationSystemID { get; set; }
        [Required(ErrorMessage = "TransactionTypeID is Required.")]
        public int TransactionTypeID { get; set; }
        public Nullable<int> BadDataID { get; set; }
        [MaxLength(50)]
        public string AuthorizationCode { get; set; }
        [Required(ErrorMessage = "TCCustID is Required.")]
        public long TCCustID { get; set; }
        [MaxLength(50)]
        public string TCToken { get; set; }
        [MaxLength(50)]
        public string TCTransID { get; set; }
        [MaxLength(50)]
        public string OriginalTCTransID { get; set; }
        [Required(ErrorMessage = "TCLinkMethod is Required.")]
        [MaxLength(50)]
        public string TCLinkMethod { get; set; }
        [MaxLength(50)]
        public string AuthorizationCodeOffline { get; set; }
        [Required(ErrorMessage = "Active is Required.")]
        public bool Active { get; set; }
        [Required(ErrorMessage = "CreatedDate is Required.")]
        public System.DateTimeOffset CreatedDate { get; set; }
        [Required(ErrorMessage = "CreatedBy is Required.")]
        [MaxLength(100)]
        public string CreatedBy { get; set; }

        public virtual Payment Payment { get; set; }
        public virtual PaymentTender PaymentTender { get; set; }
        //public virtual Company Company { get; set; }

    }
}
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IPA.Core.Data.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class PaymentResponseEMV
    {
        [Required(ErrorMessage = "PaymentResponseEMVID is Required.")]
        public long PaymentResponseEMVID { get; set; }
        [Required(ErrorMessage = "CompanyID is Required.")]
        public int CompanyID { get; set; }
        [Required(ErrorMessage = "PaymentID is Required.")]
        public long PaymentID { get; set; }
        [Required(ErrorMessage = "PaymentTenderID is Required.")]
        public long PaymentTenderID { get; set; }
        [Required(ErrorMessage = "EMVTagGroupTypeID is Required.")]
        public int EMVTagGroupTypeID { get; set; }
        [Required(ErrorMessage = "EMVTags is Required.")]
        [MaxLength(6000)]
        public string EMVTags { get; set; }
        [Required(ErrorMessage = "Ordinal is Required.")]
        public short Ordinal { get; set; }
        [Required(ErrorMessage = "Active is Required.")]
        public bool Active { get; set; }
        [Required(ErrorMessage = "CreatedDate is Required.")]
        public System.DateTimeOffset CreatedDate { get; set; }
        [Required(ErrorMessage = "CreatedBy is Required.")]
        [MaxLength(100)]
        public string CreatedBy { get; set; }

        //public virtual Company Company { get; set; }
        public virtual EMVTagGroupType EMVTagGroupType { get; set; }
        public virtual Payment Payment { get; set; }
        public virtual PaymentTender PaymentTender { get; set; }

    }
}
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IPA.Core.Data.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class PaymentSignature
    {
        [Required(ErrorMessage = "PaymentSignatureID is Required.")]
        public long PaymentSignatureID { get; set; }
        [Required(ErrorMessage = "CompanyID is Required.")]
        public int CompanyID { get; set; }
        [Required(ErrorMessage = "PaymentID is Required.")]
        public long PaymentID { get; set; }
        [Required(ErrorMessage = "PaymentTenderID is Required.")]
        public long PaymentTenderID { get; set; }
        public byte[] SignatureImage { get; set; }
        [MaxLength(50)]
        public string SignatureImageFormat { get; set; }
        [Required(ErrorMessage = "Active is Required.")]
        public bool Active { get; set; }
        [Required(ErrorMessage = "CreatedDate is Required.")]
        public System.DateTimeOffset CreatedDate { get; set; }
        [Required(ErrorMessage = "CreatedBy is Required.")]
        [MaxLength(100)]
        public string CreatedBy { get; set; }
        [Required(ErrorMessage = "UpdatedDate is Required.")]
        public System.DateTimeOffset UpdatedDate { get; set; }
        [Required(ErrorMessage = "UpdatedBy is Required.")]
        [MaxLength(100)]
        public string UpdatedBy { get; set; }
        [Required(ErrorMessage = "TCTransID is Required.")]
        [MaxLength(50)]
        public string TCTransID { get; set; }

        public virtual Payment Payment { get; set; }
        public virtual PaymentTender PaymentTender { get; set; }
        //public virtual Company Company { get; set; }

    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IPA.Core.Data.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class Status
    {
        [Required(ErrorMessage = "StatusID is Required.")]
        public long StatusID { get; set; }
        [Required(ErrorMessage = "CompanyID is Required.")]
        public int CompanyID { get; set; }
        [Required(ErrorMessage = "AppID is Required.")]
        public int AppID { get; set; }
        [Required(ErrorMessage = "Ordinal is Required.")]
        public short Ordinal { get; set; }
        public Nullable<long> PaymentID { get; set; }
        public Nullable<int> MessageID { get; set; }
        public Nullable<long> PaymentTenderID { get; set; }
        public Nullable<int> PackageDeployID { get; set; }
        [Required(ErrorMessage = "StatusTypeID is Required.")]
        public int StatusTypeID { get; set; }
        [Required(ErrorMessage = "StatusCodeID is Required.")]
        public int StatusCodeID { get; set; }
        public string RequestObject { get; set; }
        public string ResponseObject { get; set; }
        [Required(ErrorMessage = "Active is Required.")]
        public bool Active { get; set; }
        [Required(ErrorMessage = "CreatedDate is Required.")]
        public System.DateTimeOffset CreatedDate { get; set; }
        [Required(ErrorMessage = "CreatedBy is Required.")]
        [MaxLength(100)]
        public string CreatedBy { get; set; }
        [MaxLength(5000)]
        public string Host { get; set; }
        public Nullable<System.Guid> RequestID { get; set; }

        ////public virtual App App { get; set; }
        public virtual Payment Payment { get; set; }
        public virtual PaymentTender PaymentTender { get; set; }
        ////public virtual StatusCode StatusCode { get; set; }
        public virtual StatusType StatusType { get; set; }
        ////public virtual Company Company { get; set; }
        ////public virtual PackageDeploy PackageDeploy { get; set; }
        ////public virtual Message Message { get; set; }

    }
}
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IPA.Core.Data.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class UserDefinedField
    {
        [Required(ErrorMessage = "UserDefinedFieldID is Required.")]
        public long UserDefinedFieldID { get; set; }
        [Required(ErrorMessage = "TCCustID is Required.")]
        public long TCCustID { get; set; }
        public Nullable<int> UserDefinedFieldCodeID { get; set; }
        public Nullable<long> PaymentID { get; set; }
        public Nullable<long> PaymentTenderID { get; set; }
        [MaxLength(900)]
        public string ValHash { get; set; }
        [MaxLength(8000)]
        public string Val { get; set; }
        [Required(ErrorMessage = "Ordinal is Required.")]
        public byte Ordinal { get; set; }
        [Required(ErrorMessage = "Active is Required.")]
        public bool Active { get; set; }
        [Required(ErrorMessage = "CreatedDate is Required.")]
        public System.DateTimeOffset CreatedDate { get; set; }
        [Required(ErrorMessage = "CreatedBy is Required.")]
        [MaxLength(100)]
        public string CreatedBy { get; set; }
        [Required(ErrorMessage = "UpdatedDate is Required.")]
        public System.DateTimeOffset UpdatedDate { get; set; }
        [Required(ErrorMessage = "UpdatedBy is Required.")]
        [MaxLength(100)]
        public string UpdatedBy { get; set; }

        public virtual Payment Payment { get; set; }
        public virtual PaymentTender PaymentTender { get; set; }
        ////public virtual UserDefinedFieldCode UserDefinedFieldCode { get; set; }

    }
}
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IPA.Core.Data.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class Device
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Device()
        {
            //this.Configs = new HashSet<Config>();
            this.PaymentTenders = new HashSet<PaymentTender>();
            //this.DeviceInfoes = new HashSet<DeviceInfo>();
        }
        [Required(ErrorMessage = "DeviceID is Required.")]
        public long DeviceID { get; set; }
        [Required(ErrorMessage = "CompanyID is Required.")]
        public int CompanyID { get; set; }
        [Required(ErrorMessage = "ManufacturerID is Required.")]
        public int ManufacturerID { get; set; }
        [Required(ErrorMessage = "ModelID is Required.")]
        public int ModelID { get; set; }
        public Nullable<int> AppID { get; set; }
        [Required(ErrorMessage = "SerialNumber is Required.")]
        [MaxLength(30)]
        public string SerialNumber { get; set; }
        [MaxLength(30)]
        public string AssetNumber { get; set; }
        [MaxLength(20)]
        public string OSVersion { get; set; }
        [MaxLength(20)]
        public string FirmwareVersion { get; set; }
        [MaxLength(20)]
        public string FormsVersion { get; set; }
        [Required(ErrorMessage = "Debit is Required.")]
        public bool Debit { get; set; }
        public Nullable<bool> IsEMVCapable { get; set; }
        [MaxLength(15)]
        public string JDALVersion { get; set; }
        public Nullable<bool> Active { get; set; }
        [Required(ErrorMessage = "CreatedDate is Required.")]
        public System.DateTimeOffset CreatedDate { get; set; }
        [Required(ErrorMessage = "CreatedBy is Required.")]
        [MaxLength(100)]
        public string CreatedBy { get; set; }
        [Required(ErrorMessage = "UpdatedDate is Required.")]
        public System.DateTimeOffset UpdatedDate { get; set; }
        [Required(ErrorMessage = "UpdatedBy is Required.")]
        [MaxLength(100)]
        public string UpdatedBy { get; set; }
        public Nullable<bool> P2PEEnabled { get; set; }
        [MaxLength(20)]
        public string PartNumber { get; set; }

        //public virtual App App { get; set; }
        //public virtual Company Company { get; set; }
        //public virtual Manufacturer Manufacturer { get; set; }
        //public virtual Model Model { get; set; }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<Config> Configs { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PaymentTender> PaymentTenders { get; set; }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<DeviceInfo> DeviceInfoes { get; set; }

    }
}
