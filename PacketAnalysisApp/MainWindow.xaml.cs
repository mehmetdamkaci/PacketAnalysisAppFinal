using LiveCharts;
using LiveCharts.Wpf;
using NetMQ;
using NetMQ.Sockets;
using OfficeOpenXml;
using OfficeOpenXml.Style;
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
using System.Windows.Threading;
using OfficeOpenXml.Drawing.Chart;

namespace PacketAnalysisApp
{
    public partial class MainWindow : Window
    {
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

        Dictionary<string, string> chartStatuses = new Dictionary<string, string>();
        Button zoomButton = new Button();
        Button realButton = new Button();
        Button chartExportButton = new Button();


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
        }


        //Grafikleri Dışarı aktarma fonksiyonu
        private void exportChartButtonClick(object sender, RoutedEventArgs e)
        {

            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string[], int[]> selectedRow = (KeyValuePair<string[], int[]>)selecteItem;

                    string path = "";
                    Microsoft.Win32.SaveFileDialog openFileDlg = new Microsoft.Win32.SaveFileDialog();
                    openFileDlg.FileName = selectedRow.Key[0] + "_" + selectedRow.Key[1] + ".xlsx";
                    Nullable<bool> result = openFileDlg.ShowDialog();

                    if (result == true)
                    {
                        path = openFileDlg.FileName;
                    }
                    else return;

                   

                    var package = new ExcelPackage();
                    var worksheetFreqChart = package.Workbook.Worksheets.Add("Frekans Grafiği");

                    worksheetFreqChart.Cells[1, 1].Value = "ZAMAN";
                    worksheetFreqChart.Cells[1, 2].Value = "FREKANS";
                    worksheetFreqChart.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetFreqChart.Cells[1, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetFreqChart.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));
                    worksheetFreqChart.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));

                    var chart = (ExcelLineChart)worksheetFreqChart.Drawings.AddChart("Chart", eChartType.LineMarkers);
                    chart.SetPosition(1*20, 8*20);
                    chart.SetSize(1500, 300);                    
                    chart.Name = selectedRow.Key[0] + " PAKETİ " + selectedRow.Key[1] + " PROJESİ";
                    
                    int length = chartXLabels.IndexOf(chartXLabels.Last());
                    var freq = lineValuesList;

                    for(int i = 2; i <= length + 2; i++)
                    {
                        int value = freq[selectedRow.Key][i - 2];
                        worksheetFreqChart.Cells[i, 2].Value = value;
                        worksheetFreqChart.Cells[i, 1].Value = chartXLabels[i - 2];
                    }

                    chart.Series.Add(worksheetFreqChart.Cells[1, 2, worksheetFreqChart.Dimension.End.Row, 2], worksheetFreqChart.Cells[1, 1, worksheetFreqChart.Dimension.End.Row, 1]);
                    chart.Series[0].Header = selectedRow.Key[0] + " PAKETİ " + selectedRow.Key[1] + " PROJESİ";
                    if (new FileInfo(@path) != null) package.SaveAs(new FileInfo(@path));
                    package.Dispose();
                }
     
            }));
        }

        //Dışarı aktarma butonu fonksiyonu (Excel İşlemleri)
        private void exportClick(object sender, RoutedEventArgs e)
        {           
            string path = "";

            Microsoft.Win32.SaveFileDialog openFileDlg = new Microsoft.Win32.SaveFileDialog();
            openFileDlg.FileName = "veriler_" + DateTime.Now.ToString("ddMMyy") + ".xlsx";
            Nullable<bool> result = openFileDlg.ShowDialog();
            
            if (result == true)
            {
                path = openFileDlg.FileName;
            }
            else return;

            if (path.Substring(path.LastIndexOf('.') + 1, 4) != "xlsx") path += ".xlsx";

            var package = new ExcelPackage();
            var worksheetTable = package.Workbook.Worksheets.Add("Genel Tablo");
            var worksheetFrakans = package.Workbook.Worksheets.Add("Frakans Tablosu");
            var worksheetChart = package.Workbook.Worksheets.Add("Grafikler");

            exportButton.Visibility = Visibility.Collapsed;
            progressBar.Visibility = Visibility.Visible;    
            progressBar.Minimum = 0;
            progressBar.Maximum = totalReceivedPacket.Count*2 + chartXLabels.Count;
            Task.Run(() =>
            {
                ExcelPieChart pieExcelChart = (ExcelPieChart)worksheetTable.Drawings.AddChart("pieChart", eChartType.PieExploded3D);
                var pieExcelSeries = pieExcelChart.Series;
                pieExcelChart.DataLabel.ShowPercent = true;
                pieExcelChart.DataLabel.ShowValue = true;
                pieExcelChart.DataLabel.ShowLegendKey = true;

                int rowTable = 2;
                int columnFreq = 2;

                worksheetTable.Cells[1, 1].Value = "PAKET ADI";
                worksheetTable.Cells[1, 2].Value = "PROJE ADI";
                worksheetTable.Cells[1, 3].Value = "TOPLAM GELEN PAKET SAYISI";
                worksheetTable.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetTable.Cells[1, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetTable.Cells[1, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetTable.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));
                worksheetTable.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));
                worksheetTable.Cells[1, 3].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));

                int barChartPos = 2;
                string paket = "";
                int pieRow = 1;
                worksheetTable.Cells[pieRow, 19].Value = "PAKET ADI";
                worksheetTable.Cells[pieRow, 20].Value = "TOPLAM GELEN PAKET SAYISI";
                worksheetTable.Cells[pieRow, 19].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetTable.Cells[pieRow, 20].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetTable.Cells[pieRow, 19].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));
                worksheetTable.Cells[pieRow, 20].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));

                foreach (var item in totalReceivedPacket)
                {                    
                    if (paket != item.Key[0])
                    {
                        int count = 0;
                        pieRow++;
                        foreach (var item2 in totalReceivedPacket)
                        {
                            if (item2.Key[0] == item.Key[0])
                            {
                                count++;
                            }
                        }

                        var chart = worksheetTable.Drawings.AddChart(item.Key[0], eChartType.ColumnClustered);
                        chart.SetPosition(barChartPos - 1, 0, 3, 0);                              
                        var series = chart.Series.Add(worksheetTable.Cells[barChartPos, 3, rowTable + count - 1, 3], worksheetTable.Cells[barChartPos, 2, rowTable + count - 1, 2]);


                        worksheetTable.Cells[pieRow, 19].Value = item.Key[0];
                        worksheetTable.Cells[pieRow, 19].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheetTable.Cells[pieRow, 19].Style.Fill.BackgroundColor.SetColor(ColorConverter(rowColor[item.Key[0]]));
                        worksheetTable.Cells[pieRow, 19].Style.Font.Bold = true;

                        worksheetTable.Cells[pieRow, 20].Formula = "Sum(" + worksheetTable.Cells[barChartPos, 3].Address +
                                                                ":" + worksheetTable.Cells[rowTable + count - 1, 3].Address + ")";
                        worksheetTable.Cells[pieRow, 20].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheetTable.Cells[pieRow, 20].Style.Fill.BackgroundColor.SetColor(ColorConverter(rowColor[item.Key[0]]));
                        worksheetTable.Cells[pieRow, 20].Style.Font.Bold = true;
                        worksheetTable.Cells[pieRow, 20].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        series.Fill.Color = ColorConverter(rowColor[item.Key[0]]);
                        series.Header = item.Key[0];
                        chart.SetSize(800, (rowTable - barChartPos + count) * 20);
                        barChartPos = rowTable + count;
                    }

                    string keyConcatenated = item.Key[0] + "_" + item.Key[1];

                    worksheetFrakans.Cells[1, columnFreq].Value = keyConcatenated;
                    worksheetFrakans.Cells[1, columnFreq].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetFrakans.Cells[1, columnFreq].Style.Fill.BackgroundColor.SetColor(ColorConverter(rowColor[item.Key[0]]));
                    worksheetFrakans.Cells[1, columnFreq].AutoFitColumns();

                    worksheetTable.Cells[rowTable, 1].Value = item.Key[0];
                    worksheetTable.Cells[rowTable, 1].Style.Font.Bold = true;
                    worksheetTable.Cells[rowTable, 2].Style.Font.Bold = true;
                    worksheetTable.Cells[rowTable, 3].Style.Font.Bold = true;

                    worksheetTable.Cells[rowTable, 2].Value = item.Key[1];
                    worksheetTable.Cells[rowTable, 3].Value = item.Value[1];
                    worksheetTable.Cells[rowTable, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetTable.Cells[rowTable, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetTable.Cells[rowTable, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetTable.Cells[rowTable, 1].Style.Fill.BackgroundColor.SetColor(ColorConverter(rowColor[item.Key[0]]));
                    worksheetTable.Cells[rowTable, 2].Style.Fill.BackgroundColor.SetColor(ColorConverter(rowColor[item.Key[0]]));
                    worksheetTable.Cells[rowTable, 3].Style.Fill.BackgroundColor.SetColor(ColorConverter(rowColor[item.Key[0]]));
                    worksheetTable.Cells[rowTable, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    rowTable++;
                    columnFreq++;
                    paket = item.Key[0];

                    mainGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
                    
                }
                pieExcelChart.SetSize(250,500);
                pieExcelChart.SetPosition((pieRow + pieChartValues.Count + 3)*10, 19*15 + 800);
                pieExcelSeries.Add(worksheetTable.Cells[2, 20, pieRow , 20], worksheetTable.Cells[2, 19, pieRow, 19]);
                worksheetTable.Cells.AutoFitColumns();
                int length = chartXLabels.IndexOf(chartXLabels.Last());
                var freq = lineValuesList;

                worksheetFrakans.Cells["A2"].LoadFromCollection(chartXLabels.ToList().GetRange(0,length));

                for (int i = 2; i < totalReceivedPacket.Count + 2; i++)
                {
                    for (int j = 2; j <= length + 2; j++)
                    {
                        int value = freq.ElementAt(i - 2).Value[j - 2];
                        worksheetFrakans.Cells[j, i].Value = value;
                        worksheetFrakans.Cells[j,i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        var cell = worksheetFrakans.Cells[j, i];
                        var border = cell.Style.Border;
                        var fontWeight = cell.Style.Font;
                        fontWeight.Bold = true;

                        border.Top.Style = ExcelBorderStyle.Thick;
                        border.Left.Style = ExcelBorderStyle.Thick;
                        border.Bottom.Style = ExcelBorderStyle.Thick;
                        border.Right.Style = ExcelBorderStyle.Thick;

                        var brushColor = rowColor[totalReceivedPacket.Keys.ElementAt(i - 2)[0]];
                        var color = new SolidColorBrush(Color.FromArgb((byte)70, brushColor.Color.R, brushColor.Color.G, brushColor.Color.B));

                        if (value == 0)
                        {
                            worksheetFrakans.Cells[j, i].Style.Fill.PatternType = ExcelFillStyle.DarkGray;
                            worksheetFrakans.Cells[j, i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                        } 
                        else 
                        {
                            worksheetFrakans.Cells[j, i].Style.Fill.PatternType = ExcelFillStyle.LightGray;
                            worksheetFrakans.Cells[j, i].Style.Fill.BackgroundColor.SetColor(ColorConverter(color));
                        } 
                    }
                    mainGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
                }

                paket = totalReceivedPacket.ElementAt(0).Key[0];
                int chartRow = 0;
                int chartColumn = 0;
                for (int col = 2; col <= worksheetFrakans.Dimension.Columns; col++)
                {                        
                    if (paket != totalReceivedPacket.ElementAt(col - 2).Key[0])
                    {
                        chartRow++;
                        chartColumn = 0;
                    }                    
                    var chart = (ExcelLineChart)worksheetChart.Drawings.AddChart("Chart" + col, eChartType.LineMarkers);                                        
                    chart.SetPosition(chartRow*15, 0, 1 + chartColumn*13, 0);
                    chart.SetSize(800, 300);
                    var series = chart.Series.Add(worksheetFrakans.Cells[2, col, worksheetFrakans.Dimension.End.Row, col], worksheetFrakans.Cells[2, 1, worksheetFrakans.Dimension.End.Row, 1]);
                    series.Header = worksheetTable.Cells[col, 2].Text;
                    paket = totalReceivedPacket.ElementAt(col - 2).Key[0];
                    chartColumn++;
                    mainGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
                }

                mainGrid.Dispatcher.Invoke(new Action(() => progressBar.Maximum += chartRow * 15 + 10));                
                int countChartRow = 0;
                for(int i = 0; i <= chartRow; i++)
                {
                    for(int j = 1; j <= 15;  j++)
                    {
                        mainGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
                        countChartRow++;
                        worksheetChart.Cells[countChartRow, 1].Value = pieChartValues.ElementAt(i).Key;                            
                        worksheetChart.Row(countChartRow).Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheetChart.Row(countChartRow).Style.Fill.BackgroundColor.SetColor(ColorConverter(rowColor[pieChartValues.ElementAt(i).Key]));
                    }
                        
                }
                worksheetChart.Cells.AutoFitColumns();

                mainGrid.Dispatcher.Invoke(new Action(() => progressBar.Value = progressBar.Maximum));
                Thread.Sleep(1000);
                if (new FileInfo(@path) != null) package.SaveAs(new FileInfo(@path));
                package.Dispose();
                mainGrid.Dispatcher.Invoke((new Action(() =>
                {
                    progressBar.Visibility = Visibility.Collapsed;
                    progressBar.Value = 0;
                    exportLabel.Visibility = Visibility.Visible;
                })));
                Thread.Sleep(1500);
                mainGrid.Dispatcher.Invoke((new Action(() =>
                {
                    exportLabel.Visibility = Visibility.Collapsed;
                    exportButton.Visibility = Visibility.Visible;
                })));

                
            });
        }

        private void BrowseExportButton(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        //Excel Hücreleri için renk dönüştürücü
        private System.Drawing.Color ColorConverter(SolidColorBrush brush)
        {
            byte red = brush.Color.R;
            byte green = brush.Color.G;
            byte blue = brush.Color.B;
            return System.Drawing.Color.FromArgb(red, green, blue);
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

        //Grafik için butonla
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
            totalReceivedPacket = new Dictionary<string[], int[]>(new StringArrayComparer());



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

                    int[] packetAnalysisArr = { 0, 0, 0 };
                    string[] paket_proje = { enumStruct[enumMatchWindow.paketName].Values.ElementAt(i),
                                                            enumStruct[enumStruct[enumMatchWindow.paketName].Values.ElementAt(i)].Values.ElementAt(j)};
                    totalReceivedPacket.Add(paket_proje, packetAnalysisArr);
                    chartList.Add(paket_proje, new CartesianChart());
                    lineSeriesList.Add(paket_proje, new LineSeries());
                    lineValuesList.Add(paket_proje, new ChartValues<int>());
                    chartStatuses.Add(paket_proje[0] + "_" + paket_proje[1], "DEFAULT");
                }
                
            }


            dataSource.Clear();
            foreach (var data in totalReceivedPacket)
            {
                dataSource.Add(data);
            }

        }

        //Bir saniyede bir tabloyu ve frekans değerlerini güncelleyen fonksiyon
        private void UpdateFrekans(object sender, EventArgs e)
        {

            chartXLabels.Add(DateTime.Now.ToString("HH:mm:ss"));
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
                        dataSource[index] = new KeyValuePair<string[], int[]>(item.Key, new int[] { currentTotal - privTotal[i], item.Value[1] });
                    }
                }));

                totalReceivedPacket[paket_proje][0] = currentTotal - privTotal[i];
                privTotal[i] = currentTotal;
                lineValuesList[totalReceivedPacket.Keys.ElementAt(i)].Add(totalReceivedPacket[paket_proje][0]);
                lineSeriesList[totalReceivedPacket.Keys.ElementAt(i)].Values = lineValuesList[totalReceivedPacket.Keys.ElementAt(i)];
                try 
                {
                    switch (chartStatuses[paket_proje[0] + "_" + paket_proje[1]])
                    {
                        case "ZOOM-":
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].Zoom = ZoomingOptions.None;
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].Pan = PanningOptions.None;
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].AxisY[0].MinValue = -1;
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].AxisY[0].MaxValue = lineValuesList[totalReceivedPacket.Keys.ElementAt(i)].Max() + 1;
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].AxisX[0].MinValue = 0;
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].AxisX[0].MaxValue = chartXLabels.Count - 1;
                            break;
                        case "REAL":
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].AxisY[0].MinValue = -1;
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].AxisY[0].MaxValue = lineValuesList[totalReceivedPacket.Keys.ElementAt(i)].Max() + 1;
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].AxisX[0].MinValue = chartXLabels.Count - 20;
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].AxisX[0].MaxValue = chartXLabels.Count - 1;
                            break;
                        case "DEFAULT":
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].Zoom = ZoomingOptions.X;
                            chartList[totalReceivedPacket.Keys.ElementAt(i)].Pan = PanningOptions.X;                           
                            break;
                    }
                }
                catch
                {
                    continue;
                }
            }
        }


        //Chart mouse ile sürüklendiğinde oluşan event
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
        private void LoadFreqChart(object sender, RoutedEventArgs e)
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
        }

        //Detay butonuna tıklandığında oluşan event
        public void ButtonDetayClicked(object sender, RoutedEventArgs e)
        {
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

            Func<ChartPoint, string> labelPoint = chartPoint => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);
            
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
                    FontSize = 12
                };
                piechartPaket.Add(pieSeries);

            }
            pieChart.Series = piechartPaket;

            paketName = enumMatchWindow.paketName;

            paketColumn.Binding = new Binding("Key[0]");
            projeColumn.Binding = new Binding("Key[1]");
            frekansColumn.Binding = new Binding("Value[0]");
            toplamColumn.Binding = new Binding("Value[1]");
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
            enumMatchWindow.Closed += enumMatchClosed;
            enumMatchWindow.OkKaydetLog.Click += enumKaydetClick;
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

        //Paketlerin alındığı fonksiyon bir thread'te çalışır
        public void receiveData()
        {
            bytes = new byte[6]; 

            while (!stop)
            {                
                bytes = ReceivingSocketExtensions.ReceiveFrameBytes(subSocket);
                bytes = DecryptAes(bytes);

                string[] paket_proje = new string[] { enumStruct[paketName].Values.ElementAt((int)bytes[paketByte]),
                                            enumStruct[enumStruct[paketName].Values.ElementAt((int)bytes[paketByte])].Values.ElementAt((int)bytes[projeByte]) };
    
                totalReceivedPacket[paket_proje][1] += 1;

                int total = 0;
                dataGrid.Dispatcher.Invoke(new System.Action(() =>
                {

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
        private void zoomButton_Click(object sender, RoutedEventArgs e)
        {
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

        //Soket Paneldeki bağlantıyı kes butonuna tıklandığında oluşan event
        private void DisconnectButtonClicked(object sender, RoutedEventArgs e)
        {
            subscriber.Abort();
            stop = true;
            disconnect = true;
            enumMatchWindow.disconnect = disconnect;
            if (!subSocket.IsDisposed) subSocket.Close();
            borderSocketPanel.Visibility = Visibility.Collapsed;
            foreach (var button in paketButtons)
            {
                button.Value.Visibility = Visibility.Visible;
            }
        }

        //Soket Panel butonuna tıklandığında oluşan event
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
    }
}