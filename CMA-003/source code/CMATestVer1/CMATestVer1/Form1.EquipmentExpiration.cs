using System.Globalization;

namespace CMATestVer1
{
    public partial class Form1
    {
        private static string? NormalizeEquipmentExpirationForPayload(string? expiration)
        {
            string value = expiration?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value) || value == "-")
                return null;

            string[] supportedFormats =
            {
                "d/M/yy",
                "dd/MM/yy",
                "d/M/yyyy",
                "dd/MM/yyyy",
                "yyyy-MM-dd"
            };

            if (System.DateTime.TryParseExact(
                value,
                supportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime parsedDate))
            {
                return parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return value;
        }
    }
}
