using System;
using System.Windows.Data;

using SRF.Resources;

namespace SRF.Converters
{

    public class SpeedConverter : IValueConverter
    {

        public object Convert(object values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var speed = (long)values;

            if (speed > 1048576)
                return string.Format((string)parameter, speed / 1048576, Resource.MByte);

            if (speed > 1024 && speed < 1048576)
                return string.Format((string)parameter, speed / 1024, Resource.KByte);

            return string.Format((string)parameter, speed, Resource.Byte);
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

}