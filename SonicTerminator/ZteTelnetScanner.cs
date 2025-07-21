using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SonicTerminator
{
    public class ZteTelnetScanner
    {
        public async Task<string> ScanAsync(string ip, int port, string username, string password)
        {
            StringBuilder output = new StringBuilder();

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(ip, port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        var buffer = new byte[4096];
                        var readTimeout = TimeSpan.FromSeconds(10);

                        // Wait for "Login:"
                        int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string loginPrompt = Encoding.ASCII.GetString(buffer, 0, bytes);
                        output.AppendLine("[RECV] " + loginPrompt.Trim());

                        // Send username
                        byte[] userBytes = Encoding.ASCII.GetBytes(username + "\n");
                        await stream.WriteAsync(userBytes, 0, userBytes.Length);

                        await Task.Delay(500);

                        // Wait for "Password:"
                        bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string passPrompt = Encoding.ASCII.GetString(buffer, 0, bytes);
                        output.AppendLine("[RECV] " + passPrompt.Trim());

                        // Send password
                        byte[] passBytes = Encoding.ASCII.GetBytes(password + "\n");
                        await stream.WriteAsync(passBytes, 0, passBytes.Length);

                        await Task.Delay(500);

                        // Send the ZTE command
                        string command = "sendcmd 1 DB p DevAuthInfo\n";
                        byte[] cmdBytes = Encoding.ASCII.GetBytes(command);
                        await stream.WriteAsync(cmdBytes, 0, cmdBytes.Length);

                        await Task.Delay(1000); // wait for modem to respond

                        // Read full response
                        bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.ASCII.GetString(buffer, 0, bytes);
                        output.AppendLine("[RECV] " + response.Trim());

                        // Try to extract WebUserInfo
                        if (response.Contains("X_HW_WebUserInfo"))
                        {
                            string[] lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                if (line.Contains("X_HW_WebUserInfo"))
                                {
                                    output.AppendLine("[FOUND] " + line.Trim());
                                }
                            }
                        }
                        else
                        {
                            output.AppendLine("[!] Web user info not found.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                output.AppendLine("[ERROR] " + ex.Message);
            }

            return output.ToString();
        }
    }
}
