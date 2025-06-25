# Server Launcher
![image](https://github.com/user-attachments/assets/dd759245-d001-4c05-89d7-74c108ef2f07)

A robust Windows application for managing multiple server processes with real-time monitoring and control features.

## Features

- **Server Management**
  - Start, stop, and restart all servers with a single click
  - Individual server controls via context menu
  - Configurable startup delay between server launches
  - Auto-restart functionality for crashed servers
  - Option to run servers in external console windows

- **Real-time Monitoring**
  - Live server status monitoring
  - Real-time runtime tracking
  - Dedicated log view for each server
  - Consolidated application logs

- **Configuration**
  - Add, remove, and reorder server definitions
  - Configure server paths and startup parameters
  - Persistent settings via XML configuration

- **Web API Integration**
  - Optional REST API for remote server management
  - API key authentication for secure access
  - IP whitelist for additional security
  - Compatible with web monitoring interfaces

## Integration with CMS

This Server Launcher is fully compatible with the [servermonitoring](https://github.com/stanis-py/servermonitoring) plugin for Azuriom CMS. This integration enables:
- Remote management of your servers through a web interface
- Monitoring server status from anywhere
- Viewing logs remotely
- Controlling servers through a user-friendly dashboard

## Requirements

- Windows 7 or later
- .NET Framework 4.0 or later

## Installation

1. Build the application using the provided `build.bat` script
2. Copy the generated `ServerLauncher.exe` to your server directory
3. Configure the server paths in the Settings menu

## Usage

### Basic Operations
- Click "Start All" to launch all servers in the configured sequence
- Click "Stop All" to terminate all running servers
- Click "Restart All" to stop and restart all servers
- Right-click on individual servers for specific control options

### Configuration
1. Click "Settings" to open the configuration dialog
2. Add, remove, or reorder servers in the list
3. Configure the startup delay between server launches
4. Set individual server auto-restart and external window options
5. Configure the Web API if needed for remote management

### Web API
If enabled, the API supports the following operations:
- GET server status information
- Control individual servers (start/stop/restart)
- Retrieve server logs
- Requires API key authentication and IP whitelisting

## How It Works

The application maintains a list of server executables and launches them as child processes. It captures and displays the standard output of each server in real-time through a tabbed interface. The status of each server is continuously monitored, with options to automatically restart crashed services.

## Default Configuration

When first launched, the application creates a default configuration with commonly used server types. You can customize this list through the Settings menu to match your specific server infrastructure.

## Security

- Web API is disabled by default and requires explicit configuration
- API access is protected by a randomly generated key
- IP whitelisting provides additional security for the API
- All server operations are logged for audit purposes

## License

Open source software - modify and distribute as needed. 
