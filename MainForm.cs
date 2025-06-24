using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using System.Web;
using System.Xml.Serialization;

namespace ServerLauncher
{
    public partial class MainForm : Form
    {
        private ServerSettings settings = new ServerSettings();
        private Dictionary<string, Process> runningProcesses = new Dictionary<string, Process>();
        private Dictionary<string, DateTime> processStartTimes = new Dictionary<string, DateTime>();
        private Dictionary<string, TextBox> serverLogTextBoxes = new Dictionary<string, TextBox>();
        private ConcurrentDictionary<string, StringBuilder> serverLogs = new ConcurrentDictionary<string, StringBuilder>();
        private Dictionary<string, CheckBox> autoRestartCheckBoxes = new Dictionary<string, CheckBox>();
        private Dictionary<string, CheckBox> externalWindowCheckBoxes = new Dictionary<string, CheckBox>();
        private System.Windows.Forms.Timer statusUpdateTimer;
        private string settingsFilePath;
        
        // For Azuriom web integration
        private bool webIntegrationEnabled = false;
        private Thread webListenerThread;
        private HttpListener webListener;

        public MainForm()
        {
            InitializeComponent();
            
            // Settings file path is in the same directory as the executable
            settingsFilePath = Path.Combine(Application.StartupPath, "ServerSettings.xml");
            
            // Load or create settings
            LoadSettings();
            
            // Initialize UI with the server list
            InitializeServerList();
            
            // Initialize server tabs
            InitializeServerTabs();
            
            // Handle ListView resize to reposition checkboxes
            lstServers.Resize += LstServers_Resize;
            
            statusUpdateTimer = new System.Windows.Forms.Timer();
            statusUpdateTimer.Interval = 2000; // Check every 2 seconds
            statusUpdateTimer.Tick += StatusUpdateTimer_Tick;
            statusUpdateTimer.Start();
            
            // Set up context menu event handlers
            menuItemStartServer.Click += menuItemStartServer_Click;
            menuItemStopServer.Click += menuItemStopServer_Click;
            menuItemRestartServer.Click += menuItemRestartServer_Click;
            
            LogMessage("Server Launcher started. Ready to manage server applications.");
            LogMessage(string.Format("Server startup delay is set to {0} ms", settings.StartupDelay));
            LogMessage("Right-click on a server to start, stop, or restart it individually.");
            LogMessage("Click Settings to configure servers and startup delay.");
            
            // Initialize web integration if enabled
            webIntegrationEnabled = settings.ApiEnabled;
            if (webIntegrationEnabled)
            {
                InitializeWebIntegration();
            }
        }

        private void LoadSettings()
        {
            // Try to load settings from file
            if (File.Exists(settingsFilePath))
            {
                settings = ServerSettings.LoadFromFile(settingsFilePath);
                LogMessage("Settings loaded from " + settingsFilePath);
            }
            else
            {
                // Create default settings with the original server list
                settings = new ServerSettings();
                
                // Add default servers
                settings.Servers.Add(new ServerInfo("AccountDatabaseProxy", "AccountDatabaseProxy.exe"));
                settings.Servers.Add(new ServerInfo("AgentServer", "AgentServer.exe"));
                settings.Servers.Add(new ServerInfo("BattleServer", "BattleServer.exe"));
                settings.Servers.Add(new ServerInfo("DatabaseProxy", "DatabaseProxy.exe"));
                settings.Servers.Add(new ServerInfo("FieldServer", "FieldServer.exe"));
                settings.Servers.Add(new ServerInfo("MasterServer", "MasterServer.exe"));
                settings.Servers.Add(new ServerInfo("WorldServer", "WorldServer.exe"));
                settings.Servers.Add(new ServerInfo("AuthAgent", Path.Combine("AuthSystem", "AuthAgent.exe")));
                settings.Servers.Add(new ServerInfo("AuthServer", Path.Combine("AuthSystem", "AuthServer.exe")));
                settings.Servers.Add(new ServerInfo("LoginFront", Path.Combine("AuthSystem", "LoginFront.exe")));
                settings.Servers.Add(new ServerInfo("ShopServer", Path.Combine("ShopServer", "ShopServer.exe")));
                
                // Save the default settings
                settings.SaveToFile(settingsFilePath);
                LogMessage("Created default settings file at " + settingsFilePath);
            }
        }

        private void SaveSettings()
        {
            if (settings.SaveToFile(settingsFilePath))
            {
                LogMessage("Settings saved to " + settingsFilePath);
            }
        }

        private void InitializeServerList()
        {
            // Clear the existing list and checkboxes
            lstServers.Items.Clear();
            autoRestartCheckBoxes.Clear();
            externalWindowCheckBoxes.Clear();
            
            // Update the ListView with server information
            foreach (var server in settings.Servers)
            {
                var item = new ListViewItem(server.Name);
                item.SubItems.Add("Stopped");
                item.SubItems.Add("--:--:--");  // Runtime column
                item.SubItems.Add("");          // Auto-restart column (will be filled with checkbox)
                item.SubItems.Add("");          // External window column (will be filled with checkbox)
                lstServers.Items.Add(item);
                
                // Create checkbox for auto-restart
                CheckBox chkAutoRestart = new CheckBox();
                chkAutoRestart.Checked = server.AutoRestart;
                chkAutoRestart.Tag = server.Name; // Store server name for identification
                chkAutoRestart.CheckedChanged += AutoRestartCheckBox_CheckedChanged;
                
                // Position the checkbox in the auto-restart column
                Rectangle rect = lstServers.GetItemRect(lstServers.Items.Count - 1);
                int xOffset = lstServers.Columns[0].Width + lstServers.Columns[1].Width + lstServers.Columns[2].Width + 10;
                chkAutoRestart.Location = new Point(xOffset, rect.Y);
                chkAutoRestart.Size = new Size(15, 15);
                
                // Add the checkbox to the ListView
                lstServers.Controls.Add(chkAutoRestart);
                
                // Store the checkbox for later use
                autoRestartCheckBoxes[server.Name] = chkAutoRestart;
                
                // Create checkbox for external window
                CheckBox chkExternalWindow = new CheckBox();
                chkExternalWindow.Checked = server.UseExternalWindow;
                chkExternalWindow.Tag = server.Name; // Store server name for identification
                chkExternalWindow.CheckedChanged += ExternalWindowCheckBox_CheckedChanged;
                
                // Position the checkbox in the external window column
                int externalWindowOffset = xOffset + lstServers.Columns[3].Width + 10;
                chkExternalWindow.Location = new Point(externalWindowOffset, rect.Y);
                chkExternalWindow.Size = new Size(15, 15);
                
                // Add the checkbox to the ListView
                lstServers.Controls.Add(chkExternalWindow);
                
                // Store the checkbox for later use
                externalWindowCheckBoxes[server.Name] = chkExternalWindow;
            }
        }
        
        private void LstServers_Resize(object sender, EventArgs e)
        {
            // Reposition all checkboxes when ListView is resized
            UpdateServerStatus();
        }
        
        private void AutoRestartCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkBox = sender as CheckBox;
            if (chkBox != null && chkBox.Tag != null)
            {
                string serverName = chkBox.Tag.ToString();
                
                // Find the server in settings and update its auto-restart flag
                foreach (var server in settings.Servers)
                {
                    if (server.Name == serverName)
                    {
                        server.AutoRestart = chkBox.Checked;
                        LogMessage(string.Format("Auto-restart for {0} is now {1}", serverName, chkBox.Checked ? "enabled" : "disabled"));
                        
                        // Save settings
                        SaveSettings();
                        break;
                    }
                }
            }
        }
        
        private void ExternalWindowCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkBox = sender as CheckBox;
            if (chkBox != null && chkBox.Tag != null)
            {
                string serverName = chkBox.Tag.ToString();
                
                // Find the server in settings and update its external window flag
                foreach (var server in settings.Servers)
                {
                    if (server.Name == serverName)
                    {
                        server.UseExternalWindow = chkBox.Checked;
                        LogMessage(string.Format("External window for {0} is now {1}", 
                            serverName, chkBox.Checked ? "enabled" : "disabled"));
                        
                        // If the server is running, warn the user that changes will take effect on restart
                        if (runningProcesses.ContainsKey(serverName) && !runningProcesses[serverName].HasExited)
                        {
                            LogMessage(string.Format("Note: External window setting for {0} will take effect when the server is restarted", serverName));
                        }
                        
                        // Save settings
                        SaveSettings();
                        break;
                    }
                }
            }
        }
        
        private void InitializeServerTabs()
        {
            // Clear existing tabs and dictionaries
            tabControl.TabPages.Clear();
            serverLogTextBoxes.Clear();
            serverLogs.Clear();
            
            // Create a tab for each server
            foreach (var server in settings.Servers)
            {
                // Create a new tab page
                TabPage tabPage = new TabPage(server.Name);
                
                // Create a text box for the server's output
                TextBox txtServerLog = new TextBox();
                txtServerLog.Multiline = true;
                txtServerLog.ReadOnly = true;
                txtServerLog.ScrollBars = ScrollBars.Both;
                txtServerLog.Dock = DockStyle.Fill;
                txtServerLog.BackColor = Color.Black;
                txtServerLog.ForeColor = Color.LightGreen;
                txtServerLog.Font = new Font("Consolas", 9F);
                txtServerLog.Text = string.Format("-- Output for {0} will appear here when the server is started --\r\n", server.Name);
                
                // Add the text box to the tab page
                tabPage.Controls.Add(txtServerLog);
                
                // Add the tab page to the tab control
                tabControl.TabPages.Add(tabPage);
                
                // Store the text box in our dictionary
                serverLogTextBoxes[server.Name] = txtServerLog;
                
                // Initialize the log buffer
                serverLogs[server.Name] = new StringBuilder();
            }
            
            // Add a "Main Log" tab
            TabPage mainLogTab = new TabPage("Main Log");
            TextBox txtMainLog = new TextBox();
            txtMainLog.Multiline = true;
            txtMainLog.ReadOnly = true;
            txtMainLog.ScrollBars = ScrollBars.Both;
            txtMainLog.Dock = DockStyle.Fill;
            txtMainLog.BackColor = Color.Black;
            txtMainLog.ForeColor = Color.LightGreen;
            txtMainLog.Font = new Font("Consolas", 9F);
            txtMainLog.Text = "-- Main log messages will appear here --\r\n";
            mainLogTab.Controls.Add(txtMainLog);
            tabControl.TabPages.Add(mainLogTab);
            
            // Store the main log text box
            serverLogTextBoxes["MainLog"] = txtMainLog;
            serverLogs["MainLog"] = new StringBuilder();
        }
        
        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (SettingsForm settingsForm = new SettingsForm(settings))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    bool apiSettingsChanged = settings.ApiEnabled != webIntegrationEnabled;
                    
                    // Save the updated settings
                    SaveSettings();
                    
                    // Refresh the UI
                    InitializeServerList();
                    InitializeServerTabs();
                    
                    LogMessage(string.Format("Settings updated. Server startup delay is now {0} ms", settings.StartupDelay));
                    
                    // Restart web API if settings changed
                    if (apiSettingsChanged || settings.ApiEnabled)
                    {
                        webIntegrationEnabled = settings.ApiEnabled;
                        RestartWebIntegration();
                        
                        if (settings.ApiEnabled)
                        {
                            LogMessage(string.Format("API {0} on port {1}", 
                                apiSettingsChanged ? "enabled" : "settings updated", 
                                settings.ApiPort));
                        }
                        else if (apiSettingsChanged)
                        {
                            LogMessage("API disabled");
                        }
                    }
                }
            }
        }

        private void btnStartAll_Click(object sender, EventArgs e)
        {
            LogMessage("Starting all servers with a delay of " + settings.StartupDelay + " ms...");
            
            // Start servers in a separate thread to avoid UI freeze
            Thread startThread = new Thread(new ThreadStart(() =>
            {
                foreach (var server in settings.Servers)
                {
                    // Start the server on the UI thread
                    this.Invoke(new Action(() => StartServer(server)));
                    
                    // Wait for the specified delay
                    System.Threading.Thread.Sleep(settings.StartupDelay);
                }
                
                // Update status after all servers are started
                this.Invoke(new Action(() => UpdateServerStatus()));
            }));
            
            startThread.IsBackground = true;
            startThread.Start();
        }

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            LogMessage("Stopping all servers...");
            StopAllServers();
            UpdateServerStatus();
        }

        private void btnRestartAll_Click(object sender, EventArgs e)
        {
            LogMessage("Restarting all servers...");
            StopAllServers();
            
            // Small delay before starting again
            System.Threading.Thread.Sleep(1000);
            
            // Start servers in a separate thread to avoid UI freeze
            Thread startThread = new Thread(new ThreadStart(() =>
            {
                foreach (var server in settings.Servers)
                {
                    // Start the server on the UI thread
                    this.Invoke(new Action(() => StartServer(server)));
                    
                    // Wait for the specified delay
                    System.Threading.Thread.Sleep(settings.StartupDelay);
                }
                
                // Update status after all servers are started
                this.Invoke(new Action(() => UpdateServerStatus()));
            }));
            
            startThread.IsBackground = true;
            startThread.Start();
        }

        private void StartServer(ServerInfo server)
        {
            try
            {
                // Check if the server is already running in our processes
                if (runningProcesses.ContainsKey(server.Name) && !runningProcesses[server.Name].HasExited)
                {
                    LogMessage(string.Format("{0} is already running.", server.Name));
                    return;
                }
                
                // Check if the executable is already running elsewhere
                string exeName = Path.GetFileName(server.Path);
                Process[] existingProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeName));
                
                if (existingProcesses.Length > 0)
                {
                    LogMessage(string.Format("{0} is already running in another process. Skipping start.", server.Name));
                    
                    // Add the existing process to our tracking
                    runningProcesses[server.Name] = existingProcesses[0];
                    processStartTimes[server.Name] = DateTime.Now - existingProcesses[0].TotalProcessorTime;
                    
                    // Update status
                    UpdateServerStatus();
                    return;
                }

                // Start the server process
                string path = Path.Combine(Application.StartupPath, server.Path);
                
                if (!File.Exists(path))
                {
                    LogMessage(string.Format("Error: {0} does not exist.", path));
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    WorkingDirectory = Path.GetDirectoryName(path)
                };
                
                // Check if we should use an external window or redirect output
                if (server.UseExternalWindow)
                {
                    // Run in external window
                    startInfo.UseShellExecute = true;
                    startInfo.CreateNoWindow = false;
                    startInfo.WindowStyle = ProcessWindowStyle.Normal;
                    
                    // Can't redirect output when using shell execute
                    startInfo.RedirectStandardOutput = false;
                    startInfo.RedirectStandardError = false;
                }
                else
                {
                    // Run with redirected output
                    startInfo.UseShellExecute = false;
                    startInfo.CreateNoWindow = true;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                }

                Process process = new Process();
                process.StartInfo = startInfo;
                
                // Only set up data received events if we're not using an external window
                if (!server.UseExternalWindow)
                {
                    process.OutputDataReceived += (s, args) => 
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            BeginInvoke(new Action(() => 
                            {
                                LogMessage(args.Data, server.Name);
                            }));
                        }
                    };
                    process.ErrorDataReceived += (s, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            BeginInvoke(new Action(() =>
                            {
                                LogMessage("ERROR: " + args.Data, server.Name);
                            }));
                        }
                    };
                }
                else
                {
                    // If using external window, log a message about it
                    LogMessage(string.Format("{0} is running in an external window", server.Name), server.Name);
                }
                
                process.EnableRaisingEvents = true;
                process.Exited += (s, args) =>
                {
                    BeginInvoke(new Action(() =>
                    {
                        LogMessage(string.Format("{0} has exited.", server.Name), server.Name);
                        
                        // Check if auto-restart is enabled for this server
                        bool shouldAutoRestart = false;
                        foreach (var srv in settings.Servers)
                        {
                            if (srv.Name == server.Name && srv.AutoRestart)
                            {
                                shouldAutoRestart = true;
                                break;
                            }
                        }
                        
                        if (shouldAutoRestart)
                        {
                            LogMessage(string.Format("Auto-restarting {0}...", server.Name), server.Name);
                            
                            // Small delay before restarting
                            System.Threading.Thread.Sleep(1000);
                            
                            // Start the server again
                            StartServer(server);
                        }
                        
                        UpdateServerStatus();
                    }));
                };

                bool started = process.Start();
                if (started)
                {
                    runningProcesses[server.Name] = process;
                    processStartTimes[server.Name] = DateTime.Now;
                    
                    // Only begin reading output if we're not using an external window
                    if (!server.UseExternalWindow)
                    {
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                    }
                    
                    LogMessage(string.Format("Started {0}", server.Name));
                }
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Error starting {0}: {1}", server.Name, ex.Message));
            }
        }

        private void StopServer(ServerInfo server)
        {
            try
            {
                if (runningProcesses.ContainsKey(server.Name) && !runningProcesses[server.Name].HasExited)
                {
                    runningProcesses[server.Name].Kill();
                    LogMessage(string.Format("Stopped {0}", server.Name));
                }
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Error stopping {0}: {1}", server.Name, ex.Message));
            }
        }

        private void StopAllServers()
        {
            foreach (var server in settings.Servers)
            {
                StopServer(server);
            }
        }

        private void LogMessage(string message)
        {
            LogMessage(message, "MainLog");
        }
        
        private void LogMessage(string message, string serverName)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = string.Format("[{0}] {1}{2}", timestamp, message, Environment.NewLine);
            
            // Add to main log
            AppendToLog("MainLog", formattedMessage);
            
            // If this is a server-specific message, also add it to that server's log
            if (serverName != "MainLog" && serverLogs.ContainsKey(serverName))
            {
                AppendToLog(serverName, formattedMessage);
                
                // Switch to the server's tab if the message contains "ERROR"
                if (message.Contains("ERROR"))
                {
                    // Find the tab index for this server
                    for (int i = 0; i < tabControl.TabPages.Count; i++)
                    {
                        if (tabControl.TabPages[i].Text == serverName)
                        {
                            BeginInvoke(new Action(() => tabControl.SelectedIndex = i));
                            break;
                        }
                    }
                }
            }
            
            // Also append to the main text box for backward compatibility
            if (txtLog.TextLength > 100000)
            {
                txtLog.Text = txtLog.Text.Substring(txtLog.TextLength - 50000);
            }
            
            txtLog.AppendText(formattedMessage);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
        
        private void AppendToLog(string serverName, string message)
        {
            if (!serverLogs.ContainsKey(serverName) || !serverLogTextBoxes.ContainsKey(serverName))
                return;
                
            // Add to the server's log buffer
            serverLogs[serverName].Append(message);
            
            // Update the text box if we're on the UI thread
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateServerLogTextBox(serverName)));
            }
            else
            {
                UpdateServerLogTextBox(serverName);
            }
        }
        
        private void UpdateServerLogTextBox(string serverName)
        {
            if (!serverLogTextBoxes.ContainsKey(serverName))
                return;
                
            TextBox txtBox = serverLogTextBoxes[serverName];
            
            // If the text exceeds a certain length, trim it to avoid performance issues
            if (serverLogs[serverName].Length > 100000)
            {
                serverLogs[serverName] = new StringBuilder(serverLogs[serverName].ToString().Substring(50000));
            }
            
            // Update the text box
            txtBox.Text = serverLogs[serverName].ToString();
            
            // Auto-scroll to the bottom
            txtBox.SelectionStart = txtBox.Text.Length;
            txtBox.ScrollToCaret();
        }

        private void StatusUpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateServerStatus();
        }

        private void UpdateServerStatus()
        {
            for (int i = 0; i < settings.Servers.Count && i < lstServers.Items.Count; i++)
            {
                string status = "Stopped";
                string runtime = "--:--:--";
                string serverName = settings.Servers[i].Name;
                
                if (runningProcesses.ContainsKey(serverName))
                {
                    try
                    {
                        if (!runningProcesses[serverName].HasExited)
                        {
                            status = "Running";
                            
                            // Calculate runtime
                            if (processStartTimes.ContainsKey(serverName))
                            {
                                TimeSpan runningTime = DateTime.Now - processStartTimes[serverName];
                                runtime = string.Format("{0:00}:{1:00}:{2:00}", 
                                    (int)runningTime.TotalHours, 
                                    runningTime.Minutes, 
                                    runningTime.Seconds);
                            }
                        }
                        else
                        {
                            // Process has exited, remove from tracking
                            if (processStartTimes.ContainsKey(serverName))
                            {
                                processStartTimes.Remove(serverName);
                            }
                        }
                    }
                    catch
                    {
                        // Process may have exited, ignore
                    }
                }

                lstServers.Items[i].SubItems[1].Text = status;
                lstServers.Items[i].SubItems[2].Text = runtime;
                lstServers.Items[i].ForeColor = status == "Running" ? Color.Green : Color.Red;
                
                // Update checkbox positions in case the ListView has been scrolled
                Rectangle rect = lstServers.GetItemRect(i);
                
                // Update auto-restart checkbox
                if (autoRestartCheckBoxes.ContainsKey(serverName))
                {
                    int xOffset = lstServers.Columns[0].Width + lstServers.Columns[1].Width + lstServers.Columns[2].Width + 10;
                    autoRestartCheckBoxes[serverName].Location = new Point(xOffset, rect.Y);
                }
                
                // Update external window checkbox
                if (externalWindowCheckBoxes.ContainsKey(serverName))
                {
                    int xOffset = lstServers.Columns[0].Width + lstServers.Columns[1].Width + 
                                  lstServers.Columns[2].Width + lstServers.Columns[3].Width + 10;
                    externalWindowCheckBoxes[serverName].Location = new Point(xOffset, rect.Y);
                }
            }
        }
        
        // Individual server control methods
        private ServerInfo GetSelectedServer()
        {
            if (lstServers.SelectedItems.Count > 0)
            {
                int selectedIndex = lstServers.SelectedItems[0].Index;
                if (selectedIndex >= 0 && selectedIndex < settings.Servers.Count)
                {
                    return settings.Servers[selectedIndex];
                }
            }
            return null;
        }
        
        private void menuItemStartServer_Click(object sender, EventArgs e)
        {
            ServerInfo server = GetSelectedServer();
            if (server != null)
            {
                LogMessage(string.Format("Starting individual server: {0}", server.Name));
                StartServer(server);
                UpdateServerStatus();
            }
            else
            {
                LogMessage("No server selected");
            }
        }
        
        private void menuItemStopServer_Click(object sender, EventArgs e)
        {
            ServerInfo server = GetSelectedServer();
            if (server != null)
            {
                LogMessage(string.Format("Stopping individual server: {0}", server.Name));
                StopServer(server);
                UpdateServerStatus();
            }
            else
            {
                LogMessage("No server selected");
            }
        }
        
        private void menuItemRestartServer_Click(object sender, EventArgs e)
        {
            ServerInfo server = GetSelectedServer();
            if (server != null)
            {
                LogMessage(string.Format("Restarting individual server: {0}", server.Name));
                StopServer(server);
                
                // Small delay before starting again
                System.Threading.Thread.Sleep(1000);
                
                StartServer(server);
                UpdateServerStatus();
            }
            else
            {
                LogMessage("No server selected");
            }
        }

        #region Web Integration Methods

        private void InitializeWebIntegration()
        {
            try
            {
                webListener = new HttpListener();
                
                // Add both URL formats to be compatible with the Azuriom plugin
                string prefix1 = string.Format("http://*:{0}/serverlauncher/", settings.ApiPort);
                string prefix2 = string.Format("http://*:{0}/", settings.ApiPort);
                
                webListener.Prefixes.Add(prefix1);
                webListener.Prefixes.Add(prefix2);
                
                webListener.Start();

                webListenerThread = new Thread(WebListenerThreadProc);
                webListenerThread.IsBackground = true;
                webListenerThread.Start();

                LogMessage(string.Format("Web API started on port {0}", settings.ApiPort));
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Error initializing web integration: {0}", ex.Message));
                webIntegrationEnabled = false;
            }
        }

        private void RestartWebIntegration()
        {
            if (webListener != null && webListener.IsListening)
            {
                webListener.Stop();
                webListener.Close();
                webListener = null;
            }
            
            if (settings.ApiEnabled)
            {
                webIntegrationEnabled = true;
                InitializeWebIntegration();
            }
        }

        private void WebListenerThreadProc()
        {
            while (webListener != null && webListener.IsListening)
            {
                try
                {
                    // Wait for a request
                    HttpListenerContext context = webListener.GetContext();
                    
                    // Handle the request in a separate thread to avoid blocking
                    ThreadPool.QueueUserWorkItem(ProcessRequest, context);
                }
                catch (Exception ex)
                {
                    if (webListener != null && webListener.IsListening)
                    {
                        this.Invoke(new Action(() => LogMessage(string.Format("Web server error: {0}", ex.Message))));
                    }
                }
            }
        }
        
        private void ProcessRequest(object state)
        {
            HttpListenerContext context = (HttpListenerContext)state;
            string responseText = "Unauthorized";
            int statusCode = 401;
            
            try
            {
                string clientIp = context.Request.RemoteEndPoint.Address.ToString();
                string requestPath = context.Request.Url.AbsolutePath.ToLower();
                string method = context.Request.HttpMethod.ToUpper();
                
                // Log the incoming request
                LogMessage(string.Format("API Request: {0} {1} from {2}", method, requestPath, clientIp));
                
                // IP Whitelist check
                if (!IsIpWhitelisted(clientIp))
                {
                    LogMessage(string.Format("Unauthorized access attempt from {0}", clientIp));
                    responseText = "IP not whitelisted";
                    statusCode = 403;
                }
                else
                {
                    // Authentication check
                    bool isAuthenticated = false;
                    string apiKeyHeader = context.Request.Headers["X-API-Key"];
                    
                    // Also check query string for API key
                    if (string.IsNullOrEmpty(apiKeyHeader))
                    {
                        apiKeyHeader = GetQueryParam(context.Request.Url.Query, "api_key");
                    }
                    
                    // Validate API key
                    if (!string.IsNullOrEmpty(apiKeyHeader) && apiKeyHeader == settings.ApiKey)
                    {
                        isAuthenticated = true;
                    }
                    
                    if (isAuthenticated)
                    {
                        // Normalize the path to handle both formats
                        if (requestPath.StartsWith("/serverlauncher/"))
                        {
                            requestPath = requestPath.Substring("/serverlauncher".Length);
                        }
                        
                        // Handle the endpoints
                        if (requestPath.Equals("/") || requestPath.Equals("/status"))
                        {
                            // Return status of all servers
                            responseText = GetServerStatusJson();
                            statusCode = 200;
                        }
                        else if (requestPath.Equals("/start") && method == "POST")
                        {
                            // Start specific server or all servers
                            string serverName = GetQueryParam(context.Request.Url.Query, "server");
                            
                            if (string.IsNullOrEmpty(serverName))
                            {
                                // Start all servers
                                this.Invoke(new Action(() => btnStartAll_Click(null, null)));
                                responseText = "{\"success\":true, \"message\":\"Starting all servers...\"}";
                            }
                            else
                            {
                                // Start specific server
                                StartServerByName(serverName);
                                responseText = string.Format("{{\"success\":true, \"message\":\"Starting server {0}...\"}}", serverName);
                            }
                            statusCode = 200;
                        }
                        else if (requestPath.Equals("/stop") && method == "POST")
                        {
                            // Stop specific server or all servers
                            string serverName = GetQueryParam(context.Request.Url.Query, "server");
                            
                            if (string.IsNullOrEmpty(serverName))
                            {
                                // Stop all servers
                                this.Invoke(new Action(() => btnStopAll_Click(null, null)));
                                responseText = "{\"success\":true, \"message\":\"Stopping all servers...\"}";
                            }
                            else
                            {
                                // Stop specific server
                                StopServerByName(serverName);
                                responseText = string.Format("{{\"success\":true, \"message\":\"Stopping server {0}...\"}}", serverName);
                            }
                            statusCode = 200;
                        }
                        else if (requestPath.Equals("/restart") && method == "POST")
                        {
                            // Restart specific server or all servers
                            string serverName = GetQueryParam(context.Request.Url.Query, "server");
                            
                            if (string.IsNullOrEmpty(serverName))
                            {
                                // Restart all servers
                                this.Invoke(new Action(() => btnRestartAll_Click(null, null)));
                                responseText = "{\"success\":true, \"message\":\"Restarting all servers...\"}";
                            }
                            else
                            {
                                // Restart specific server
                                RestartServerByName(serverName);
                                responseText = string.Format("{{\"success\":true, \"message\":\"Restarting server {0}...\"}}", serverName);
                            }
                            statusCode = 200;
                        }
                        else if (requestPath.Equals("/logs"))
                        {
                            // Get logs for a specific server
                            string serverName = GetQueryParam(context.Request.Url.Query, "server");
                            
                            if (!string.IsNullOrEmpty(serverName) && serverLogs.ContainsKey(serverName))
                            {
                                responseText = GetServerLogs(serverName);
                                statusCode = 200;
                            }
                            else
                            {
                                responseText = "{\"success\":false, \"message\":\"Server not found\"}";
                                statusCode = 404;
                            }
                        }
                        else
                        {
                            responseText = "{\"success\":true, \"message\":\"Server Launcher API\", \"endpoints\":[\"status\", \"start\", \"stop\", \"restart\", \"logs\"]}";
                            statusCode = 200;
                        }
                    }
                    else
                    {
                        // Authentication failed
                        LogMessage(string.Format("Invalid API key from {0}", clientIp));
                        responseText = "{\"success\":false, \"message\":\"Invalid API key\"}";
                        statusCode = 401;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Error processing request: {0}", ex.Message));
                responseText = string.Format("{{\"success\":false, \"message\":\"Internal server error: {0}\"}}", ex.Message);
                statusCode = 500;
            }
            
            try
            {
                // Set content type to JSON
                context.Response.ContentType = "application/json";
                
                // Set CORS headers to allow cross-origin requests
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-API-Key");
                
                // Set status code
                context.Response.StatusCode = statusCode;
                
                // Write response
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseText);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                
                // Log the response
                LogMessage(string.Format("API Response: {0} - {1} bytes", statusCode, buffer.Length));
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Error sending response: {0}", ex.Message));
            }
            finally
            {
                // Always close the response
                context.Response.Close();
            }
        }

        private bool IsIpWhitelisted(string ipAddress)
        {
            // If no whitelist is defined, deny all access
            if (settings.WhitelistedIps == null || settings.WhitelistedIps.Count == 0)
                return false;
                
            // Check if the IP is in the whitelist
            return settings.WhitelistedIps.Contains(ipAddress) || settings.WhitelistedIps.Contains("*");
        }
        
        private string GetQueryParam(string query, string name)
        {
            if (string.IsNullOrEmpty(query))
                return null;
                
            // Remove the leading '?' if present
            if (query.StartsWith("?"))
                query = query.Substring(1);
                
            // Split the query into name-value pairs
            string[] pairs = query.Split('&');
            
            // Find the requested parameter
            foreach (string pair in pairs)
            {
                string[] parts = pair.Split('=');
                if (parts.Length == 2 && parts[0].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return HttpUtility.UrlDecode(parts[1]);
                }
            }
            
            return null;
        }
        
        private void StartServerByName(string serverName)
        {
            try
            {
                ServerInfo server = FindServerByName(serverName);
                if (server != null)
                {
                    this.Invoke(new Action(() => 
                    {
                        StartServer(server);
                        UpdateServerStatus();
                    }));
                    
                    LogMessage(string.Format("API requested to start server {0}", serverName));
                }
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Error starting server {0}: {1}", serverName, ex.Message));
            }
        }
        
        private void StopServerByName(string serverName)
        {
            try
            {
                ServerInfo server = FindServerByName(serverName);
                if (server != null)
                {
                    this.Invoke(new Action(() => 
                    {
                        StopServer(server);
                        UpdateServerStatus();
                    }));
                    
                    LogMessage(string.Format("API requested to stop server {0}", serverName));
                }
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Error stopping server {0}: {1}", serverName, ex.Message));
            }
        }
        
        private void RestartServerByName(string serverName)
        {
            try
            {
                ServerInfo server = FindServerByName(serverName);
                if (server != null)
                {
                    this.Invoke(new Action(() => 
                    {
                        StopServer(server);
                        
                        // Small delay before starting again
                        System.Threading.Thread.Sleep(1000);
                        
                        StartServer(server);
                        UpdateServerStatus();
                    }));
                    
                    LogMessage(string.Format("API requested to restart server {0}", serverName));
                }
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Error restarting server {0}: {1}", serverName, ex.Message));
            }
        }
        
        private ServerInfo FindServerByName(string serverName)
        {
            if (string.IsNullOrEmpty(serverName))
                return null;
                
            foreach (var server in settings.Servers)
            {
                if (server.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase))
                {
                    return server;
                }
            }
            
            return null;
        }

        private string GetServerStatusJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\n  \"success\": true,\n");
            sb.Append("  \"data\": {\n");
            sb.Append("    \"servers\": [\n");
            
            for (int i = 0; i < settings.Servers.Count; i++)
            {
                bool isRunning = false;
                string runtime = "--:--:--";
                string serverName = settings.Servers[i].Name;
                
                if (runningProcesses.ContainsKey(serverName))
                {
                    try
                    {
                        if (!runningProcesses[serverName].HasExited)
                        {
                            isRunning = true;
                            
                            // Calculate runtime
                            if (processStartTimes.ContainsKey(serverName))
                            {
                                TimeSpan runningTime = DateTime.Now - processStartTimes[serverName];
                                runtime = string.Format("{0:00}:{1:00}:{2:00}", 
                                    (int)runningTime.TotalHours, 
                                    runningTime.Minutes, 
                                    runningTime.Seconds);
                            }
                        }
                    }
                    catch { }
                }
                
                sb.AppendFormat("      {{\"name\": \"{0}\", \"status\": \"{1}\", \"runtime\": \"{2}\", \"autoRestart\": {3}, \"externalWindow\": {4}}}", 
                    serverName, 
                    (isRunning ? "Running" : "Stopped"),
                    runtime,
                    settings.Servers[i].AutoRestart ? "true" : "false",
                    settings.Servers[i].UseExternalWindow ? "true" : "false");
                
                if (i < settings.Servers.Count - 1)
                    sb.Append(",\n");
            }
            
            sb.Append("\n    ]\n  }\n}");
            return sb.ToString();
        }
        
        private string GetServerLogs(string serverName)
        {
            if (serverLogs.ContainsKey(serverName))
            {
                StringBuilder logs = new StringBuilder();
                logs.Append("{\n  \"success\": true,\n");
                logs.Append("  \"data\": {\n");
                logs.Append("    \"logs\": [");
                
                // Get the last 100 lines
                string[] lines = serverLogs[serverName].ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int startIndex = Math.Max(0, lines.Length - 100);
                
                bool firstLine = true;
                for (int i = startIndex; i < lines.Length; i++)
                {
                    if (!string.IsNullOrEmpty(lines[i]))
                    {
                        if (!firstLine)
                        {
                            logs.Append(", ");
                        }
                        else
                        {
                            firstLine = false;
                        }
                        
                        logs.AppendFormat("\"{0}\"", lines[i].Replace("\"", "\\\""));
                    }
                }
                
                logs.Append("]\n  }\n}");
                return logs.ToString();
            }
            
            return "{\"success\": true, \"data\": {\"logs\": []}}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Shut down the web listener if it's running
            if (webListener != null && webListener.IsListening)
            {
                webListener.Stop();
                webListener.Close();
                webListener = null;
            }
            
            // Stop all running processes
            StopAllServers();
            
            base.OnFormClosing(e);
        }
        
        #endregion
    }

    [Serializable]
    public class ServerInfo
    {
        [XmlElement("n")]
        public string Name { get; set; }
        
        [XmlElement("Path")]
        public string Path { get; set; }
        
        [XmlElement("AutoRestart")]
        public bool AutoRestart { get; set; }
        
        [XmlElement("UseExternalWindow")]
        public bool UseExternalWindow { get; set; }

        // Parameterless constructor for XML serialization
        public ServerInfo() 
        {
            AutoRestart = false;
            UseExternalWindow = false;
        }

        public ServerInfo(string name, string path)
        {
            Name = name;
            Path = path;
            AutoRestart = false;
            UseExternalWindow = false;
        }
    }
} 