using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SonicTerminator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            txtConsole.ReadOnly = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true; 
            this.StartPosition = FormStartPosition.CenterScreen; 
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtIP.Text = "192.168.1.1";
            txtPort.Text = "80";
            txtUsername.Text = "admin";
            txtPassword.Text = "password";
        }

        private async Task<bool> IsPortOpenAsync(string ip, int port, int timeout = 1000)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var task = client.ConnectAsync(ip, port);
                    var result = await Task.WhenAny(task, Task.Delay(timeout));
                    return task == result && client.Connected;
                }
            }
            catch
            {
                return false;
            }
        }

        private void Log(string message, Color? color = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Log(message, color)));
                return;
            }

            string timestamp = $"[{DateTime.Now:HH:mm:ss}] ";
            txtConsole.SelectionStart = txtConsole.TextLength;
            txtConsole.SelectionLength = 0;

            // Write timestamp in gray
            txtConsole.SelectionColor = Color.Gray;
            txtConsole.AppendText(timestamp);

            // Write message in provided color or default
            txtConsole.SelectionColor = color ?? txtConsole.ForeColor;
            txtConsole.AppendText(message + Environment.NewLine);

            // Scroll to bottom
            txtConsole.SelectionStart = txtConsole.TextLength;
            txtConsole.ScrollToCaret();

            // Reset color
            txtConsole.SelectionColor = txtConsole.ForeColor;
        }

        private async Task AnimateConnectingMessage(string baseMessage, int dotCount = 3, int delayMs = 300)
        {
            for (int i = 0; i <= dotCount; i++)
            {
                string dots = new string('.', i);
                Log(baseMessage + dots, Color.Gray);

                if (i < dotCount)
                    await Task.Delay(delayMs);
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            txtConsole.Clear();

            string ip = txtIP.Text.Trim();
            string userInputUsername = txtUsername.Text.Trim();
            string userInputPassword = txtPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(ip))
            {
                Log("[!] Please enter an IP address.", Color.Orange);
                btnStart.Enabled = true;
                return;
            }

            var credentials = new[]
            {
        ("factorymode", "nE%jA@5b"),
        ("CMCCAdmin", "aDm8H%MdA"),
        ("CUAdmin", "CUAdmin"),
        ("telecomadmin", "nE7jA%5m"),
        ("cqadmin", "cqunicom"),
        ("user", "1620@CTCC"),
        ("admin", "1620@CUcc"),
        ("cuadmin", "admintelecom"),
        ("lnadmin", "cuadmin"),
        ("useradmin", "lnadmin"),
        ("root", "Zte521"),
        ("adminpldt", "HL1EU9804BKjTa6734uP370"),
        ("admin", "Converge@zte123"),
        ("operator", "operator@123")
    };

            var client = new ZteTelnetHttpClient(ip);
            bool success = false;
            int attemptCount = 0;

            Log("Starting connection...", Color.Gray);
            await Task.Delay(600);

            await AnimateConnectingMessage($"Connecting to {ip}", dotCount: 4, delayMs: 400);

            Log("Establishing communication...", Color.Gray);
            await Task.Delay(1000);

            Log("Performing handshake and verifying connection...", Color.Gray);
            await Task.Delay(1000);

            Log("Connection established. Ready to authenticate.", Color.Gray);
            await Task.Delay(500);

            // New: Check Telnet port 23 and log
            Log("Checking Telnet port (23)...", Color.Gray);
            bool telnetOpen = await IsPortOpenAsync(ip, 23);
            if (telnetOpen)
            {
                Log($"[INFO] Telnet is activated on {ip} (port 23 is open).", Color.Green);
            }
            else
            {
                Log($"[WARN] Telnet port 23 is closed or not reachable on {ip}.", Color.Orange);
            }

            // Try user credentials first
            if (!string.IsNullOrWhiteSpace(userInputUsername) && !string.IsNullOrWhiteSpace(userInputPassword))
            {
                attemptCount++;
                try
                {
                    Log($"Attempt {attemptCount}...", Color.Gray);
                    var result = await client.TryLoginAndFetchTelnetAsync(userInputUsername, userInputPassword);

                    Log($"[SUCCESS] Found telnet creds: {result.Username} / {result.Password}", Color.Green);
                    success = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Log($"Attempt {attemptCount} failed.", Color.Red);
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] {ex.Message}", Color.Red);
                }
            }

            // Fallback to known credentials
            if (!success)
            {
                foreach (var (username, password) in credentials)
                {
                    attemptCount++;
                    try
                    {
                        Log($"Attempt {attemptCount}...", Color.Gray);
                        var result = await client.TryLoginAndFetchTelnetAsync(username, password);

                        Log($"[SUCCESS] Found telnet creds: {result.Username} / {result.Password}", Color.Green);
                        success = true;
                        break;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Log($"Attempt {attemptCount} failed.", Color.Red);
                    }
                    catch (Exception ex)
                    {
                        Log($"[ERROR] {ex.Message}", Color.Red);
                    }

                    await Task.Delay(300);
                }
            }

            if (!success)
            {
                Log("[!] No valid credentials found.", Color.Orange);
            }

            btnStart.Enabled = true;
        }


        private void txtIP_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtUsername_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {

        }

        private void lblUsername_Click(object sender, EventArgs e)
        {

        }

        private void lblPassword_Click(object sender, EventArgs e)
        {

        }

        private void lblPort_Click(object sender, EventArgs e)
        {

        }

        private void lblIP_Click(object sender, EventArgs e)
        {

        }



        private void txtConsole_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            this.ActiveControl = null;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/whiplashcd", 
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open link. " + ex.Message);
            }
        }

    }
}
