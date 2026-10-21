namespace TEST_DEF_03N
{
    public partial class Form1
    {
        private void InitializeHelpFeature()
        {
            if (txt_help != null)
            {
                txt_help.Cursor = Cursors.Hand;
                txt_help.Click += btn_help_Click;
            }
        }

        private void btn_help_Click(object? sender, EventArgs e)
        {
            using var helpPopup = new HelpPopupForm();
            helpPopup.ShowDialog(this);
        }
    }
}
