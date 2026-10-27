// =========================================================================================
// [DEVELOPER GUIDE]
// Form1.ServerAddress.cs: "การตั้งค่าและการตรวจสอบความถูกต้องของ Server IP และ Port"
// - ดึงค่า IP และ Port ที่ผู้ใช้ตั้งไว้ในหน้าต่าง Login (Settings1.Default)
// - ตรวจสอบความถูกต้องของ IPv4 Address และหมายเลข Port (1-65535)
// - สร้าง URL ปลายทางสำหรับ Auth Login (/api/auth/login) และ QC Result (/api/qc/result)
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System.Net;

namespace TEST_DEF_03N
{
    public partial class Form1
    {
        private const string DefaultServerIp = "192.168.109.54";
        private const string DefaultServerPort = "5088";
        private const string LoginApiPath = "/api/auth/login";
        private const string QcResultApiPath = "/api/qc/result";

        private void InitializeServerAddressFeature()
        {
            // IP และ Port ถูกจัดเก็บและตั้งค่าผ่านหน้าต่าง Login Popup
        }

        private bool TryGetBaseServerUri(out Uri? baseUri)
        {
            baseUri = null;
            string ipText = string.IsNullOrWhiteSpace(Settings1.Default.SavedServerIp)
                ? DefaultServerIp
                : Settings1.Default.SavedServerIp.Trim();
            string portText = string.IsNullOrWhiteSpace(Settings1.Default.SavedServerPort)
                ? DefaultServerPort
                : Settings1.Default.SavedServerPort.Trim();

            if (!IPAddress.TryParse(ipText, out IPAddress? address)
                || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                MessageBox.Show(this,
                    "กรุณากำหนด IP Address ให้ถูกต้องในหน้าต่าง Login (เช่น 192.168.109.54)",
                    "Server IP ไม่ถูกต้อง",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show(this,
                    "กรุณากำหนด Port ในหน้าต่าง Login เป็นตัวเลขตั้งแต่ 1 ถึง 65535",
                    "Server Port ไม่ถูกต้อง",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            baseUri = new UriBuilder("http", address.ToString(), port).Uri;
            return true;
        }

        private bool TryGetQcResultApiUrl(out string apiUrl)
        {
            apiUrl = string.Empty;
            if (!TryGetBaseServerUri(out Uri? baseUri) || baseUri == null)
                return false;

            apiUrl = new Uri(baseUri, QcResultApiPath).AbsoluteUri;
            return true;
        }

        private bool TryGetLoginApiUrl(out string apiUrl)
        {
            apiUrl = string.Empty;
            if (!TryGetBaseServerUri(out Uri? baseUri) || baseUri == null)
                return false;

            apiUrl = new Uri(baseUri, LoginApiPath).AbsoluteUri;
            return true;
        }
    }
}
