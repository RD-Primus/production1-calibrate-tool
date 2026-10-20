namespace CMATestVer1
{
    public partial class Form1
    {
        private void InitializeHiddenServerTestButton()
        {
            // ปุ่มทั้งสองถูกซ่อนใน Form1_Load
            // btnPost  → Ctrl+Shift+P
            // sendTosever → Ctrl+Shift+T
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Ctrl+Shift+P → toggle ปุ่ม btnPost
            if (keyData == (Keys.Control | Keys.Shift | Keys.P))
            {
                btnPost.Visible = !btnPost.Visible;
                if (btnPost.Visible)
                    btnPost.BringToFront();
                return true;
            }

            // Ctrl+Shift+T → toggle ปุ่ม sendTosever (test send)
            if (keyData == (Keys.Control | Keys.Shift | Keys.T))
            {
                sendTosever.Visible = !sendTosever.Visible;
                if (sendTosever.Visible)
                    sendTosever.BringToFront();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
