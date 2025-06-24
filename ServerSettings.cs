using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ServerLauncher
{
    [Serializable]
    public class ServerSettings
    {
        // Delay between starting each server in milliseconds
        [XmlElement("StartupDelay")]
        public int StartupDelay { get; set; }
        
        // List of server information
        [XmlArray("Servers")]
        [XmlArrayItem("ServerInfo")]
        public List<ServerInfo> Servers { get; set; }
        
        // API Configuration for Azuriom integration
        [XmlElement("ApiKey")]
        public string ApiKey { get; set; }
        
        [XmlElement("ApiPort")]
        public int ApiPort { get; set; }
        
        [XmlArray("WhitelistedIps")]
        [XmlArrayItem("Ip")]
        public List<string> WhitelistedIps { get; set; }
        
        [XmlElement("ApiEnabled")]
        public bool ApiEnabled { get; set; }
        
        public ServerSettings()
        {
            // Default values
            StartupDelay = 500;  // Default 500ms delay
            Servers = new List<ServerInfo>();
            WhitelistedIps = new List<string>();
            ApiPort = 8080;
            ApiEnabled = false;
        }

        // Generate a new random API key (32 characters)
        public static string GenerateApiKey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[24]; // 24 bytes will generate a 32 character string when base64 encoded
                rng.GetBytes(bytes);
                
                // Convert to Base64 string and remove any non-alphanumeric characters
                string key = Convert.ToBase64String(bytes)
                    .Replace("/", "")
                    .Replace("+", "")
                    .Replace("=", "");
                
                return key;
            }
        }
        
        // Save settings to a file
        public bool SaveToFile(string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ServerSettings));
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(fs, this);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving settings: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        // Load settings from a file
        public static ServerSettings LoadFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ServerSettings));
                    using (FileStream fs = new FileStream(filePath, FileMode.Open))
                    {
                        return (ServerSettings)serializer.Deserialize(fs);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading settings: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            // Return default settings if file doesn't exist or loading failed
            return new ServerSettings();
        }
    }
} 