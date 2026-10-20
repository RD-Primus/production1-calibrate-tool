using System.Globalization;

namespace CMATestVer1
{
    public partial class Form1
    {
        internal const string DescriptionPassText = "Final assembly functional inspection completed: PASS";
        private const string DescriptionFailPrefix = "Final assembly functional inspection completed: FAIL";

        private readonly object _descriptionLock = new object();
        private readonly HashSet<string> _automaticFailureReasons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _manualFailureReasons = new List<string>();
        private bool _autoDescriptionEnabled;
        private string _manualDescriptionOther = string.Empty;
        private string _descriptionSerialNumber = string.Empty;
        private string _currentPayloadDescription = DescriptionPassText;

        private void InitializeDescriptionFeature()
        {
            _autoDescriptionEnabled = Properties.Settings.Default.SavedAutoDescription;
            btn_description.Click += btn_description_Click;
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
            using var dialog = new DescriptionForm(
                _autoDescriptionEnabled,
                _manualFailureReasons,
                GetSavedTemperaturePoint(),
                _manualDescriptionOther,
                requireReason);

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return false;

            _autoDescriptionEnabled = dialog.AutoEnabled;
            Properties.Settings.Default.SavedAutoDescription = _autoDescriptionEnabled;
            Properties.Settings.Default.Save();

            _manualFailureReasons.Clear();
            _manualFailureReasons.AddRange(dialog.GetAllSelectedReasons());
            _manualDescriptionOther = dialog.OtherText;

            IEnumerable<string> reasons = _autoDescriptionEnabled
                ? GetAutomaticFailureReasons()
                : GetManualFailureReasons();
            _currentPayloadDescription = BuildFailDescription(reasons);
            return true;
        }

        private string GetSavedTemperaturePoint()
        {
            string? entry = _manualFailureReasons.FirstOrDefault(reason =>
                reason.StartsWith("Temperature (", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(entry))
                return string.Empty;

            int open = entry.IndexOf('(');
            int close = entry.LastIndexOf(')');
            return open >= 0 && close > open ? entry.Substring(open + 1, close - open - 1) : string.Empty;
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

            AddTextFailure(reasons, _RelayResult, "Relay");
            AddTextFailure(reasons, _wlResult, "WL");
            AddTextFailure(reasons, _wl_AL2Result, "Alarm2");
            AddTextFailure(reasons, _hpResult, "HP");
            AddTextFailure(reasons, _hp_AL2Result, "Alarm2");
            AddTextFailure(reasons, _alarm1Result, "Alarm1");
            AddTextFailure(reasons, _Compressor, "Compressor");
            AddTextFailure(reasons, _HotFan, "HotFan");
            AddTextFailure(reasons, _CoolFan, "CoolFan");
            AddTextFailure(reasons, _btnFunctionResult, "Button F");
            AddTextFailure(reasons, _btnDownResult, "Button Down");
            AddTextFailure(reasons, _btnUpResult, "Button Up");
            AddTextFailure(reasons, _ledResult, "LED");

            if (IsNumericFailure(_verify200Result, 98, 100)
                || IsNumericFailure(_verify8000Result, -7.4, -5.4)
                || IsNumericFailure(_verify2000Result, 24.6, 26.6)
                || IsNumericFailure(_verify20Result, 19, 21)
                || IsNumericFailure(_verify30Result, 29, 31)
                || IsNumericFailure(_verify40Result, 39, 41)
                || IsNumericFailure(_verify50Result, 49, 51))
            {
                reasons.Add("Temperature");
            }

            return reasons
                .Where(reason => !string.IsNullOrWhiteSpace(reason))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void AddTextFailure(ICollection<string> reasons, string value, string name)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length > 0
                && normalized != "-"
                && !normalized.Equals("OK", StringComparison.OrdinalIgnoreCase)
                && !normalized.Equals("PASS", StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add(name);
            }
        }

        private static bool IsNumericFailure(string value, double min, double max)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length == 0 || normalized == "-")
                return false;

            const NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands;
            if (!double.TryParse(normalized, styles, CultureInfo.CurrentCulture, out double measured)
                && !double.TryParse(normalized, styles, CultureInfo.InvariantCulture, out measured))
            {
                return true;
            }

            return measured < min || measured > max;
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
            _descriptionSerialNumber = txtSerialNumber?.Text.Trim() ?? string.Empty;
            _currentPayloadDescription = DescriptionPassText;
        }

        private void EnsureDescriptionBelongsToCurrentSerial()
        {
            string currentSerial = txtSerialNumber.Text.Trim();
            if (string.Equals(_descriptionSerialNumber, currentSerial, StringComparison.OrdinalIgnoreCase))
                return;

            ResetDescriptionForNewTest();
            _descriptionSerialNumber = currentSerial;
        }
    }
}
