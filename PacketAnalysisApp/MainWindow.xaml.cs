using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using LiveCharts;
using LiveCharts.Definitions.Charts;
using LiveCharts.Wpf;
using LiveCharts.Wpf.Charts.Base;
using NetMQ;
using NetMQ.Sockets;
using LiveCharts.Defaults;

namespace PacketAnalysisApp
{
    class StringArrayComparer : IEqualityComparer<string[]>
    {
        public bool Equals(string[] x, string[] y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(string[] obj)
        {
            int hash = 17;
            foreach (var s in obj)
            {
                hash = hash * 31 + s.GetHashCode();
            }
            return hash;
        }
    }
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


    public partial class MainWindow : Window
    {
        byte[] Key = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20 };
        byte[] IV = new byte[] { 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30 };
        int paketByte = 0;
        int projeByte = 1;

        bool startConnect = true;
        bool disconnect = false;
        bool stop = false;
        Thread subscriber;
        SubscriberSocket subSocket = new SubscriberSocket();

        bool equalsKey = false;
        bool rowColorStart = true;
        Dictionary<string, SolidColorBrush> rowColor = new Dictionary<string, SolidColorBrush>();

        EnumMatchWindow enumMatchWindow = new EnumMatchWindow();
        Dictionary<string, Dictionary<int, string>> enumStruct = new Dictionary<string, Dictionary<int, string>>();
        Dictionary<string[], int[]> totalReceivedaPacket = new Dictionary<string[], int[]>(new StringArrayComparer());

        ObservableCollection<KeyValuePair<string[], int[]>> dataSource = new ObservableCollection<KeyValuePair<string[], int[]>>();

        SeriesCollection piechartPaket;
        //Dictionary<string, SeriesCollection> piechartPaket = new Dictionary<string, SeriesCollection>();

        string paketName = string.Empty;

        Dictionary<string, ChartValues<int>> chartValuesList = new Dictionary<string, ChartValues<int>>();

        private DispatcherTimer timer;

        byte[] bytes;
        int[] privTotal;

        TextBlock textBlock;
        DataGridRow row;

        Dictionary<string[], CartesianChart> chartList = new Dictionary<string[], CartesianChart>();
        Dictionary<string[], LineSeries> lineSeriesList = new Dictionary<string[], LineSeries>();
        Dictionary<string[], ChartValues<int>> lineValuesList = new Dictionary<string[], ChartValues<int>>();
        ObservableCollection<string> chartXLabels = new ObservableCollection<string>();        

        Dictionary<string, string> chartStatuses = new Dictionary<string, string>();
        Button zoomButton = new Button();
        Button realButton = new Button();
        string chartStatus = "DEFAULT";


        //----------------- PAKET BUTONLARI ----------------------
        Dictionary<string, Button> paketButtons  = new Dictionary<string, Button>();
        List<Button> buttonsToRemove = new List<Button>();

        Dictionary<string, CartesianChart> barCharts = new Dictionary<string, CartesianChart>();
        Dictionary<string, ColumnSeries> barColumnSeries = new Dictionary<string, ColumnSeries>();
        List<CartesianChart> barChartsToRemove = new List<CartesianChart>();
        public MainWindow()
        {            
            InitializeComponent();
            startConnect = true;
            disconnectButton.IsEnabled = false;
            subscriber = new Thread(new ThreadStart(receiveData));
            // -------------------- EVENTLER --------------------
            enumMatchWindow.Closed += enumMatchClosed;
            enumMatchWindow.OkKaydetLog.Click += enumKaydetClick;
            enumMatchWindow.UpdatedList += EnumMatchingWindow_UpdatedList;


            //zoomButton.Click += Button_Click;

            // -------------------- ENUM YAPISININ OLULŞTURULMASI --------------------
            enumStruct = enumMatchWindow.enumStructMain;

            // -------------------- TOPLAM PAKET SAYISININ TUTAN YAPI --------------------
            //createTotalPacketDict();

            //updateGrid();

            // -------------------- ZERO MQ DATA ALMA THREAD --------------------
            //Thread subscriber = new Thread(new ThreadStart(receiveData));
            //subscriber.IsBackground = true;
            //subscriber.Start();
        }


        // -------------------- PENCERE MOUSE EVENTLERİ --------------------
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private bool IsMaximized = false;
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (IsMaximized)
                {
                    this.WindowState = WindowState.Normal;
                    this.Width = 1080;
                    this.Height = 720;

                    IsMaximized = false;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;

                    IsMaximized = true;
                }
            }
        }

        private void zoomButtonLoaded(object sender, RoutedEventArgs e)
        {
            zoomButton = sender as Button;
            //zoomButton.Click += Button_Click;
        }

        private void realButtonLoaded(object sender, RoutedEventArgs e)
        {
            realButton = sender as Button;
            //zoomButton.Click += Button_Click;
        }

        public void createTotalPacketDict()
        {
            equalsKey = false;
            barCharts = new Dictionary<string, CartesianChart>();
            barColumnSeries = new Dictionary<string, ColumnSeries>();

            buttonsToRemove = new List<Button>();
            buttonsToRemove = buttonPiePanel.Children.OfType<Button>().ToList();
            barChartsToRemove = buttonPiePanel.Children.OfType<CartesianChart>().ToList();

            chartList = new Dictionary<string[], CartesianChart>();
            lineSeriesList = new Dictionary<string[], LineSeries>();
            lineValuesList = new Dictionary<string[], ChartValues<int>>();
            chartXLabels = new ObservableCollection<string>();
            chartStatuses = new Dictionary<string, string>();
            paketButtons = new Dictionary<string, Button>();
            totalReceivedaPacket = new Dictionary<string[], int[]>(new StringArrayComparer());

            for (int i = 0; i < enumStruct[enumMatchWindow.paketName].Count; i++)
            {
                string name = enumStruct[enumMatchWindow.paketName].Values.ElementAt(i);
                barColumnSeries.Add(name, new ColumnSeries());
                barColumnSeries[name].Title = name;
                barColumnSeries[name].Values = new ChartValues<int>();

                CartesianChart barChart = new CartesianChart();
                barChart.Name = enumStruct[enumMatchWindow.paketName].Values.ElementAt(i);
                barChart.Visibility = Visibility.Collapsed;
                barChart.Height = 200;
                barChart.DisableAnimations = true;
                barCharts.Add(name, barChart);

                barCharts[name].Series = new SeriesCollection { barColumnSeries[name] };

                barCharts[name].AxisX = new AxesCollection { new Axis { Labels = new List<string>() } };

                Button paketButton = new Button();
                paketButton.Name = name;
                paketButton.Margin = new Thickness(200, 10, 200, 10);
                paketButton.Content = paketButton.Name;
                paketButton.HorizontalContentAlignment = HorizontalAlignment.Center;
                paketButton.VerticalContentAlignment = VerticalAlignment.Center;
                paketButton.Background = Brushes.LightGray;
                paketButton.Foreground = Brushes.Black;
                paketButton.FontWeight = FontWeights.Bold;
                paketButton.FontSize = 14;
                paketButtons.Add(paketButton.Name, paketButton);
                paketButton.Click += paketButtonClicked;

                buttonPiePanel.Children.Add(paketButton);
                buttonPiePanel.Children.Add(barChart);

                for (int j = 0; j < enumStruct[enumStruct[enumMatchWindow.paketName].Values.ElementAt(i)].Values.Count; j++)
                {
                    barCharts[name].AxisX[0].Labels.Add(enumStruct[enumStruct[enumMatchWindow.paketName].Values.ElementAt(i)].Values.ElementAt(j));
                    barColumnSeries[name].Values.Add(0);

                    //MessageBox.Show(enumStruct[enumMatchWindow.paketName].Values.ElementAt(i));
                    int[] deneme = { 0, 0, 0 };
                    string[] paket_proje = { enumStruct[enumMatchWindow.paketName].Values.ElementAt(i),
                                                            enumStruct[enumStruct[enumMatchWindow.paketName].Values.ElementAt(i)].Values.ElementAt(j)};
                    totalReceivedaPacket.Add(paket_proje, deneme);
                    chartList.Add(paket_proje, new CartesianChart());
                    lineSeriesList.Add(paket_proje, new LineSeries());
                    lineValuesList.Add(paket_proje, new ChartValues<int>());
                    chartStatuses.Add(paket_proje[0] + "_" + paket_proje[1], "DEFAULT");
                }
                
            }


            dataSource.Clear();
            foreach (var data in totalReceivedaPacket)
            {
                dataSource.Add(data);
            }
            //dataSource.Add(totalReceivedaPacket);

        }

        private void UpdateFrekans(object sender, EventArgs e)
        {

            chartXLabels.Add(DateTime.Now.ToString("HH:mm:ss"));
            for (int i = 0; i < totalReceivedaPacket.Count; i++)
            {
                string[] paket_proje = totalReceivedaPacket.Keys.ElementAt(i);
                //string[] paket_proje = {enumStruct[paketName].Values.ElementAt(i),
                //                       enumStruct[enumStruct[enumMatchWindow.paketName].Values.ElementAt(i)].Values.ElementAt(i)};
                int currentTotal = totalReceivedaPacket[paket_proje][1];
                dataGrid.Dispatcher.Invoke(new Action(() =>
                {
                    var item = dataSource.FirstOrDefault(j => j.Key == paket_proje);
                    if (item.Key == null)
                    {
                    }
                    else
                    {
                        //dataSource.Remove(item);
                        int index = dataSource.IndexOf(item);
                        //item.Value[1] = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1];
                        //dataSource.Add(item);
                        dataSource[index] = new KeyValuePair<string[], int[]>(item.Key, new int[] { currentTotal - privTotal[i], item.Value[1] });
                        //dataSource[index].Value[1] += 1;
                    }
                }));

                //int currentTotal = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt(i)][1];
                totalReceivedaPacket[paket_proje][0] = currentTotal - privTotal[i];
                privTotal[i] = currentTotal;
                lineValuesList[totalReceivedaPacket.Keys.ElementAt(i)].Add(totalReceivedaPacket[paket_proje][0]);
                lineSeriesList[totalReceivedaPacket.Keys.ElementAt(i)].Values = lineValuesList[totalReceivedaPacket.Keys.ElementAt(i)];
                try 
                {
                    switch (chartStatuses[paket_proje[0] + "_" + paket_proje[1]])
                    {
                        case "ZOOM-":
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].Zoom = ZoomingOptions.None;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].Pan = PanningOptions.None;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisY[0].MinValue = lineValuesList[totalReceivedaPacket.Keys.ElementAt(i)].Min();
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisY[0].MaxValue = lineValuesList[totalReceivedaPacket.Keys.ElementAt(i)].Max();
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisX[0].MinValue = 0;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisX[0].MaxValue = chartXLabels.Count - 1;
                            break;
                        case "REAL":
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisY[0].MinValue = 0;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisY[0].MaxValue = lineValuesList[totalReceivedaPacket.Keys.ElementAt(i)].Max();
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisX[0].MinValue = chartXLabels.Count - 20;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisX[0].MaxValue = chartXLabels.Count - 1;
                            break;
                        case "DEFAULT":
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].Zoom = ZoomingOptions.X;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].Pan = PanningOptions.X;                           
                            break;
                    }
                }
                catch
                {
                    continue;
                }
                //cv.Add(totalReceivedaPacket[enumStruct[paketName].Values.ElementAt(i)][0]);
                //lineSeries.Values = cv;
            }
            //int currentTotal = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1];
            //totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][0] = currentTotal - privTotal;
            //privTotal = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1];            
        }

        

        private void ChartPanEvent(object sender, MouseButtonEventArgs e )
        {

            CartesianChart chart = sender as CartesianChart;
            if (chart != null)
            {
                chartStatuses[chart.Name] = "DEFAULT";
            }

            //chartStatus = "DEFAULT";
        }


        private void ChartZoomEvent(object sender, MouseWheelEventArgs e)
        {

            CartesianChart chart = sender as CartesianChart;
            if (chart != null)
            {
                chartStatuses[chart.Name] = "DEFAULT";
            }

            //chartStatus = "DEFAULT";
        }

        

        public void deneme()
        {
            Task.Run(() =>
            {
                //MessageBox.Show("Pie Chart Yüklendi");
                rowColor.Clear();
                pieChart.Dispatcher.Invoke(new Action(() =>
                {
                    foreach (Series series in pieChart.Series)
                    {
                        if (series is PieSeries pieSeries)
                        {
                            if (series.Fill is SolidColorBrush solidColorBrush)
                            {
                                //MessageBox.Show(series.Title);
                                rowColor.Add(series.Title, solidColorBrush);
                            }
                        }
                    }

                    if (rowColor.Count == paketButtons.Count)
                    {
                        foreach (var btn in paketButtons)
                        {
                            btn.Value.Background = rowColor[btn.Key];
                            ((ColumnSeries)barCharts[btn.Key].Series[0]).Fill = rowColor[btn.Key];
                        }
                    }
                    //setColor();
                }));
            });
            
            
        }
        private void DataGridLoaded(object sender, DataGridRowEventArgs e)
        {
            Task task = new Task(() => {

                dataGrid.Dispatcher.Invoke(() =>
                {
                    DataGridRow row = e.Row;
                    KeyValuePair<string[], int[]> item = (KeyValuePair<string[], int[]>)row.Item;
                    DataGridCell cell = GetCell(row, 0);

                    try
                    {
                        if (cell != null)
                        {
                            cell.Background = rowColor[item.Key[0]];
                        }


                        row.FontWeight = FontWeights.Bold;
                        SolidColorBrush newSolidColorBrush = new SolidColorBrush(Color.FromArgb((byte)70, rowColor[item.Key[0]].Color.R,
                                                                rowColor[item.Key[0]].Color.G, rowColor[item.Key[0]].Color.B));

                        //row.Background.Opacity = 50;
                        row.Background = newSolidColorBrush;
                        row.Foreground = Brushes.Black;
                        row.Foreground.Opacity = 255;
                        //row.Background.Freeze();
                    }
                    catch { }
                });
                });

            task.Start();
            Task.Run(() => task);
            //if (!equalsKey)
            //{
            //    for (int i = 0; i<totalReceivedaPacket.Count; i++)
            //    {
            //        equalsKey = rowColor.ContainsKey(totalReceivedaPacket.Keys.ElementAt(i)[0]);
            //    }
            //}


            


            //try
            //{
            //    dataGrid.Dispatcher.Invoke(new Action(() =>
            //    {
                    
            //        int count = 0;
            //        foreach (var Row in dataGrid.Items)
            //        {

            //            //count++;
            //            //MessageBox.Show(count.ToString());

            //            var row = dataGrid.ItemContainerGenerator.ContainerFromItem(Row) as DataGridRow;
            //            KeyValuePair<string[], int[]> item = (KeyValuePair<string[], int[]>)row.Item;
            //            DataGridCell cell = GetCell(row, 0);
            //            cell.Background = rowColor[item.Key[0]];
            //            row.Foreground = rowColor[item.Key[0]];
            //            row.FontWeight = FontWeights.Bold;
            //        }
            //    }));
            //    dataGrid.ItemsSource = dataSource.ToList();
            //}
            //catch { }

        }

        // -------------------- Ayarlar Buton Fonksiyonu --------------------
        public void AyarlarClicked(object sender, RoutedEventArgs e)
        {

            //if(timer != null) timer.Stop();
            enumMatchWindow.Show();
        }

        private void LoadTextBlock(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    chartList[selectedRow.Key] = sender as CartesianChart;
                    chartList[selectedRow.Key].Name = selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1);
                    chartList[selectedRow.Key].Height = 200;
                    chartList[selectedRow.Key].Series = new SeriesCollection { lineSeriesList[selectedRow.Key] };
                    chartList[selectedRow.Key].AxisX[0].Labels = chartXLabels;
                }
            }));            

            //chartDeneme.Series = new SeriesCollection(lineSeries);

            //CartesianChart chart = sender as CartesianChart;
            //chart.DataContext = chartViewModels[selectedRow.Key];

        }

        public void ButtonDetayClicked(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Detay Butonuna Tıklandı");
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                //var selectedRow = dataGrid.SelectedItem;
                DataGridRow selectedRow = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(dataGrid.SelectedItem);
                if (selectedRow != null)
                {
                    KeyValuePair<string[], int[]> a = (KeyValuePair<string[], int[]>)selectedRow.Item;
                    row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(a);
                    if (row.DetailsVisibility != Visibility.Visible) row.DetailsVisibility = Visibility.Visible;
                    else row.DetailsVisibility = Visibility.Collapsed;
                }
            }));

            //row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(dataGrid.SelectedCells[0].Item);            


            //enumMatchWindow.Show();
        }
        // -------------------- FİLTRE FONKSİYONU --------------------
        List<KeyValuePair<string[], int[]>> filterModeList = new List<KeyValuePair<string[], int[]>>();
        public void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            filterModeList.Clear();
            if (searchBox.Text.Equals(""))
            {
                filterModeList.AddRange(dataSource.ToList());
            }
            else
            {
                foreach (KeyValuePair<string[], int[]> packet in dataSource)
                {
                    if (packet.Key[0].Contains(searchBox.Text) || packet.Key[1].Contains(searchBox.Text) 
                        || packet.Key[0].Contains(searchBox.Text.ToUpper()) || packet.Key[1].Contains(searchBox.Text.ToUpper()))
                    {
                        filterModeList.Add(packet);
                    }
                }
            }
            dataGrid.ItemsSource = filterModeList.ToList();
        }
        private void enumKaydetClick(object sender, RoutedEventArgs e)
        {
            enumStruct = enumMatchWindow.enumStruct;
            if (timer != null) timer.Stop();
            createTotalPacketDict();
            updateGrid();
        }


        public void updateGrid()
        {
            rowColorStart = true;
            rowColor = new Dictionary<string, SolidColorBrush>();

            foreach (var button in buttonsToRemove)
            {
                buttonPiePanel.Children.Remove(button);
            }

            foreach (var chart in barChartsToRemove)
            {
                buttonPiePanel.Children.Remove(chart);
            }

            chartValuesList = new Dictionary<string, ChartValues<int>>();
            piechartPaket = new SeriesCollection();

            Func<ChartPoint, string> labelPoint = chartPoint => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);
            
            for (int i = 0; i < enumStruct[enumMatchWindow.paketName].Values.Count; i++)
            {
                string name = enumStruct[enumMatchWindow.paketName].Values.ElementAt(i);
                chartValuesList.Add(name, new ChartValues<int> { 0 });
                PieSeries pieSeries = new PieSeries
                {
                    Title = name,
                    Values = chartValuesList[name],
                    DataLabels = true,
                    LabelPoint = labelPoint,
                    FontSize = 12
                };
                piechartPaket.Add(pieSeries);


                //rowColor.Add(enumStruct[enumMatchWindow.paketName].Values.ElementAt(i), new SolidColorBrush(sColor.Color));


                //chartViewModels.Add(totalReceivedaPacket.Keys.ElementAt(i), new RealTimeChartViewModel());


            }
            pieChart.Series = piechartPaket;


            // PieChart'taki dilim renklerini alın

            paketName = enumMatchWindow.paketName;

            paketColumn.Binding = new Binding("Key[0]");
            projeColumn.Binding = new Binding("Key[1]");
            frekansColumn.Binding = new Binding("Value[0]");
            toplamColumn.Binding = new Binding("Value[1]");
            dataGrid.ItemsSource = dataSource;

            //dataGrid.ItemsSource = totalReceivedaPacket.ToList();
            //dataGrid.ItemsSource = enumStruct[enumMatchWindow.paketName];

            // -------------------- FREKANS İÇİN TIMER --------------------
            privTotal = new int[totalReceivedaPacket.Count];
            privTotal = privTotal.Select(x => 0).ToArray();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += UpdateFrekans;
            timer.Start();

        }

        private T FindVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;

                if (child == null)
                {
                    child = FindVisualChild<T>(v);
                }

                if (child != null)
                {
                    break;
                }
            }

            return child;
        }

        private DataGridCell GetCell(DataGridRow row, int columnIndex)
        {
            if (row != null && columnIndex >= 0)
            {
                DataGridCellsPresenter cellsPresenter = FindVisualChild<DataGridCellsPresenter>(row);
                if (cellsPresenter != null)
                {
                    return (DataGridCell)cellsPresenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);
                }
            }
            return null;
        }

        public void setColor()
        {
            //try
            //{
            //    dataGrid.Dispatcher.Invoke(new Action(() =>
            //    {
            //        int count = 0;
            //        foreach (var Row in dataGrid.Items)piechartloaded
            //        {
            //            //count++;
            //            //MessageBox.Show(count.ToString());

            //            var row = dataGrid.ItemContainerGenerator.ContainerFromItem(Row) as DataGridRow;
            //            KeyValuePair<string[], int[]> item = (KeyValuePair<string[], int[]>)row.Item;
            //            DataGridCell cell = GetCell(row, 0);
            //            cell.Background = rowColor[item.Key[0]];
            //            row.Foreground = rowColor[item.Key[0]];
            //            row.FontWeight = FontWeights.Bold;
            //        }                    
            //    }));
            //    dataGrid.ItemsSource = dataSource;
            //}
            //catch { }

        }

        private void PieChartLoaded(object sender, RoutedEventArgs e)
        {
            //deneme();
        }

        private void enumMatchClosed(object sender, EventArgs e)
        {
            enumMatchWindow = new EnumMatchWindow();
            enumMatchWindow.Closed += enumMatchClosed;
            enumMatchWindow.OkKaydetLog.Click += enumKaydetClick;
            enumMatchWindow.UpdatedList += EnumMatchingWindow_UpdatedList;
            //for (int i = 0; i < enumStruct.Count; i++)
            //{
            //    Dictionary<int, string> enumValue = enumStruct[enumStruct.Keys.ElementAt(i)];

            //    textBox.Text +=  enumStruct.Keys.ElementAt(i) + "\n{";
            //    for (int j = 0; j < enumValue.Count; j++)
            //    {
            //        textBox.Text += "\t" + enumValue.Keys.ElementAt(j) + " = " + enumValue.Values.ElementAt(j) + "\n";
            //    }
            //    textBox.Text += "}\n";
            //}
        }

        private void EnumMatchingWindow_UpdatedList(Dictionary<string, Dictionary<int, string>> updatedList)
        {
            //enumStruct = updatedList;
            //textBox.Text = string.Empty;
        }

        public byte[] DecryptAes(byte[] encrypted)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                    {
                        csDecrypt.Write(encrypted, 0, encrypted.Length);
                        csDecrypt.FlushFinalBlock();
                        return msDecrypt.ToArray();
                    }
                }
            }
        }

        public void receiveData()
        {
            bytes = new byte[6]; 

            while (!stop)
            {                
                bytes = ReceivingSocketExtensions.ReceiveFrameBytes(subSocket);
                bytes = DecryptAes(bytes);

                if (rowColorStart)
                {
                    deneme();
                    rowColorStart = false;
                }

                string[] paket_proje = new string[] { enumStruct[paketName].Values.ElementAt((int)bytes[paketByte]),
                                            enumStruct[enumStruct[paketName].Values.ElementAt((int)bytes[paketByte])].Values.ElementAt((int)bytes[projeByte]) };
    
                //CompDict(paket_proje);
                totalReceivedaPacket[paket_proje][1] += 1;


                int total = 0;
                dataGrid.Dispatcher.Invoke(new System.Action(() =>
                {   
                    int idx = 0;
                    foreach(var data in totalReceivedaPacket)
                    {
                        if (data.Key[0] == paket_proje[0])
                        {
                            total += data.Value[1];
                            barColumnSeries[paket_proje[0]].Values[idx] = data.Value[1];
                            idx++;
                        }                          
                    }

                    chartValuesList[paket_proje[0]][0] = total;
                    //chartValuesList[paket_proje[0]][0] = totalReceivedaPacket[paket_proje][1] + 1;
                    //piechartPaket[idx].Values = chartValuesList[paket_proje[0]];


                    var item = dataSource.FirstOrDefault(i => i.Key.SequenceEqual(paket_proje));
                    if (item.Key == null)
                    {
                        dataSource.Add(new KeyValuePair<string[], int[]>(paket_proje, totalReceivedaPacket[paket_proje]));
                    }
                    else
                    {

                        int index = dataSource.IndexOf(item);
                        dataSource[index].Value[1] += 1;
                    }
                        
                    //dataGrid.Items.Refresh();
                        
                    dataGrid.ItemsSource = dataSource;
                }));
            }
        }

        private void MainAppClosed(object sender, EventArgs e)
        {
            subscriber.Abort();
            subSocket.Dispose();
            //subSocket.Close();
            Environment.Exit(0);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("zoom butonuna tıklandı");
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    chartStatuses[selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1)] = "ZOOM-";
                }
            }));
            //chartStatus = "ZOOM-";
        }
        private void realButton_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    chartStatuses[selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1)] = "REAL";
                }
            }));
            //chartStatus = "REAL";
        }

        private void paketButtonClicked(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            foreach(var paketButton in paketButtons)
            {
                if(paketButton.Key != clickedButton.Name)
                {

                    if (paketButton.Value.Visibility != Visibility.Collapsed)
                    {
                        paketButton.Value.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        paketButton.Value.Visibility = Visibility.Visible;
                    }
                }
            }

            foreach(var bar in barCharts)
            {
                if(bar.Key == clickedButton.Name)
                {
                    if(bar.Value.Visibility == Visibility.Collapsed)
                    {
                        bar.Value.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        bar.Value.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void ConnectButtonClicked(object sender, RoutedEventArgs e)
        {
            
            if (timer != null) timer.Stop();
            //Thread subscriber = new Thread(new ThreadStart(receiveData));
            subscriber.Abort();
            //Thread.Sleep(1000);
            stop = true;
            //Thread.Sleep(1000);
            subSocket.Dispose();
            

            try
            {
                //127.0.0.1:12345
                subSocket = new SubscriberSocket();
                subSocket.Options.ReceiveHighWatermark = 1000;
                subSocket.Connect("tcp://" + ipBox.Text + ":" + portBox.Text);
                subSocket.SubscribeToAnyTopic();
                stop = false;
                disconnectButton.IsEnabled = true;
                //timer.Start();
            }
            catch
            {
                MessageBox.Show("IP Adresini ve Portu Kontrol Ediniz");
                return;
            }

            if (startConnect)
            {
                createTotalPacketDict();
                updateGrid();
                this.WindowState = WindowState.Maximized;
                startConnect = false;
            }
            else
            {
                if (timer != null) timer.Start();
                foreach (var button in paketButtons)
                {
                    button.Value.Visibility = Visibility.Visible;
                }
            }
            borderSocketPanel.Visibility = Visibility.Collapsed;

            subscriber = new Thread(new ThreadStart(receiveData));
            subscriber.IsBackground = true;
            subscriber.Start();

        }

        private void DisconnectButtonClicked(object sender, RoutedEventArgs e)
        {
            subscriber.Abort();
            stop = true;
            disconnect = true;
            if (!subSocket.IsDisposed) subSocket.Close();
        }

        private void SocketPanelButtonClicked(object sender, RoutedEventArgs e)
        {

            if (!startConnect)
            {
                if (borderSocketPanel.Visibility == Visibility.Visible)
                {
                    borderSocketPanel.Visibility = Visibility.Collapsed;
                    foreach (var button in paketButtons)
                    {
                        button.Value.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    borderSocketPanel.Visibility = Visibility.Visible;
                    foreach (var button in paketButtons)
                    {
                        button.Value.Visibility = Visibility.Collapsed;
                    }
                    foreach (var chart in barCharts)
                    {
                        chart.Value.Visibility = Visibility.Collapsed;
                    }
                }
            }

        }

        private void realTimeChart_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }
    }
}