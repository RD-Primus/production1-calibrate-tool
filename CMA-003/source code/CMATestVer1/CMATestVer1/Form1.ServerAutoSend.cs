namespace CMATestVer1
{
    public partial class Form1
    {
        private enum ServerPostResult
        {
            Skipped,
            Success,
            Timeout,
            Failed
        }

        private bool _automaticServerPostInProgress;
        private bool _testFinalizationInProgress;

        private async Task CompleteTestAndPersistAsync(
            string modeLabel,
            string testStatus)
        {
            if (_testFinalizationInProgress)
            {
                LogToRx("ข้ามการสรุปผลซ้ำ เนื่องจากกำลังบันทึกผลอยู่...", Color.Orange);
                return;
            }

            _testFinalizationInProgress = true;
            bool shouldSendToServer = sent_server.Checked;

            btnRun.Enabled = false;
            bntStop.Enabled = false;
            txtSerialNumber.ReadOnly = true;
            sent_server.Enabled = false;

            try
            {
                if (!PrepareDescriptionForPersistence(testStatus))
                {
                    LogToRx($"{modeLabel}: ยกเลิกการบันทึก เนื่องจากยังไม่ได้บันทึกหมายเหตุ", Color.Orange);
                    return;
                }

                bool excelSaved = SaveTestDataToExcel(testStatus);
                if (!excelSaved)
                {
                    LogToRx($"{modeLabel}: การทดสอบจบแล้ว แต่บันทึก Excel ไม่สำเร็จ", Color.Red);
                    return;
                }

                ServerPostResult serverResult = shouldSendToServer
                    ? await SendResultToServerWhenEnabledAsync(testStatus)
                    : ServerPostResult.Skipped;

                ShowFinalCompletionMessage(modeLabel, testStatus, serverResult);
            }
            finally
            {
                _testFinalizationInProgress = false;
                sent_server.Enabled = IsOperatorLoggedIn;
                btnRun.Enabled = !backgroundWorkerOhm.IsBusy;
                txtSerialNumber.ReadOnly = false;
                txtSerialNumber.Focus();
                UpdateOpenForm4ButtonState();
            }
        }

        private async Task<ServerPostResult> SendResultToServerWhenEnabledAsync(
            string testStatus)
        {

            if (_automaticServerPostInProgress)
            {
                LogToRx("ไม่สามารถส่ง Server ซ้ำได้ เนื่องจากกำลังส่งผลอยู่...", Color.Orange);
                return ServerPostResult.Failed;
            }

            _automaticServerPostInProgress = true;

            try
            {
                LogToRx("บันทึก Excel สำเร็จ กำลังส่งผลไป Server...", Color.Blue);
                return await PostResultAsync(testStatus);
            }
            finally
            {
                _automaticServerPostInProgress = false;
            }
        }

        private void ShowFinalCompletionMessage(
            string modeLabel,
            string testStatus,
            ServerPostResult serverResult)
        {
            string resultText = $"ผลทดสอบ: {testStatus}";

            switch (serverResult)
            {
                case ServerPostResult.Success:
                    LogToRx($"{modeLabel}: บันทึก Excel และส่ง Server เสร็จสิ้น", Color.Green);
                    BigMessageBox.Show(
                        $"{modeLabel}, บันทึก Excel และส่ง Server เสร็จสิ้น!\n\n{resultText}",
                        "Completed", MessageBoxIcon.Information,
                        MessageBoxButtons.OK, fontSize: 14f);
                    break;

                case ServerPostResult.Skipped:
                    LogToRx($"{modeLabel}: บันทึก Excel เสร็จสิ้น", Color.Green);
                    BigMessageBox.Show(
                        $"{modeLabel} และบันทึก Excel เสร็จสิ้น!\n\n{resultText}",
                        "Completed", MessageBoxIcon.Information,
                        MessageBoxButtons.OK, fontSize: 14f);
                    break;

                case ServerPostResult.Timeout:
                    LogToRx($"{modeLabel}: Excel บันทึกแล้ว แต่ Server Timeout", Color.Red);
                    BigMessageBox.Show(
                        $"{modeLabel} และบันทึก Excel เสร็จสิ้น\nแต่ Server ไม่ตอบกลับภายใน 10 วินาที\n\n{resultText}",
                        "Server Timeout", MessageBoxIcon.Warning,
                        MessageBoxButtons.OK, fontSize: 14f);
                    break;

                default:
                    LogToRx($"{modeLabel}: Excel บันทึกแล้ว แต่ส่ง Server ไม่สำเร็จ", Color.Red);
                    BigMessageBox.Show(
                        $"{modeLabel} และบันทึก Excel เสร็จสิ้น\nแต่ส่ง Server ไม่สำเร็จ\n\n{resultText}",
                        "Server Error", MessageBoxIcon.Warning,
                        MessageBoxButtons.OK, fontSize: 14f);
                    break;
            }
        }
    }
}
