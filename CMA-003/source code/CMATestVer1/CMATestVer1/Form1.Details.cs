using Newtonsoft.Json;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CMATestVer1
{
    public partial class Form1
    {
        private const string DefaultFirmware = "AirControl c v1.02.d43(255 KB)";
        private const string DefaultDisplayFirmware = "MCU_DISPLAY_AIR_CONTROL V1.03.hex(84.1 KB)";
        private const string DefaultRevision = "F-PD-07 Rev.6";

        private List<EquipmentRecord> _equipmentDatabaseRecords = EquipmentRecord.CreateDefaults();
        private string _firmwareValue = DefaultFirmware;
        private string _displayFirmwareValue = DefaultDisplayFirmware;
        private string _revisionValue = DefaultRevision;

        private void InitializeDetailsFeature()
        {
            _firmwareValue = GetSavedValue(Properties.Settings.Default.SavedFirmware, DefaultFirmware);
            _displayFirmwareValue = GetSavedValue(Properties.Settings.Default.SavedFirmwareDis, DefaultDisplayFirmware);
            _revisionValue = GetSavedValue(Properties.Settings.Default.SavedREV, DefaultRevision);
            _equipmentDatabaseRecords = LoadSavedEquipmentRecords();
            ApplyLegacyEquipmentExpiry();

            btn_details.Click += BtnDetails_Click;
        }

        private static string GetSavedValue(string? savedValue, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(savedValue) ? defaultValue : savedValue.Trim();
        }

        private void BtnDetails_Click(object? sender, EventArgs e)
        {
            using var popup = new DetailsPopupForm(
                _equipmentDatabaseRecords,
                _firmwareValue,
                _displayFirmwareValue,
                _revisionValue);

            if (popup.ShowDialog(this) != DialogResult.OK)
                return;

            _equipmentDatabaseRecords = popup.EquipmentRecords
                .Select(item => item.Clone())
                .ToList();
            _firmwareValue = popup.MainFirmware;
            _displayFirmwareValue = popup.DisplayFirmware;
            _revisionValue = popup.Revision;

            SaveDetailSettings();
        }

        private void SaveDetailSettings()
        {
            Properties.Settings.Default.SavedFirmware = _firmwareValue;
            Properties.Settings.Default.SavedFirmwareDis = _displayFirmwareValue;
            Properties.Settings.Default.SavedREV = _revisionValue;
            Properties.Settings.Default.SavedEquipmentDetails =
                JsonConvert.SerializeObject(_equipmentDatabaseRecords);

            EquipmentRecord primaryEquipment = _equipmentDatabaseRecords[0];
            Properties.Settings.Default.SavedEquipmentText =
                $"{primaryEquipment.Series} | Exp. {primaryEquipment.Expiration}";
            Properties.Settings.Default.Save();
        }

        private static List<EquipmentRecord> LoadSavedEquipmentRecords()
        {
            string savedJson = Properties.Settings.Default.SavedEquipmentDetails?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(savedJson))
                return EquipmentRecord.CreateDefaults();

            try
            {
                List<EquipmentRecord>? records =
                    JsonConvert.DeserializeObject<List<EquipmentRecord>>(savedJson);

                if (records == null
                    || records.Count != 4
                    || records.Any(item => string.IsNullOrWhiteSpace(item.Name)
                                           || string.IsNullOrWhiteSpace(item.Series)
                                           || string.IsNullOrWhiteSpace(item.EquipmentId)))
                {
                    return EquipmentRecord.CreateDefaults();
                }

                List<EquipmentRecord> defaults = EquipmentRecord.CreateDefaults();
                for (int index = 0; index < records.Count; index++)
                {
                    if (string.IsNullOrWhiteSpace(records[index].Expiration))
                        records[index].Expiration = defaults[index].Expiration;
                }

                return records;
            }
            catch (JsonException)
            {
                return EquipmentRecord.CreateDefaults();
            }
        }

        private void ApplyLegacyEquipmentExpiry()
        {
            if (_equipmentDatabaseRecords.Count == 0)
                return;

            string legacyText = Properties.Settings.Default.SavedEquipmentText?.Trim() ?? "";
            Match match = Regex.Match(
                legacyText,
                @"^(?<equipment>.+?)\s*\|\s*Exp\.?\s*(?<expiry>.+?)\s*$",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                return;

            EquipmentRecord primaryEquipment = _equipmentDatabaseRecords[0];
            primaryEquipment.Expiration = match.Groups["expiry"].Value.Trim();
        }

        private List<QcEquipment> GetEquipmentCodesForPayload()
        {
            var result = new List<QcEquipment>();

            foreach (var item in _equipmentDatabaseRecords)
            {
                if (string.IsNullOrWhiteSpace(item.Name) ||
                    string.IsNullOrWhiteSpace(item.Series) ||
                    string.IsNullOrWhiteSpace(item.EquipmentId))
                {
                    continue;
                }

                bool hasExpiration = !string.IsNullOrWhiteSpace(item.Expiration) && item.Expiration.Trim() != "-";

                string? expireDate = hasExpiration
                    ? NormalizeEquipmentExpiryDate(item.Expiration.Trim())
                    : null;

                result.Add(new QcEquipment
                {
                    Name = item.Name.Trim(),
                    Series = item.Series.Trim(),
                    EquipmentId = item.EquipmentId.Trim(),
                    ExpireDate = expireDate
                });
            }

            if (result.Count == 0)
            {
                foreach (var defaultItem in EquipmentRecord.CreateDefaults())
                {
                    bool hasExp = !string.IsNullOrWhiteSpace(defaultItem.Expiration) && defaultItem.Expiration.Trim() != "-";
                    result.Add(new QcEquipment
                    {
                        Name = defaultItem.Name,
                        Series = defaultItem.Series,
                        EquipmentId = defaultItem.EquipmentId,
                        ExpireDate = hasExp ? NormalizeEquipmentExpiryDate(defaultItem.Expiration) : null
                    });
                }
            }

            return result;
        }

        private string GetEquipmentExcelText()
        {
            var records = _equipmentDatabaseRecords != null && _equipmentDatabaseRecords.Count > 0
                ? _equipmentDatabaseRecords
                : EquipmentRecord.CreateDefaults();

            var parts = new List<string>();

            for (int i = 0; i < records.Count; i++)
            {
                var item = records[i];
                if (string.IsNullOrWhiteSpace(item.Series) && string.IsNullOrWhiteSpace(item.Name))
                    continue;

                string label;
                // สำหรับ S-T ให้ระบุ Name + Series (เช่น Magnetic Contactor S-T)
                if (item.Series.Trim().Equals("S-T", StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(item.Name) && item.Name.Contains("Contactor", StringComparison.OrdinalIgnoreCase)))
                {
                    string name = string.IsNullOrWhiteSpace(item.Name) ? "Magnetic Contactor" : item.Name.Trim();
                    string series = string.IsNullOrWhiteSpace(item.Series) ? "S-T" : item.Series.Trim();
                    label = $"{name} {series}";
                }
                else
                {
                    label = !string.IsNullOrWhiteSpace(item.Series) ? item.Series.Trim() : item.Name.Trim();
                }

                bool hasExpiration = !string.IsNullOrWhiteSpace(item.Expiration) && item.Expiration.Trim() != "-";
                if (hasExpiration)
                {
                    string expiry = NormalizeEquipmentExpiryDate(item.Expiration.Trim());
                    parts.Add($"{label} วันหมดอายุ {expiry}");
                }
                else
                {
                    parts.Add(label);
                }
            }

            if (parts.Count == 0)
                return "เครื่องมือ OS-270 วันหมดอายุ 1/4/26, PH-07, PH-01, Magnetic Contactor S-T";

            return "เครื่องมือ " + string.Join(", ", parts);
        }

        private static string NormalizeEquipmentExpiryDate(string rawDate)
        {
            if (rawDate == "-")
                return rawDate;

            string[] parts = Regex.Split(rawDate.Trim(), @"[/\-.]");
            if (parts.Length != 3
                || !int.TryParse(parts[0], out int day)
                || !int.TryParse(parts[1], out int month)
                || !int.TryParse(parts[2], out int enteredYear))
            {
                return rawDate;
            }

            int gregorianYear;
            if (enteredYear >= 2400)
                gregorianYear = enteredYear - 543;
            else if (enteredYear >= 43 && enteredYear <= 99)
                gregorianYear = enteredYear + 1957;
            else if (enteredYear >= 0 && enteredYear <= 42)
                gregorianYear = enteredYear + 2000;
            else
                gregorianYear = enteredYear;

            try
            {
                var parsedDate = new DateTime(gregorianYear, month, day);
                return parsedDate.ToString("d/M/yy", CultureInfo.InvariantCulture);
            }
            catch (ArgumentOutOfRangeException)
            {
                return rawDate;
            }
        }
    }
}
