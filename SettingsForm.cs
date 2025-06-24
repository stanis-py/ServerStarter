using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace ServerLauncher
{
    public partial class SettingsForm : Form
    {
        private ServerSettings settings;
        private bool isDirty = false;
        private TabControl tabControl;
        private TextBox txtApiKey;
        private NumericUpDown numApiPort;
        private CheckBox chkApiEnabled;
        private ListView lstIpWhitelist;
        private Button btnAddIp;
        private Button btnRemoveIp;
        private Button btnGenerateApiKey;
        private Button btnSaveApiSettings;

        public SettingsForm(ServerSettings currentSettings)
        {
            InitializeComponent();
            settings = currentSettings;
            
            // Add a TabControl for better organization
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(12, 12);
            tabControl.Size = new Size(626, 380); // Increased size to match larger form
            tabControl.TabIndex = 0;
            
            // Create the Servers tab
            TabPage serversTab = new TabPage("Servers");
            
            // Create the API Settings tab
            TabPage apiTab = new TabPage("API Settings");
            
            // Add tabs to the TabControl
            tabControl.Controls.Add(serversTab);
            tabControl.Controls.Add(apiTab);
            
            // Add the TabControl to the form
            this.Controls.Add(tabControl);
            
            // Move existing server controls to the Servers tab
            serversTab.Controls.Add(lstServers);
            serversTab.Controls.Add(lblServerList);
            serversTab.Controls.Add(btnMoveUp);
            serversTab.Controls.Add(btnMoveDown);
            serversTab.Controls.Add(btnAdd);
            serversTab.Controls.Add(btnRemove);
            serversTab.Controls.Add(lblStartupDelay);
            serversTab.Controls.Add(numDelay);
            serversTab.Controls.Add(lblMilliseconds);
            
            // Increase the size of the server list view
            lstServers.Size = new Size(500, 260);
            
            // Fix button alignment in the Servers tab
            FixButtonAlignment();
            
            // Add controls for API settings
            Label lblApiInfo = new Label();
            lblApiInfo.Text = "Configure API settings for integration with server monitoring plugin";
            lblApiInfo.Location = new Point(10, 10);
            lblApiInfo.Size = new Size(500, 20);
            apiTab.Controls.Add(lblApiInfo);
            
            chkApiEnabled = new CheckBox();
            chkApiEnabled.Text = "Enable API";
            chkApiEnabled.Location = new Point(10, 35);
            chkApiEnabled.Size = new Size(100, 20);
            chkApiEnabled.Checked = settings.ApiEnabled;
            chkApiEnabled.CheckedChanged += new EventHandler(chkApiEnabled_CheckedChanged);
            apiTab.Controls.Add(chkApiEnabled);
            
            Label lblApiKey = new Label();
            lblApiKey.Text = "API Key:";
            lblApiKey.Location = new Point(10, 65);
            lblApiKey.Size = new Size(100, 20);
            apiTab.Controls.Add(lblApiKey);
            
            txtApiKey = new TextBox();
            txtApiKey.Location = new Point(120, 65);
            txtApiKey.Size = new Size(300, 20);
            txtApiKey.Text = settings.ApiKey;
            txtApiKey.ReadOnly = true; // API key should not be manually edited
            apiTab.Controls.Add(txtApiKey);
            
            btnGenerateApiKey = new Button();
            btnGenerateApiKey.Text = "Generate New Key";
            btnGenerateApiKey.Location = new Point(430, 65);
            btnGenerateApiKey.Size = new Size(120, 23);
            btnGenerateApiKey.Click += new EventHandler(btnGenerateApiKey_Click);
            apiTab.Controls.Add(btnGenerateApiKey);
            
            // Add "Save API Settings" button
            btnSaveApiSettings = new Button();
            btnSaveApiSettings.Text = "Save API Settings";
            btnSaveApiSettings.Location = new Point(430, 95);
            btnSaveApiSettings.Size = new Size(120, 23);
            btnSaveApiSettings.Click += new EventHandler(btnSaveApiSettings_Click);
            apiTab.Controls.Add(btnSaveApiSettings);
            
            Label lblApiPort = new Label();
            lblApiPort.Text = "Port:";
            lblApiPort.Location = new Point(10, 125);
            lblApiPort.Size = new Size(100, 20);
            apiTab.Controls.Add(lblApiPort);
            
            numApiPort = new NumericUpDown();
            numApiPort.Location = new Point(120, 125);
            numApiPort.Size = new Size(100, 20);
            numApiPort.Minimum = 1025;
            numApiPort.Maximum = 65535;
            numApiPort.Value = settings.ApiPort;
            numApiPort.ValueChanged += new EventHandler(numApiPort_ValueChanged);
            apiTab.Controls.Add(numApiPort);
            
            Label lblIpWhitelist = new Label();
            lblIpWhitelist.Text = "IP Whitelist (add '*' to allow all):";
            lblIpWhitelist.Location = new Point(10, 155);
            lblIpWhitelist.Size = new Size(200, 20);
            apiTab.Controls.Add(lblIpWhitelist);
            
            lstIpWhitelist = new ListView();
            lstIpWhitelist.Location = new Point(10, 185);
            lstIpWhitelist.Size = new Size(450, 180);
            lstIpWhitelist.View = View.Details;
            lstIpWhitelist.FullRowSelect = true;
            lstIpWhitelist.Columns.Add("IP Address", 430);
            apiTab.Controls.Add(lstIpWhitelist);
            
            // Add IPs from settings to the list
            if (settings.WhitelistedIps != null)
            {
                foreach (string ip in settings.WhitelistedIps)
                {
                    lstIpWhitelist.Items.Add(ip);
                }
            }
            
            btnAddIp = new Button();
            btnAddIp.Text = "Add";
            btnAddIp.Location = new Point(470, 185);
            btnAddIp.Size = new Size(75, 23);
            btnAddIp.Click += new EventHandler(btnAddIp_Click);
            apiTab.Controls.Add(btnAddIp);
            
            btnRemoveIp = new Button();
            btnRemoveIp.Text = "Remove";
            btnRemoveIp.Location = new Point(470, 214);
            btnRemoveIp.Size = new Size(75, 23);
            btnRemoveIp.Click += new EventHandler(btnRemoveIp_Click);
            apiTab.Controls.Add(btnRemoveIp);
            
            // Position Save and Cancel buttons at the bottom of the form
            int buttonWidth = 100;
            int rightMargin = 10;
            int verticalSpacing = 5;
            
            // Position Save button
            btnSave.Size = new Size(buttonWidth, btnSave.Height);
            btnSave.Location = new Point(this.ClientSize.Width - buttonWidth * 2 - rightMargin - verticalSpacing, 
                tabControl.Bottom + verticalSpacing);
            
            // Position Cancel button
            btnCancel.Size = new Size(buttonWidth, btnCancel.Height);
            btnCancel.Location = new Point(this.ClientSize.Width - buttonWidth - rightMargin, 
                tabControl.Bottom + verticalSpacing);
            
            // Make sure the buttons are on top of the tab control
            this.Controls.SetChildIndex(btnSave, 0);
            this.Controls.SetChildIndex(btnCancel, 0);
        }

        private void FixButtonAlignment()
        {
            // Fix Move Up/Down buttons alignment
            int buttonWidth = 100;
            int rightMargin = 10;
            int verticalSpacing = 5;
            
            // Calculate right side position
            int rightPosition = lstServers.Right + rightMargin;
            
            // Position Move Up button
            btnMoveUp.Size = new Size(buttonWidth, btnMoveUp.Height);
            btnMoveUp.Location = new Point(rightPosition, lstServers.Top);
            
            // Position Move Down button
            btnMoveDown.Size = new Size(buttonWidth, btnMoveDown.Height);
            btnMoveDown.Location = new Point(rightPosition, btnMoveUp.Bottom + verticalSpacing);
            
            // Position Add button
            btnAdd.Size = new Size(buttonWidth, btnAdd.Height);
            btnAdd.Location = new Point(rightPosition, btnMoveDown.Bottom + verticalSpacing * 4);
            
            // Position Remove button
            btnRemove.Size = new Size(buttonWidth, btnRemove.Height);
            btnRemove.Location = new Point(rightPosition, btnAdd.Bottom + verticalSpacing);
        }
        
        private void SettingsForm_Load(object sender, EventArgs e)
        {
            // Set the delay value
            numDelay.Value = settings.StartupDelay;
            
            // Load servers into list
            RefreshServerList();
        }
        
        private void RefreshServerList()
        {
            lstServers.Items.Clear();
            foreach (ServerInfo server in settings.Servers)
            {
                ListViewItem item = new ListViewItem(server.Name);
                item.SubItems.Add(server.Path);
                lstServers.Items.Add(item);
            }
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            if (lstServers.SelectedItems.Count > 0)
            {
                int selectedIndex = lstServers.SelectedItems[0].Index;
                if (selectedIndex > 0)
                {
                    // Swap in the settings list
                    ServerInfo temp = settings.Servers[selectedIndex];
                    settings.Servers[selectedIndex] = settings.Servers[selectedIndex - 1];
                    settings.Servers[selectedIndex - 1] = temp;
                    
                    // Refresh the list
                    RefreshServerList();
                    
                    // Restore selection on moved item
                    lstServers.Items[selectedIndex - 1].Selected = true;
                    lstServers.Focus();
                    
                    isDirty = true;
                }
            }
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            if (lstServers.SelectedItems.Count > 0)
            {
                int selectedIndex = lstServers.SelectedItems[0].Index;
                if (selectedIndex < settings.Servers.Count - 1)
                {
                    // Swap in the settings list
                    ServerInfo temp = settings.Servers[selectedIndex];
                    settings.Servers[selectedIndex] = settings.Servers[selectedIndex + 1];
                    settings.Servers[selectedIndex + 1] = temp;
                    
                    // Refresh the list
                    RefreshServerList();
                    
                    // Restore selection on moved item
                    lstServers.Items[selectedIndex + 1].Selected = true;
                    lstServers.Focus();
                    
                    isDirty = true;
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
                dialog.Title = "Select a server executable";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Ask for a friendly name
                    string fileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                    string friendlyName = fileName;
                    
                    using (var nameDialog = new TextInputDialog("Enter a name for this server:", fileName))
                    {
                        if (nameDialog.ShowDialog() == DialogResult.OK)
                        {
                            friendlyName = nameDialog.InputValue;
                        }
                    }
                    
                    // Create new server info
                    ServerInfo newServer = new ServerInfo(friendlyName, dialog.FileName);
                    settings.Servers.Add(newServer);
                    
                    // Refresh the list
                    RefreshServerList();
                    
                    // Select the new item
                    lstServers.Items[lstServers.Items.Count - 1].Selected = true;
                    lstServers.Focus();
                    
                    isDirty = true;
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lstServers.SelectedItems.Count > 0)
            {
                int selectedIndex = lstServers.SelectedItems[0].Index;
                
                // Confirm deletion
                DialogResult result = MessageBox.Show(
                    "Are you sure you want to remove this server?",
                    "Confirm Removal",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                    
                if (result == DialogResult.Yes)
                {
                    // Remove from settings
                    settings.Servers.RemoveAt(selectedIndex);
                    
                    // Refresh list
                    RefreshServerList();
                    
                    isDirty = true;
                }
            }
        }

        private void numDelay_ValueChanged(object sender, EventArgs e)
        {
            settings.StartupDelay = (int)numDelay.Value;
            isDirty = true;
        }
        
        private void chkApiEnabled_CheckedChanged(object sender, EventArgs e)
        {
            settings.ApiEnabled = chkApiEnabled.Checked;
            isDirty = true;
        }
        
        private void numApiPort_ValueChanged(object sender, EventArgs e)
        {
            settings.ApiPort = (int)numApiPort.Value;
            isDirty = true;
        }
        
        private void btnAddIp_Click(object sender, EventArgs e)
        {
            using (var ipDialog = new TextInputDialog("Enter IP address to whitelist:", ""))
            {
                if (ipDialog.ShowDialog() == DialogResult.OK)
                {
                    string ip = ipDialog.InputValue.Trim();
                    
                    // Validate IP (allow * as wildcard)
                    if (ip == "*" || IsValidIpAddress(ip))
                    {
                        // Add to listview
                        lstIpWhitelist.Items.Add(ip);
                        
                        // Add to settings
                        if (settings.WhitelistedIps == null)
                        {
                            settings.WhitelistedIps = new List<string>();
                        }
                        
                        settings.WhitelistedIps.Add(ip);
                        isDirty = true;
                    }
                    else
                    {
                        MessageBox.Show("Please enter a valid IP address or '*' for all IPs.", "Invalid IP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void btnRemoveIp_Click(object sender, EventArgs e)
        {
            if (lstIpWhitelist.SelectedItems.Count > 0)
            {
                string ip = lstIpWhitelist.SelectedItems[0].Text;
                
                // Remove from listview
                lstIpWhitelist.Items.Remove(lstIpWhitelist.SelectedItems[0]);
                
                // Remove from settings
                settings.WhitelistedIps.Remove(ip);
                isDirty = true;
            }
        }
        
        private bool IsValidIpAddress(string ip)
        {
            IPAddress address;
            return IPAddress.TryParse(ip, out address);
        }

        private void btnGenerateApiKey_Click(object sender, EventArgs e)
        {
            // Confirm before replacing existing key
            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                DialogResult result = MessageBox.Show(
                    "Generating a new API key will invalidate the existing key. Any applications using the current key will need to be updated. Continue?",
                    "Confirm New API Key",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                    
                if (result == DialogResult.No)
                {
                    return;
                }
            }
            
            // Generate a new API key
            string newKey = ServerSettings.GenerateApiKey();
            txtApiKey.Text = newKey;
            settings.ApiKey = newKey;
            isDirty = true;
            
            // Show a message to copy the key
            MessageBox.Show(
                "A new API key has been generated. Make sure to copy this key to your server monitoring plugin.",
                "API Key Generated",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        
        private void btnSaveApiSettings_Click(object sender, EventArgs e)
        {
            // Validate API settings
            if (chkApiEnabled.Checked && string.IsNullOrEmpty(txtApiKey.Text))
            {
                MessageBox.Show("An API key is required when API is enabled. Please generate an API key.", 
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Save API settings
            settings.ApiKey = txtApiKey.Text;
            settings.ApiEnabled = chkApiEnabled.Checked;
            settings.ApiPort = (int)numApiPort.Value;
            
            // Update whitelisted IPs
            if (settings.WhitelistedIps == null)
            {
                settings.WhitelistedIps = new List<string>();
            }
            else
            {
                settings.WhitelistedIps.Clear();
            }
            
            foreach (ListViewItem item in lstIpWhitelist.Items)
            {
                settings.WhitelistedIps.Add(item.Text);
            }
            
            // Mark as dirty so the main form will save the settings
            isDirty = true;
            
            // Save settings to file immediately
            string settingsFilePath = Path.Combine(Application.StartupPath, "ServerSettings.xml");
            if (settings.SaveToFile(settingsFilePath))
            {
                MessageBox.Show("API settings saved successfully to file.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("API settings saved in memory but there was an error saving to file.", "Partial Save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Validate API settings if enabled
            if (settings.ApiEnabled)
            {
                if (string.IsNullOrEmpty(settings.ApiKey))
                {
                    MessageBox.Show("An API key is required when API is enabled. Please generate an API key.", 
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    tabControl.SelectedIndex = 1; // Switch to API tab
                    return;
                }
                
                if (settings.WhitelistedIps == null || settings.WhitelistedIps.Count == 0)
                {
                    MessageBox.Show("At least one IP address must be whitelisted when API is enabled.", 
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    tabControl.SelectedIndex = 1; // Switch to API tab
                    return;
                }
            }
            
            // Make sure all API settings are properly saved
            settings.ApiEnabled = chkApiEnabled.Checked;
            settings.ApiKey = txtApiKey.Text;
            settings.ApiPort = (int)numApiPort.Value;
            
            // Update whitelisted IPs
            if (settings.WhitelistedIps == null)
            {
                settings.WhitelistedIps = new List<string>();
            }
            else
            {
                settings.WhitelistedIps.Clear();
            }
            
            foreach (ListViewItem item in lstIpWhitelist.Items)
            {
                settings.WhitelistedIps.Add(item.Text);
            }
            
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (isDirty)
            {
                DialogResult result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to cancel?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                    
                if (result == DialogResult.No)
                {
                    return;
                }
            }
            
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
    
    // Simple dialog for text input
    public class TextInputDialog : Form
    {
        private TextBox txtInput;
        private Label lblPrompt;
        private Button btnOK;
        private Button btnCancel;
        
        public string InputValue { get { return txtInput.Text; } }
        
        public TextInputDialog(string prompt, string defaultValue = "")
        {
            // Set up form
            this.Text = "Input";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new System.Drawing.Size(300, 150);
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
            
            // Create controls
            lblPrompt = new Label();
            lblPrompt.Text = prompt;
            lblPrompt.Location = new System.Drawing.Point(12, 9);
            lblPrompt.Size = new System.Drawing.Size(260, 20);
            
            txtInput = new TextBox();
            txtInput.Text = defaultValue;
            txtInput.Location = new System.Drawing.Point(12, 32);
            txtInput.Size = new System.Drawing.Size(260, 20);
            
            btnOK = new Button();
            btnOK.Text = "OK";
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new System.Drawing.Point(116, 70);
            btnOK.Size = new System.Drawing.Size(75, 23);
            
            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(197, 70);
            btnCancel.Size = new System.Drawing.Size(75, 23);
            
            // Add controls to form
            this.Controls.Add(lblPrompt);
            this.Controls.Add(txtInput);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);
        }
    }
} 