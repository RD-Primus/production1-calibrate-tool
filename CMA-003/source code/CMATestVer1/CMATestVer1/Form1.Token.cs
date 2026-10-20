namespace CMATestVer1
{
    public partial class Form1
    {
        private void InitializeTokenFeature()
        {
            txt_token.Text = Properties.Settings.Default.SavedApiToken ?? "";
            txt_token.UseSystemPasswordChar = true;
            btn_tokenopen.Text = "👁";
            btn_tokenopen.AccessibleName = "แสดง Token";
            txt_token.TextChanged += TxtToken_TextChanged;
            btn_tokenopen.Click += BtnTokenOpen_Click;
        }

        private void BtnTokenOpen_Click(object? sender, EventArgs e)
        {
            bool showToken = txt_token.UseSystemPasswordChar;
            int caretPosition = txt_token.SelectionStart;

            txt_token.UseSystemPasswordChar = !showToken;
            btn_tokenopen.Text = showToken ? "◉" : "👁";
            btn_tokenopen.AccessibleName = showToken ? "ซ่อน Token" : "แสดง Token";

            txt_token.SelectionStart = Math.Min(caretPosition, txt_token.TextLength);
            txt_token.SelectionLength = 0;
        }

        private void TxtToken_TextChanged(object? sender, EventArgs e)
        {
            Properties.Settings.Default.SavedApiToken = txt_token.Text.Trim();
            Properties.Settings.Default.Save();
        }
    }
}
