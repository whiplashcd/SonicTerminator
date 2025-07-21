using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Threading.Tasks;

namespace SonicTerminator
{
    public class ZteTelnetHttpClient
    {
        private readonly string _baseUrl;
        private readonly CookieContainer _cookies = new CookieContainer();

        public ZteTelnetHttpClient(string ip)
        {
            _baseUrl = $"http://{ip}";
        }

        public async Task<(string Username, string Password)> TryLoginAndFetchTelnetAsync(string username, string password)
        {
            await ResetAsync();
            await RequestFactoryModeAsync();

            var (cipher, _) = await GetCipherAsync();

            if (!await CheckLoginAuthAsync(cipher, username, password))
                throw new UnauthorizedAccessException("Invalid credentials.");

            return await FetchTelnetCredentialsAsync(cipher);
        }

        private async Task ResetAsync()
        {
            try { await SendRequestAsync("/webFac", Encoding.ASCII.GetBytes("SendSq.gch")); } catch { }
        }

        private async Task RequestFactoryModeAsync()
        {
            try { await SendRequestAsync("/webFac", Encoding.ASCII.GetBytes("RequestFactoryMode.gch")); } catch { }
        }

        private async Task<(SimpleAes cipher, int index)> GetCipherAsync()
        {
            int rand = new Random().Next(0, 59);
            byte[] data = await SendRequestAsync("/webFac", Encoding.ASCII.GetBytes($"SendSq.gch?rand={rand}\r\n"));

            byte[] keyPool;
            int index;
            if (data.Length == 0)
            {
                keyPool = HexStringToByteArray(_aes_key_ver1);
                index = rand;
            }
            else if (Encoding.ASCII.GetString(data).Contains("newrand"))
            {
                int newrand = int.Parse(Encoding.ASCII.GetString(data).Replace("newrand=", "").Trim());
                index = ((0x1000193 * rand) & 0x3F ^ newrand) % 60;
                keyPool = HexStringToByteArray(_aes_key_ver2);
            }
            else throw new Exception("Unrecognized response");

            byte[] key = new byte[24];
            for (int i = 0; i < 24; i++)
                key[i] = (byte)(keyPool[index + i] ^ 0xA5);

            return (new SimpleAes(key), index);
        }

        private async Task<bool> CheckLoginAuthAsync(SimpleAes cipher, string username, string password)
        {
            string payload = $"CheckLoginAuth.gch?version50&user={username}&pass={password}";
            byte[] encrypted = cipher.Encrypt(Pad(Encoding.ASCII.GetBytes(payload)));
            byte[] response = await SendRequestAsync("/webFacEntry", encrypted);
            return response.Length > 0;
        }

        private async Task<(string Username, string Password)> FetchTelnetCredentialsAsync(SimpleAes cipher)
        {
            byte[] encrypted = cipher.Encrypt(Pad(Encoding.ASCII.GetBytes("FactoryMode.gch?mode=2&user=notused")));
            byte[] response = await SendRequestAsync("/webFacEntry", encrypted);

            string decrypted = Encoding.ASCII.GetString(Unpad(cipher.Decrypt(response))).Trim('\0');

            var query = HttpUtility.ParseQueryString(new Uri(_baseUrl + "/" + decrypted).Query);

            return (
                Username: query["user"] ?? "(not found)",
                Password: query["pass"] ?? "(not found)"
            );
        }

        private async Task<byte[]> SendRequestAsync(string path, byte[] data)
        {
            var req = (HttpWebRequest)WebRequest.Create(_baseUrl + path);
            req.Method = "POST";
            req.ContentType = "application/octet-stream";
            req.CookieContainer = _cookies;

            using (var stream = await req.GetRequestStreamAsync())
            {
                await stream.WriteAsync(data, 0, data.Length);
            }

            using (var resp = (HttpWebResponse)await req.GetResponseAsync())
            using (var mem = new MemoryStream())
            {
                await resp.GetResponseStream().CopyToAsync(mem);
                return mem.ToArray();
            }
        }

        private byte[] Pad(byte[] input)
        {
            int padLen = 16 - (input.Length % 16);
            Array.Resize(ref input, input.Length + padLen);
            return input;
        }

        private byte[] Unpad(byte[] input)
        {
            int i = input.Length - 1;
            while (i >= 0 && input[i] == 0) i--;
            byte[] result = new byte[i + 1];
            Array.Copy(input, result, i + 1);
            return result;
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return result;
        }

        private const string _aes_key_ver1 =
            "7b56b0f7da0e6852c819f32b849079e562f8ead2649387df73d7fbccaafe7543" +
            "1c29df4c522c6e7b453d1ff1debc27858a4591be3813de673208541175f4d3b4" +
            "a4b312866723994c617fb1d230df47f17693a38c95d359bf878ef3b3e4764988";

        private const string _aes_key_ver2 =
            "8c2365d1fc324537112871630720691473e7d453132436c2b5e1fccf8a9a4189" +
            "3c49cf5c728c9eeb750d3fd1fecc57657a35213e68537e970248747195345384" +
            "b4c3e2d6273de65d729cbc3d03fd76c19c25a89247e4180f243f4f67ec97f499";
    }

    // Simple AES ECB mode for .NET 4.7.3 (no native support)
    public class SimpleAes
    {
        private readonly System.Security.Cryptography.Aes aes;

        public SimpleAes(byte[] key)
        {
            aes = System.Security.Cryptography.Aes.Create();
            aes.Key = key;
            aes.Mode = System.Security.Cryptography.CipherMode.ECB;
            aes.Padding = System.Security.Cryptography.PaddingMode.None;
        }

        public byte[] Encrypt(byte[] data)
        {
            using (var enc = aes.CreateEncryptor())
            {
                return enc.TransformFinalBlock(data, 0, data.Length);
            }
        }

        public byte[] Decrypt(byte[] data)
        {
            using (var dec = aes.CreateDecryptor())
            {
                return dec.TransformFinalBlock(data, 0, data.Length);
            }
        }
    }
}
