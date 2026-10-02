using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Sockets;

namespace NetworkMonitor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Ping pentru un singur IP sau hostname
        private async void PingButton_Click(object sender, RoutedEventArgs e)
        {
            string target = TargetInput.Text;

            if (string.IsNullOrWhiteSpace(target))
            {
                ResultText.Text = "Please enter an IP address or hostname.";
                return;
            }

            ResultText.Text = "Status: Pinging...";

            using Ping ping = new Ping();

            try
            {
                PingReply reply = await ping.SendPingAsync(target);

                if (reply.Status == IPStatus.Success)
                {
                    ResultText.Text =
                        $"Status: Online\n" +
                        $"IP: {reply.Address}\n" +
                        $"Response time: {reply.RoundtripTime} ms";
                }
                else
                {
                    ResultText.Text =
                        $"Status: No response ({reply.Status})";
                }
            }
            catch (Exception)
            {
                ResultText.Text =
                    "Status: Invalid address or connection error.";
            }
        }

        // Scanează dispozitivele din rețeaua locală
        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            DevicesList.Items.Clear();
            ScanButton.IsEnabled = false;

            DevicesList.Items.Add("Scanning network...");

            string? localIP = GetLocalIPv4();

            if (localIP == null)
            {
                DevicesList.Items.Clear();
                DevicesList.Items.Add("Could not detect the local IPv4 address.");
                ScanButton.IsEnabled = true;
                return;
            }

            string[] parts = localIP.Split('.');

            string networkPrefix = $"{parts[0]}.{parts[1]}.{parts[2]}";

            List<Task<string?>> scanTasks = new List<Task<string?>>();

            for (int i = 1; i <= 254; i++)
            {
                string ipAddress = $"{networkPrefix}.{i}";

                scanTasks.Add(PingDeviceAsync(ipAddress));
            }

            string?[] results = await Task.WhenAll(scanTasks);

            DevicesList.Items.Clear();

            foreach (string? result in results)
            {
                if (result != null)
                {
                    DevicesList.Items.Add(result);
                }
            }

            DevicesList.Items.Add("Scan completed.");

            ScanButton.IsEnabled = true;
        }

        // Verifică individual dacă un dispozitiv răspunde
        private async Task<string?> PingDeviceAsync(string ipAddress)
        {
            using Ping ping = new Ping();

            try
            {
                PingReply reply =
                    await ping.SendPingAsync(ipAddress, 500);

                if (reply.Status == IPStatus.Success)
                {
                    return $"{ipAddress} - Online - " +
                           $"{reply.RoundtripTime} ms";
                }
            }
            catch
            {
                // Dispozitivul nu a răspuns sau a apărut o eroare.
            }

            return null;
        }

        private string? GetLocalIPv4()
        {
            string hostName = Dns.GetHostName();

            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address.ToString();
                }
            }

            return null;
        }
    }
}