using System.Globalization;
using ClosedXML.Excel;

namespace TEST_DEF_03N
{
    public partial class Form1
    {
        internal const string DescriptionPassText = "Final assembly functional inspection completed: PASS";
        private const string DescriptionFailPrefix = "Final assembly functional inspection completed: FAIL";

        private readonly object _descriptionLock = new object();
        private readonly HashSet<string> _automaticFailureReasons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _manualFailureReasons = new List<string>();
        private bool _autoDescriptionEnabled = true;
        private string _manualDescriptionOther = string.Empty;
        private string _descriptionSerialNumber = string.Empty;
        private string _currentPayloadDescription = DescriptionPassText;

        private void InitializeDescriptionFeature()
        {
            try
            {
                _autoDescriptionEnabled = Settings1.Default.SavedAutoDescription;
            }
            catch
            {
                _autoDescriptionEnabled = true;
            }

            if (txt_note != null)
            {
                txt_note.Click += btn_description_Click;
            }
        }

        private void btn_description_Click(object? sender, EventArgs e)
        {
            EnsureDescriptionBelongsToCurrentSerial();
            ShowDescriptionDialog(requireReason: false);
        }

        private bool PrepareDescriptionForPersistence(string testStatus)
        {
            EnsureDescriptionBelongsToCurrentSerial();

            if (string.Equals(testStatus, "PASS", StringComparison.OrdinalIgnoreCase))
            {
                _currentPayloadDescription = DescriptionPassText;
                return true;
            }

            if (_autoDescriptionEnabled)
            {
                _currentPayloadDescription = BuildFailDescription(GetAutomaticFailureReasons());
                return true;
            }

            return ShowDescriptionDialog(requireReason: true);
        }

        private bool ShowDescriptionDialog(bool requireReason)
        {
            var (savedSensorType, savedTempPoint) = GetSavedSensorAndTemperature();

            using var dialog = new DescriptionForm(
                _autoDescriptionEnabled,
                _manualFailureReasons,
                savedSensorType,
                savedTempPoint,
                _manualDescriptionOther,
                requireReason);

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return false;

            _autoDescriptionEnabled = dialog.AutoEnabled;
            try
            {
                Settings1.Default.SavedAutoDescription = _autoDescriptionEnabled;
                Settings1.Default.Save();
            }
            catch { }

            _manualFailureReasons.Clear();
            _manualFailureReasons.AddRange(dialog.GetAllSelectedReasons());
            _manualDescriptionOther = dialog.OtherText;

            IEnumerable<string> reasons = _autoDescriptionEnabled
                ? GetAutomaticFailureReasons()
                : GetManualFailureReasons();
            _currentPayloadDescription = BuildFailDescription(reasons);
            return true;
        }

        private (string SensorType, string TempPoint) GetSavedSensorAndTemperature()
        {
            string? entry = _manualFailureReasons.FirstOrDefault(reason =>
                reason.StartsWith("Temperature", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(entry))
                return (string.Empty, string.Empty);

            int open = entry.IndexOf('(');
            int close = entry.LastIndexOf(')');
            if (open >= 0 && close > open)
            {
                string inner = entry.Substring(open + 1, close - open - 1).Trim();
                string[] parts = inner.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string temp = parts[^1];
                    string sensor = string.Join(" ", parts.Take(parts.Length - 1));
                    return (sensor, temp);
                }
                return (inner, string.Empty);
            }

            return (string.Empty, string.Empty);
        }

        private IEnumerable<string> GetManualFailureReasons()
        {
            foreach (string reason in _manualFailureReasons)
                yield return reason;

            if (!string.IsNullOrWhiteSpace(_manualDescriptionOther))
                yield return $"Other: {_manualDescriptionOther.Trim()}";
        }

        private IReadOnlyCollection<string> GetAutomaticFailureReasons()
        {
            var reasons = new List<string>();
            lock (_descriptionLock)
                reasons.AddRange(_automaticFailureReasons);

            try
            {
                ModelReportTarget? target = _modelReportTargets
                    .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Path) && File.Exists(item.Path));

                if (target != null)
                {
                    using var workbook = new XLWorkbook(target.Path);
                    IXLWorksheet sheet = workbook.Worksheet(1);
                    List<QcItem> items = BuildQcItemsFromReport(sheet, target.Row, txt_model.Text.Trim());

                    foreach (var item in items)
                    {
                        if (string.Equals(item.Result, "FAIL", StringComparison.OrdinalIgnoreCase))
                        {
                            if (item.TestName.Contains("Relay Output", StringComparison.OrdinalIgnoreCase))
                                reasons.Add("Output Relay");
                            else if (item.TestName.Contains("Alarm", StringComparison.OrdinalIgnoreCase))
                                reasons.Add("Alarm Relay");
                            else if (item.TestName.Contains("°C", StringComparison.OrdinalIgnoreCase))
                                reasons.Add($"Temperature ({item.TestName})");
                            else
                                reasons.Add(item.TestName);
                        }
                    }
                }
            }
            catch { }

            return reasons
                .Where(reason => !string.IsNullOrWhiteSpace(reason))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string BuildFailDescription(IEnumerable<string> reasons)
        {
            string[] uniqueReasons = reasons
                .Where(reason => !string.IsNullOrWhiteSpace(reason))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return uniqueReasons.Length == 0
                ? $"{DescriptionFailPrefix} | Failed items: Unspecified"
                : $"{DescriptionFailPrefix} | Failed items: {string.Join(", ", uniqueReasons)}";
        }

        private string GetDescriptionForPayload()
        {
            return string.IsNullOrWhiteSpace(_currentPayloadDescription)
                ? DescriptionPassText
                : _currentPayloadDescription;
        }

        private void RecordAutomaticFailureReason(string reason)
        {
            lock (_descriptionLock)
                _automaticFailureReasons.Add(reason);
        }

        private void ResetDescriptionForNewTest()
        {
            lock (_descriptionLock)
                _automaticFailureReasons.Clear();

            _manualFailureReasons.Clear();
            _manualDescriptionOther = string.Empty;
            _descriptionSerialNumber = txt_SN?.Text.Trim() ?? string.Empty;
            _currentPayloadDescription = DescriptionPassText;
        }

        private void EnsureDescriptionBelongsToCurrentSerial()
        {
            string currentSerial = txt_SN?.Text.Trim() ?? string.Empty;
            if (string.Equals(_descriptionSerialNumber, currentSerial, StringComparison.OrdinalIgnoreCase))
                return;

            ResetDescriptionForNewTest();
            _descriptionSerialNumber = currentSerial;
        }
    }
}
