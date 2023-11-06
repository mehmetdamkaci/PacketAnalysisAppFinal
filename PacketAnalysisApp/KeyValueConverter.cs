using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace PacketAnalysisApp
{
    public class KeyValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string[] keyArray)
            {
                if (keyArray.Length == 2)
                {
                    return keyArray[0] + " PAKETİ " + keyArray[1] + " PROJESİ FREKANS GRAFİĞİ";                    
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }  
}
