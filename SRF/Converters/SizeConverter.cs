using System;
using System.Windows.Data;

using SRF.Resources;

namespace SRF.Converters
{

    public class SizeConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var param           = (string)parameter;
            var currentSize     = (long)values[0];
            var currentFullSize = (long)values[1];
            var size            = (long)values[2];
            var fullSize        = (long)values[3];
            string temp         = Resource.Byte;
            string temp1        = Resource.Byte;

            if (currentFullSize > 1048576)
            {
                currentSize /= 1048576;
                currentFullSize /= 1048576;
                temp = Resource.MByte;
            }
            else if (currentFullSize > 1024 && currentFullSize < 1048576)
            {
                currentSize /= 1024;
                currentFullSize /= 1024;
                temp = Resource.KByte;
            }

            if (fullSize > 1048576)
            {
                size /= 1048576;
                fullSize /= 1048576;
                temp1 = Resource.MByte;
            }
            else if (fullSize > 1024 && fullSize < 1048576)
            {
                size /= 1024;
                fullSize /= 1024;
                temp1 = Resource.KByte;
            }

            return string.Format(param, currentSize, currentFullSize, temp, size, fullSize, temp1);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

}