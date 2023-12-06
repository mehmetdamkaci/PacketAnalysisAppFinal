using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PacketAnalysisApp
{
    public class EnumMatchedGridConverter : IValueConverter
    {
        string packetName;
        public EnumMatchedGridConverter(string addValue)
        {
            this.packetName = addValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string keyArray)
            {
                return packetName + "." + keyArray;
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
