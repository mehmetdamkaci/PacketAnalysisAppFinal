using LiveCharts;
using LiveCharts.Wpf;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PacketAnalysisApp
{
    public partial class MainWindow : Window
    {
        int saveLength = 600;
        DataKeeper dataKeeper = new DataKeeper();
        Export export = new Export();

        string paket = "";

        byte[] Key = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20 };
        byte[] IV = new byte[] { 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30 };
        int paketByte = 0;
        int projeByte = 1;

        bool startConnect = true;
        bool disconnect = false;
        bool stop = false;
        Thread subscriber;
        SubscriberSocket subSocket = new SubscriberSocket();

        bool rowColorStart = true;
        Dictionary<string, SolidColorBrush> rowColor = new Dictionary<string, SolidColorBrush>();

        EnumMatchWindow enumMatchWindow = new EnumMatchWindow();
        Dictionary<string, Dictionary<int, string>> enumStruct = new Dictionary<string, Dictionary<int, string>>();
        Dictionary<string[], int[]> totalReceivedPacket = new Dictionary<string[], int[]>(new StringArrayComparer());

        ObservableCollection<KeyValuePair<string[], int[]>> dataSource = new ObservableCollection<KeyValuePair<string[], int[]>>();

        SeriesCollection piechartPaket;

        string paketName = string.Empty;

        Dictionary<string, ChartValues<int>> pieChartValues = new Dictionary<string, ChartValues<int>>();

        private DispatcherTimer timer;

        byte[] bytes;
        int[] privTotal;

        DataGridRow row;

        Dictionary<string[], CartesianChart> chartList = new Dictionary<string[], CartesianChart>();
        Dictionary<string[], LineSeries> lineSeriesList = new Dictionary<string[], LineSeries>();
        Dictionary<string[], ChartValues<int>> lineValuesList = new Dictionary<string[], ChartValues<int>>();
        ObservableCollection<string> chartXLabels = new ObservableCollection<string>();

        Dictionary<string[], StackPanel> chartExportPanel = new Dictionary<string[], StackPanel>();
        Dictionary<string, string> chartStatuses = new Dictionary<string, string>();
        Button zoomButton = new Button();
        Button realButton = new Button();
        Button chartExportButton = new Button();

        Dictionary<string[], StackPanel> dimChartExportPanel = new Dictionary<string[], StackPanel>();
        Dictionary<string[], CartesianChart> dimChartList = new Dictionary<string[], CartesianChart>();
        Dictionary<string[], LineSeries> dimLineSeriesList = new Dictionary<string[], LineSeries>(new StringArrayComparer());
        Dictionary<string[], ChartValues<int>> dimLineValuesList = new Dictionary<string[], ChartValues<int>>(new StringArrayComparer());
        Dictionary<string[], ObservableCollection<string>> dimChartXLabels = new Dictionary<string[], ObservableCollection<string>>(new StringArrayComparer());

        Dictionary<string, string> dimChartStatuses = new Dictionary<string, string>();
        Button dimZoomButton = new Button();
        Button dimRealButton = new Button();
        Button dimChartExportButton = new Button();

        bool dimExceptAdded = false;
        Dictionary<string[], int> expectedFreq = new Dictionary<string[], int>(new StringArrayComparer());
        Dictionary<string[], int> expectedDim = new Dictionary<string[], int>(new StringArrayComparer());
        Dictionary<string[], StackPanel> dimLabelStacks = new Dictionary<string[], StackPanel>(new StringArrayComparer());

        bool freqExceptAdded = false;
        Dictionary<string[], StackPanel> freqLabelStacks = new Dictionary<string[], StackPanel>(new StringArrayComparer());
        Dictionary<string[], TextBox> expectedFreqBoxs = new Dictionary<string[], TextBox>(new StringArrayComparer());
        Dictionary<string[], TextBox> expectedDimBoxs = new Dictionary<string[], TextBox>(new StringArrayComparer());

        //----------------- PAKET BUTONLARI ----------------------
        Dictionary<string, Button> paketButtons  = new Dictionary<string, Button>();
        List<Button> buttonsToRemove = new List<Button>();

        Dictionary<string, CartesianChart> barCharts = new Dictionary<string, CartesianChart>();
        Dictionary<string, ColumnSeries> barColumnSeries = new Dictionary<string, ColumnSeries>();
        List<CartesianChart> barChartsToRemove = new List<CartesianChart>();
        public MainWindow()
        {                        
            InitializeComponent();
            
            exportButton.IsEnabled = false;
            startConnect = true;
            disconnectButton.IsEnabled = false;
            subscriber = new Thread(new ThreadStart(receiveData));
            // -------------------- EVENTLER --------------------
            enumMatchWindow.Closed += enumMatchClosed;
            enumMatchWindow.OkKaydetLog.Click += enumKaydetClick;

            // -------------------- ENUM YAPISININ OLULŞTURULMASI --------------------
            enumStruct = enumMatchWindow.enumStructMain;
            expectedFreq = enumMatchWindow.expectedFreq;
            expectedDim = enumMatchWindow.expectedDim;
            enumMatchWindow.ExpectedButtonClickedEvent += ExpectedFreqClicked;
            enumMatchWindow.ExpectedDimButtonClickedEvent += ExpectedDimClicked;

            pieChart.DataTooltip = null;
        }

        private void ExpectedDimClicked(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                for (int i = 0; i < expectedDim.Count; i++)
                {
                    dimChartList[expectedDim.ElementAt(i).Key].Dispatcher.Invoke(() =>
                    {
                        if (dimChartList[expectedDim.ElementAt(i).Key].AxisY.Count > 0)
                        {
                            if (dimChartList[expectedDim.ElementAt(i).Key].AxisY[0].Sections.Count > 0)
                            {
                                dimChartList[expectedDim.ElementAt(i).Key].AxisY[0].Sections.Clear();
                                dimChartList[expectedDim.ElementAt(i).Key].AxisY[0].Sections = new SectionsCollection
                            {
                                new AxisSection
                                {
                                    Value = expectedDim[expectedDim.ElementAt(i).Key],
                                    SectionWidth = 0,
                                    Stroke = Brushes.Red,
                                    SectionOffset = 0,
                                    StrokeThickness = 2.5,
                                }
                            };
                                expectedDimBoxs[expectedDim.ElementAt(i).Key].Text = expectedDim[expectedDim.ElementAt(i).Key].ToString();
                            }
                        }
                    });
                }
            }));
        }

        private void ExpectedFreqClicked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < expectedFreq.Count; i++)
            {
                chartList[expectedFreq.ElementAt(i).Key].Dispatcher.Invoke(() =>
                {
                    if (chartList[expectedFreq.ElementAt(i).Key].AxisY.Count > 0)
                    {
                        if (chartList[expectedFreq.ElementAt(i).Key].AxisY[0].Sections.Count > 0)
                        {
                            chartList[expectedFreq.ElementAt(i).Key].AxisY[0].Sections.Clear();
                            chartList[expectedFreq.ElementAt(i).Key].AxisY[0].Sections = new SectionsCollection
                            {
                                new AxisSection
                                {
                                    Value = expectedFreq[expectedFreq.ElementAt(i).Key],
                                    SectionWidth = 0,
                                    Stroke = Brushes.Red,
                                    SectionOffset = 0,
                                    StrokeThickness = 2.5,
                                }
                            };
                            expectedFreqBoxs[expectedFreq.ElementAt(i).Key].Text = expectedFreq[expectedFreq.ElementAt(i).Key].ToString();
                        }
                    }
                });
            }
        }

        //Grafikleri Dışarı aktarma fonksiyonu
        private void dimExportChartButtonClick(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(20);
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    //export.ExportOnlyChart(dataGrid, "BOYUT", selectedRow.Key, dimChartXLabels[selectedRow.Key], dimLineValuesList[selectedRow.Key], dimChartExportPanel[selectedRow.Key]);
                    string fileName = selectedRow.Key[0] + "_" + selectedRow.Key[1];

                    Microsoft.Win32.SaveFileDialog openFileDlg = new Microsoft.Win32.SaveFileDialog();
                    openFileDlg.FileName = "BOYUT" + "_" + fileName + ".xlsx";
                    Nullable<bool> result = openFileDlg.ShowDialog();

                    string savePath = "";
                    if (result == true)
                    {
                        savePath = openFileDlg.FileName;
                    }
                    else
                    {
                        progressBar.Visibility = Visibility.Collapsed;
                        exportButton.Visibility = Visibility.Visible;
                        return;
                    }
                   
                    if (savePath.Substring(savePath.LastIndexOf('.') + 1, 4) != "xlsx") savePath += ".xlsx";

                    dataKeeper.writeData("BOYUT", fileName, dimLineValuesList[selectedRow.Key].ToList<int>(), dimChartXLabels[selectedRow.Key].ToList<string>());
                    dimLineValuesList[selectedRow.Key].Clear();
                    dimChartXLabels[selectedRow.Key].Clear();
                    dataKeeper.readData("BOYUT", fileName, savePath, dimChartExportPanel[selectedRow.Key]);
                }
            }));
        }
        private void exportChartButtonClick(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(20);
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    //export.ExportOnlyChart(dataGrid, "FREKANS", selectedRow.Key, chartXLabels, lineValuesList[selectedRow.Key], chartExportPanel[selectedRow.Key]);
                    string fileName = selectedRow.Key[0] + "_" + selectedRow.Key[1];

                    Microsoft.Win32.SaveFileDialog openFileDlg = new Microsoft.Win32.SaveFileDialog();
                    openFileDlg.FileName = "FREKANS" + "_" + fileName + ".xlsx";
                    Nullable<bool> result = openFileDlg.ShowDialog();

                    string savePath = "";
                    if (result == true)
                    {
                        savePath = openFileDlg.FileName;
                    }
                    else return;
                    if (savePath.Substring(savePath.LastIndexOf('.') + 1, 4) != "xlsx") savePath += ".xlsx";

                    dataKeeper.writeData("FREKANS", fileName, lineValuesList[selectedRow.Key].ToList<int>(), chartXLabels.ToList<string>());
                    lineValuesList[selectedRow.Key].Clear();
                    chartXLabels.Clear();
                    dataKeeper.readData("FREKANS", fileName, savePath, chartExportPanel[selectedRow.Key]);
                }
            }));            
        }

        
        //Dışarı aktarma butonu fonksiyonu (Excel İşlemleri)

        public void exportAll()
        {
            for(int i = 0; i < totalReceivedPacket.Count; i++)
            {
                string fileName = totalReceivedPacket.ElementAt(i).Key[0] + "_" + totalReceivedPacket.ElementAt(i).Key[1];
                dataKeeper.writeData("FREKANS", fileName, lineValuesList.ElementAt(i).Value.ToList<int>(), chartXLabels.ToList<string>());
                lineValuesList.ElementAt(i).Value.Clear();

                dataKeeper.writeData("BOYUT", fileName, dimLineValuesList.ElementAt(i).Value.ToList<int>(), dimChartXLabels.ElementAt(i).Value.ToList<string>());
                dimLineValuesList.ElementAt(i).Value.Clear();
                dimChartXLabels.ElementAt(i).Value.Clear();
            } chartXLabels.Clear();
        }
        
        private void exportClick(object sender, RoutedEventArgs e)
        {
            dataKeeper.colors = rowColor;

            Task.Run(() =>
            {
                dataGrid.Dispatcher.Invoke(new Action(() =>
                {
                    exportButton.Visibility = Visibility.Collapsed;
                    progressBar.Visibility = Visibility.Visible;

                    Microsoft.Win32.SaveFileDialog openFileDlg = new Microsoft.Win32.SaveFileDialog();
                    Nullable<bool> result = openFileDlg.ShowDialog();

                    string savePath = "";
                    if (result == true)
                    {
                        savePath = openFileDlg.FileName;
                    }
                    else return;
                    if (savePath.Substring(savePath.LastIndexOf('.') + 1, 4) != "xlsx") savePath += ".xlsx";

                    exportAll();
                    dataKeeper.mainExport(totalReceivedPacket, savePath, progressBar, exportButton, exportLabel);
                }));
            });



            //export.MainExport(dataGrid, progressBar, totalReceivedPacket, rowColor, chartXLabels, lineValuesList, pieChartValues, exportButton, exportLabel, dimChartXLabels, dimLineValuesList);
        }

        private void BrowseExportButton(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
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

        public void barChartFilter(string name)
        {

            foreach (var paketButton in paketButtons)
            {
                if (paketButton.Key != name)
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

            foreach (var bar in barCharts)
            {
                if (bar.Key == name)
                {
                    if (bar.Value.Visibility == Visibility.Collapsed)
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

        //PieCharta tıklandığında filtreleme işlemleri
        private void PieChartClick(object sender, ChartPoint chartpoint)
        {
            bool control = false;
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                
                for (int i = 0; i < dataGrid.Items.Count; i++)
                {
                    DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                    if (row != null)
                    {
                        string[] name = ((KeyValuePair<string[], int[]>)row.Item).Key;

                        if (paket == chartpoint.SeriesView.Title)
                        {
                            row.Visibility = Visibility.Visible;
                            barCharts[name[0]].Visibility = Visibility.Collapsed;
                            paketButtons[name[0]].Visibility = Visibility.Visible;
                            control = true;
                        }
                        else if (paket != chartpoint.SeriesView.Title & name[0] == chartpoint.SeriesView.Title)
                        {
                            row.Visibility = Visibility.Visible;

                        }
                        else
                        {
                            if (row.Visibility == Visibility.Visible) row.Visibility = Visibility.Collapsed;
                        }                        
                    }
                }
            }));


            if (!control) 
            {
                paket = chartpoint.SeriesView.Title;
                foreach (var paketButton in paketButtons)
                {
                    if (paketButton.Key == chartpoint.SeriesView.Title) paketButton.Value.Visibility = Visibility.Visible;
                    else paketButton.Value.Visibility = Visibility.Collapsed;
                }

                foreach (var bar in barCharts)
                {
                    if (bar.Key == chartpoint.SeriesView.Title) bar.Value.Visibility = Visibility.Visible;
                    else bar.Value.Visibility = Visibility.Collapsed;
                }
            } 
            else paket = "";
        }

        //Grafik için butonlar

        private void LoadedChartExportPanel(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(() =>
            {
                dataGrid.Dispatcher.Invoke(new Action(() =>
                {
                    var selecteItem = dataGrid.SelectedItem;
                    if (selecteItem != null)
                    {                        
                        KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                        chartExportPanel[selectedRow.Key] = sender as StackPanel;
                        chartExportPanel[selectedRow.Key].Children[1].Visibility = Visibility.Collapsed;
                        chartExportPanel[selectedRow.Key].Children[2].Visibility = Visibility.Collapsed;
                    }
                }));
            });
        }
        private void LoadedDimChartExportPanel(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(() =>
            {
                dataGrid.Dispatcher.Invoke(new Action(() =>
                {
                    var selecteItem = dataGrid.SelectedItem;
                    if (selecteItem != null)
                    {
                        KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                        dimChartExportPanel[selectedRow.Key] = sender as StackPanel;
                        dimChartExportPanel[selectedRow.Key].Children[1].Visibility = Visibility.Collapsed;
                        dimChartExportPanel[selectedRow.Key].Children[2].Visibility = Visibility.Collapsed;
                    }
                }));
            });
        }
        private void exportChartButtonLoaded(object sender, RoutedEventArgs e)
        {
            chartExportButton = sender as Button;
        }

        private void zoomButtonLoaded(object sender, RoutedEventArgs e)
        {
            zoomButton = sender as Button;
        }

        private void realButtonLoaded(object sender, RoutedEventArgs e)
        {
            realButton = sender as Button;
        }

        
        //Enum Dosyası değiştirildiğinde ve program başlatıldığında veri yapılarını oluşturan fonksiyon
        public void createDataStruct()
        {
            dimLabelStacks = new Dictionary<string[], StackPanel>(new StringArrayComparer());
            expectedDimBoxs = new Dictionary<string[], TextBox>(new StringArrayComparer());
            expectedFreqBoxs = new Dictionary<string[], TextBox>(new StringArrayComparer());
            freqLabelStacks = new Dictionary<string[], StackPanel>(new StringArrayComparer());
            chartExportPanel = new Dictionary<string[], StackPanel>();
            dimChartExportPanel = new Dictionary<string[], StackPanel>();

            barCharts = new Dictionary<string, CartesianChart>();
            barColumnSeries = new Dictionary<string, ColumnSeries>();

            buttonsToRemove = new List<Button>();
            buttonsToRemove = buttonPiePanel.Children.OfType<Button>().ToList();
            barChartsToRemove = buttonPiePanel.Children.OfType<CartesianChart>().ToList();

            chartList = new Dictionary<string[], CartesianChart>(new StringArrayComparer());
            lineSeriesList = new Dictionary<string[], LineSeries>();
            lineValuesList = new Dictionary<string[], ChartValues<int>>(new StringArrayComparer());
            chartXLabels = new ObservableCollection<string>();
            chartStatuses = new Dictionary<string, string>();
            paketButtons = new Dictionary<string, Button>();
            totalReceivedPacket = new Dictionary<string[], int[]>(new StringArrayComparer());

            dimChartList = new Dictionary<string[], CartesianChart>(new StringArrayComparer());
            dimLineSeriesList = new Dictionary<string[], LineSeries>(new StringArrayComparer());
            dimLineValuesList = new Dictionary<string[], ChartValues<int>>(new StringArrayComparer());
            dimChartXLabels = new Dictionary<string[], ObservableCollection<string>>(new StringArrayComparer());
            dimChartStatuses = new Dictionary<string, string>();

            for (int i = 0; i < enumStruct[enumMatchWindow.paketName].Count; i++)
            {
                string name = enumStruct[enumMatchWindow.paketName].Values.ElementAt(i);
                barColumnSeries.Add(name, new ColumnSeries {});
                barColumnSeries[name].Title = name;
                barColumnSeries[name].Values = new ChartValues<int>();

                CartesianChart barChart = new CartesianChart();
                barChart.Name = enumStruct[enumMatchWindow.paketName].Values.ElementAt(i);
                barChart.Visibility = Visibility.Collapsed;
                barChart.Height = 200;
                barChart.DisableAnimations = true;
                barCharts.Add(name, barChart);

                barCharts[name].Series = new SeriesCollection { barColumnSeries[name] };

                barCharts[name].AxisX = new AxesCollection { new Axis { Labels = new List<string>(), LabelsRotation = 60, Separator = new LiveCharts.Wpf.Separator { Step = 1 } } };

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

                    int[] packetAnalysisArr = { 0, 0, 0, 0 };
                    string[] paket_proje = { enumStruct[enumMatchWindow.paketName].Values.ElementAt(i),
                                                            enumStruct[enumStruct[enumMatchWindow.paketName].Values.ElementAt(i)].Values.ElementAt(j)};
                    totalReceivedPacket.Add(paket_proje, packetAnalysisArr);
                    chartList.Add(paket_proje, new CartesianChart());
                    lineSeriesList.Add(paket_proje, new LineSeries());
                    lineValuesList.Add(paket_proje, new ChartValues<int>());
                    chartStatuses.Add(paket_proje[0] + "_" + paket_proje[1], "DEFAULT");
                    chartExportPanel.Add(paket_proje, new StackPanel());
                    
                    dimChartList.Add(paket_proje, new CartesianChart());
                    dimLineSeriesList.Add(paket_proje, new LineSeries());
                    dimLineValuesList.Add(paket_proje, new ChartValues<int>());
                    dimChartXLabels.Add(paket_proje, new ObservableCollection<string>());
                    dimChartStatuses.Add(paket_proje[0] + "_" + paket_proje[1], "DEFAULT");
                    dimChartExportPanel.Add(paket_proje, new StackPanel());

                    freqLabelStacks.Add(paket_proje, new StackPanel());
                    expectedFreqBoxs.Add(paket_proje, new TextBox());
                    dimLabelStacks.Add(paket_proje, new StackPanel());
                    expectedDimBoxs.Add(paket_proje, new TextBox());

                }
            }

            dataSource.Clear();
            foreach (var data in totalReceivedPacket)
            {
                dataSource.Add(data);
            }
            dataKeeper.packetName = enumMatchWindow.paketName;
            dataKeeper.fileNames = totalReceivedPacket.Keys.ToList();

            dataKeeper.CreateDir();
        }

        //Bir saniyede bir tabloyu ve frekans değerlerini güncelleyen fonksiyon
        private void UpdateFrekans(object sender, EventArgs e)
        {
            bool dataKeep = false;
            Task.Run(() =>
            {
                chartXLabels.Add(DateTime.Now.ToString("HH:mm:ss"));
                
                if(chartXLabels.Count == saveLength)
                {
                    dataKeep = true;
                }
                for (int i = 0; i < totalReceivedPacket.Count; i++)
                {
                    string[] paket_proje = totalReceivedPacket.Keys.ElementAt(i);
                    int currentTotal = totalReceivedPacket[paket_proje][1];
                    dataGrid.Dispatcher.Invoke(new Action(() =>
                    {
                        var item = dataSource.FirstOrDefault(j => j.Key == paket_proje);
                        if (item.Key == null)
                        {
                        }
                        else
                        {
                            int index = dataSource.IndexOf(item);
                            dataSource[index] = new KeyValuePair<string[], int[]>(item.Key, new int[] { currentTotal - privTotal[i], item.Value[1], item.Value[2], item.Value[3] });
                        }

                        totalReceivedPacket[paket_proje][0] = currentTotal - privTotal[i];
                        privTotal[i] = currentTotal;

                        lineValuesList[totalReceivedPacket.Keys.ElementAt(i)].Add(totalReceivedPacket[paket_proje][0]);
                        lineSeriesList[totalReceivedPacket.Keys.ElementAt(i)].Values = lineValuesList[totalReceivedPacket.Keys.ElementAt(i)];

                        if (dataKeep)
                        {                            
                            string fileName = paket_proje[0] + "_" + paket_proje[1];
                            dataKeeper.writeData("FREKANS", fileName, lineValuesList[paket_proje].ToList<int>(), chartXLabels.ToList<string>());
                            lineValuesList[paket_proje].Clear();                                                            
                        }
                        
                        setChartStatues(chartList[totalReceivedPacket.Keys.ElementAt(i)], lineValuesList[totalReceivedPacket.Keys.ElementAt(i)],
                                        chartXLabels, chartStatuses[paket_proje[0] + "_" + paket_proje[1]]);

                        setChartStatues(dimChartList[paket_proje], dimLineValuesList[paket_proje],
                                        dimChartXLabels[paket_proje], dimChartStatuses[paket_proje[0] + "_" + paket_proje[1]]);
                    }));
                }
                if(dataKeep) { chartXLabels.Clear(); }
            });
        }

        public void setChartStatues(CartesianChart chart, ChartValues<int> value, ObservableCollection<string> label, string chartName)
        {

            try
            {
                switch (chartName)
                {
                    case "ZOOM-":
                        chart.Zoom = ZoomingOptions.None;
                        chart.Pan = PanningOptions.None;
                        chart.AxisY[0].MinValue = -1;
                        chart.AxisY[0].MaxValue = (chart.AxisY[0].Sections.Count > 0) ?
                            ((chart.AxisY[0].Sections[0].Value > value.Max()) ? chart.AxisY[0].MaxValue = chart.AxisY[0].Sections[0].Value + 1 : 
                            chart.AxisY[0].MaxValue = value.Max() + 1) :
                            chart.AxisY[0].MaxValue = value.Max() + 1;
                        chart.AxisX[0].MinValue = 0;
                        chart.AxisX[0].MaxValue = label.Count - 1;
                        break;
                    case "REAL":
                        chart.AxisY[0].MinValue = -1;
                        chart.AxisY[0].MaxValue = (chart.AxisY[0].Sections.Count > 0) ? 
                            ((chart.AxisY[0].Sections[0].Value > value.Max()) ? chart.AxisY[0].MaxValue = chart.AxisY[0].Sections[0].Value + 1 :
                            chart.AxisY[0].MaxValue = value.Max() + 1) : 
                            chart.AxisY[0].MaxValue = value.Max() + 1;
                        chart.AxisX[0].MinValue = label.Count - 20;
                        chart.AxisX[0].MaxValue = label.Count - 1;
                        break;
                    case "DEFAULT":

                        dataGrid.Dispatcher.Invoke(new Action(() =>
                        {
                            if (chart.AxisY.Count > 0)
                            {
                                chart.AxisY[0].MinValue = -1;
                                chart.AxisY[0].MaxValue = (chart.AxisY[0].Sections.Count > 0) ?
                                    ((chart.AxisY[0].Sections[0].Value > value.Max()) ? chart.AxisY[0].MaxValue = chart.AxisY[0].Sections[0].Value + 1 :
                                    chart.AxisY[0].MaxValue = value.Max() + 1) :
                                    chart.AxisY[0].MaxValue = value.Max() + 1;

                                chart.Zoom = ZoomingOptions.X;
                                chart.Pan = PanningOptions.X;
                            }

                        }));

                        
                        break;
                }
            }
            catch
            {
                
            }
        }

        //Chart mouse ile sürüklendiğinde oluşan event
        private void DimChartPanEvent(object sender, MouseButtonEventArgs e)
        {
            CartesianChart chart = sender as CartesianChart;
            if (chart != null)
            {
                dimChartStatuses[chart.Name] = "DEFAULT";
            }
        }        
        private void ChartPanEvent(object sender, MouseButtonEventArgs e )
        {
            CartesianChart chart = sender as CartesianChart;
            if (chart != null)
            {
                chartStatuses[chart.Name] = "DEFAULT";
            }
        }

        //Mouse ile grafik büyütülmek istendiğinde oluşan event
        private void ChartZoomEvent(object sender, MouseWheelEventArgs e)
        {
            CartesianChart chart = sender as CartesianChart;
            if (chart != null)
            {
                chartStatuses[chart.Name] = "DEFAULT";
            }

        }
        private void DimChartZoomEvent(object sender, MouseWheelEventArgs e)
        {
            CartesianChart chart = sender as CartesianChart;
            if (chart != null)
            {
                dimChartStatuses[chart.Name] = "DEFAULT";
            }

        }

        //tablonun paketlerine göre renklerini ayarlayan fonksiyon
        public void setColor()
        {
           Task.Run(() =>
            {
                rowColor.Clear();
                pieChart.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {

                        foreach (Series series in pieChart.Series)
                        {
                            if (series is PieSeries pieSeries)
                            {
                                if (series.Fill is SolidColorBrush solidColorBrush)
                                {
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

                        dataGrid.Dispatcher.Invoke(new Action(() =>
                        {
                            for (int i = 0; i < dataGrid.Items.Count; i++)
                            {
                                
                                DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                                if (row != null)
                                {

                                    string name = ((KeyValuePair<string[], int[]>)row.Item).Key[0];
                                    DataGridCell cell = GetCell(row, 0);
                                    cell.Background = rowColor[name];
                                    row.Background = rowColor[name];

                                    

                                    SolidColorBrush newSolidColorBrush = new SolidColorBrush(Color.FromArgb((byte)150, rowColor[name].Color.R,
                                        rowColor[name].Color.G, rowColor[name].Color.B));

                                    lineSeriesList[((KeyValuePair<string[], int[]>)row.Item).Key].Fill = new SolidColorBrush(Color.FromArgb((byte)150, rowColor[name].Color.R,
                                        rowColor[name].Color.G, rowColor[name].Color.B));
                                    lineSeriesList[((KeyValuePair<string[], int[]>)row.Item).Key].Stroke = Brushes.Black;


                                    dimLineSeriesList[((KeyValuePair<string[], int[]>)row.Item).Key].Fill = new SolidColorBrush(Color.FromArgb((byte)150, rowColor[name].Color.R,
                                        rowColor[name].Color.G, rowColor[name].Color.B));
                                    dimLineSeriesList[((KeyValuePair<string[], int[]>)row.Item).Key].Stroke = Brushes.Black;

                                    row.FontWeight = FontWeights.Bold;

                                    row.Background = newSolidColorBrush;
                                    row.Foreground = Brushes.Black;

                                }
                            }
                        }));
                    }
                    catch 
                    {
                        rowColorStart = true;
                    }

                }));
            });
        }

        // -------------------- Ayarlar Buton Fonksiyonu --------------------
        public void AyarlarClicked(object sender, RoutedEventArgs e)
        {
            enumMatchWindow.Show();
        }

        //Frekans grafikleri yüklendiğinde oluşan event
        private void LoadDimChart(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {                    
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    dimChartList[selectedRow.Key] = sender as CartesianChart;
                    //MessageBox.Show(selectedRow.Key[0] + " " + selectedRow.Key[1]);
                    if (!dimExceptAdded)
                    {
                        dimChartList[selectedRow.Key].AxisY[0].Sections.Clear();
                        dimChartList[selectedRow.Key].AxisY[0].Sections = new SectionsCollection
                    {
                        new AxisSection
                        {
                            Value = expectedDim[selectedRow.Key],
                            SectionWidth = 0,
                            Stroke = Brushes.Red,
                            SectionOffset = 0,
                            StrokeThickness = 2.5,
                        }
                    };
                        dimExceptAdded = true;
                    }

                    dimChartList[selectedRow.Key].Name = selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1);
                    dimChartList[selectedRow.Key].Height = 200;
                    dimChartList[selectedRow.Key].Series = new SeriesCollection { dimLineSeriesList[selectedRow.Key] };
                    dimChartList[selectedRow.Key].AxisX[0].Labels = dimChartXLabels[selectedRow.Key];
                }
            }));
        }

        private void ExpectedDimTextBoxLoad(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    expectedDimBoxs[selectedRow.Key] = sender as TextBox;
                    expectedDimBoxs[selectedRow.Key].Text = expectedDim[selectedRow.Key].ToString();
                    expectedDimBoxs[selectedRow.Key].Name = selectedRow.Key[0] + "_" + selectedRow.Key[1];
                }
            }));
        }

        private void ExpectedTextBoxLoad(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    expectedFreqBoxs[selectedRow.Key] = sender as TextBox;
                    expectedFreqBoxs[selectedRow.Key].Text = expectedFreq[selectedRow.Key].ToString();
                    expectedFreqBoxs[selectedRow.Key].Name = selectedRow.Key[0] + "_" + selectedRow.Key[1];
                }
            }));
        }

        private void DimLabelStackLoaded(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    dimLabelStacks[selectedRow.Key] = sender as StackPanel;
                }
            }));
        }
        private void FreqLabelStackLoaded(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    freqLabelStacks[selectedRow.Key] = sender as StackPanel;                    
                }
            }));
        }

        private void LoadFreqChart(object sender, RoutedEventArgs e)
        {            
            //expectedFreq = enumMatchWindow.expectedFreq;            
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    chartList[selectedRow.Key] = sender as CartesianChart;
                    if (!freqExceptAdded)
                    {
                        chartList[selectedRow.Key].AxisY[0].Sections.Clear();
                        chartList[selectedRow.Key].AxisY[0].Sections = new SectionsCollection
                    {
                        new AxisSection
                        {
                            Value = expectedFreq[selectedRow.Key],
                            SectionWidth = 0,
                            Stroke = Brushes.Red,
                            SectionOffset = 0,
                            StrokeThickness = 2.5,
                        }
                    };
                        freqExceptAdded = true;
                    }                
                    chartList[selectedRow.Key].Name = selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1);
                    chartList[selectedRow.Key].Height = 200;
                    chartList[selectedRow.Key].Series = new SeriesCollection { lineSeriesList[selectedRow.Key] };
                    chartList[selectedRow.Key].AxisX[0].Labels = chartXLabels;

                }
            }));            
        }

        //Detay butonuna tıklandığında oluşan event
        public void ButtonDetayClicked(object sender, RoutedEventArgs e)
        {
            dimExceptAdded = false;
            freqExceptAdded = false;

            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                DataGridRow selectedRow = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(dataGrid.SelectedItem);
                if (selectedRow != null)
                {
                    KeyValuePair<string[], int[]> a = (KeyValuePair<string[], int[]>)selectedRow.Item;
                    row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(a);
                    if (row.DetailsVisibility != Visibility.Visible) row.DetailsVisibility = Visibility.Visible;
                    else row.DetailsVisibility = Visibility.Collapsed;                   
                }
            }));
        }

        //Enum eşleştirilmesi tamamlandığında oluşan event
        private void enumKaydetClick(object sender, RoutedEventArgs e)
        {
            enumStruct = enumMatchWindow.enumStruct;
            if (timer != null) timer.Stop();
            createDataStruct();
            updateGrid();
        }

        //Program başladığında ve enum dosyası değiştiğinde grafiği güncelleyen event
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

            pieChartValues = new Dictionary<string, ChartValues<int>>();
            piechartPaket = new SeriesCollection();

            Func<ChartPoint, string> labelPoint = chartPoint => string.Format("{0} ({1:P})\n{2}", chartPoint.Y, chartPoint.Participation, chartPoint.SeriesView.Title);
            
            for (int i = 0; i < enumStruct[enumMatchWindow.paketName].Values.Count; i++)
            {
                string name = enumStruct[enumMatchWindow.paketName].Values.ElementAt(i);
                pieChartValues.Add(name, new ChartValues<int> { 0 });
                PieSeries pieSeries = new PieSeries
                {
                    Title = name,
                    Values = pieChartValues[name],
                    DataLabels = true,
                    LabelPoint = labelPoint,
                    FontSize = 12,
                    PushOut = 0                                        
                };
                piechartPaket.Add(pieSeries);

            }
            pieChart.Series = piechartPaket;

            paketName = enumMatchWindow.paketName;

            paketColumn.Binding = new Binding("Key[0]");
            projeColumn.Binding = new Binding("Key[1]");
            frekansColumn.Binding = new Binding("Value[0]");
            toplamColumn.Binding = new Binding("Value[1]");
            boyutColumn.Binding = new Binding("Value[2]");
            toplamBoyutColumn.Binding = new Binding("Value[3]");
            dataGrid.ItemsSource = dataSource;

            // -------------------- FREKANS İÇİN TIMER --------------------
            privTotal = new int[totalReceivedPacket.Count];
            privTotal = privTotal.Select(x => 0).ToArray();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += UpdateFrekans;
            timer.Start();

        }

        //Hücreleri bulmayı sağlayan fonksiyon
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

        private void enumMatchClosed(object sender, EventArgs e)
        {
            
            enumMatchWindow = new EnumMatchWindow();
            expectedFreq = enumMatchWindow.expectedFreq;
            expectedDim = enumMatchWindow.expectedDim;
            enumMatchWindow.Closed += enumMatchClosed;
            enumMatchWindow.OkKaydetLog.Click += enumKaydetClick;
            enumMatchWindow.ExpectedButtonClickedEvent += ExpectedFreqClicked;
            enumMatchWindow.ExpectedDimButtonClickedEvent += ExpectedDimClicked;

        }

        private void ExpectedDimBoxKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == System.Windows.Input.Key.Enter)
            {

                dataGrid.Dispatcher.Invoke(new Action(() =>
                {
                    for (int i = 0; i < dataGrid.Items.Count; i++)
                    {
                        DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                        if (row != null)
                        {
                            string[] name = ((KeyValuePair<string[], int[]>)row.Item).Key;
                            if (name[0] + "_" + name[1] == (sender as TextBox).Name)
                            {
                                expectedDim[name] = Convert.ToInt32((sender as TextBox).Text);
                                enumMatchWindow.configData.Dim[name[0] + "." + name[1]] = expectedDim[name];
                                File.WriteAllText("PacketConfig.json", JsonConvert.SerializeObject(enumMatchWindow.configData, Formatting.Indented));
                                ExpectedDimClicked(sender, e);
                                return;
                            }

                        }
                    }
                }));
            }
        }
        private void ExpectedBoxKeyDown(object sender, KeyEventArgs e)
        {
            
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                
                dataGrid.Dispatcher.Invoke(new Action(() =>
                {
                    for (int i = 0; i < dataGrid.Items.Count; i++)
                    {
                        DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                        if (row != null)
                        {
                            string[] name = ((KeyValuePair<string[], int[]>)row.Item).Key;
                            if (name[0] + "_" + name[1] == (sender as TextBox).Name)
                            {                                
                                expectedFreq[name] = Convert.ToInt32((sender as TextBox).Text);
                                enumMatchWindow.configData.Freq[name[0] + "." + name[1]] = expectedFreq[name];
                                File.WriteAllText("PacketConfig.json", JsonConvert.SerializeObject(enumMatchWindow.configData, Formatting.Indented));
                                ExpectedFreqClicked(sender, e);
                                return;
                            }
                            
                        }
                    }
                }));
            }
        }

        //Filtrelemeyi sağlayan fonksiyon
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                dataGrid.Dispatcher.Invoke(new Action(() =>
                {
                    for (int i = 0; i < dataGrid.Items.Count; i++)
                    {
                        DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                        if (row != null)
                        {
                            string[] name = ((KeyValuePair<string[], int[]>)row.Item).Key;

                            if (!(name[0].Contains(searchBox.Text) || name[1].Contains(searchBox.Text)
                                || name[0].Contains(searchBox.Text.ToLower()) || name[1].Contains(searchBox.Text.ToLower())
                                || name[0].Contains(searchBox.Text.ToUpper()) || name[1].Contains(searchBox.Text.ToUpper())))
                            {
                                row.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                row.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }));
            }
        }

        //AES-256 şifresi çözen fonksiyon
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

        public void setExpectedLabel(string[] key, Dictionary<string[], ChartValues<int>> values, Dictionary<string[], StackPanel> stacks, 
                                         Dictionary<string[],int> expectedValue, string type)
        {
            if (stacks[key].Children.Count > 0 & !rowColorStart)
            {
                double packetTotal = values[key].Count;
                int eq = values[key].Count(chartValue => chartValue == expectedValue[key]);
                int down = values[key].Count(chartValue => chartValue < expectedValue[key]);
                int up = values[key].Count(chartValue => chartValue > expectedValue[key]);
                int zero = values[key].Count(chartValue => chartValue == 0);

                ((Label)((StackPanel)stacks[key].Children[0]).Children[0]).Content = type + " Sıfır : " + zero.ToString()
                                                    + " (%" + ((double)(zero / packetTotal) * 100).ToString("F2") + ")";
                ((Label)((StackPanel)stacks[key].Children[0]).Children[0]).Background = rowColor[key[0]];
                ((Label)((StackPanel)stacks[key].Children[0]).Children[0]).BorderBrush = Brushes.WhiteSmoke;
                ((Label)((StackPanel)stacks[key].Children[0]).Children[0]).BorderThickness = new Thickness(2, 2, 2, 1);

                ((Label)((StackPanel)stacks[key].Children[0]).Children[1]).Content = type + " Beklenen " + type + "ta : " + eq.ToString()
                                                    + " (%" + ((double)(eq / packetTotal) * 100).ToString("F2") + ")";
                ((Label)((StackPanel)stacks[key].Children[0]).Children[1]).Background = rowColor[key[0]];
                ((Label)((StackPanel)stacks[key].Children[0]).Children[1]).BorderBrush = Brushes.WhiteSmoke;
                ((Label)((StackPanel)stacks[key].Children[0]).Children[1]).BorderThickness = new Thickness(2, 1, 2, 2);

                ((Label)((StackPanel)stacks[key].Children[1]).Children[0]).Content = type + " Beklenen " + type + " Üstünde : " + up.ToString()
                                                                            + " (%" + ((double)(up / packetTotal) * 100).ToString("F2") + ")";
                ((Label)((StackPanel)stacks[key].Children[1]).Children[0]).Background = rowColor[key[0]];
                ((Label)((StackPanel)stacks[key].Children[1]).Children[0]).BorderBrush = Brushes.WhiteSmoke;
                ((Label)((StackPanel)stacks[key].Children[1]).Children[0]).BorderThickness = new Thickness(2, 2, 2, 1);

                ((Label)((StackPanel)stacks[key].Children[1]).Children[1]).Content = type + " Beklenen " + type + " Altında : " + down.ToString()
                                                    + " (%" + ((double)(down / packetTotal) * 100).ToString("F2") + ")";
                ((Label)((StackPanel)stacks[key].Children[1]).Children[1]).Background = rowColor[key[0]];
                ((Label)((StackPanel)stacks[key].Children[1]).Children[1]).BorderBrush = Brushes.WhiteSmoke;
                ((Label)((StackPanel)stacks[key].Children[1]).Children[1]).BorderThickness = new Thickness(2, 1, 2, 2);
            }
        }

        //Paketlerin alındığı fonksiyon bir thread'te çalışır
        public void receiveData()
        {
            //bytes = new byte[6]; 

            while (!stop)
            {                
                bytes = ReceivingSocketExtensions.ReceiveFrameBytes(subSocket);
                bytes = DecryptAes(bytes);

                string[] paket_proje = new string[] { enumStruct[paketName].Values.ElementAt((int)bytes[paketByte]),
                                            enumStruct[enumStruct[paketName].Values.ElementAt((int)bytes[paketByte])].Values.ElementAt((int)bytes[projeByte]) };
    
                totalReceivedPacket[paket_proje][1] += 1;
                totalReceivedPacket[paket_proje][2] = bytes.Length;
                totalReceivedPacket[paket_proje][3] += bytes.Length;
                
                int total = 0;
                dataGrid.Dispatcher.Invoke(new System.Action(() =>
                {

                    setExpectedLabel(paket_proje, lineValuesList, freqLabelStacks, expectedFreq, "Frekans");
                    setExpectedLabel(paket_proje, dimLineValuesList, dimLabelStacks, expectedDim, "Boyut");

                    dimLineValuesList[paket_proje].Add(bytes.Length);
                    dimLineSeriesList[paket_proje].Values = dimLineValuesList[paket_proje];
                    dimChartXLabels[paket_proje].Add(DateTime.Now.ToString("HH:mm:ss:fff"));

                    if (dimLineValuesList[paket_proje].Count == saveLength)
                    {
                        string fileName = paket_proje[0] + "_" + paket_proje[1];
                        dataKeeper.writeData("BOYUT", fileName, dimLineValuesList[paket_proje].ToList<int>(), dimChartXLabels[paket_proje].ToList<string>());
                        dimLineValuesList[paket_proje].Clear();
                        dimChartXLabels[paket_proje].Clear();
                    }

                    if (rowColorStart & dataGrid.Items.Count == totalReceivedPacket.Count)
                    {
                        setColor();
                        exportButton.IsEnabled = true;
                        rowColorStart = false;
                    }

                    int idx = 0;
                    foreach(var data in totalReceivedPacket)
                    {
                        if (data.Key[0] == paket_proje[0])
                        {
                            total += data.Value[1];
                            barColumnSeries[paket_proje[0]].Values[idx] = data.Value[1];
                            idx++;
                        }                          
                    }                    

                    pieChartValues[paket_proje[0]][0] = total;

                    var item = dataSource.FirstOrDefault(i => i.Key.SequenceEqual(paket_proje));
                    if (item.Key == null)
                    {
                        dataSource.Add(new KeyValuePair<string[], int[]>(paket_proje, totalReceivedPacket[paket_proje]));
                    }
                    else
                    {
                        int index = dataSource.IndexOf(item);
                        dataSource[index].Value[1] += 1;
                        dataSource[index].Value[2] = bytes.Length;
                        dataSource[index].Value[3] += bytes.Length;
                        dataSource[index] = new KeyValuePair<string[], int[]>(item.Key, new int[] { item.Value[0], item.Value[1], item.Value[2], item.Value[3] });
                    }
                }));
            }
        }

        //Program kapandığında oluşan event
        private void MainAppClosed(object sender, EventArgs e)
        {
            subscriber.Abort();
            subSocket.Dispose();
            Environment.Exit(0);
        }

        //Grafikteki ZOOM- butonuna tıklandığında oluşan event
        private void dimZoomButton_Click(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(20);
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    dimChartStatuses[selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1)] = "ZOOM-";
                }
            }));
        }
        //Grafikteki REAL butonuna tıklandığında oluşan event
        private void dimRealButton_Click(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(20);
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    dimChartStatuses[selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1)] = "REAL";
                }
            }));
        }

        private void zoomButton_Click(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(20);
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    chartStatuses[selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1)] = "ZOOM-";
                }
            }));
        }
        //Grafikteki REAL butonuna tıklandığında oluşan event
        private void realButton_Click(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(20);
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    chartStatuses[selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1)] = "REAL";
                }
            }));
        }

        //Paket butonlarına tıklandığında oluşan event
        private void paketButtonClicked(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            barChartFilter(clickedButton.Name);
        }

        //Soket Paneldeki bağlan butonuna tıklandığında oluşan event
        private void ConnectButtonClicked(object sender, RoutedEventArgs e)
        {

            disconnect = false;
            if (timer != null) timer.Stop();
            subscriber.Abort();
            stop = true;
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
            }
            catch
            {
                MessageBox.Show("IP Adresini ve Portu Kontrol Ediniz");
                return;
            }

            if (startConnect)
            {
                createDataStruct();
                updateGrid();
                this.WindowState = WindowState.Maximized;
                startConnect = false;
            }
            else
            {
                if (timer != null) timer.Start();
            }

            subscriber = new Thread(new ThreadStart(receiveData));
            subscriber.IsBackground = true;
            subscriber.Start();
            connectButton.IsEnabled = false;
            disconnectButton.IsEnabled = true;
        }

        //Soket Paneldeki bağlantıyı kes butonuna tıklandığında oluşan event
        private void DisconnectButtonClicked(object sender, RoutedEventArgs e)
        {
            subscriber.Abort();
            stop = true;
            disconnect = true;
            enumMatchWindow.disconnect = disconnect;
            if (!subSocket.IsDisposed) subSocket.Close();
            disconnectButton.IsEnabled = false;
            connectButton.IsEnabled = true;

        }       
    }
}