using System.Net;

namespace CMATestVer1
{
    public partial class Form1
    {
        private const string DefaultServerIp = "192.168.109.54";
        private const string DefaultServerPort = "5088";
        private const string QcResultApiPath = "/api/qc/result";

        private void InitializeServerAddressFeature()
        {
            txt_IP.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.SavedServerIp)
                ? DefaultServerIp
                : Properties.Settings.Default.SavedServerIp.Trim();

            txt_Port.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.SavedServerPort)
                ? DefaultServerPort
                : Properties.Settings.Default.SavedServerPort.Trim();

            txt_IP.TextChanged += ServerAddress_TextChanged;
            txt_Port.TextChanged += ServerAddress_TextChanged;
        }

        private void ServerAddress_TextChanged(object? sender, EventArgs e)
        {
            Properties.Settings.Default.SavedServerIp = txt_IP.Text.Trim();
            Properties.Settings.Default.SavedServerPort = txt_Port.Text.Trim();
            Properties.Settings.Default.Save();
        }

        private bool TryGetQcResultApiUrl(out string apiUrl)
        {
            apiUrl = string.Empty;
            string ipText = txt_IP.Text.Trim();
            string portText = txt_Port.Text.Trim();

            if (!IPAddress.TryParse(ipText, out IPAddress? address)
                || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                BigMessageBox.Show(
                    "กรุณากรอก IP Address ให้ถูกต้อง เช่น 192.168.109.54",
                    "Server IP ไม่ถูกต้อง",
                    MessageBoxIcon.Warning,
                    MessageBoxButtons.OK,
                    fontSize: 14f);
                txt_IP.Focus();
                return false;
            }

            if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
            {
                BigMessageBox.Show(
                    "กรุณากรอก Port เป็นตัวเลขตั้งแต่ 1 ถึง 65535",
                    "Server Port ไม่ถูกต้อง",
                    MessageBoxIcon.Warning,
                    MessageBoxButtons.OK,
                    fontSize: 14f);
                txt_Port.Focus();
                return false;
            }

            apiUrl = new UriBuilder("http", address.ToString(), port, QcResultApiPath)
                .Uri
                .AbsoluteUri;
            return true;
        }
    }
}
