using LiveCharts;
using System;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace PacketAnalysisApp
{
    public class RealTimeChartViewModel : INotifyPropertyChanged
    {

        ChartValues<double> chartValues;
        private ObservableCollection<string> labels;

        public RealTimeChartViewModel()
        {
            chartValues = new ChartValues<double>();
            labels = new ObservableCollection<string>();
        }

        public ChartValues<double> ChartValues
        {
            get { return chartValues; }
            set
            {
                chartValues = value;
                OnPropertyChanged("ChartValuess");
            }
        }

        public ObservableCollection<string> Labels
        {
            get { return labels; }
            set
            {
                labels = value;
                OnPropertyChanged("Labelss");
            }
        }

        public void AddDataPoint(double value)
        {
            chartValues.Add(value);
            labels.Add(DateTime.Now.ToString("HH:mm:ss"));
            OnPropertyChanged("ChartValuess");
            OnPropertyChanged("Labelss");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
