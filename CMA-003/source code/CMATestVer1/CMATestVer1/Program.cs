namespace CMATestVer1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // 1. เพิ่มบรรทัดนี้เข้าไปเป็นบรรทัดแรกสุด
            //Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            ApplicationConfiguration.Initialize();  
            Application.Run(new Form1(""));
        }
    }
}