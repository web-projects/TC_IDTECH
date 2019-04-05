using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.ConfigIDTech.Configuration
{
    [Serializable]
    public class Msr
    {
       /*public enum MSR_SETTING
       {
            BEEPER_SETTING       = 0x11,   // 11 - Beep Setting
            CHAR_DELAY           = 0x12,   // 12 - Character Delay
            TRACK_SELECTION      = 0x13,   // 13 - Track Selection
            POLLING_INTERVAL     = 0x14,   // 14 - Polling Interval
            TRACK_SEPARATOR      = 0x17,   // 17 - Track Separator
            SEND_OPTION          = 0x19,   // 19 - Send Option
            MSR_READING          = 0x1A,   // 1a - MSR Reading
            DECODING_DIRECTION   = 0x1D,   // 1d - Decoding Direction
            TERMINATOR           = 0x21,   // 21 - Terminator
            FOREIGN_KB           = 0x24,   // 24 - Foreign KB 
            SET_ID               = 0x30,   // 30 - Set ID
            TRACK_1_PREFIX       = 0x34,   // 34 - Track 1 Prefix
            TRACK_2_PREFIX       = 0x35,   // 35 - Track 2 Prefix
            TRACK_3_PREFIX       = 0x36,   // 36 - Track 3 Prefix
            TRACK_1_SUFFIX       = 0x37,   // 37 - Track 1 Suffix
            TRACK_2_SUFFIX       = 0x38,   // 38 - Track 2 Suffix
            TRACK_3_SUFFIX       = 0x39,   // 39 - Track 3 Suffix
            PREPAN_NOT_MASK      = 0x49,   // 49 - PRE-PAN to not mask
            POSTPAN_NOT_MASK     = 0x4A,   // 4a - POST-PAN to not mask
            MASK_PATH_WITH_CHAR  = 0x4B,   // 4b - mask the PAN with this character
            MASK_EXPIRATION      = 0x50,   // 50 - mask or display expiration date
            INCLUDE_MOD10_CHKD   = 0x55,   // 55 - include mod10 check digit
            DUPT_KEY             = 0x58,   // 58 - DUKPT Key
            HASH_OPTION          = 0x5C,   // 5c - Hash Option
            LRC_CHARACTER        = 0x60,   // 60 - LRC character
            TRACK1_7BIT_START    = 0x61,   // 61 - Track 1 7 Bit Start Char
            T15B_START           = 0x63,   // 63 - T15B Start
            TRACK2_7BIT_START    = 0x64,   // 64 - Track 2 7 Bit Start Char
            T25B_START           = 0x65,   // 65 - T25BStart
            TRACK3_7BIT_START    = 0x66,   // 66 - Track 3 7 Bit Start Char
            T35B_START           = 0x68,   // 68 - T35BStart
            TRACK1_END_SENTINEL  = 0x69,   // 69 - Track 1 End Sentinel
            TRACK2_END_SENTINEL  = 0x6A,   // 6a - Track 2 End Sentinel
            TRACK3_END_SENTINEL  = 0x6B,   // 6b - Track 3 End Sentinel
            BEEP_SETTING         = 0x6C,   // 6c - Beep Setting
            TRACK2_ERROR_CODE    = 0x6D,   // 6d - Track 2 error code
            TRACK3_ERROR_CODE    = 0x6E,   // 6e - Track 3 error code
            SECURED_OUTPUT_LRC   = 0x6F,   // 6f - Secured output format Lrc option
            JIS_T12_SS_ES        = 0x72,   // 72 - JIS T12 SS/ES
            JIS_T3_SS_ES         = 0x73,   // 73 - JIS T3 SS/ES
            CHECK_TRACK_SYNC     = 0x7B,   // 7b - check for track sync bits
            ENCRYPTION_OPTION    = 0x84,   // 84 - Encryption Option (Forced encryption or not)
            ONLY_ENCRYPT_STRUCT  = 0x85,   // 85 - Only Encryption Structure
            MASKED_CLEAR_DATA    = 0x86,   // 86 - Masked  / clear data sending option
            HASH_TYPE_SELECTION  = 0x88,   // 88 - Hash type selection
            T3_EXP_DATA_POSITION = 0x89,   // 89 - T3 Exp Data Position
            REMOTE_KEY_INJECTION = 0xAD,   // ad - Remote Key Injection Timeout
            EQUIPMENT_SETTING    = 0xAE,   // ae - Equip Setting
            BITWISE_CUST_SETTING = 0xAF,   // af - Bitwise customer settings
            PREAMBLE_D2          = 0xD2,   // d2 - Preamble
            PREAMBLE_D3          = 0xD3,   // d3 - Postamble
        };

       public List<MSRSettings> msr_settings = new List<MSRSettings>()
       { 
            new MSRSettings(MSR_SETTING.BEEPER_SETTING      , "Beep Setting",                                 ""),
            new MSRSettings(MSR_SETTING.CHAR_DELAY          , "Character Delay",                              ""),
            new MSRSettings(MSR_SETTING.TRACK_SELECTION     , "Track Selection",                              ""),
            new MSRSettings(MSR_SETTING.POLLING_INTERVAL    , "Polling Interval",                             ""),
            new MSRSettings(MSR_SETTING.TRACK_SEPARATOR     , "Track Separator",                              ""),
            new MSRSettings(MSR_SETTING.SEND_OPTION         , "Send Option",                                  ""),
            new MSRSettings(MSR_SETTING.MSR_READING         , "MSR Reading",                                  ""),
            new MSRSettings(MSR_SETTING.DECODING_DIRECTION  , "Decoding Direction",                           ""),
            new MSRSettings(MSR_SETTING.TERMINATOR          , "Terminator",                                   ""),
            new MSRSettings(MSR_SETTING.FOREIGN_KB          , "Foreign KB ",                                  ""),
            new MSRSettings(MSR_SETTING.SET_ID              , "Set ID",                                       ""),
            new MSRSettings(MSR_SETTING.TRACK_1_PREFIX      , "Track 1 Prefix",                               ""),
            new MSRSettings(MSR_SETTING.TRACK_2_PREFIX      , "Track 2 Prefix",                               ""),
            new MSRSettings(MSR_SETTING.TRACK_3_PREFIX      , "Track 3 Prefix",                               ""),
            new MSRSettings(MSR_SETTING.TRACK_1_SUFFIX      , "Track 1 Suffix",                               ""),
            new MSRSettings(MSR_SETTING.TRACK_2_SUFFIX      , "Track 2 Suffix",                               ""),
            new MSRSettings(MSR_SETTING.TRACK_3_SUFFIX      , "Track 3 Suffix",                               ""),
            new MSRSettings(MSR_SETTING.PREPAN_NOT_MASK     , "PRE-PAN to not mask",                          ""),
            new MSRSettings(MSR_SETTING.POSTPAN_NOT_MASK    , "POST-PAN to not mask",                         ""),
            new MSRSettings(MSR_SETTING.MASK_PATH_WITH_CHAR , "mask the PAN with this character",             ""),
            new MSRSettings(MSR_SETTING.MASK_EXPIRATION     , "mask or display expiration date",              ""),
            new MSRSettings(MSR_SETTING.INCLUDE_MOD10_CHKD  , "include mod10 check digit",                    ""),
            new MSRSettings(MSR_SETTING.DUPT_KEY            , "DUKPT Key",                                    ""),
            new MSRSettings(MSR_SETTING.HASH_OPTION         , "Hash Option",                                  ""),
            new MSRSettings(MSR_SETTING.LRC_CHARACTER       , "LRC character",                                ""),
            new MSRSettings(MSR_SETTING.TRACK1_7BIT_START   , "Track 1 7 Bit Start Char",                     ""),
            new MSRSettings(MSR_SETTING.T15B_START          , "T15B Start",                                   ""),
            new MSRSettings(MSR_SETTING.TRACK2_7BIT_START   , "Track 2 7 Bit Start Char",                     ""),
            new MSRSettings(MSR_SETTING.T25B_START          , "T25BStart",                                    ""),
            new MSRSettings(MSR_SETTING.TRACK3_7BIT_START   , "Track 3 7 Bit Start Char",                     ""),
            new MSRSettings(MSR_SETTING.T35B_START          , "T35BStart",                                    ""),
            new MSRSettings(MSR_SETTING.TRACK1_END_SENTINEL , "Track 1 End Sentinel",                         ""),
            new MSRSettings(MSR_SETTING.TRACK2_END_SENTINEL , "Track 2 End Sentinel",                         ""),
            new MSRSettings(MSR_SETTING.TRACK3_END_SENTINEL , "Track 3 End Sentinel",                         ""),
            new MSRSettings(MSR_SETTING.BEEP_SETTING        , "Beep Setting",                                 ""),
            new MSRSettings(MSR_SETTING.TRACK2_ERROR_CODE   , "Track 2 error code",                           ""),
            new MSRSettings(MSR_SETTING.TRACK3_ERROR_CODE   , "Track 3 error code",                           ""),
            new MSRSettings(MSR_SETTING.SECURED_OUTPUT_LRC  , "Secured output format Lrc option",             ""),
            new MSRSettings(MSR_SETTING.JIS_T12_SS_ES       , "JIS T12 SS/ES",                                ""),
            new MSRSettings(MSR_SETTING.JIS_T3_SS_ES        , "JIS T3 SS/ES",                                 ""),
            new MSRSettings(MSR_SETTING.CHECK_TRACK_SYNC    , "check for track sync bits",                    ""),
            new MSRSettings(MSR_SETTING.ENCRYPTION_OPTION   , "Encryption Option (Forced encryption or not)", ""),
            new MSRSettings(MSR_SETTING.ONLY_ENCRYPT_STRUCT , "Only Encryption Structure",                    ""),
            new MSRSettings(MSR_SETTING.MASKED_CLEAR_DATA   , "Masked  / clear data sending option",          ""),
            new MSRSettings(MSR_SETTING.HASH_TYPE_SELECTION , "Hash type selection",                          ""),
            new MSRSettings(MSR_SETTING.T3_EXP_DATA_POSITION, "T3 Exp Data Position",                         ""),
            new MSRSettings(MSR_SETTING.REMOTE_KEY_INJECTION, "Remote Key Injection Timeout",                 ""),
            new MSRSettings(MSR_SETTING.EQUIPMENT_SETTING   , "Equip Setting",                                ""), 
            new MSRSettings(MSR_SETTING.BITWISE_CUST_SETTING, "Bitwise customer settings",                    ""), 
            new MSRSettings(MSR_SETTING.PREAMBLE_D2         , "Preamble",                                     ""),
            new MSRSettings(MSR_SETTING.PREAMBLE_D3         , "Postamble",                                    "")
       };*/
       
        public List<MSRSettings> msr_settings = new List<MSRSettings>()
       { 
            new MSRSettings("11", "Beep Setting",                                 ""),
            new MSRSettings("12", "Character Delay",                              ""),
            new MSRSettings("13", "Track Selection",                              ""),
            new MSRSettings("14", "Polling Interval",                             ""),
            new MSRSettings("17", "Track Separator",                              ""),
            new MSRSettings("19", "Send Option",                                  ""),
            new MSRSettings("1a", "MSR Reading",                                  ""),
            new MSRSettings("1d", "Decoding Direction",                           ""),
            new MSRSettings("21", "Terminator",                                   ""),
            new MSRSettings("24", "Foreign KB ",                                  ""),
            new MSRSettings("30", "Set ID",                                       ""),
            new MSRSettings("34", "Track 1 Prefix",                               ""),
            new MSRSettings("35", "Track 2 Prefix",                               ""),
            new MSRSettings("36", "Track 3 Prefix",                               ""),
            new MSRSettings("37", "Track 1 Suffix",                               ""),
            new MSRSettings("38", "Track 2 Suffix",                               ""),
            new MSRSettings("39", "Track 3 Suffix",                               ""),
            new MSRSettings("49", "PRE-PAN to not mask",                          ""),
            new MSRSettings("4a", "POST-PAN to not mask",                         ""),
            new MSRSettings("4b", "mask the PAN with this character",             ""),
            new MSRSettings("50", "mask or display expiration date",              ""),
            new MSRSettings("55", "include mod10 check digit",                    ""),
            new MSRSettings("58", "DUKPT Key",                                    ""),
            new MSRSettings("5c", "Hash Option",                                  ""),
            new MSRSettings("60", "LRC character",                                ""),
            new MSRSettings("61", "Track 1 7 Bit Start Char",                     ""),
            new MSRSettings("63", "T15B Start",                                   ""),
            new MSRSettings("64", "Track 2 7 Bit Start Char",                     ""),
            new MSRSettings("65", "T25BStart",                                    ""),
            new MSRSettings("66", "Track 3 7 Bit Start Char",                     ""),
            new MSRSettings("68", "T35BStart",                                    ""),
            new MSRSettings("69", "Track 1 End Sentinel",                         ""),
            new MSRSettings("6a", "Track 2 End Sentinel",                         ""),
            new MSRSettings("6b", "Track 3 End Sentinel",                         ""),
            new MSRSettings("6c", "Beep Setting",                                 ""),
            new MSRSettings("6d", "Track 2 error code",                           ""),
            new MSRSettings("6e", "Track 3 error code",                           ""),
            new MSRSettings("6f", "Secured output format Lrc option",             ""),
            new MSRSettings("72", "JIS T12 SS/ES",                                ""),
            new MSRSettings("73", "JIS T3 SS/ES",                                 ""),
            new MSRSettings("7b", "check for track sync bits",                    ""),
            new MSRSettings("84", "Encryption Option (Forced encryption or not)", ""),
            new MSRSettings("85", "Only Encryption Structure",                    ""),
            new MSRSettings("86", "Masked  / clear data sending option",          ""),
            new MSRSettings("88", "Hash type selection",                          ""),
            new MSRSettings("89", "T3 Exp Data Position",                         ""),
            new MSRSettings("ad", "Remote Key Injection Timeout",                 ""),
            new MSRSettings("ae", "Equip Setting",                                ""), 
            new MSRSettings("af", "Bitwise customer settings",                    ""), 
            new MSRSettings("d2", "Preamble",                                     ""),
            new MSRSettings("d3", "Postamble",                                    "")
       };
    }

    [Serializable]
    public class MSRSettings
    {
        [JsonProperty(PropertyName = "function_id", Order = 1)]
        public string function_id { get; set; }
        [JsonProperty(PropertyName = "name", Order = 2)]
        public string name { get; set; }
        [JsonProperty(PropertyName = "value", Order = 3)]
        public string value { get; set; }
 
        /*[JsonIgnore]
        public byte function_value { get; }
        public MSRSettings(MSR_SETTING function_id, string name, string value)
        {
            this.function_value = (byte) function_id;
            this.function_id = this.function_value.ToString("x");
            this.name = name;
            this.value = value;
        }*/

        [JsonConstructor]
        public MSRSettings(string _function_id, string _name, string _value)
        {
            this.function_id = _function_id;
            this.name = _name;
            this.value = _value;
        }
    }
}
