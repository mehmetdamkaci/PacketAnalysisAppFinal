using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PacketAnalysisApp
{
    /// <summary>
    /// Popup.xaml etkileşim mantığı
    /// </summary>
    public partial class Popup : Window
    {
        public bool resultExport = false;
        public bool resultDelFile = false;
        public Popup()
        {
            InitializeComponent();
        }

        public delegate void PopupEventHandler(object sender, RoutedEventArgs e);
        public event PopupEventHandler PopupEvent;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Visibility = Visibility.Collapsed;
            if(exportCheckBox.IsChecked == true)
            {
                resultExport = true;
            }
            if(saveCheckBox.IsChecked == true) 
            {
                resultDelFile = true;
            }

            PopupEvent?.Invoke(sender, e);
        }
    }
}
