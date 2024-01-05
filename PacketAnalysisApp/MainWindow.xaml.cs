using LiveCharts;
using LiveCharts.Wpf;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
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
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace PacketAnalysisApp
{
    public partial class MainWindow : Window
    {
        byte[] Key = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20 };
        byte[] IV = new byte[] { 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30 };
        byte[] bytes;

        bool sliderChanged = false;
        bool dataLoading = false;
        bool appClosing = false;
        bool writeFinished = false;
        bool settingsWindowVis = false;
        bool startConnect = true;
        bool disconnect = false;
        bool stop = false;
        bool rowColorStart = true;
        bool freqExceptAdded = false;
        bool dimExceptAdded = false;

        string folderPath;
        string nowDate = null;
        string paket = "";
        string paketName = string.Empty;

        int saveLength;
        int tempLength;
        int paketByte = 0;
        int projeByte = 1;
        int[] privTotal;

        Popup popUp = new Popup();
        DataKeeper dataKeeper = new DataKeeper();
        SettingsWindow settingsWindow = new SettingsWindow();
        SubscriberSocket subSocket = new SubscriberSocket();

        Thread subscriber;
        SeriesCollection piechartPaket;
        DataGridRow row;
        DispatcherTimer timer;

        Button zoomButton = new Button();
        Button realButton = new Button();
        Button chartExportButton = new Button();
        Button dimZoomButton = new Button();
        Button dimRealButton = new Button();
        Button dimChartExportButton = new Button();

        List<KeyValuePair<string[], int[]>> playDataStruct;
        List<string> freqTimes = new List<string>();
        List<Button> buttonsToRemove = new List<Button>();
        List<CartesianChart> barChartsToRemove = new List<CartesianChart>();

        ObservableCollection<KeyValuePair<string[], int[]>> dataSource = new ObservableCollection<KeyValuePair<string[], int[]>>();

        Dictionary<string, ChartValues<int>> pieChartValues = new Dictionary<string, ChartValues<int>>();
        Dictionary<string[], CartesianChart> chartList = new Dictionary<string[], CartesianChart>();
        Dictionary<string[], LineSeries> lineSeriesList = new Dictionary<string[], LineSeries>();
        Dictionary<string[], ChartValues<int>> lineValuesList = new Dictionary<string[], ChartValues<int>>();
        Dictionary<string[], List<string>> chartXLabels = new Dictionary<string[], List<string>>(new StringArrayComparer());
        Dictionary<string[], ChartValues<int>> tempLineValuesList = new Dictionary<string[], ChartValues<int>>();
        Dictionary<string[], List<string>> tempChartXLabels = new Dictionary<string[], List<string>>(new StringArrayComparer());
        Dictionary<string[], StackPanel> chartExportPanel = new Dictionary<string[], StackPanel>();
        Dictionary<string, string> chartStatuses = new Dictionary<string, string>();
        Dictionary<string[], StackPanel> dimChartExportPanel = new Dictionary<string[], StackPanel>();
        Dictionary<string[], CartesianChart> dimChartList = new Dictionary<string[], CartesianChart>();
        Dictionary<string[], LineSeries> dimLineSeriesList = new Dictionary<string[], LineSeries>(new StringArrayComparer());
        Dictionary<string[], ChartValues<int>> dimLineValuesList = new Dictionary<string[], ChartValues<int>>(new StringArrayComparer());
        Dictionary<string[], List<string>> dimChartXLabels = new Dictionary<string[], List<string>>(new StringArrayComparer());
        Dictionary<string[], ChartValues<int>> tempDimLineValuesList = new Dictionary<string[], ChartValues<int>>(new StringArrayComparer());
        Dictionary<string[], List<string>> tempDimChartXLabels = new Dictionary<string[], List<string>>(new StringArrayComparer());
        Dictionary<string, string> dimChartStatuses = new Dictionary<string, string>();
        Dictionary<string[], int> expectedFreq = new Dictionary<string[], int>(new StringArrayComparer());
        Dictionary<string[], int> expectedDim = new Dictionary<string[], int>(new StringArrayComparer());
        Dictionary<string[], StackPanel> dimLabelStacks = new Dictionary<string[], StackPanel>(new StringArrayComparer());
        Dictionary<string[], StackPanel> freqLabelStacks = new Dictionary<string[], StackPanel>(new StringArrayComparer());
        Dictionary<string[], TextBox> expectedFreqBoxs = new Dictionary<string[], TextBox>(new StringArrayComparer());
        Dictionary<string[], TextBox> expectedDimBoxs = new Dictionary<string[], TextBox>(new StringArrayComparer());
        Dictionary<string, Button> paketButtons = new Dictionary<string, Button>();
        Dictionary<string, CartesianChart> barCharts = new Dictionary<string, CartesianChart>();
        Dictionary<string, ColumnSeries> barColumnSeries = new Dictionary<string, ColumnSeries>();
        Dictionary<string, SolidColorBrush> rowColor = new Dictionary<string, SolidColorBrush>();
        Dictionary<string, Dictionary<int, string>> enumStruct = new Dictionary<string, Dictionary<int, string>>();
        Dictionary<string[], int[]> totalReceivedPacket = new Dictionary<string[], int[]>(new StringArrayComparer());

        public MainWindow()
        {
            InitializeComponent();

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(Environment.CurrentDirectory + "\\loading.gif");
            image.EndInit();
            ImageBehavior.SetAnimatedSource(loading, image);

            AppClosed += MainAppClosed;
            dataKeeper.ExportFinished += exportAllChart;            
            exportButton.IsEnabled = false;
            startConnect = true;
            disconnectButton.IsEnabled = false;
            dataRemove.IsEnabled = false;
            subscriber = new Thread(new ThreadStart(receiveData));
            // -------------------- EVENTLER --------------------
            settingsWindow.Closed += enumMatchClosed;
            settingsWindow.SaveClickedEvent += enumKaydetClick;
            settingsWindow.DisconnectEvent += DisconnectButtonClicked;
            settingsWindow.nowDate = dataKeeper.nowDate;

            // -------------------- ENUM YAPISININ OLULŞTURULMASI --------------------

            expectedFreq = settingsWindow.expectedFreq;
            expectedDim = settingsWindow.expectedDim;
            enumStruct = settingsWindow.enumStruct;
            settingsWindow.UpdateClickedEvent += ExpectedFreqClicked;
            settingsWindow.ChartUpdating += ChartUpdate;
            saveLength = settingsWindow.lenChart;
            tempLength = settingsWindow.lenBuffer;

            popUp.PopupEvent += resultSet;

            writeFinished = dataKeeper.writeFinished;

            pieChart.DataTooltip = null;
        }

        private void setting(object sender, RoutedEventArgs e)
        {
            expectedFreq = settingsWindow.expectedFreq;
            expectedDim = settingsWindow.expectedDim;
            enumStruct = settingsWindow.enumStruct;
            settingsWindow.nowDate = dataKeeper.nowDate;
            settingsWindow.UpdateClickedEvent += ExpectedFreqClicked;
            settingsWindow.Show();
        }

        public void setSection(bool added, Dictionary<string[], CartesianChart> charts, Dictionary<string[], int> expecteds, Dictionary<string[], TextBox> boxs)
        {
            for (int i = 0; i < expecteds.Count; i++)
            {
                if (added)
                {
                    charts[expecteds.ElementAt(i).Key].Dispatcher.Invoke( () =>
                    {
                        if (charts[expecteds.ElementAt(i).Key].AxisY.Count > 0)
                        {
                            if (charts[expecteds.ElementAt(i).Key].AxisY[0].Sections.Count > 0)
                            {
                                charts[expecteds.ElementAt(i).Key].AxisY[0].Sections.CollectionChanged += SectionSelectionChanged;
                                charts[expecteds.ElementAt(i).Key].AxisY[0].Sections.Add(new AxisSection
                                {
                                    Value = expecteds[expecteds.ElementAt(i).Key],
                                    SectionWidth = 0,
                                    Stroke = Brushes.Red,
                                    SectionOffset = 0,
                                    StrokeThickness = 2.5,
                                    DisableAnimations = true,
                                });

                                boxs[expecteds.ElementAt(i).Key].Text = expecteds[expecteds.ElementAt(i).Key].ToString();
                            }
                        }
                    });
                }

            }
        }

        private async void ExpectedFreqClicked(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                dataGrid.Dispatcher.Invoke(new Action(() =>
                {
                    setSection(freqExceptAdded, chartList, expectedFreq, expectedFreqBoxs);

                    setSection(dimExceptAdded, dimChartList, expectedDim, expectedDimBoxs);

                }));
            });
        }

        private void SectionSelectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SectionsCollection collection = (SectionsCollection)sender;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                collection[collection.Count - 2].StrokeThickness = 0;
            }
            
        }

        public delegate void ExportChartHandler();
        public event ExportChartHandler ExportChartEvent;
        
        public void exportAllChart()
        {
            logLabel.Visibility = Visibility.Collapsed;
            if (appClosing)
            {
                ExportChartEvent += ExportFinished;
                string folderPathFreq = Path.Combine(folderPath, "FREKANS");
                string folderPathDim = Path.Combine(folderPath, "BOYUT");
                if (!Directory.Exists(folderPathFreq)) Directory.CreateDirectory(folderPathFreq);
                if (!Directory.Exists(folderPathDim)) Directory.CreateDirectory(folderPathDim);


                dataGrid.Dispatcher.Invoke(new Action(() =>
                {
                    for (int i = 0; i < totalReceivedPacket.Count; i++)
                    {
                        string fileName = totalReceivedPacket.ElementAt(i).Key[0] + "_" + totalReceivedPacket.ElementAt(i).Key[1];
                        string savePath = Path.Combine(folderPath, fileName);
                        dataKeeper.ChartExport("BOYUT", fileName, Path.Combine(folderPathDim, fileName + ".xlsx"));
                        dataKeeper.ChartExport("FREKANS", fileName, Path.Combine(folderPathFreq, fileName + ".xlsx"));
                    }
                }));
                ExportChartEvent?.Invoke();

              
            }
        }

        //Grafikleri Dışarı aktarma fonksiyonu
        private async void dimExportChartButtonClick(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);
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

                    writeTempData();
                    dataKeeper.readData("BOYUT", fileName, savePath, dimChartExportPanel[selectedRow.Key]);
                }
            }));
        }
        private void exportChartButtonClick(object sender, RoutedEventArgs e)
        {
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

                    //dataKeeper.writeData("FREKANS", fileName, lineValuesList[selectedRow.Key].ToList<int>(), chartXLabels[selectedRow.Key].ToList<string>());
                    //lineValuesList[selectedRow.Key].Clear();
                    //chartXLabels[selectedRow.Key].Clear();
                    writeTempData();
                    dataKeeper.readData("FREKANS", fileName, savePath, chartExportPanel[selectedRow.Key]);
                }
            }));
        }


        //Dışarı aktarma butonu fonksiyonu (Excel İşlemleri)

        public void exportAll()
        {
            for (int i = 0; i < totalReceivedPacket.Count; i++)
            {
                string fileName = totalReceivedPacket.ElementAt(i).Key[0] + "_" + totalReceivedPacket.ElementAt(i).Key[1];
                dataKeeper.writeData("FREKANS", fileName, lineValuesList.ElementAt(i).Value.ToList<int>(), chartXLabels.ElementAt(i).Value.ToList<string>());
                lineValuesList.ElementAt(i).Value.Clear();
                chartXLabels.ElementAt(i).Value.Clear();

                dataKeeper.writeData("BOYUT", fileName, dimLineValuesList.ElementAt(i).Value.ToList<int>(), dimChartXLabels.ElementAt(i).Value.ToList<string>());
                dimLineValuesList.ElementAt(i).Value.Clear();
                dimChartXLabels.ElementAt(i).Value.Clear();
            }
        }

        private void exportClick(object sender, RoutedEventArgs e)
        {
            dataKeeper.colors = rowColor;
            string savePath = null;

            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                bool result = false;

                //if (!appClosing)
                //{
                //    Microsoft.Win32.SaveFileDialog openFileDlg = new Microsoft.Win32.SaveFileDialog();
                //    openFileDlg.FileName = settingsWindow.packetName + ".xlsx";

                //    result = (bool)openFileDlg.ShowDialog();
                //    savePath = openFileDlg.FileName;
                //}
                //else
                //{
                //    result = true;
                //    savePath = Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"), "PacketAnalysis");
                //    savePath = savePath + "\\DATA\\Export\\";
                //    if(!Directory.Exists(savePath))
                //    {
                //        Directory.CreateDirectory(savePath);
                //    }
                //    savePath = savePath + settingsWindow.packetName + "_" + dataKeeper.nowDate;
                //}

                Microsoft.Win32.SaveFileDialog openFileDlg = new Microsoft.Win32.SaveFileDialog();
                openFileDlg.FileName = settingsWindow.packetName;
                result = (bool)openFileDlg.ShowDialog();
                savePath = openFileDlg.FileName;

                if (result == true)
                {
                    if (appClosing)
                    {
                        
                        string name = settingsWindow.packetName + ".xlsx";                        
                        folderPath = Path.Combine(openFileDlg.FileName, dataKeeper.nowDate);
                        if(!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }
                        savePath = Path.Combine(folderPath, name);                        
                    }
                    else
                    {
                        savePath = openFileDlg.FileName + ".xlsx";
                        if (savePath.Substring(savePath.LastIndexOf('.') + 1, 4) != "xlsx") savePath += ".xlsx";
                    }
                    ///Directory.CreateDirectory(Path.Combine(openFileDlg.FileName.Substring(), settingsWindow.packetName));
                    
                    writeFinished = false;
                    exportButton.Visibility = Visibility.Collapsed;
                    loading.Visibility = Visibility.Visible;

                    logLabel.Content = "Veriler Dışarıya Aktarılıyor...";
                    
                    Task.Run(() =>
                    {
                        dataGrid.Dispatcher.Invoke(() =>
                        {
                            
                            Task task = new Task(writeTempData);
                            task.RunSynchronously();
                            //exportAll();
                            dataKeeper.mainExport(totalReceivedPacket, savePath, loading, exportButton, exportLabel);
                        });

                    });                    
                }
                else return;
            }));

            //writeTempData();
            //export.MainExport(dataGrid, progressBar, totalReceivedPacket, rowColor, chartXLabels, lineValuesList, pieChartValues, exportButton, exportLabel, dimChartXLabels, dimLineValuesList);
        }

        private void exportTask(object savePath)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                exportAll();
                dataKeeper.mainExport(totalReceivedPacket, (string)savePath, loading, exportButton, exportLabel);
            }));
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
            //zoomButton = sender as Button;
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
            lineSeriesList = new Dictionary<string[], LineSeries>(new StringArrayComparer());
            lineValuesList = new Dictionary<string[], ChartValues<int>>(new StringArrayComparer());
            chartXLabels = new Dictionary<string[], List<string>>(new StringArrayComparer());
            tempChartXLabels = new Dictionary<string[], List<string>>(new StringArrayComparer());
            chartStatuses = new Dictionary<string, string>();
            paketButtons = new Dictionary<string, Button>();
            totalReceivedPacket = new Dictionary<string[], int[]>(new StringArrayComparer());

            dimChartList = new Dictionary<string[], CartesianChart>(new StringArrayComparer());
            dimLineSeriesList = new Dictionary<string[], LineSeries>(new StringArrayComparer());
            dimLineValuesList = new Dictionary<string[], ChartValues<int>>(new StringArrayComparer());
            dimChartXLabels = new Dictionary<string[], List<string>>(new StringArrayComparer());
            tempDimLineValuesList = new Dictionary<string[], ChartValues<int>>(new StringArrayComparer());
            tempDimChartXLabels = new Dictionary<string[], List<string>>(new StringArrayComparer());
            dimChartStatuses = new Dictionary<string, string>();

            for (int i = 0; i < enumStruct[settingsWindow.packetName].Count; i++)
            {
                string name = enumStruct[settingsWindow.packetName].Values.ElementAt(i);
                barColumnSeries.Add(name, new ColumnSeries { });
                barColumnSeries[name].Title = name;
                barColumnSeries[name].Values = new ChartValues<int>();

                CartesianChart barChart = new CartesianChart();
                barChart.Name = enumStruct[settingsWindow.packetName].Values.ElementAt(i);
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

                for (int j = 0; j < enumStruct[enumStruct[settingsWindow.packetName].Values.ElementAt(i)].Values.Count; j++)
                {
                    barCharts[name].AxisX[0].Labels.Add(enumStruct[enumStruct[settingsWindow.packetName].Values.ElementAt(i)].Values.ElementAt(j));
                    barColumnSeries[name].Values.Add(0);

                    int[] packetAnalysisArr = { 0, 0, 0, 0 };
                    string[] paket_proje = { enumStruct[settingsWindow.packetName].Values.ElementAt(i),
                                                            enumStruct[enumStruct[settingsWindow.packetName].Values.ElementAt(i)].Values.ElementAt(j)};
                    totalReceivedPacket.Add(paket_proje, packetAnalysisArr);
                    chartList.Add(paket_proje, new CartesianChart());
                    lineSeriesList.Add(paket_proje, new LineSeries());
                    lineValuesList.Add(paket_proje, new ChartValues<int>());
                    tempLineValuesList.Add(paket_proje, new ChartValues<int>());
                    chartStatuses.Add(paket_proje[0] + "_" + paket_proje[1], "DEFAULT");
                    chartExportPanel.Add(paket_proje, new StackPanel());
                    chartXLabels.Add(paket_proje, new List<string>());
                    tempChartXLabels.Add(paket_proje, new List<string>());

                    dimChartList.Add(paket_proje, new CartesianChart());
                    dimLineSeriesList.Add(paket_proje, new LineSeries());
                    dimLineValuesList.Add(paket_proje, new ChartValues<int>());
                    tempDimLineValuesList.Add(paket_proje, new ChartValues<int>());
                    dimChartXLabels.Add(paket_proje, new List<string>());
                    tempDimChartXLabels.Add(paket_proje, new List<string>());
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
            dataKeeper.packetName = settingsWindow.packetName;
            dataKeeper.fileNames = totalReceivedPacket.Keys.ToList();

            if (!dataLoading) 
            {
                dataKeeper.CreateDir();
                nowDate = dataKeeper.nowDate;
                settingsWindow.nowDate = dataKeeper.nowDate;
                settingsWindow.CopyConfigFile();
            }

            
            settingsWindow.InitExpectedValue();
        }

        //Bir saniyede bir tabloyu ve frekans değerlerini güncelleyen fonksiyon
        private void UpdateFrekans(object sender, EventArgs e)
        {

            Task.Run(() =>
            {
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

                        string fileName = paket_proje[0] + "_" + paket_proje[1];


                        if (lineValuesList[paket_proje].Count == saveLength)
                        {
                            lineValuesList[paket_proje].RemoveAt(0);
                            chartXLabels[paket_proje].RemoveAt(0);

                            chartXLabels[paket_proje].Add(DateTime.Now.ToString("HH:mm:ss"));
                            lineValuesList[totalReceivedPacket.Keys.ElementAt(i)].Add(totalReceivedPacket[paket_proje][0]);
                            lineSeriesList[totalReceivedPacket.Keys.ElementAt(i)].Values = lineValuesList[totalReceivedPacket.Keys.ElementAt(i)];

                            tempChartXLabels[paket_proje].Add(chartXLabels[paket_proje].Last());
                            tempLineValuesList[paket_proje].Add(lineValuesList[paket_proje].Last());
                        }
                        else
                        {
                            chartXLabels[paket_proje].Add(DateTime.Now.ToString("HH:mm:ss"));
                            lineValuesList[totalReceivedPacket.Keys.ElementAt(i)].Add(totalReceivedPacket[paket_proje][0]);
                            lineSeriesList[totalReceivedPacket.Keys.ElementAt(i)].Values = lineValuesList[totalReceivedPacket.Keys.ElementAt(i)];

                            tempChartXLabels[paket_proje].Add(chartXLabels[paket_proje].Last());
                            tempLineValuesList[paket_proje].Add(lineValuesList[paket_proje].Last());
                            //dataKeeper.writeOneData("FREKANS", fileName, lineValuesList[paket_proje].Last(), chartXLabels[paket_proje].Last());
                        }

                        if (tempChartXLabels[paket_proje].Count >= tempLength & dataKeeper.writeFinished)
                        {
                            dataKeeper.writeData("FREKANS", fileName, tempLineValuesList[paket_proje].ToList(), tempChartXLabels[paket_proje].ToList());
                            tempLineValuesList[paket_proje].Clear();
                            tempChartXLabels[paket_proje].Clear();
                            //setExpectedAnalysis(paket_proje, "FREKANS");
                        }

                        setChartStatues(chartList[totalReceivedPacket.Keys.ElementAt(i)], lineValuesList[totalReceivedPacket.Keys.ElementAt(i)],
                                        chartXLabels[paket_proje], chartStatuses[paket_proje[0] + "_" + paket_proje[1]]);

                        setChartStatues(dimChartList[paket_proje], dimLineValuesList[paket_proje],
                                        dimChartXLabels[paket_proje], dimChartStatuses[paket_proje[0] + "_" + paket_proje[1]]);
                    }));
                }
            });
        }

        public void setChartStatues(CartesianChart chart, ChartValues<int> value, List<string> label, string chartName)
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
                            ((chart.AxisY[0].Sections[chart.AxisY[0].Sections.Count - 1].Value > value.Max()) ? chart.AxisY[0].MaxValue = chart.AxisY[0].Sections[chart.AxisY[0].Sections.Count - 1].Value + 1 :
                            chart.AxisY[0].MaxValue = value.Max() + 1) :
                            chart.AxisY[0].MaxValue = value.Max() + 1;

                        chart.AxisX[0].MinValue = 0;
                        chart.AxisX[0].MaxValue = label.Count - 1;
                        break;
                    case "REAL":
                        chart.AxisY[0].MinValue = -1;
                        chart.AxisY[0].MaxValue = (chart.AxisY[0].Sections.Count > 0) ?
                            ((chart.AxisY[0].Sections[chart.AxisY[0].Sections.Count - 1].Value > value.Max()) ? chart.AxisY[0].MaxValue = chart.AxisY[0].Sections[chart.AxisY[0].Sections.Count - 1].Value + 1 :
                            chart.AxisY[0].MaxValue = value.Max() + 1) :
                            chart.AxisY[0].MaxValue = value.Max() + 1;

                        chart.AxisX[0].MinValue = 0;
                        chart.AxisX[0].MaxValue = saveLength - 1;
                        break;
                    case "DEFAULT":
                        dataGrid.Dispatcher.Invoke(new Action(() =>
                        {
                            if (chart.AxisY.Count > 0)
                            {
                                chart.AxisY[0].MinValue = -1;
                                chart.AxisY[0].MaxValue = (chart.AxisY[0].Sections.Count > 0) ?
                                    ((chart.AxisY[0].Sections[chart.AxisY[0].Sections.Count - 1].Value > value.Max()) ? chart.AxisY[0].MaxValue = chart.AxisY[0].Sections[chart.AxisY[0].Sections.Count - 1].Value + 1 :
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
                if (dataLoading) chart.Pan = PanningOptions.X;
            }
        }
        private void ChartPanEvent(object sender, MouseButtonEventArgs e)
        {
            CartesianChart chart = sender as CartesianChart;
            if (chart != null)
            {
                chartStatuses[chart.Name] = "DEFAULT";
                if (dataLoading) chart.Pan = PanningOptions.X;
            }
        }

        //Mouse ile grafik büyütülmek istendiğinde oluşan event
        private void ChartZoomEvent(object sender, MouseWheelEventArgs e)
        {
            CartesianChart chart = sender as CartesianChart;
            if (chart != null)
            {
                chartStatuses[chart.Name] = "DEFAULT";
                if(dataLoading) chart.Zoom = ZoomingOptions.X;
            }

        }
        private void DimChartZoomEvent(object sender, MouseWheelEventArgs e)
        {
            CartesianChart chart = sender as CartesianChart;
            if (chart != null)
            {
                dimChartStatuses[chart.Name] = "DEFAULT";
                if (dataLoading) chart.Zoom = ZoomingOptions.X;

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
                        settingsWindow.colors = rowColor;
                        settingsWindow.setColor();
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
            setting(sender, e);

            settingsWindowVis = true;
            //settingsWindow.Show();
            
            settingsWindow.InitIcon();
            //settingsWindow.showUpdateListView();
            settingsWindow.setColor();            
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
                    if (!dimExceptAdded)
                    {
                        //dimChartList[selectedRow.Key].AxisY[0].Sections.Clear();
                        dimChartList[selectedRow.Key].AxisY[0].Sections = new SectionsCollection
                    {
                        new AxisSection
                        {
                            Value = expectedDim[selectedRow.Key],
                            SectionWidth = 0,
                            Stroke = Brushes.Red,
                            SectionOffset = 0,
                            StrokeThickness = 2.5,
                            DisableAnimations = true,
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
                    setExpectedLabel(selectedRow.Key, dimLineValuesList, dimLabelStacks, expectedDim, "Boyut");
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
                    setExpectedLabel(selectedRow.Key, lineValuesList, freqLabelStacks, expectedFreq, "Frekans");
                    
                }
            }));
        }

        private void LoadFreqChart(object sender, RoutedEventArgs e)
        {
            //expectedFreq = enumMatchWindow.expectedFreq;            
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                //var selecteItem = dataGrid.SelectedItem;
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
                            DisableAnimations = true,
                        }
                    };
                        freqExceptAdded = true;
                    }
                    chartList[selectedRow.Key].Name = selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1);
                    chartList[selectedRow.Key].Height = 200;
                    chartList[selectedRow.Key].Series = new SeriesCollection { lineSeriesList[selectedRow.Key] };
                    chartList[selectedRow.Key].AxisX[0].Labels = chartXLabels[selectedRow.Key];

                }
            }));
        }

        //Detay butonuna tıklandığında oluşan event

        private static T FindAncestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            } while (current != null);
            return null;
        }

        public void ButtonDetayClicked(object sender, RoutedEventArgs e)
        {
            dimExceptAdded = false;
            freqExceptAdded = false;

            Button detayClicked = sender as Button;

            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> a = (KeyValuePair<string[], int[]>)selecteItem;
                    row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(a);
                    if (row.DetailsVisibility != Visibility.Visible) row.DetailsVisibility = Visibility.Visible;
                    else row.DetailsVisibility = Visibility.Collapsed;
                }
            }));
        }

        //Enum eşleştirilmesi tamamlandığında oluşan event
        private void enumKaydetClick(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Name != "dataLoad") 
            {
                enumStruct = settingsWindow.enumStruct;
                expectedFreq = settingsWindow.expectedFreq;
                expectedDim = settingsWindow.expectedDim;
                createDataStruct();
                updateGrid();
            } 
            else 
            {
                enumStruct = settingsWindow.enumStruct;
                expectedFreq = settingsWindow.expectedFreq;
                expectedDim = settingsWindow.expectedDim;

                createDataStruct();
                updateGrid();
                FillEnumStruct();
            }
            if (timer != null) timer.Stop();

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

            Func<ChartPoint, string> labelPoint = chartPoint => string.Format("{0} \n ({1:P})", chartPoint.Y, chartPoint.Participation);

            for (int i = 0; i < enumStruct[settingsWindow.packetName].Values.Count; i++)
            {
                string name = enumStruct[settingsWindow.packetName].Values.ElementAt(i);
                pieChartValues.Add(name, new ChartValues<int> { 0 });
                PieSeries pieSeries = new PieSeries
                {
                    Title = name,
                    Values = pieChartValues[name],
                    DataLabels = true,
                    LabelPoint = labelPoint,
                    FontSize = 10,
                    PushOut = 0
                };
                piechartPaket.Add(pieSeries);

            }
            pieChart.Series = piechartPaket;

            paketName = settingsWindow.packetName;

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
            if(!dataLoading) timer.Start();
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
            settingsWindow = new SettingsWindow();
            settingsWindow.nowDate = dataKeeper.nowDate;
            expectedFreq = settingsWindow.expectedFreq;
            expectedDim = settingsWindow.expectedDim;
            settingsWindow.Closed += enumMatchClosed;
            settingsWindow.SaveClickedEvent += enumKaydetClick;
            settingsWindow.UpdateClickedEvent += ExpectedFreqClicked;
            settingsWindow.ChartUpdating += ChartUpdate;
            settingsWindow.DisconnectEvent += DisconnectButtonClicked;
            settingsWindow.colors = rowColor;
            settingsWindowVis = false;
        }

        private void SetTempLineValue(int count, int chartlen, string[] paket_proje, string type)
        {
            if (type == "Frekans")
            {
                if (count > chartlen)
                {
                    tempLineValuesList[paket_proje].AddRange(lineValuesList[paket_proje].Take(count - chartlen));
                    tempChartXLabels[paket_proje].ToList().AddRange(chartXLabels[paket_proje].Take(count - chartlen));

                    List<int> tempVal = lineValuesList[paket_proje].ToList();
                    tempVal.RemoveRange(0, count - chartlen);
                    lineValuesList[paket_proje].Clear();
                    lineValuesList[paket_proje].AddRange(tempVal);

                    List<string> tempLabel = chartXLabels[paket_proje].ToList();
                    tempLabel.RemoveRange(0, count - chartlen);
                    chartXLabels[paket_proje].Clear();
                    chartXLabels[paket_proje].AddRange(tempLabel);
                }
            }
            else
            {
                if (count > chartlen)
                {
                    tempDimLineValuesList[paket_proje].AddRange(dimLineValuesList[paket_proje].Take(count - chartlen));
                    tempDimChartXLabels[paket_proje].ToList().AddRange(dimChartXLabels[paket_proje].Take(count - chartlen));

                    List<int> tempValDim = dimLineValuesList[paket_proje].ToList();
                    tempValDim.RemoveRange(0, count - chartlen);
                    dimLineValuesList[paket_proje].Clear();
                    dimLineValuesList[paket_proje].AddRange(tempValDim);

                    List<string> tempLabelDim = dimChartXLabels[paket_proje].ToList();
                    tempLabelDim.RemoveRange(0, count - chartlen);
                    dimChartXLabels[paket_proje].Clear();
                    dimChartXLabels[paket_proje].AddRange(tempLabelDim);
                }
            }

        }

        private void ChartUpdate(object sender, RoutedEventArgs e)
        {            
            int chartlen = Convert.ToInt32(settingsWindow.chartLengthBox.Text);
            if (Convert.ToInt32(settingsWindow.chartLengthBox.Text) < saveLength)
            {
                writeFinished = false;
                for (int i = 0; i < lineSeriesList.Count; i++)
                {
                    string[] paket_proje = lineValuesList.ElementAt(i).Key;
                    int count = lineValuesList[paket_proje].Count;

                    SetTempLineValue(count, chartlen, paket_proje, "Frekans");


                    int dimCount = dimLineValuesList[paket_proje].Count;

                    SetTempLineValue(dimCount, chartlen, paket_proje, "Boyut");

                }
                writeTempData();
            }
            saveLength = settingsWindow.lenChart;
            tempLength = settingsWindow.lenBuffer;
        }

        private void ExpectedBoxKeyDownEvent(object sender, string type, Dictionary<string[], int> expectedList, KeyEventArgs e)
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
                            try
                            {
                                Convert.ToInt32((sender as TextBox).Text);
                            }
                            catch
                            {
                                MessageBox.Show("Beklenen" + type + " Değeri Pozitif Bir Tamsayı Olmalı");
                                return;
                            }

                            if (Convert.ToInt32((sender as TextBox).Text) < 0)
                            {
                                MessageBox.Show("Beklenen " + type + " Değeri Sıfırdan Küçük Olamaz");
                                return;
                            }

                            expectedList[name] = Convert.ToInt32((sender as TextBox).Text);
                            if (type == "Frekans") settingsWindow.configData.Freq[name[0] + "." + name[1]] = expectedList[name];
                            else settingsWindow.configData.Dim[name[0] + "." + name[1]] = expectedList[name];
                            settingsWindow.updateExpetedBox(name);
                            File.WriteAllText("PacketConfig.json", JsonConvert.SerializeObject(settingsWindow.configData, Formatting.Indented));
                            ExpectedFreqClicked(sender, e);
                            if (!settingsWindowVis) { settingsWindow.showUpdateListView(); }
                            return;
                        }

                    }
                }
            }));

        }
        private void ExpectedDimBoxKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == System.Windows.Input.Key.Enter)
            {

                ExpectedBoxKeyDownEvent(sender, "Boyut", expectedDim, e);

            }
        }

        private void ExpectedBoxKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == System.Windows.Input.Key.Enter)
            {

                ExpectedBoxKeyDownEvent(sender, "Frekans", expectedFreq, e);
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
                                 Dictionary<string[], int> expectedValue, string type)
        {
            if (stacks[key].Children.Count > 0 & (!rowColorStart || dataLoading))
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

        public void writeTempData()
        {
            for (int i = 0; i < tempDimLineValuesList.Count; i++)
            {
                string[] paket_proje = tempDimLineValuesList.ElementAt(i).Key;
                string fileName = paket_proje[0] + "_" + paket_proje[1];
                dataKeeper.writeData("BOYUT", fileName, tempDimLineValuesList[paket_proje].ToList(), tempDimChartXLabels[paket_proje].ToList());
                tempDimChartXLabels[paket_proje].Clear();
                tempDimLineValuesList[paket_proje].Clear();

                dataKeeper.writeData("FREKANS", fileName, tempLineValuesList[paket_proje].ToList(), tempChartXLabels[paket_proje].ToList());
                tempChartXLabels[paket_proje].Clear();
                tempLineValuesList[paket_proje].Clear();
                writeFinished = true;
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

                int a = totalReceivedPacket.Keys.ToList().IndexOf(paket_proje);

                totalReceivedPacket[paket_proje][1] += 1;
                totalReceivedPacket[paket_proje][2] = bytes.Length;
                totalReceivedPacket[paket_proje][3] += bytes.Length;

                dataGrid.Dispatcher.Invoke(new System.Action(() =>
                {
                    setExpectedLabel(paket_proje, lineValuesList, freqLabelStacks, expectedFreq, "Frekans");
                    setExpectedLabel(paket_proje, dimLineValuesList, dimLabelStacks, expectedDim, "Boyut");

                    string fileName = paket_proje[0] + "_" + paket_proje[1];

                    if (dimLineValuesList[paket_proje].Count == saveLength)
                    {
                        //dataKeeper.writeOneData("BOYUT", fileName, dimLineValuesList[paket_proje].Last(), dimChartXLabels[paket_proje].Last());

                        dimChartXLabels[paket_proje].RemoveAt(0);
                        dimLineValuesList[paket_proje].RemoveAt(0);

                        dimLineValuesList[paket_proje].Add(bytes.Length);
                        dimLineSeriesList[paket_proje].Values = dimLineValuesList[paket_proje];
                        dimChartXLabels[paket_proje].Add(DateTime.Now.ToString("HH:mm:ss:fff"));

                        tempDimChartXLabels[paket_proje].Add(dimChartXLabels[paket_proje].Last());
                        tempDimLineValuesList[paket_proje].Add(dimLineValuesList[paket_proje].Last());
                    }
                    else
                    {
                        dimLineValuesList[paket_proje].Add(bytes.Length);
                        dimLineSeriesList[paket_proje].Values = dimLineValuesList[paket_proje];
                        dimChartXLabels[paket_proje].Add(DateTime.Now.ToString("HH:mm:ss:fff"));

                        tempDimChartXLabels[paket_proje].Add(dimChartXLabels[paket_proje].Last());
                        tempDimLineValuesList[paket_proje].Add(dimLineValuesList[paket_proje].Last());
                        //dataKeeper.writeOneData("BOYUT", fileName, dimLineValuesList[paket_proje].Last(), dimChartXLabels[paket_proje].Last());
                    }

                    if (tempDimChartXLabels[paket_proje].Count >= tempLength & dataKeeper.writeFinished)
                    {
                        dataKeeper.writeData("BOYUT", fileName, tempDimLineValuesList[paket_proje].ToList(), tempDimChartXLabels[paket_proje].ToList());
                        tempDimChartXLabels[paket_proje].Clear();
                        tempDimLineValuesList[paket_proje].Clear();
                        //setExpectedAnalysis(paket_proje, "BOYUT");
                    }

                    if (rowColorStart & dataGrid.Items.Count == totalReceivedPacket.Count)
                    {
                        setColor();
                        exportButton.IsEnabled = true;
                        rowColorStart = false;
                    }

                    updateBarPieChart(paket_proje);

                    dataSourceUpdate(paket_proje, totalReceivedPacket[paket_proje][0], bytes.Length);

                }));
            }
        }


        public void clearData()
        {
            //dataSource.Clear();
            dataGrid.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < totalReceivedPacket.Count; i++)
                {
                    string[] paket_proje = totalReceivedPacket.ElementAt(i).Key;

                    totalReceivedPacket[paket_proje] = new[] { 0, 0, 0, 0 };
                    dimChartXLabels[paket_proje].Clear();
                    dimLineValuesList[paket_proje].Clear();

                    chartXLabels[paket_proje].Clear();
                    lineValuesList[paket_proje].Clear();

                    pieChartValues[paket_proje[0]][0] = 0;

                    var item = dataSource.FirstOrDefault(j => j.Key.SequenceEqual(paket_proje));
                    if (item.Key == null)
                    {
                        dataSource.Add(new KeyValuePair<string[], int[]>(paket_proje, totalReceivedPacket[paket_proje]));
                    }
                    else
                    {
                        int index = dataSource.IndexOf(item);
                        dataSource[index].Value[1] += 0;
                        dataSource[index].Value[2] = 0;
                        dataSource[index].Value[3] += 0;
                        dataSource[index] = new KeyValuePair<string[], int[]>(item.Key, new int[] { 0, 0, 0, 0 });
                    }
                }
            });
        }

        //Program kapandığında oluşan event
        delegate void AppClosedEventHandler(object sender, EventArgs e);
        event AppClosedEventHandler AppClosed;  
        private void ExportFinished()
        {
            object sender = new object();
            EventArgs e = new EventArgs();

            if (dataLoading) logLabel.Content = "Kayıtlı Veriler Görüntüleniyor";
            else logLabel.Content = string.Empty;

            if (appClosing)
            {
                if (popUp.resultDelFile)
                {
                    writeFinished = false;
                    string folderPath = Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"), "PacketAnalysis\\DATA\\" + paketName + "\\" + nowDate);
                    Directory.Delete(folderPath, true);
                    AppClosed?.Invoke(sender, e);
                }
                else if (!popUp.resultDelFile)
                {
                    //exportAll();
                    AppClosed?.Invoke(sender, e);
                }
                else { };
            }

        }
        private void MainAppClosed(object sender = null, EventArgs e=null)
        {
            subscriber.Abort();
            subSocket.Dispose();
            Environment.Exit(0);
        }

        public void resultSet(object sender, RoutedEventArgs e)
        {
            if (popUp.resultExport)
            {
                this.Visibility = Visibility.Collapsed;
                popUp.progressBar.Visibility = Visibility.Visible;
                popUp.pText.Visibility = Visibility.Visible;
                exportClick(sender, e);
            }
            else if (!popUp.resultExport)
            {
                //exportAll();
                ExportFinished();
            }
        }

        private void MainAppClosing(object sender, CancelEventArgs e)
        {
            if(exportButton.IsEnabled)
            {
                e.Cancel = true;
                appClosing = true;

                popUp = new Popup();
                popUp.PopupEvent += resultSet;
                popUp.Closed += PopUpClosed;
                popUp.Show();
                
            }
            else
            {
                MainAppClosed(sender, e);
            }
        }

        private void PopUpClosed(object sender, EventArgs e)
        {
            appClosing = false;
        }

        //Grafikteki ZOOM- butonuna tıklandığında oluşan event
        private void dimZoomButton_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;
                    dimChartStatuses[selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1)] = "ZOOM-";
                    if(dataLoading) setChartStatues(dimChartList[selectedRow.Key], dimLineValuesList[selectedRow.Key],
                                                    dimChartXLabels[selectedRow.Key], dimChartStatuses[selectedRow.Key[0] + "_" + selectedRow.Key[1]]);
                }
            }));
        }
        //Grafikteki REAL butonuna tıklandığında oluşan event
        private void dimRealButton_Click(object sender, RoutedEventArgs e)
        {
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
            Button zoomClicked = (Button)sender;
            DataGridRow row = FindAncestor<DataGridRow>(zoomClicked);

            var selecteItem = dataGrid.SelectedItem;
            if (row != null)
            {
                KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)row.Item;
                chartStatuses[selectedRow.Key.ElementAt(0) + "_" + selectedRow.Key.ElementAt(1)] = "ZOOM-";
                if(dataLoading) setChartStatues(chartList[selectedRow.Key], lineValuesList[selectedRow.Key],
                                        chartXLabels[selectedRow.Key], chartStatuses[selectedRow.Key[0] + "_" + selectedRow.Key[1]]);
            }
            else MessageBox.Show("row null");

        }
        //Grafikteki REAL butonuna tıklandığında oluşan event
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
            settingsWindow.disconnect = disconnect;

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
            settingsWindow.disconnect = disconnect;
            if (!subSocket.IsDisposed) subSocket.Close();
            disconnectButton.IsEnabled = false;
            connectButton.IsEnabled = true;

        }

        private async void dataLoad_Click(object sender, RoutedEventArgs e)
        {
            
            VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog();
            dlg.SelectedPath = Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"), "PacketAnalysis\\DATA");
            dlg.ShowNewFolderButton = true;

            if (dlg.ShowDialog() == true)
            {
                string dataLoadPath = dlg.SelectedPath;
                
                DirectoryInfo directoryInfo = new DirectoryInfo(dataLoadPath);
                FileInfo[] files = directoryInfo.GetFiles("*.json");
                
                if(files.Length == 0)
                {
                    MessageBox.Show("Seçilen Klasörde Konfig Dosyası Bulunmamaktadır.");
                    return; 
                }

                loading.Visibility = Visibility.Visible;
                exportButton.Visibility = Visibility.Collapsed;
                dataLoad.IsEnabled = false;
                dataRemove.IsEnabled = true;
                DisconnectButtonClicked(sender, e);
                await Task.Delay(1000);
                if (timer != null) timer.Stop();
                dataLoading = true;

                settingsWindow = new SettingsWindow(dataLoadPath + "\\" + files[0].Name);
                //settingsWindow.jsonPath = "C:\\Users\\PC_4232\\AppData\\Roaming\\PacketAnalysis\\DATA\\YZB_PAKET\\13-12-23--13-45\\PackageConfig.json";
                enumStruct = settingsWindow.enumStruct;

                connectButton.IsEnabled = false;
                enumKaydetClick(sender, e);
            }
        }        

        public delegate void PiechartColorHandler();
        public event PiechartColorHandler ColorChanged;
        public void FillChart(string chartType)
        {
            ColorChanged += setColorLoad;
            for (int i = 0; i < totalReceivedPacket.Count; i++)
            {
                string[] paket_proje = totalReceivedPacket.ElementAt(i).Key;

                int row = 2;

                string fileName = paket_proje[0] + "_" + paket_proje[1];
                string date = settingsWindow.jsonPath.Replace(settingsWindow.jsonPath.Substring(settingsWindow.jsonPath.LastIndexOf("\\")), "");
                date = date.Substring(date.LastIndexOf("\\") + 1);
                dataKeeper.savedDataMode(date);
                dateLabel.Content = "Tarih: " + date;
                string typePath = Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"), "PacketAnalysis\\DATA\\" + settingsWindow.packetName + "\\" + date + "\\" + chartType + "\\");

                using (StreamReader sr = new StreamReader(Path.Combine(typePath, fileName + ".txt")))
                {
                    row = 1;
                    while (!sr.EndOfStream)
                    {
                        //progressBar.Value += 1;
                        string[] data = sr.ReadLine().Split(',');
                        if (row > 1)
                        {
                            object value = null;
                            try
                            {
                                value = int.Parse(data[1]);
                            }
                            catch
                            {
                                value = data[1];
                            }

                            if (chartType == "FREKANS")
                            {
                                chartXLabels[paket_proje].Add(data[0]);
                                lineValuesList[paket_proje].Add((int)value);
                                lineSeriesList[paket_proje].Values = lineValuesList[paket_proje];
                            }
                            else
                            {
                                dimChartXLabels[paket_proje].Add(data[0]);
                                dimLineValuesList[paket_proje].Add((int)value);
                                dimLineSeriesList[paket_proje].Values = dimLineValuesList[paket_proje];
                            }
                        }

                        row++;
                    }                   

                    pieChartValues[paket_proje[0]][0] += dimLineValuesList[paket_proje].Count;

                    var item = dataSource.FirstOrDefault(j => j.Key == paket_proje);
                    if (item.Key == null)
                    {
                    }
                    else
                    {
                        int index = dataSource.IndexOf(item);
                        dataSource[index] = new KeyValuePair<string[], int[]>(item.Key, new int[] { item.Value[0], dimLineValuesList[paket_proje].Count, item.Value[2], dimLineValuesList[paket_proje].Sum() });
                        totalReceivedPacket[paket_proje] = dataSource[index].Value;
                    }                                        
                }
            }
            int idx = 0;
            string name = totalReceivedPacket.ElementAt(0).Key[0];
            foreach (var data in totalReceivedPacket)
            {
                if(name != data.Key[0])
                {
                    idx = 0;
                }

                barColumnSeries[data.Key[0]].Values[idx] = dimLineValuesList[data.Key].Count;
                idx++;

                name = data.Key[0];
            }
            ColorChanged?.Invoke();

            setPlayPauseIcon("PlayButtonIcon.png");

            dataGrid.Dispatcher.Invoke(() => {
                playingBar.Visibility = Visibility.Visible;
                exportButton.Visibility = Visibility.Collapsed;
            }) ;
        }

        private void setPlayPauseIcon(string pathIcon)
        {
            BitmapImage playSource = new BitmapImage();
            playSource.BeginInit();
            playSource.UriSource = new Uri((Path.Combine(Environment.CurrentDirectory, pathIcon)));
            playSource.EndInit();
            playPauseImage.Source = playSource;
        }

        private void SliderDragStart(object sender, DragStartedEventArgs e)
        {
            sliderChanged = true;
        }

        private void ChangedValueSlider(object sender, EventArgs e)
        {

            string time = freqTimes.ElementAt((Convert.ToInt32(playingBar.Value)));
            sliderText.Text = time;            

        }

        private void SliderDragCompleted(object sender, DragCompletedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(() =>
            {
                string time = freqTimes.ElementAt((Convert.ToInt32(playingBar.Value)));
                sliderText.Text = time;
                int index = playDataStruct.FindIndex(item => item.Key[2] == time);

                bool forward = DateTime.ParseExact(time, "HH:mm:ss", CultureInfo.InvariantCulture)
                               > DateTime.ParseExact(chartXLabels.ElementAt(0).Value.Last(), "HH:mm:ss", CultureInfo.InvariantCulture);


                if(!forward)
                {
                    removeData(lineValuesList, chartXLabels, time, "HH:mm:ss");
                    removeData(dimLineValuesList, dimChartXLabels, time, "HH:mm:ss:fff");
                    SetDataSource();
                }
                else
                {
                    int startIdx = lastIndex;
                    playData(playDataStruct, startIdx, index, false);
                    SetDataSource();
                }
         
                playData(playDataStruct, index, playDataStruct.Count);
            });
        }

        public void SetDataSource()
        {
            dataGrid.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < totalReceivedPacket.Count; i++)
                {
                    string[] paket_proje = totalReceivedPacket.ElementAt(i).Key;
                    totalReceivedPacket[paket_proje][1] = dimChartXLabels[paket_proje].Count;
                    totalReceivedPacket[paket_proje][3] = dimLineValuesList[paket_proje].Sum();
                    dimLineSeriesList[paket_proje].Values = dimLineValuesList[paket_proje];
                }

                for (int i = 0; i < totalReceivedPacket.Count; i++)
                {
                    string[] paket_proje = totalReceivedPacket.ElementAt(i).Key;

                    var item = dataSource.FirstOrDefault(x => x.Key.SequenceEqual(paket_proje));
                    if (item.Key == null)
                    {
                        dataSource.Add(new KeyValuePair<string[], int[]>(paket_proje, totalReceivedPacket[paket_proje]));
                    }
                    else
                    {
                        int index = dataSource.IndexOf(item);
                        dataSource[index].Value[1] = totalReceivedPacket[paket_proje][1];
                        dataSource[index].Value[3] = totalReceivedPacket[paket_proje][3];
                        dataSource[index] = new KeyValuePair<string[], int[]>(item.Key, new int[] { item.Value[0], item.Value[1], item.Value[2], item.Value[3] });
                    }

                    int total = 0;
                    int idx = 0;
                    foreach (var data in totalReceivedPacket)
                    {
                        if (data.Key[0] == paket_proje[0])
                        {
                            total += data.Value[1];
                            barColumnSeries[paket_proje[0]].Values[idx] = data.Value[1];
                            idx++;
                        }
                    }
                    pieChartValues[paket_proje[0]][0] = total;
                }
            });
        }

        public void removeData(Dictionary<string[], ChartValues<int>> values, Dictionary<string[], List<string>> labels, string data, string dateFormat)
        {
            var newDictionary = labels
                        .Select(kvp =>
                        {
                            string[] key = kvp.Key;
                            List<string> labelList = kvp.Value;
                            int length = kvp.Value.Count;
                            var msData = DateTime.ParseExact(data, "HH:mm:ss", CultureInfo.InvariantCulture);
                            int index = labelList.FindIndex(item => item.Contains(data));
                            //int index = labelList.IndexOf(data);
                            if (index == -1) index = labelList.IndexOf(labelList.Where(x => DateTime.ParseExact(x, dateFormat, CultureInfo.InvariantCulture) > msData)
                                                               .OrderBy(x => DateTime.ParseExact(x, dateFormat, CultureInfo.InvariantCulture)).FirstOrDefault());

                            if (index == 0) {
                                labelList.Clear();
                                values[key].Clear();
                            } 

                            if (index != -1 && index < labelList.Count - 1)
                            {
                                labelList.RemoveRange(index , labelList.Count - (index));
                                for(int i = index ; i< length; i++)
                                {
                                    values[key].RemoveAt(values[key].Count - 1);
                                }
                            }
                            return new KeyValuePair<string[], List<string>>(kvp.Key, labelList);
                        })
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            labels = newDictionary;
        }

        public async void ConcatList()
        {
            foreach(var time in chartXLabels.ElementAt(0).Value)
            {
                freqTimes.Add(time);
            }            

            await Task.Run(() =>
            {
                List<string> allValues = dimChartXLabels.Values.SelectMany(list => list).ToList();
                List<string[]> allKeys = dimChartXLabels.SelectMany(kv => kv.Value.Select(_ => kv.Key)).ToList();
                List<int> allLineValue = dimLineValuesList.Values.SelectMany(list => list).ToList();

                var sortedCombined = allValues.Zip(allKeys, (value, key) => new { Value = value, Key = key })
                                          .OrderBy(item => DateTime.ParseExact(item.Value, "HH:mm:ss:fff", null))
                                          .ToList();

                var sortedCombined2 = allLineValue.Zip(allValues, (integer, value) => new { Integer = integer, Value = value })
                                               .OrderBy(item => DateTime.ParseExact(item.Value, "HH:mm:ss:fff", null))
                                               .ToList();

                List<string> sortedValues = sortedCombined.Select(item => item.Value).ToList();
                List<string[]> sortedKeys = sortedCombined.Select(item => item.Key).ToList();
                List<int> sortedDim = sortedCombined2.Select(item => item.Integer).ToList();

                List<KeyValuePair<string[], KeyValuePair<string, int>>> result_deneme1 = new List<KeyValuePair<string[], KeyValuePair<string, int>>>();

                for (int i = 0; i < sortedKeys.Count; i++)
                {
                    KeyValuePair<string, int> keyVal = new KeyValuePair<string, int>(sortedValues[i], sortedDim[i]);
                    KeyValuePair<string[], KeyValuePair<string, int>> KV = new KeyValuePair<string[], KeyValuePair<string, int>>(sortedKeys[i], keyVal);
                    result_deneme1.Add(KV);
                }

                List<string> allValues2 = chartXLabels.Values.SelectMany(list => list).ToList();
                List<string[]> allKeys2 = chartXLabels.SelectMany(kv => kv.Value.Select(_ => kv.Key)).ToList();
                List<int> allLineValue2 = lineValuesList.Values.SelectMany(list => list).ToList();

                var sortedCombined3 = allValues2.Zip(allKeys2, (value, key) => new { Value = value, Key = key })
                                          .OrderBy(item => DateTime.ParseExact(item.Value, "HH:mm:ss", null))
                                          .ToList();

                var sortedCombined4 = allLineValue2.Zip(allValues2, (integer, value) => new { Integer = integer, Value = value })
                                               .OrderBy(item => DateTime.ParseExact(item.Value, "HH:mm:ss", null))
                                               .ToList();

                List<string> sortedValues2 = sortedCombined3.Select(item => item.Value).ToList();
                List<string[]> sortedKeys2 = sortedCombined3.Select(item => item.Key).ToList();
                List<int> sortedDim2 = sortedCombined4.Select(item => item.Integer).ToList();

                List<KeyValuePair<string[], KeyValuePair<string, int>>> result_deneme = new List<KeyValuePair<string[], KeyValuePair<string, int>>>();

                for (int i = 0; i < sortedKeys2.Count; i++)
                {
                    KeyValuePair<string, int> keyVal = new KeyValuePair<string, int>(sortedValues2[i], sortedDim2[i]);
                    KeyValuePair<string[], KeyValuePair<string, int>> KV = new KeyValuePair<string[], KeyValuePair<string, int>>(sortedKeys2[i], keyVal);
                    result_deneme.Add(KV);
                }

                playDataStruct = MatchAndCreateList(result_deneme, result_deneme1);
                clearData();
                playData(playDataStruct, 0, playDataStruct.Count);
                //PrintMatchedList(a);
            });
        }

        public void updateBarPieChart(string[] paket_proje)
        {
            int total = 0;
            int idx = 0;
            foreach (var data in totalReceivedPacket)
            {
                if (data.Key[0] == paket_proje[0])
                {
                    total += data.Value[1];
                    barColumnSeries[paket_proje[0]].Values[idx] = data.Value[1];
                    idx++;
                }
            }

            pieChartValues[paket_proje[0]][0] = total;
        }

        public void dataSourceUpdate(string[] paket_proje, int freq, int dim)
        {
            var item = dataSource.FirstOrDefault(x => x.Key.SequenceEqual(paket_proje));
            if (item.Key == null)
            {
                dataSource.Add(new KeyValuePair<string[], int[]>(paket_proje, totalReceivedPacket[paket_proje]));
            }
            else
            {
                int index = dataSource.IndexOf(item);
                dataSource[index].Value[0] = freq;
                dataSource[index].Value[1] = totalReceivedPacket[paket_proje][1];
                dataSource[index].Value[2] = dim;
                dataSource[index].Value[3] = totalReceivedPacket[paket_proje][3];
                dataSource[index] = new KeyValuePair<string[], int[]>(item.Key, new int[] { item.Value[0], item.Value[1], item.Value[2], item.Value[3] });
            }
        }

        public int lastIndex;
        public void playData(List<KeyValuePair<string[], int[]>> list, int startIdx, int stopIdx, bool play = true)
        {
            SetDataSource();
            sliderChanged = false;

            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                playingBar.Maximum = freqTimes.Count - 1;
                playingBar.Minimum = 0;

                loading.Visibility = Visibility.Collapsed;
                playingBar.Visibility = Visibility.Visible;
                logLabel.Visibility = Visibility.Collapsed;

                setPlayPauseIcon("PauseButtonIcon.png");
                playPauseButton.Name = "pause";
            }));

            Task.Run( async () =>
            {
                try
                {
                    for (int i = startIdx; i < stopIdx; i++)
                    {
                        if (sliderChanged) 
                        {
                            lastIndex = i;
                            return;
                        }

                        if (!dataLoading) return;

                        string[] key = list.ElementAt(i).Key;
                        int[] value = list.ElementAt(i).Value;
                        int freq = value[0];
                        int dim = value[1];
                        string freqLabel = key[2];
                        string dimLabel = key[3];
                        string[] paket_proje = new[] { key[0], key[1] };

                        int delay = (int)((long)DateTime.ParseExact(list.ElementAt(i + 1).Key[3], "HH:mm:ss:fff", null).TimeOfDay.TotalMilliseconds
                                   - (long)DateTime.ParseExact(dimLabel, "HH:mm:ss:fff", null).TimeOfDay.TotalMilliseconds);

                        if (dim > 0)
                        {
                            totalReceivedPacket[paket_proje][1] += 1;
                            totalReceivedPacket[paket_proje][2] = dim;
                            totalReceivedPacket[paket_proje][3] += dim;

                            dataGrid.Dispatcher.Invoke(new System.Action(() =>
                            {
                                int playBarValue = freqTimes.FindIndex(x => x == freqLabel);
                                if (playBarValue != -1) playingBar.Value = playBarValue;
                                
                                string fileName = paket_proje[0] + "_" + paket_proje[1];


                                dimLineValuesList[paket_proje].Add(dim);
                                dimLineSeriesList[paket_proje].Values = dimLineValuesList[paket_proje];
                                dimChartXLabels[paket_proje].Add(dimLabel);

                                if (!chartXLabels[paket_proje].Contains(freqLabel))
                                {   


                                    chartXLabels[paket_proje].Add(freqLabel);
                                    lineValuesList[paket_proje].Add(freq);
                                    lineSeriesList[paket_proje].Values = lineValuesList[paket_proje];

                                }

                                if (rowColorStart & dataGrid.Items.Count == totalReceivedPacket.Count)
                                {
                                    setColor();
                                    exportButton.IsEnabled = true;
                                    rowColorStart = false;
                                }

                                updateBarPieChart(paket_proje);

                                dataSourceUpdate(paket_proje, freq, dim);

                            }));

                            if(play) await Task.Delay(delay);
                        }

                        else
                        {
                            dataGrid.Dispatcher.Invoke(new Action(() =>
                            {
                                int playBarValue = freqTimes.FindIndex(x => x == freqLabel);
                                if (playBarValue != -1) playingBar.Value = playBarValue;

                                if (!dataLoading) return;
                                var item = dataSource.FirstOrDefault(x => x.Key.SequenceEqual(paket_proje));
                                if (item.Key != null)
                                {
                                    int index = dataSource.IndexOf(item);
                                    dataSource[index].Value[0] = freq;
                                    dataSource[index] = new KeyValuePair<string[], int[]>(item.Key, new int[] { item.Value[0], item.Value[1], item.Value[2], item.Value[3] });
                                }

                                chartXLabels[paket_proje].Add(freqLabel);
                                lineValuesList[paket_proje].Add(freq);
                                lineSeriesList[paket_proje].Values = lineValuesList[paket_proje];

                            }));

                            if(dim == -1 & play) await Task.Delay(1000 / totalReceivedPacket.Count);
                        }
                    }
                }
                catch { }

                if (play)
                {
                    dataGrid.Dispatcher.Invoke(new Action(() =>
                    {
                        setPlayPauseIcon("PlayButtonIcon.png");
                        playPauseButton.Name = "play";
                    }));
                }

            });
        }

        private void PlayPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if(button.Name == "pause")
            {
                setPlayPauseIcon("PlayButtonIcon.png");
                playPauseButton.Name = "play";
                sliderChanged = true;
            }
            else if(button.Name == "play")
            {
                setPlayPauseIcon("PauseButtonIcon.png");
                playPauseButton.Name = "pause";
                if (playingBar.Value == playingBar.Maximum) 
                {
                    clearData();
                    playData(playDataStruct, 0, playDataStruct.Count);
                } 
                else playData(playDataStruct, lastIndex, playDataStruct.Count);
            }
            else
            {
                ConcatList();

                loading.Visibility = Visibility.Visible;
                exportButton.Visibility = Visibility.Collapsed;
            }
        }


        public bool ArrComparer(string[] x, string[] y)
        {
            return x.Length == 2 && y.Length == 2 &&
                   x[0] == y[0] && x[1] == y[1];
        }


        public List<KeyValuePair<string[], int[]>> MatchAndCreateList(List<KeyValuePair<string[], KeyValuePair<string, int>>> list1, List<KeyValuePair<string[], KeyValuePair<string, int>>> list2)
        {
            List<KeyValuePair<string[], int[]>> result = new List<KeyValuePair<string[], int[]>>();

            for (int i = 0; i<list1.Count; i++)
            {
                for(int j = 0; j < list2.Count; j++)
                {
                    bool match = ArrComparer(list1.ElementAt(i).Key, list2.ElementAt(j).Key) && list2.ElementAt(j).Value.Key.Contains(list1.ElementAt(i).Value.Key);

                    if (match)
                    {
                        string[] key = new[] { list1.ElementAt(i).Key[0], list1.ElementAt(i).Key[1], list1.ElementAt(i).Value.Key, list2.ElementAt(j).Value.Key };
                        int[] value = new[] { list1.ElementAt(i).Value.Value, list2.ElementAt(j).Value.Value };
                        result.Add(new KeyValuePair<string[], int[]>(key, value));
                    }
                    else
                    {                        
                        string[] key = new[] { list1.ElementAt(i).Key[0], list1.ElementAt(i).Key[1], list1.ElementAt(i).Value.Key, "" };
                        int[] value = new[] { list1.ElementAt(i).Value.Value, 0 };
                    }
                }
            }

            var list3 = list1.Select(item =>
            {
                string[] firstArray = item.Key;
                string secondArrayElement = item.Value.Key;
                return new string[] { firstArray[0], firstArray[1], secondArrayElement};
            }).ToList();

            //var list3 = list1.Select(x => x.Value.Key).ToList();
            var list4 = result.Select(x => 
            {
                return new string[] { x.Key[0], x.Key[1], x.Key[3].Substring(0, x.Key[3].Length - 4) };
            }).ToList();

            List<KeyValuePair<string[], int[]>> tempResult = new List<KeyValuePair<string[], int[]>>();


            for (int i=0; i< list3.Count; i++)
            {
                if(!list4.Any(arr => arr[2].SequenceEqual(list3[i][2])))
                {
                    string[] key = new[] { list1.ElementAt(i).Key[0], list1.ElementAt(i).Key[1], list1.ElementAt(i).Value.Key, list1.ElementAt(i).Value.Key + ":000" };
                    int[] value = new[] { list1.ElementAt(i).Value.Value, -1 };
                    tempResult.Add(new KeyValuePair<string[], int[]>(key, value));
                }
                else
                {
                    if(!list4.Any(arr => arr.SequenceEqual(list3[i])))
                    {
                        string[] key = new[] { list1.ElementAt(i).Key[0], list1.ElementAt(i).Key[1], list1.ElementAt(i).Value.Key, list1.ElementAt(i).Value.Key + ":000" };
                        int[] value = new[] { list1.ElementAt(i).Value.Value, 0 };
                        tempResult.Add(new KeyValuePair<string[], int[]>(key, value));
                    }
                }
            }

            var temp = result.Concat(tempResult).ToList();

            List < KeyValuePair<string[], int[]>> sortedResult = temp.OrderBy(entry => DateTime.ParseExact(entry.Key[3], "HH:mm:ss:fff", null)).ToList();

            return sortedResult;
        }


        public void FillEnumStruct() 
        {            
            FillChart("FREKANS");
            FillChart("BOYUT");
        }

        private async void setColorLoad()        
        {
            await Task.Delay(1000);
            setColor();
            //exportButton.Visibility = Visibility.Visible;
            loading.Visibility = Visibility.Collapsed;
        }

        private void dataRemove_Click(object sender, RoutedEventArgs e)
        {
            dataLoading = false;
            playingBar.Visibility = Visibility.Collapsed;
            exportButton.Visibility = Visibility.Visible;
            playPauseButton.Visibility = Visibility.Collapsed;
            dataLoad.IsEnabled = true; dataRemove.IsEnabled = false;
            logLabel.Content = string.Empty;
            dateLabel.Content = "Tarih: ";
            settingsWindow = new SettingsWindow();
            
            enumKaydetClick(sender, e);
            connectButton.IsEnabled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ConcatList();

            loading.Visibility = Visibility.Visible;
            exportButton.Visibility = Visibility.Collapsed;
        }
    }
}