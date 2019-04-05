using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HidLibrary;

// Include DeviceConfiguration.dll in the References list
using IPA.CommonInterface;
using IPA.CommonInterface.Interfaces;
using IPA.CommonInterface.Helpers;
using IPA.CommonInterface.ConfigIDTech;
using IPA.CommonInterface.ConfigIDTech.Configuration;
using System.Collections;
using IPA.LoggerManager;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using IPA.Core.Client.Dal.Models;
using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Services;
using System.Configuration;
using System.Reflection;

namespace IPA.MainApp
{
    public enum DEV_USB_MODE
    {
        USB_HID_MODE = 0,
        USB_KYB_MODE = 1
    }

    public partial class Application : Form
    {
        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        /********************************************************************************************************/
        // ATTRIBUTES SECTION
        /********************************************************************************************************/
        #region -- attributes section --

        public Panel appPnl;

        bool formClosing = false;

        // AppDomain Artifacts
        AppDomainCfg appDomainCfg;
        AppDomain appDomainDevice;
        IDevicePlugIn devicePlugin;
        const string MODULE_NAME = "DeviceConfiguration";
        const string PLUGIN_NAME = "IPA.DAL.RBADAL.DeviceCfg";
        string ASSEMBLY_NAME = typeof(IPA.DAL.RBADAL.DeviceCfg).Assembly.FullName;

        // Application Configuration
        bool tc_show_settings_tab;
        bool tc_show_configuration_tab;
        bool tc_show_raw_mode_tab;
        bool tc_show_terminal_data_tab;
        bool tc_show_json_tab;
        bool tc_always_on_top;
        int  tc_transaction_timeout;
        int  tc_transaction_collection_timeout;
        int  tc_minimum_transaction_length;
        bool isNonAugusta;

        DEV_USB_MODE dev_usb_mode;

        Stopwatch stopWatch;
        internal static System.Timers.Timer TransactionTimer { get; set; }
        Color TEXTBOX_FORE_COLOR;

        // Always on TOP
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
        #endregion

        public Application()
        {
            InitializeComponent();

            this.Text = "IDTECH Device Discovery Application";

            // Settings Tab
            string show_settings_tab = System.Configuration.ConfigurationManager.AppSettings["tc_show_settings_tab"] ?? "false";
            bool.TryParse(show_settings_tab, out tc_show_settings_tab);
            if(!tc_show_settings_tab)
            {
                tabControl1.TabPages.Remove(tabPage2);
            }

            // Configuration Tab
            string show_configuration_tab = System.Configuration.ConfigurationManager.AppSettings["tc_show_configuration_tab"] ?? "false";
            bool.TryParse(show_configuration_tab, out tc_show_configuration_tab);
            if(!tc_show_configuration_tab)
            {
                tabControl1.TabPages.Remove(tabPage3);
            }

            // Raw Mode Tab
            string show_raw_mode_tab = System.Configuration.ConfigurationManager.AppSettings["tc_show_raw_mode_tab"] ?? "false";
            bool.TryParse(show_raw_mode_tab, out tc_show_raw_mode_tab);
            if(!tc_show_raw_mode_tab)
            {
                tabControl1.TabPages.Remove(tabPage4);
            }

            // Terminal Data Tab
            string show_terminal_data_tab = System.Configuration.ConfigurationManager.AppSettings["tc_show_terminal_data_tab"] ?? "false";
            bool.TryParse(show_terminal_data_tab, out tc_show_terminal_data_tab);
            if(!tc_show_terminal_data_tab)
            {
                tabControl1.TabPages.Remove(tabPage5);
            }

            // Json Tab
            string show_json_tab = System.Configuration.ConfigurationManager.AppSettings["tc_show_json_tab"] ?? "false";
            bool.TryParse(show_json_tab, out tc_show_json_tab);
            if(!tc_show_json_tab)
            {
                tabControl1.TabPages.Remove(tabPage6);
            }

            // Application Always on Top
            string always_on_top = System.Configuration.ConfigurationManager.AppSettings["tc_always_on_top"] ?? "true";
            bool.TryParse(always_on_top, out tc_always_on_top);

            // Transaction Timer
            tc_transaction_timeout = 2000;
            string transaction_timeout = System.Configuration.ConfigurationManager.AppSettings["tc_transaction_timeout"] ?? "2000";
            int.TryParse(transaction_timeout, out tc_transaction_timeout);

            // Transaction Collection Timer
            tc_transaction_collection_timeout = 5000;
            string transaction_collection_timeout = System.Configuration.ConfigurationManager.AppSettings["tc_transaction_collection_timeout"] ?? "5000";
            int.TryParse(transaction_collection_timeout, out tc_transaction_collection_timeout);

            // Transaction Minimum Length
            tc_minimum_transaction_length = 1000;
            string minimum_transaction_length = System.Configuration.ConfigurationManager.AppSettings["tc_minimum_transaction_length"] ?? "1000";
            int.TryParse(minimum_transaction_length, out tc_minimum_transaction_length);

            // Original Forecolor
            TEXTBOX_FORE_COLOR = txtCardData.ForeColor;

            // Setup Logging
            SetupLogging();

            // Initialize Device
            InitalizeDevice();
        }

        /********************************************************************************************************/
        // FORM ELEMENTS
        /********************************************************************************************************/
        #region -- form elements --
    
        private void OnFormLoad(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            if(tc_always_on_top)
            {
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            formClosing = true;

            if (devicePlugin != null)
            {
                try
                {
                    devicePlugin.SetFormClosing(formClosing);
                }
                catch(Exception ex)
                {
                    Logger.error("main: Form1_FormClosing() - exception={0}", (object) ex.Message);
                }
            }
        }

        void SetupLogging()
        {
            try
            {
                var logLevels = ConfigurationManager.AppSettings["IPA.DAL.Application.Client.LogLevel"]?.Split('|') ?? new string[0];
                if(logLevels.Length > 0)
                {
                    string fullName = Assembly.GetEntryAssembly().Location;
                    string logname = System.IO.Path.GetFileNameWithoutExtension(fullName) + ".log";
                    string path = System.IO.Directory.GetCurrentDirectory(); 
                    string filepath = path + "\\" + logname;

                    int levels = 0;
                    foreach(var item in logLevels)
                    {
                        foreach(var level in LogLevels.LogLevelsDictonary.Where(x => x.Value.Equals(item)).Select(x => x.Key))
                        {
                            levels += (int)level;
                        }
                    }

                    Logger.SetFileLoggerConfiguration(filepath, levels);

                    Logger.info( "LOGGING INITIALIZED.");

                    //Logger.info( "LOG ARG1:", "1111");
                    //Logger.info( "LOG ARG1:{0}, ARG2:{1}", "1111", "2222");
                    Logger.debug("THIS IS A DEBUG STRING");
                    Logger.warning("THIS IS A WARNING");
                    Logger.error("THIS IS AN ERROR");
                    Logger.fatal("THIS IS FATAL");
                }
            }
            catch(Exception e)
            {
                Logger.error("main: SetupLogging() - exception={0}", (object) e.Message);
            }
        }

        #endregion

        /********************************************************************************************************/
        // DELEGATES SECTION
        /********************************************************************************************************/
        #region -- delegates section --

        private void InitalizeDeviceUI(object sender, DeviceNotificationEventArgs e)
        {
            InitalizeDevice(true);
        }

        private void ProcessCardDataUI(object sender, DeviceNotificationEventArgs e)
        {
            ProcessCardData(e.Message);
        }

        private void ProcessCardDataErrorUI(object sender, DeviceNotificationEventArgs e)
        {
            ProcessCardDataError(e.Message[0]);
        }

        private void UnloadDeviceConfigurationDomain(object sender, DeviceNotificationEventArgs e)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                // Terminate Transaction Timer if running
                TransactionTimer?.Stop();

                ClearUI();

                // Unload The Plugin
                appDomainCfg.UnloadPlugin(appDomainDevice);

                // wait for a new device to connect
                WaitForDeviceToConnect();

            }).Start();
        }

        private void GetDeviceConfigurationUI(object sender, DeviceNotificationEventArgs e)
        {
            GetDeviceConfiguration(e.Message);
        }

        private void SetDeviceConfigurationUI(object sender, DeviceNotificationEventArgs e)
        {
            SetDeviceConfiguration(e.Message);
        }

        private void SetDeviceModeUI(object sender, DeviceNotificationEventArgs e)
        {
            SetDeviceMode(e.Message);
        }

        private void SetExecuteResultUI(object sender, DeviceNotificationEventArgs e)
        {
            SetExecuteResult(e.Message);
        }

        private void ShowJsonConfigUI(object sender, DeviceNotificationEventArgs e)
        {
            ShowJsonConfig(e.Message);
        }

        private void ShowTerminalDataUI(object sender, DeviceNotificationEventArgs e)
        {
            ShowTerminalData(e.Message);
        }

        private void SetModeButtonEnabledUI(object sender, DeviceNotificationEventArgs e)
        {
            SetModeButtonEnabled(e.Message);
        }

        protected void OnDeviceNotificationUI(object sender, DeviceNotificationEventArgs args)
        {
            Logger.debug("main: notification type={0}", args.NotificationType);

            switch (args.NotificationType)
            {
                case NOTIFICATION_TYPE.NT_INITIALIZE_DEVICE:
                {
                    break;
                }

                case NOTIFICATION_TYPE.NT_DEVICE_UPDATE_CONFIG:
                {
                    UpdateUI();
                    break;
                }

                case NOTIFICATION_TYPE.NT_UNLOAD_DEVICE_CONFIGDOMAIN:
                {
                    UnloadDeviceConfigurationDomain(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_PROCESS_CARDDATA:
                {
                    ProcessCardDataUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_PROCESS_CARDDATA_ERROR:
                {
                    ProcessCardDataErrorUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_GET_DEVICE_CONFIGURATION:
                {
                    GetDeviceConfigurationUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SET_DEVICE_CONFIGURATION:
                {
                    SetDeviceConfigurationUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SET_DEVICE_MODE:
                {
                    SetDeviceModeUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SET_EXECUTE_RESULT:
                {
                    SetExecuteResultUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SHOW_TERMINAL_DATA:
                {
                    ShowTerminalDataUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SHOW_JSON_CONFIG:
                {
                    ShowJsonConfigUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_ENABLE_MODE_BUTTON:
                {
                    SetModeButtonEnabledUI(sender, args);
                    break;
                }
            }
        }

        #endregion

        /********************************************************************************************************/
        // GUI - DELEGATE SECTION
        /********************************************************************************************************/
        #region -- gui delegate section --

        private void ClearUI()
        {
            if (InvokeRequired)
            {
                MethodInvoker Callback = new MethodInvoker(ClearUI);
                Invoke(Callback);
            }
            else
            {
                this.lblSerialNumber.Text = "";
                this.lblFirmwareVersion.Text = "";
                this.lblModelName.Text = "";
                this.lblModelNumber.Text = "";
                this.lblPort.Text = "";
                this.txtCardData.Text = "";

                // Disable Tab(s)
                this.tabPage1.Enabled = false;
                this.tabPage2.Enabled = false;
                this.tabPage3.Enabled = false;
                this.tabPage4.Enabled = false;
                this.tabPage5.Enabled = false;
                this.tabPage6.Enabled = false;

                this.txtCardData.Text = "";
                this.txtCardData.ForeColor = this.txtCardData.BackColor;
                this.btnShowTags.Text = "TAGS";
                this.btnShowTags.Enabled = false;
                this.btnShowTags.Visible = false;
                this.listView1.Visible = false;
            }
        }

        private void UpdateUI()
        {
            if (InvokeRequired)
            {
                MethodInvoker Callback = new MethodInvoker(UpdateUI);
                Invoke(Callback);
            }
            else
            {
                SetConfiguration();
            }
        }

        #endregion

        /********************************************************************************************************/
        // DEVICE ARTIFACTS
        /********************************************************************************************************/
        #region -- device artifacts --

        private void ClearConfiguration()
        {
            Debug.WriteLine("main: Clear GUI elements =========================================================");
            if (InvokeRequired)
            {
                MethodInvoker Callback = new MethodInvoker(ClearConfiguration);
                Invoke(Callback);
            }
            else
            {
                this.lblSerialNumber.Text = "";
                this.lblFirmwareVersion.Text = "";
                this.lblModelName.Text = "";
                this.lblModelNumber.Text = "";
                this.lblPort.Text = "";
                this.txtCardData.Text = "";
            }
        }

        private void SetConfiguration()
        {
            Debug.WriteLine("main: update GUI elements =========================================================");

            this.lblSerialNumber.Text = "";
            this.lblFirmwareVersion.Text = "";
            this.lblModelName.Text = "";
            this.lblModelNumber.Text = "";
            this.lblPort.Text = "";
            this.txtCardData.Text = "";

            try
            {
                string[] config = devicePlugin.GetConfig();

                if (config != null)
                {
                    this.lblSerialNumber.Text = config[0];
                    this.lblFirmwareVersion.Text = config[1];
                    this.lblModelName.Text = config[2];
                    this.lblModelNumber.Text = config[3];

                    // value expected: either dashed or space separated
                    string [] worker = null;
                    if(config[4] != null)
                    {
                        if(config[4].Trim().Contains(' '))
                        {
                            worker = config[4].Trim().Split(' ');
                        }
                        else
                        {
                            worker = config[4].Trim().Split('-');
                        }
                    }
                    if(worker != null)
                    {
                        string worker1 = worker[0];
                        if(!worker1.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                        {
                            worker1 += "/";
                            if(worker[1].Equals("KB"))
                            {
                                worker1 += "KEYBOARD";
                            }
                            else
                            {
                                worker1 += worker[1];
                            }
                        }
                        this.lblPort.Text = worker1;
                    }
                    else
                    {
                        this.lblPort.Text = "UNKNOWN";
                    }
                }

                // Enable Buttons
                this.btnCardRead.Enabled = (dev_usb_mode == DEV_USB_MODE.USB_HID_MODE) ? true : false;

                // Enable Tab(s)
                this.tabPage1.Enabled = true;
                this.tabPage2.Enabled = tc_show_settings_tab;
                this.tabPage3.Enabled = tc_show_configuration_tab;
                this.tabPage4.Enabled = tc_show_raw_mode_tab;
                this.tabPage5.Enabled = tc_show_terminal_data_tab;
                this.picBoxConfigWait.Visible  = false;

                // KB Mode
                if(dev_usb_mode == DEV_USB_MODE.USB_KYB_MODE)
                {
                    tabControl1.SelectedTab = this.tabPage1;
                    this.txtCardData.ReadOnly = false;
                    this.txtCardData.GotFocus += CardDataTextBoxGotFocus;
                    this.txtCardData.ForeColor = this.txtCardData.BackColor;

                    stopWatch = new Stopwatch();
                    stopWatch.Start();

                    // Transaction Timer
                    SetTransactionTimer();

                    this.Invoke(new MethodInvoker(() =>
                    {
                        this.txtCardData.Focus();
                    }));
                }
                else
                {
                    TransactionTimer?.Stop();
                    this.txtCardData.ForeColor = TEXTBOX_FORE_COLOR;
                    this.txtCardData.ReadOnly = true;
                    this.txtCardData.GotFocus -= CardDataTextBoxGotFocus;
                }
            }
            catch(Exception ex)
            {
                Logger.error("main: SetConfiguration() exception={0}", (object)ex.Message);
            }
        }

        private void SetTransactionTimer()
        {
            TransactionTimer = new System.Timers.Timer(tc_transaction_timeout);
            TransactionTimer.AutoReset = false;
            TransactionTimer.Elapsed += (sender, e) => RaiseTimerExpired(new TimerEventArgs { Timer = TimerType.Transaction });
            TransactionTimer.Start();
        }

        private void RaiseTimerExpired(TimerEventArgs e)
        {
            TransactionTimer?.Stop();

            // Check for valid collection and completion of collection
            if(stopWatch.ElapsedMilliseconds > tc_transaction_collection_timeout)
            {
                int minTransactionLen = tc_minimum_transaction_length;
                if (isNonAugusta)
                    minTransactionLen = 120;
                if(this.txtCardData.Text.Length > minTransactionLen)
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        string data = txtCardData.Text;
                        if (isNonAugusta)
                        {
                            txtCardData.Text = "*** TRANSACTION DATA CAPTURED : MSR ***";
                        }
                        else
                        {
                            txtCardData.Text = "*** TRANSACTION DATA CAPTURED : EMV ***";
                        }
                        this.txtCardData.ForeColor = TEXTBOX_FORE_COLOR;
                        // Set TAGS to display
                        SetTagData(data, false);
                    }));
                }
                else
                {
                    // This could be an MSR fallback transaction
                    this.Invoke(new MethodInvoker(() =>
                    {
                        string data = txtCardData.Text;
                        if(data.Length > 0)
                        {
                            string message = devicePlugin.GetErrorMessage(data);
                            txtCardData.Text = message;
                            this.txtCardData.ForeColor = TEXTBOX_FORE_COLOR;
                            Debug.WriteLine("main: card data=[{0}]", (object) data);
                        }
                        else
                        {
                            SetTransactionTimer();
                        }
                    }));
                }
            }
            else
            {
                SetTransactionTimer();
            }

            Debug.WriteLine("main: transaction timer raised ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            this.Invoke(new MethodInvoker(() =>
            {
                if(!string.IsNullOrEmpty(this.txtCardData.Text))
                {
                    Debug.WriteLine("main: card data length=[0}", this.txtCardData.Text.Length);
                }
            }));
        }

        private void InitalizeDevice(bool unload = false)
        {
            // Unload Domain
            if (unload && appDomainCfg != null)
            {
                appDomainCfg.UnloadPlugin(appDomainDevice);

                // Test Unload
                appDomainCfg.TestIfUnloaded(devicePlugin);
            }

            appDomainCfg = new AppDomainCfg();

            // AppDomain Interface
            appDomainDevice = appDomainCfg.CreateAppDomain(MODULE_NAME);

            // Load Interface
            devicePlugin = appDomainCfg.InstantiatePlugin(appDomainDevice, ASSEMBLY_NAME, PLUGIN_NAME);

            // Initialize interface
            if (devicePlugin != null)
            {
                // Disable Tab(s)
                this.tabPage1.Enabled = false;
                this.tabPage2.Enabled = false;
                this.tabPage3.Enabled = false;
                this.tabPage4.Enabled = false;
                this.tabPage5.Enabled = false;

                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Debug.WriteLine("\nmain: new device detected! +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n");

                    // Setup DeviceCfg Event Handlers
                    devicePlugin.OnDeviceNotification += new EventHandler<DeviceNotificationEventArgs>(this.OnDeviceNotificationUI);

                    if(tc_show_json_tab && dev_usb_mode == DEV_USB_MODE.USB_HID_MODE)
                    {
                        try
                        {
                            this.Invoke(new MethodInvoker(() =>
                            {
                                if(tabControl1.Contains(tabPage6))
                                {
                                    this.picBoxJsonWait.Visible = true;
                                    this.tabControl1.SelectedTab = this.tabPage6;
                                }
                            }));
                        }
                        catch(Exception ex)
                        {
                            Logger.error("main: InitalizeDevice() exception={0}", (object)ex.Message);
                        }
                    }

                    try
                    {
                        // Initialize Device
                        devicePlugin.DeviceInit();
                        Debug.WriteLine("main: loaded plugin={0} ++++++++++++++++++++++++++++++++++++++++++++", (object)devicePlugin.PluginName);
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine("main: InitalizeDevice() exception={0}", (object)ex.Message);
                        if(ex.Message.Equals("NoDevice"))
                        {
                            WaitForDeviceToConnect();
                        }
                    }
                }).Start();
            }
        }

        private void WaitForDeviceToConnect()
        {
            if(dev_usb_mode == DEV_USB_MODE.USB_HID_MODE)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    tabControl1.SelectedTab = this.tabPage1;
                    if(tabControl1.Contains(tabPage2))
                    {
                        tabControl1.TabPages.Remove(tabPage2);
                    }
                    if(tabControl1.Contains(tabPage3))
                    {
                        tabControl1.TabPages.Remove(tabPage3);
                    }
                    if(tabControl1.Contains(tabPage4))
                    {
                        tabControl1.TabPages.Remove(tabPage4);
                    }
                    if(tabControl1.Contains(tabPage5))
                    {
                        tabControl1.TabPages.Remove(tabPage5);
                    }
                    if(tabControl1.Contains(tabPage6))
                    {
                        tabControl1.TabPages.Remove(tabPage6);
                    }
                }));
            }

            // Wait for a new device to connect
            new Thread(() =>
            {
                bool foundit = false;
                Thread.CurrentThread.IsBackground = true;

                Debug.Write("Waiting for new device to connect");

                string description = "";

                // Wait for a device to attach
                while (!formClosing && !foundit)
                {
                    HidDevice device = HidDevices.Enumerate(Device_IDTech.IDTechVendorID).FirstOrDefault();

                    if (device != null)
                    {
                        foundit = true;
                        description = device.Attributes.ProductHexId;
                        device.CloseDevice();
                    }
                    else
                    {
                        Debug.Write(".");
                        Thread.Sleep(1000);
                    }
                }

                // Initialize Device
                if (!formClosing && foundit)
                {
                    Debug.WriteLine("found one with ID={0}", (object) description);

                    Thread.Sleep(3000);

                    // Initialize Device
                    InitalizeDeviceUI(this, new DeviceNotificationEventArgs());
                }

            }).Start();
        }

        private void SetTagData(string tags, bool isHIDMode)
        {
            Debug.WriteLine("main: card data=[{0}]", (object) tags);
            string [] parsed = devicePlugin.ParseCardData(tags);
            if(parsed.Length > 0)
            {
                if(listView1.Items.Count > 0)
                {
                    listView1.Items.Clear();
                }
                string cardnumber = "";
                foreach(string val in parsed)
                {
                    string [] tlv = val.Split(':');
                    ListViewItem item1 = new ListViewItem(tlv[0], 0);
                    item1.SubItems.Add(tlv[1]);
                    listView1.Items.Add(item1);

                    if(isHIDMode)
                    {
                        if(tlv[0].Equals("5A"))
                        {
                            cardnumber = tlv[1];
                            txtCardData.Text += "\r\nCARD NUMBER: " + cardnumber;
                        }
                    }
                    else
                    {
                        if(tlv[0].Equals("DFEF5B"))
                        {
                            cardnumber = tlv[1];
                            txtCardData.Text += "\r\nCARD NUMBER: " + cardnumber;
                        }
                    }
                }

                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                this.btnShowTags.Enabled = true;
                this.btnShowTags.Visible = true;
            }
        }

        private void ProcessCardData(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    object [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                    // update the view only once
                    bool updateView = true;
                    bool.TryParse((string) data[4], out updateView);
                    if(updateView)
                    {
                        this.txtCardData.Text = (string) data[0];
                        this.btnCardRead.Enabled = (dev_usb_mode == DEV_USB_MODE.USB_HID_MODE) ? true : false;

                        this.txtCardData.ForeColor = TEXTBOX_FORE_COLOR;

                        // Set TAGS to display
                        bool isHIDMode = true;
                        if(bool.TryParse((string) data[3], out isHIDMode))
                        {
                            SetTagData((string) data[1], isHIDMode);
                        }
                        else
                        {
                            SetTagData((string) data[1], isHIDMode);
                        }

                        // Enable Tab(s)
                        this.tabPage1.Enabled = true;
                        this.tabPage2.Enabled = tc_show_settings_tab;
                        this.tabPage3.Enabled = tc_show_configuration_tab;
                        this.tabPage4.Enabled = tc_show_raw_mode_tab;
                        this.tabPage5.Enabled = tc_show_terminal_data_tab;
                    }

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        devicePlugin.CardReadNextState(data[2]);
                    }).Start();
                }
                catch (Exception exp)
                {
                    Debug.WriteLine("main: ProcessCardData() - exception={0}", (object)exp.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }
    
        private void ProcessCardDataError(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                //string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                this.txtCardData.Text = payload.ToString();
                this.btnCardRead.Enabled = (dev_usb_mode == DEV_USB_MODE.USB_HID_MODE) ? true : false;

                // Enable Tab(s)
                this.tabPage1.Enabled = true;
                this.tabPage2.Enabled = tc_show_settings_tab;
                this.tabPage3.Enabled = tc_show_configuration_tab;
                this.tabPage4.Enabled = tc_show_raw_mode_tab;
                this.tabPage5.Enabled = tc_show_terminal_data_tab;
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void GetDeviceConfiguration(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
            try
            {
                this.btnReadConfig.Enabled = true;

                string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();

                if(data.Length == 5)
                {
                    this.lblExpMask.Text    = data[0];
                    this.lblPanDigits.Text  = data[1];
                    this.lblSwipeForce.Text = data[2];
                    this.lblSwipeMask.Text  = data[3];
                    this.lblMsrSetting.Text = data[4];
                }

                // Enable Tabs
                this.tabPage1.Enabled = true;
                this.tabPage2.Enabled = tc_show_settings_tab;
                this.tabPage3.Enabled = tc_show_configuration_tab;
                this.tabPage4.Enabled = tc_show_raw_mode_tab;
                this.tabPage5.Enabled = tc_show_terminal_data_tab;
                this.picBoxConfigWait.Visible = false;
                this.picBoxJsonWait.Visible = false;
            }
            catch (Exception exp)
            {
                Debug.WriteLine("main: GetDeviceConfiguration() - exception={0}", (object)exp.Message);
            }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void SetDeviceConfiguration(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    // update settings in panel
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();

                    // Expiration Mask
                    this.cBxExpirationMask.Checked = data[0].Equals("Masked", StringComparison.OrdinalIgnoreCase) ? true : false;

                    // PAN Clear Digits
                    this.txtPAN.Text = data[1];

                    // Swipe Force Mask
                    string [] values = data[2].Split(',');

                    // Process Individual values
                    if(values.Length == 4)
                    {
                        string [] track1 = values[0].Split(':');
                        string [] track2 = values[1].Split(':');
                        string [] track3 = values[2].Split(':');
                        string [] track3Card0 = values[3].Split(':');
                        string t1Value = track1[1].Trim();
                        string t2Value = track2[1].Trim();
                        string t3Value = track3[1].Trim();
                        string t3Card0Value = track3Card0[1].Trim();
                        bool t1Val = t1Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;
                        bool t2Val = t2Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;
                        bool t3Val = t3Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;
                        bool t3Card0Val = t3Card0Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;

                        // Compare to existing values
                        if(this.cBxTrack1.Checked != t1Val) {
                            this.cBxTrack1.Checked = t1Val;
                        }

                        if(this.cBxTrack2.Checked != t2Val) {
                            this.cBxTrack2.Checked = t2Val;
                        }

                        if(this.cBxTrack3.Checked != t3Val) {
                            this.cBxTrack3.Checked = t3Val;
                        }

                        if(this.cBxTrack3Card0.Checked != t3Card0Val) {
                            this.cBxTrack3Card0.Checked = t3Card0Val;
                        }

                        // Swipe Mask
                        values = data[3].Split(',');

                        // Process Individual values
                        track1 = values[0].Split(':');
                        track2 = values[1].Split(':');
                        track3 = values[2].Split(':');

                        t1Value = track1[1].Trim();
                        t2Value = track2[1].Trim();
                        t3Value = track3[1].Trim();

                        t1Val = t1Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;
                        t2Val = t2Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;
                        t3Val = t3Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;

                        // Compare to existing values
                        if(this.cBxSwipeMaskTrack1.Checked != t1Val) {
                            this.cBxSwipeMaskTrack1.Checked = t1Val;
                        }

                        if(this.cBxSwipeMaskTrack2.Checked != t2Val) {
                            this.cBxSwipeMaskTrack2.Checked = t2Val;
                        }

                        if(this.cBxSwipeMaskTrack3.Checked != t3Val) {
                            this.cBxSwipeMaskTrack3.Checked = t3Val;
                        }
                    }

                    // Enable Button
                    this.btnReadConfig.Enabled = true;

                    // Enable Tabs
                    this.tabPage1.Enabled = true;
                    this.tabPage2.Enabled = tc_show_settings_tab;
                    this.tabPage3.Enabled = tc_show_configuration_tab;
                    this.tabPage4.Enabled = tc_show_raw_mode_tab;
                    this.tabPage5.Enabled = tc_show_terminal_data_tab;
                    this.picBoxConfigWait.Visible  = false;
                    this.picBoxJsonWait.Visible  = false;
                }
                catch (Exception exp)
                {
                    Debug.WriteLine("main: SetDeviceConfiguration() - exception={0}", (object)exp.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void SetDeviceMode(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                    this.btnMode.Text = data[0];
                    this.btnMode.Visible = true;
                    this.btnMode.Enabled = true;
                    isNonAugusta = false;
                    if (data[0].Contains("OLDIDTECH"))
                    {
                        string [] split = data[0].Split(':');
                        isNonAugusta = true;
                        //this.btnMode.Text = "Set to " + (split[1].Contains("HID") ? "HID" : "KB");
                        this.btnMode.Text = split[1];

                        // Startup Transition to HID mode
                        if (this.picBoxJsonWait.Visible == true)
                        {
                            this.picBoxJsonWait.Visible = false;
                            tabControl1.SelectedTab = this.tabPage1;
                        }

                        // Only have btnCardRead for HID devices.  KB devices always read cards.
                        this.btnCardRead.Enabled = (split[1].Contains("HID")); 
                        dev_usb_mode = (data[0].Contains("HID")) ? DEV_USB_MODE.USB_KYB_MODE : DEV_USB_MODE.USB_HID_MODE;

                        if (tabControl1.Contains(tabPage2))
                        {
                            tabControl1.TabPages.Remove(tabPage2);
                        }
                        if (tabControl1.Contains(tabPage3))
                        {
                            tabControl1.TabPages.Remove(tabPage3);
                        }
                        if (tabControl1.Contains(tabPage4))
                        {
                            tabControl1.TabPages.Remove(tabPage4);
                        }
                        if (tabControl1.Contains(tabPage5))
                        {
                            tabControl1.TabPages.Remove(tabPage5);
                        }
                        if (tabControl1.Contains(tabPage6))
                        {
                            tabControl1.TabPages.Remove(tabPage6);
                        }
                        tabControl1.SelectedTab = this.tabPage1;
                    }
                    else if (data[0].Contains("HID") || data[0].Contains("UNKNOWN"))
                    {
                        if(data[0].Contains("UNKNOWN"))
                        {
                            this.btnMode.Visible = false;
                        }
                        else
                        {
                            dev_usb_mode = DEV_USB_MODE.USB_KYB_MODE;
                        }

                        // Startup Transition to HID mode
                        if(this.picBoxJsonWait.Visible == true)
                        {
                            this.picBoxJsonWait.Visible = false;
                            tabControl1.SelectedTab = this.tabPage1;
                        }

                        this.btnCardRead.Enabled = false;

                        if(tabControl1.Contains(tabPage2))
                        {
                            tabControl1.TabPages.Remove(tabPage2);
                        }
                        if(tabControl1.Contains(tabPage3))
                        {
                            tabControl1.TabPages.Remove(tabPage3);
                        }
                        if(tabControl1.Contains(tabPage4))
                        {
                            tabControl1.TabPages.Remove(tabPage4);
                        }
                        if(tabControl1.Contains(tabPage5))
                        {
                            tabControl1.TabPages.Remove(tabPage5);
                        }
                        if(tabControl1.Contains(tabPage6))
                        {
                            tabControl1.TabPages.Remove(tabPage6);
                        }
                        tabControl1.SelectedTab = this.tabPage1;
                    }
                    else
                    {
                        dev_usb_mode = DEV_USB_MODE.USB_HID_MODE;

                        this.btnCardRead.Enabled = true;

                        if(!tabControl1.Contains(tabPage2) && tc_show_settings_tab)
                        {
                            tabControl1.TabPages.Add(tabPage2);
                        }
                        if(!tabControl1.Contains(tabPage3) && tc_show_configuration_tab)
                        {
                            tabControl1.TabPages.Add(tabPage3);
                        }
                        if(!tabControl1.Contains(tabPage4) && tc_show_raw_mode_tab)
                        {
                            tabControl1.TabPages.Add(tabPage4);
                        }
                        if(!tabControl1.Contains(tabPage5) && tc_show_terminal_data_tab)
                        {
                            tabControl1.TabPages.Add(tabPage5);
                        }
                        if(!tabControl1.Contains(tabPage6) && tc_show_json_tab)
                        {
                            tabControl1.TabPages.Add(tabPage6);
                            tabControl1.SelectedTab = this.tabPage6;
                            this.tabPage6.Enabled = true;
                            this.picBoxJsonWait.Visible = true;
                        }
                    }
                }
                catch (Exception exp)
                {
                    Debug.WriteLine("main: SetDeviceConfiguration() - exception={0}", (object)exp.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void SetExecuteResult(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                    this.txtCommandResult.Text = "RESPONSE: [" + data[0] + "]";
                    this.btnExecute.Enabled = true;
                }
                catch (Exception exp)
                {
                    Debug.WriteLine("main: SetDeviceConfiguration() - exception={0}", (object)exp.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void ShowTerminalData(object payload)
        {
            if(dev_usb_mode == DEV_USB_MODE.USB_HID_MODE)
            {
                // Invoker with Parameter(s)
                MethodInvoker mi = () =>
                {
                    try
                    {
                        if(tc_show_terminal_data_tab)
                        {
                            string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                            this.txtTerminalData.Text = data[0];
                        }
                    }
                    catch (Exception exp)
                    {
                        Debug.WriteLine("main: ShowTerminalData() - exception={0}", (object) exp.Message);
                    }
                };

                if (InvokeRequired)
                {
                    BeginInvoke(mi);
                }
                else
                {
                    Invoke(mi);
                }
            }
        }

        private void ShowJsonConfig(object payload)
        {
            if(dev_usb_mode == DEV_USB_MODE.USB_HID_MODE)
            {
                // Invoker with Parameter(s)
                MethodInvoker mi = () =>
                {
                    try
                    {
                        if(tc_show_json_tab)
                        {
                            string [] filename = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                            this.txtJson.Text = File.ReadAllText(filename[0]);
                            tabControl1.SelectedTab = this.tabPage6;
                            this.picBoxJsonWait.Visible = false;
                        }
                    }
                    catch (Exception exp)
                    {
                        Debug.WriteLine("main: ShowJsonConfig() - exception={0}", (object) exp.Message);
                    }
                };

                if (InvokeRequired)
                {
                    BeginInvoke(mi);
                }
                else
                {
                    Invoke(mi);
                }
            }
            else if(this.picBoxJsonWait.Visible == true)
            {
                this.picBoxJsonWait.Visible = false;
                tabControl1.SelectedTab = this.tabPage1;
            }
        }

        private void SetModeButtonEnabled(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                    this.btnMode.Enabled = data[0].Equals("Enable") ? true : false;
                }
                catch (Exception exp)
                {
                    Debug.WriteLine("main: SetModeButtonEnabled() - exception={0}", (object) exp.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        #endregion

        /**************************************************************************/
        // CONFIGURATION TAB
        /**************************************************************************/
        #region -- configuration tab --

        public List<MsrConfigItem> configExpirationMask;
        public List<MsrConfigItem> configPanDigits;
        public List<MsrConfigItem> configSwipeForceEncryption;
        public List<MsrConfigItem> configSwipeMask;

        public static void SetDeviceConfig(IDevicePlugIn devicePlugin, object payload)
        {
            try
            {
                // Make call to DeviceCfg
                devicePlugin.SetDeviceConfiguration(payload);
            }
            catch (Exception exp)
            {
                Debug.WriteLine("main: SetDeviceConfiguration() - exception={0}", (object)exp.Message);
            }
        }

        private void OnConfigurationControlActive(object sender, EventArgs e)
        {
            ConfigSerializer serializer = devicePlugin.GetConfigSerializer();

            // Update settings
            if(serializer != null)
            {
                // EXPIRATION MASK
                this.cBxExpirationMask.Checked = serializer?.terminalCfg?.user_configuration?.expiration_masking?? false;

                // PAN DIGITS
                this.txtPAN.Text = serializer?.terminalCfg?.user_configuration?.pan_clear_digits.ToString();

                // SWIPE FORCE
                this.cBxTrack1.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track1?? false;
                this.cBxTrack2.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track2?? false;
                this.cBxTrack3.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track3?? false;
                this.cBxTrack3Card0.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track3card0?? false;

                // SWIPE MASK
                this.cBxSwipeMaskTrack1.Checked = serializer?.terminalCfg?.user_configuration?.swipe_mask.track1?? false;
                this.cBxSwipeMaskTrack2.Checked = serializer?.terminalCfg?.user_configuration?.swipe_mask.track2?? false;
                this.cBxSwipeMaskTrack3.Checked = serializer?.terminalCfg?.user_configuration?.swipe_mask.track3?? false;

                // Invoker without Parameter(s)
                this.Invoke((MethodInvoker)delegate()
                {
                    this.OnConfigureClick(this, null);
                });
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                ConfigSerializer serializer = devicePlugin.GetConfigSerializer();

                // Update Configuration File
                if(serializer != null)
                {
                    // Update Data: EXPIRATION MASKING
                    serializer.terminalCfg.user_configuration.expiration_masking = this.cBxExpirationMask.Checked;
                    // PAN Clear Digits
                    serializer.terminalCfg.user_configuration.pan_clear_digits = Convert.ToInt32(this.txtPAN.Text);
                    // Swipe Force Mask
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track1 = this.cBxTrack1.Checked;
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track2 = this.cBxTrack2.Checked;
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track3 = this.cBxTrack3.Checked;
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track3card0 = this.cBxTrack3Card0.Checked;
                    // Swipe Mask
                    serializer.terminalCfg.user_configuration.swipe_mask.track1 = this.cBxSwipeMaskTrack1.Checked;
                    serializer.terminalCfg.user_configuration.swipe_mask.track2 = this.cBxSwipeMaskTrack2.Checked;
                    serializer.terminalCfg.user_configuration.swipe_mask.track3 = this.cBxSwipeMaskTrack3.Checked;

                    // WRITE to Config
                    serializer.WriteConfig();
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine("main: SaveConfiguration() - exception={0}", (object)exp.Message);
            }
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            if(this.txtCommand.Text.Length > 5)
            {
                this.btnExecute.Visible = true;
            }
            else
            {
                this.btnExecute.Visible = false;
            }
        }

        private void OnCardDataKeyEvent(object sender, KeyEventArgs e)
        {
            if(dev_usb_mode == DEV_USB_MODE.USB_KYB_MODE)
            {
                //Debug.WriteLine("main: key down event => key={0}", e.KeyData);
                // Start a new collection
                if(stopWatch.ElapsedMilliseconds > tc_transaction_collection_timeout)
                {
                    this.txtCardData.Text = "";
                    this.txtCardData.ForeColor = this.txtCardData.BackColor;
                    this.btnShowTags.Enabled = false;
                    this.btnShowTags.Visible = false;
                    this.listView1.Visible = false;
                    SetTransactionTimer();
                    Debug.WriteLine("main: new scan detected ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                }
                stopWatch.Restart();
            }
        }

        private void CardDataTextBoxGotFocus(object sender, EventArgs args)
        {
            HideCaret(this.txtCardData.Handle);
        }

        private void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab.Name.Equals("tabPage3"))
            {
                // Configuration Mode
                this.Invoke(new MethodInvoker(() =>
                {
                    this.tabPage3.Enabled = true;
                    this.picBoxConfigWait.Enabled = true;
                    this.picBoxConfigWait.Visible = true;
                }));
            }
            else if (tabControl1.SelectedTab.Name.Equals("tabPage4"))
            {
                // Raw Mode TabPage: set focus to command field
                this.Invoke(new MethodInvoker(() =>
                {
                    this.txtCommand.Focus();
                }));
            }
        }

        #endregion
        /**************************************************************************/
        // ACTIONS TAB
        /**************************************************************************/
        #region -- actions tab --
        private void OnCardReadClick(object sender, EventArgs e)
        {
            // Disable Tab(s)
            this.tabPage1.Enabled = false;
            this.tabPage2.Enabled = false;
            this.tabPage3.Enabled = false;
            this.tabPage4.Enabled = false;
            this.tabPage5.Enabled = false;
            this.tabPage6.Enabled = false;

            this.btnCardRead.Enabled = false;

            this.btnShowTags.Text = "TAGS";
            this.btnShowTags.Enabled = false;
            this.btnShowTags.Visible = false;
            this.listView1.Visible = false;

            // Clear field
            this.txtCardData.Text = "";
            this.txtCardData.ForeColor = this.txtCardData.BackColor;

            // Set Focus to reader input
            this.txtCardData.Focus();

            // MSR Read
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                devicePlugin.GetCardData();
            }).Start();
        }

        private void OnModeClick(object sender, EventArgs e)
        {
            string mode = this.btnMode.Text;
            TransactionTimer?.Stop();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    devicePlugin.SetDeviceMode(mode);
                }
                catch(Exception ex)
                {
                    Debug.WriteLine("main: OnModeClick() exception={0}", (object)ex.Message);
                }

            }).Start();

            // Disable MODE Button
            this.btnMode.Enabled = false;
            // Clear Card Data
            this.txtCardData.Text = "";
        }

        private void OnConfigureClick(object sender, EventArgs e)
        {
            // Disable Tabs
            this.tabPage1.Enabled = false;
            this.tabPage2.Enabled = false;
            this.tabPage3.Enabled = false;
            this.tabPage4.Enabled = false;
            this.tabPage5.Enabled = false;

            this.Invoke(new MethodInvoker(() =>
            {
                this.picBoxConfigWait.Visible  = true;
                this.picBoxConfigWait.Refresh();
                System.Windows.Forms.Application.DoEvents();
            }));

            // EXPIRATION MASK
            configExpirationMask = new List<MsrConfigItem>
            {
                { new MsrConfigItem() { Name="expirationmask", Id=(int)EXPIRATION_MASK.MASK, Value=string.Format("{0}", this.cBxExpirationMask.Checked.ToString()) }},
            };

            // PAN DIGITS
            configPanDigits = new List<MsrConfigItem>
            {
            { new MsrConfigItem() { Name="digits", Id=(int)PAN_DIGITS.DIGITS, Value=string.Format("{0}", this.txtPAN.Text) }},
            };

            // SWIPE FORCE
            configSwipeForceEncryption = new List<MsrConfigItem>
            {
                { new MsrConfigItem() { Name="track1",      Id=(int)SWIPE_FORCE_ENCRYPTION.TRACK1, Value=string.Format("{0}",      this.cBxTrack1.Checked.ToString()) }},
                { new MsrConfigItem() { Name="track2",      Id=(int)SWIPE_FORCE_ENCRYPTION.TRACK2, Value=string.Format("{0}",      this.cBxTrack2.Checked.ToString()) }},
                { new MsrConfigItem() { Name="track3",      Id=(int)SWIPE_FORCE_ENCRYPTION.TRACK3, Value=string.Format("{0}",      this.cBxTrack3.Checked.ToString()) }},
                { new MsrConfigItem() { Name="track3Card0", Id=(int)SWIPE_FORCE_ENCRYPTION.TRACK3CARD0, Value=string.Format("{0}", this.cBxTrack3Card0.Checked.ToString()) }}
            };

            // SWIPE MASK
            configSwipeMask = new List<MsrConfigItem>
            {
                { new MsrConfigItem() { Name="track1", Id=(int)SWIPE_MASK.TRACK1, Value=string.Format("{0}", this.cBxSwipeMaskTrack1.Checked.ToString()) }},
                { new MsrConfigItem() { Name="track2", Id=(int)SWIPE_MASK.TRACK2, Value=string.Format("{0}", this.cBxSwipeMaskTrack2.Checked.ToString()) }},
                { new MsrConfigItem() { Name="track3", Id=(int)SWIPE_MASK.TRACK3, Value=string.Format("{0}", this.cBxSwipeMaskTrack3.Checked.ToString()) }}
            };

            // Build Payload Package
            object payload = new object[4] { configExpirationMask, configPanDigits, configSwipeForceEncryption, configSwipeMask };

            // Save to Configuration File
            if(e != null)
            {
                SaveConfiguration();
            }

            // Settings Read
            new Thread(() => SetDeviceConfig(devicePlugin, payload)).Start();
        }

        private void OnReadConfigClick(object sender, EventArgs e)
        {
            btnReadConfig.Enabled = false;

            // Clear fields
            this.lblExpMask.Text = "";
            this.lblPanDigits.Text = "";
            this.lblSwipeForce.Text = "";
            this.lblSwipeMask.Text = "";
            this.lblMsrSetting.Text = "";

            // Disable Tabs
            this.tabPage1.Enabled = false;
            this.tabPage2.Enabled = false;
            this.tabPage3.Enabled = false;
            this.tabPage4.Enabled = false;

            // Settings Read
            new Thread(() =>
            {
            Thread.CurrentThread.IsBackground = true;

            try
            {
                devicePlugin.GetDeviceConfiguration();
            }
            catch (Exception exp)
            {
                Debug.WriteLine("main: btnReadConfig_Click() - exception={0}", (object)exp.Message);
            }
            }).Start();
        }

        private void OnExecuteCommandClick(object sender, EventArgs e)
        {
            string command = this.txtCommand.Text;

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                devicePlugin.DeviceCommand(command, true);
            }).Start();

            this.btnExecute.Enabled = false;
            this.txtCommandResult.Text = "";
        }

        private void OnCloseJsonClick(object sender, EventArgs e)
        {
            if(tc_show_json_tab && tabControl1.Contains(tabPage6))
            {
                tabControl1.TabPages.Remove(tabPage6);
            }
        }

        private void OnShowTagsClick(object sender, EventArgs e)
        {
            if(this.btnShowTags.Text.Equals("CLOSE"))
            {
                this.btnShowTags.Text = "TAGS";
                this.listView1.Visible = false;
            }
            else
            {
                this.btnShowTags.Text = "CLOSE";
                this.listView1.Visible = true;
            }
        }

        #endregion
    }
}
