using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace SRF.Converters
{

    public class IPAddressValidationRule : ValidationRule
    {

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null)
                return new ValidationResult(false, "String is null.");

            var address = (string)value;
            var regex = new Regex(@"[1-2]?[0-9]?[0-9]\.[1-2]?[0-9]?[0-9]\.[1-2]?[0-9]?[0-9]\.[1-2]?[0-9]?[0-9]", RegexOptions.IgnoreCase);

            if (!regex.IsMatch(address))
            {
                return new ValidationResult(false, "IP-address is invalid.");
            }

            return ValidationResult.ValidResult;
        }

    }

}
