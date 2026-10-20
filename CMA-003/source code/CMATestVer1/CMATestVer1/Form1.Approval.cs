using System.Globalization;

namespace CMATestVer1
{
    public partial class Form1
    {
        private QcApproval BuildQcApproval(DateTime approvalTime)
        {
            return new QcApproval
            {
                Tester = new QcApprovalTester
                {
                    Name = txt_name.Text.Trim(),
                    DateTime = new DateTimeOffset(approvalTime).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture)
                },
                Confirm = new QcApprovalOfficer
                {
                    Name = null,
                    DateTime = null
                },
                Approver = new QcApprovalOfficer
                {
                    Name = null,
                    DateTime = null
                }
            };
        }
    }
}
