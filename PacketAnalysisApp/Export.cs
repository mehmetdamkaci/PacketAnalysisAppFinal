using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Collections.ObjectModel;
using LiveCharts;
using System.Globalization;
using System.Collections;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace PacketAnalysisApp
{
    public class Export
    {
        public void ExportOnlyChart(DataGrid datagrid, string ChartType, string[] packetProje, ObservableCollection<string> chartXLabels, ChartValues<int> ChartYValue, StackPanel ExportPanel)
        {
                       
            Task.Run( () =>
            {
                string path = "";
                Microsoft.Win32.SaveFileDialog openFileDlg = new Microsoft.Win32.SaveFileDialog();
                openFileDlg.FileName = ChartType + "_" + packetProje[0] + "_" + packetProje[1] + ".xlsx";
                Nullable<bool> result = openFileDlg.ShowDialog();

                if (result == true)
                {
                    path = openFileDlg.FileName;
                }
                else return;

                datagrid.Dispatcher.Invoke(async () =>
                {
                    var package = new ExcelPackage();
                    var worksheetFreqChart = package.Workbook.Worksheets.Add("Frekans Grafiği");

                    worksheetFreqChart.Cells[1, 1].Value = "ZAMAN";
                    worksheetFreqChart.Cells[1, 2].Value = ChartType;
                    worksheetFreqChart.Cells[1, 4].Value = "TOPLAM";
                    worksheetFreqChart.Cells[1, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheetFreqChart.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetFreqChart.Cells[1, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetFreqChart.Cells[1, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetFreqChart.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));
                    worksheetFreqChart.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));
                    worksheetFreqChart.Cells[1, 4].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));

                    worksheetFreqChart.Cells[1, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    var chart = (ExcelLineChart)worksheetFreqChart.Drawings.AddChart("Chart", eChartType.LineMarkers);
                    chart.SetPosition(3 * 20, 9 * 20);
                    chart.SetSize(1500, 300);
                    chart.Name = packetProje[0] + " PAKETİ " + packetProje[1] + " PROJESİ";

                    int length = chartXLabels.IndexOf(chartXLabels.Last());

                    Label title = (Label)ExportPanel.Children[0];
                    title.Visibility = Visibility.Collapsed;
                    ProgressBar chartExportProgress = (ProgressBar)ExportPanel.Children[1];
                    Label chartExportLabel = (Label)ExportPanel.Children[2];
                    chartExportProgress.Visibility = Visibility.Visible;
                    chartExportProgress.Maximum = length;
                    chartExportProgress.Minimum = 0;
                    chartExportProgress.Value = 0;
                    chartExportProgress.Height = 5;
                    chartExportProgress.Margin = new Thickness(20, 5, 20, 0);


                    for (int i = 2; i <= length + 2; i++)
                    {
                        chartExportProgress.Value += 1;

                        int value = ChartYValue[i - 2];
                        worksheetFreqChart.Cells[i, 2].Value = value;
                        worksheetFreqChart.Cells[i, 1].Value = chartXLabels[i - 2];
                    }

                    var scale = worksheetFreqChart.ConditionalFormatting.AddTwoColorScale(worksheetFreqChart.Cells[2, 2, worksheetFreqChart.Dimension.End.Row, 2]);
                    scale.LowValue.Color = System.Drawing.Color.FromArgb(255, 255, 192, 192);
                    scale.HighValue.Color = System.Drawing.Color.Green;

                    await Task.Delay(1000);
                    chartExportProgress.Visibility = Visibility.Collapsed;
                    chartExportProgress.Value = 0;
                    chartExportLabel.Visibility = Visibility.Visible;
                    await Task.Delay(1000);
                    chartExportLabel.Visibility = Visibility.Collapsed;
                    title.Visibility = Visibility.Visible;

                    worksheetFreqChart.Cells[2, 4].Formula = "Sum(" + worksheetFreqChart.Cells[2, 2].Address +
                                    ":" + worksheetFreqChart.Cells[length + 2, 2].Address + ")";
                    worksheetFreqChart.Cells[2, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetFreqChart.Cells[2, 4].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.Green));

                    chart.Series.Add(worksheetFreqChart.Cells[2, 2, worksheetFreqChart.Dimension.End.Row, 2], worksheetFreqChart.Cells[2, 1, worksheetFreqChart.Dimension.End.Row, 1]);
                    chart.Series[0].Header = packetProje[0] + " PAKETİ " + packetProje[1] + " PROJESİ";
                    worksheetFreqChart.Cells.AutoFitColumns();
                    worksheetFreqChart.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    
                    //colorScale.HighValue = 
                    //colorScale = ((ExcelRange)worksheetFreqChart.Cells[2, 2, worksheetFreqChart.Dimension.End.Row, 2])


                    if (new FileInfo(@path) != null)
                    {
                        package.SaveAs(new FileInfo(@path));
                    }
                    package.Dispose();
                });

            });

        }

        public void MainExport(DataGrid dataGrid, ProgressBar progressBar, Dictionary<string[], int[]> totalPacket, Dictionary<string, SolidColorBrush> colors, 
                               ObservableCollection<string> Xlabels, Dictionary<string[], ChartValues<int>> Ylabels, Dictionary<string, ChartValues<int>> pieChartValues, 
                               Button exportButton, Label exportLabel, Dictionary<string[], ObservableCollection<string>> dimValues, Dictionary<string[], ChartValues<int>> dimYlabels)
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
            var worksheetChart = package.Workbook.Worksheets.Add("Frekans Grafikleri");
            var worksheetBoyut = package.Workbook.Worksheets.Add("Boyut Tablosu");
            var worksheetBoyutChart = package.Workbook.Worksheets.Add("Boyut Grafikleri");

            exportButton.Visibility = Visibility.Collapsed;
            progressBar.Visibility = Visibility.Visible;
            progressBar.Minimum = 0;
            progressBar.Maximum = totalPacket.Count * 2 + Xlabels.Count;
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
                worksheetTable.Cells[1, 4].Value = "TOPLAM BOYUT";
                worksheetTable.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetTable.Cells[1, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetTable.Cells[1, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetTable.Cells[1, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetTable.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));
                worksheetTable.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));
                worksheetTable.Cells[1, 3].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));
                worksheetTable.Cells[1, 4].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));

                int barChartPos = 2;
                string paket = "";
                int pieRow = 1;
                worksheetTable.Cells[pieRow, 19].Value = "PAKET ADI";
                worksheetTable.Cells[pieRow, 20].Value = "TOPLAM GELEN PAKET SAYISI";
                worksheetTable.Cells[pieRow, 19].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetTable.Cells[pieRow, 20].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetTable.Cells[pieRow, 19].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));
                worksheetTable.Cells[pieRow, 20].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.LightGray));

                foreach (var item in totalPacket)
                {
                    if (paket != item.Key[0])
                    {
                        int count = 0;
                        pieRow++;
                        foreach (var item2 in totalPacket)
                        {
                            if (item2.Key[0] == item.Key[0])
                            {
                                count++;
                            }
                        }

                        var chart = worksheetTable.Drawings.AddChart(item.Key[0], eChartType.ColumnClustered);
                        chart.SetPosition(barChartPos - 1, 0, 4, 0);
                        var series = chart.Series.Add(worksheetTable.Cells[barChartPos, 3, rowTable + count - 1, 3], worksheetTable.Cells[barChartPos, 2, rowTable + count - 1, 2]);


                        worksheetTable.Cells[pieRow, 19].Value = item.Key[0];
                        worksheetTable.Cells[pieRow, 19].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheetTable.Cells[pieRow, 19].Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[item.Key[0]]));
                        worksheetTable.Cells[pieRow, 19].Style.Font.Bold = true;

                        worksheetTable.Cells[pieRow, 20].Formula = "Sum(" + worksheetTable.Cells[barChartPos, 3].Address +
                                                                ":" + worksheetTable.Cells[rowTable + count - 1, 3].Address + ")";
                        worksheetTable.Cells[pieRow, 20].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheetTable.Cells[pieRow, 20].Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[item.Key[0]]));
                        worksheetTable.Cells[pieRow, 20].Style.Font.Bold = true;
                        worksheetTable.Cells[pieRow, 20].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        series.Fill.Color = ColorConverter(colors[item.Key[0]]);
                        series.Header = item.Key[0];
                        chart.SetSize(800, (rowTable - barChartPos + count) * 20);
                        barChartPos = rowTable + count;
                    }

                    string keyConcatenated = item.Key[0] + "_" + item.Key[1];

                    worksheetFrakans.Cells[1, columnFreq].Value = keyConcatenated;
                    worksheetFrakans.Cells[1, columnFreq].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetFrakans.Cells[1, columnFreq].Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[item.Key[0]]));
                    worksheetFrakans.Cells[1, columnFreq].AutoFitColumns();

                    worksheetBoyut.Cells[1, columnFreq*3 - 4].Value = keyConcatenated;
                    worksheetBoyut.Cells[1, columnFreq*3 - 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetBoyut.Cells[1, columnFreq*3 - 4].Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[item.Key[0]]));
                    worksheetBoyut.Cells[1, columnFreq*3 - 4].AutoFitColumns();

                    worksheetTable.Cells[rowTable, 1].Style.Font.Bold = true;
                    worksheetTable.Cells[rowTable, 2].Style.Font.Bold = true;
                    worksheetTable.Cells[rowTable, 3].Style.Font.Bold = true;
                    worksheetTable.Cells[rowTable, 4].Style.Font.Bold = true;

                    worksheetTable.Cells[rowTable, 1].Value = item.Key[0];
                    worksheetTable.Cells[rowTable, 2].Value = item.Key[1];
                    worksheetTable.Cells[rowTable, 3].Value = item.Value[1];
                    worksheetTable.Cells[rowTable, 4].Value = item.Value[3];

                    worksheetTable.Cells[rowTable, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetTable.Cells[rowTable, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetTable.Cells[rowTable, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetTable.Cells[rowTable, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;

                    worksheetTable.Cells[rowTable, 1].Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[item.Key[0]]));
                    worksheetTable.Cells[rowTable, 2].Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[item.Key[0]]));
                    worksheetTable.Cells[rowTable, 3].Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[item.Key[0]]));
                    worksheetTable.Cells[rowTable, 4].Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[item.Key[0]]));

                    worksheetTable.Cells[rowTable, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheetTable.Cells[rowTable, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    rowTable++;
                    columnFreq++;
                    paket = item.Key[0];

                    dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));

                }
                pieExcelChart.SetSize(300, 500);
                pieExcelChart.SetPosition((pieRow + colors.Count + 3) * 10, 19 * 15 + 800);
                pieExcelSeries.Add(worksheetTable.Cells[2, 20, pieRow, 20], worksheetTable.Cells[2, 19, pieRow, 19]);
                Change_3DPieChart_Color(pieExcelChart, colors);
                worksheetTable.Cells.AutoFitColumns();

                ExcelTableToChart(worksheetTable, worksheetChart, worksheetFrakans, Xlabels, progressBar, colors, totalPacket, Ylabels, dataGrid);

                paket = totalPacket.ElementAt(0).Key[0];
                int chartRow = 0;
                int chartColumn = 0;
                for (int i = 2; i < dimValues.Count+2; i++)
                {
                    if (paket != totalPacket.ElementAt(i - 2).Key[0])
                    {
                        chartRow++;
                        chartColumn = 0;
                    }

                    int length = dimValues.ElementAt(i - 2).Value.IndexOf(dimValues.ElementAt(i - 2).Value.Last());
                    worksheetBoyut.Cells[2,i*3-5,length + 5,i*3-5].LoadFromCollection(dimValues.ElementAt(i - 2).Value.ToList().GetRange(0, length));
                    worksheetBoyut.Cells.AutoFitColumns();
                    worksheetBoyut.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    for (int j = 2; j < length + 2; j++)
                    {
                        worksheetBoyut.Cells[j, i * 3 - 4].Value = dimYlabels.ElementAt(i - 2).Value[j - 2];
                        var cell = worksheetBoyut.Cells[j, i * 3 - 4];
                        var border = cell.Style.Border;
                        var fontWeight = cell.Style.Font;
                        fontWeight.Bold = true;

                        border.Top.Style = ExcelBorderStyle.Thick;
                        border.Top.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                        border.Left.Style = ExcelBorderStyle.Thick;
                        border.Left.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                        border.Bottom.Style = ExcelBorderStyle.Thick;
                        border.Bottom.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                        border.Right.Style = ExcelBorderStyle.Thick;
                        border.Right.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                    }
                    var chart = (ExcelLineChart)worksheetBoyutChart.Drawings.AddChart("ChartDim" + i, eChartType.LineMarkers);
                    chart.SetPosition(chartRow * 15, 0, 1 + chartColumn * 13, 0);
                    chart.SetSize(800, 300);
                    var series = chart.Series.Add(worksheetBoyut.Cells[2, i * 3 - 4, length + 1, i * 3 - 4], worksheetBoyut.Cells[2, i * 3 - 5, length + 1, i * 3 - 5]);
                    series.Header = worksheetTable.Cells[i, 2].Text;
                    paket = totalPacket.ElementAt(i - 2).Key[0];
                    chartColumn++;
                    dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
                }                                

                dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Maximum += chartRow * 15 + 10));
                int countChartRow = 0;
                for (int i = 0; i <= chartRow; i++)
                {
                    for (int j = 1; j <= 15; j++)
                    {
                        dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
                        countChartRow++;
                        worksheetBoyutChart.Cells[countChartRow, 1].Value = colors.ElementAt(i).Key;
                        worksheetBoyutChart.Row(countChartRow).Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheetBoyutChart.Row(countChartRow).Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[colors.ElementAt(i).Key]));
                    }

                }
                worksheetBoyutChart.Cells.AutoFitColumns();


                //setDimTable(worksheetTable, worksheetBoyut, worksheetBoyutChart, largestCollection, progressBar, colors, totalPacket, dimYlabels, dataGrid, dimValues);
                //worksheetBoyut.Cells["A2"].LoadFromCollection(largestCollection);
                ////////////////////////////////////////////////////////////////////////////////////////////////

                //for(int k=0; k<dimValues.Count; k++)
                //{
                //    int length = dimValues.ElementAt(k).Value.IndexOf(Xlabels.Last());
                //    var freq = dimYlabels;

                //    worksheetBoyut.Cells["A2"].LoadFromCollection(Xlabels.ToList().GetRange(0, length));


                //    for (int i = 2; i < totalPacket.Count + 2; i++)
                //    {
                //        for (int j = 2; j <= length + 1; j++)
                //        {
                //            int value = freq.ElementAt(i - 2).Value[j - 2];
                //            worksheetBoyut.Cells[j, i].Value = value;
                //            worksheetBoyut.Cells[j, i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                //            var cell = worksheetBoyut.Cells[j, i];
                //            var border = cell.Style.Border;
                //            var fontWeight = cell.Style.Font;
                //            fontWeight.Bold = true;

                //            border.Top.Style = ExcelBorderStyle.Thick;
                //            border.Top.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                //            border.Left.Style = ExcelBorderStyle.Thick;
                //            border.Left.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                //            border.Bottom.Style = ExcelBorderStyle.Thick;
                //            border.Bottom.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                //            border.Right.Style = ExcelBorderStyle.Thick;
                //            border.Right.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));

                //            if (value == 0)
                //            {
                //                worksheetBoyut.Cells[j, i].Style.Fill.PatternType = ExcelFillStyle.Solid;
                //                worksheetBoyut.Cells[j, i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 255, 192, 192));
                //            }
                //        }
                //        dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
                //    }
                //}

                //paket = totalPacket.ElementAt(0).Key[0];
                //int chartRow = 0;
                //int chartColumn = 0;
                //for (int col = 2; col <= worksheetBoyut.Dimension.Columns; col++)
                //{
                //    if (paket != totalPacket.ElementAt(col - 2).Key[0])
                //    {
                //        chartRow++;
                //        chartColumn = 0;
                //    }
                //    if ((int)worksheetBoyut.Cells[2, col, worksheetBoyut.Dimension.End.Row, col].Max(x => x.Value) != 0)
                //    {
                //        var scale = worksheetBoyut.ConditionalFormatting.AddTwoColorScale(worksheetBoyut.Cells[2, col, worksheetBoyut.Dimension.End.Row, col]);
                //        scale.LowValue.Color = System.Drawing.Color.FromArgb(255, 255, 192, 192);
                //        scale.HighValue.Color = System.Drawing.Color.Green;
                //    }
                //    var chart = (ExcelLineChart)worksheetChart.Drawings.AddChart("Chart" + col, eChartType.LineMarkers);
                //    chart.SetPosition(chartRow * 15, 0, 1 + chartColumn * 13, 0);
                //    chart.SetSize(800, 300);
                //    var series = chart.Series.Add(worksheetBoyut.Cells[2, col, worksheetBoyut.Dimension.End.Row, col], worksheetBoyut.Cells[2, 1, worksheetBoyut.Dimension.End.Row, 1]);
                //    series.Header = worksheetTable.Cells[col, 2].Text;
                //    paket = totalPacket.ElementAt(col - 2).Key[0];
                //    chartColumn++;
                //    dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
                //}

                //dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Maximum += chartRow * 15 + 10));
                //int countChartRow = 0;
                //for (int i = 0; i <= chartRow; i++)
                //{
                //    for (int j = 1; j <= 15; j++)
                //    {
                //        dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
                //        countChartRow++;
                //        worksheetChart.Cells[countChartRow, 1].Value = pieChartValues.ElementAt(i).Key;
                //        worksheetChart.Row(countChartRow).Style.Fill.PatternType = ExcelFillStyle.Solid;
                //        worksheetChart.Row(countChartRow).Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[pieChartValues.ElementAt(i).Key]));
                //    }

                //}
                //worksheetChart.Cells.AutoFitColumns();

                ////////////////////////////////////////////////////////////////////////////////////
                dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value = progressBar.Maximum));
                Thread.Sleep(1000);
                if (new FileInfo(@path) != null) package.SaveAs(new FileInfo(@path));
                package.Dispose();
                dataGrid.Dispatcher.Invoke((new Action(() =>
                {
                    progressBar.Visibility = Visibility.Collapsed;
                    progressBar.Value = 0;
                    exportLabel.Visibility = Visibility.Visible;
                })));
                Thread.Sleep(1500);
                dataGrid.Dispatcher.Invoke((new Action(() =>
                {
                    exportLabel.Visibility = Visibility.Collapsed;
                    exportButton.Visibility = Visibility.Visible;
                })));


            });
        }



        public void setDimTable(ExcelWorksheet worksheetMain, ExcelWorksheet worksheetValue, ExcelWorksheet worksheetChart, ObservableCollection<string> Xlabels, ProgressBar progressBar, 
                                Dictionary<string, SolidColorBrush> colors, Dictionary<string[], int[]> totalPacket, Dictionary<string[], ChartValues<int>> Ylabels, DataGrid dataGrid, Dictionary<string[], ObservableCollection<string>> dimValues)
        {
            int length = Xlabels.IndexOf(Xlabels.Last());
            var freq = Ylabels;

            worksheetValue.Cells["A2"].LoadFromCollection(Xlabels.ToList().GetRange(0, length));

            

            for (int i = 2; i < totalPacket.Count + 2; i++)
            {
                for (int j = 2; j <= length + 1; j++)
                {
                    if (dimValues.ElementAt(i - 2).Value.Contains(Xlabels[j - 2]))
                    {
                        int index = dimValues.ElementAt(i - 2).Value.IndexOf(Xlabels[j - 2]);
                        int value = freq.ElementAt(i - 2).Value[index];
                        worksheetValue.Cells[j, i].Value = value;
                        worksheetValue.Cells[j, i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        var cell = worksheetValue.Cells[j, i];
                        var border = cell.Style.Border;
                        var fontWeight = cell.Style.Font;
                        fontWeight.Bold = true;

                        border.Top.Style = ExcelBorderStyle.Thick;
                        border.Top.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                        border.Left.Style = ExcelBorderStyle.Thick;
                        border.Left.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                        border.Bottom.Style = ExcelBorderStyle.Thick;
                        border.Bottom.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                        border.Right.Style = ExcelBorderStyle.Thick;
                        border.Right.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));

                        if (value == 0)
                        {
                            worksheetValue.Cells[j, i].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheetValue.Cells[j, i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 255, 192, 192));
                        }
                    }
                }
                dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
            }
            worksheetValue.Cells.AutoFitColumns();

            string paket = totalPacket.ElementAt(0).Key[0];
            int chartRow = 0;
            int chartColumn = 0;
            for (int col = 2; col <= worksheetValue.Dimension.Columns; col++)
            {
                if (paket != totalPacket.ElementAt(col - 2).Key[0])
                {
                    chartRow++;
                    chartColumn = 0;
                }
                var chart = (ExcelLineChart)worksheetChart.Drawings.AddChart("ChartDim" + col, eChartType.LineMarkers);
                chart.SetPosition(chartRow * 15, 0, 1 + chartColumn * 13, 0);
                chart.SetSize(800, 300);
                var series = chart.Series.Add(worksheetValue.Cells[2, col, worksheetValue.Dimension.End.Row, col], worksheetValue.Cells[2, 1, worksheetValue.Dimension.End.Row, 1]);
                series.Header = worksheetMain.Cells[col, 2].Text;
                paket = totalPacket.ElementAt(col - 2).Key[0];
                chartColumn++;
                dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
            }

            dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Maximum += chartRow * 15 + 10));
            int countChartRow = 0;
            for (int i = 0; i <= chartRow; i++)
            {
                for (int j = 1; j <= 15; j++)
                {
                    dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
                    countChartRow++;
                    worksheetChart.Cells[countChartRow, 1].Value = colors.ElementAt(i).Key;
                    worksheetChart.Row(countChartRow).Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetChart.Row(countChartRow).Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[colors.ElementAt(i).Key]));
                }

            }
            worksheetChart.Cells.AutoFitColumns();
        }
                                                    
        public void ExcelTableToChart(ExcelWorksheet worksheetMain, ExcelWorksheet worksheetChart, ExcelWorksheet worksheetValue, ObservableCollection<string> Xlabels, ProgressBar progressBar,
                                      Dictionary<string, SolidColorBrush> colors, Dictionary<string[], int[]> totalPacket, Dictionary<string[], ChartValues<int>> Ylabels, DataGrid dataGrid)
        {
            int length = Xlabels.IndexOf(Xlabels.Last());
            var freq = Ylabels;

            worksheetValue.Cells["A2"].LoadFromCollection(Xlabels.ToList().GetRange(0, length));

            for (int i = 2; i < totalPacket.Count + 2; i++)
            {
                for (int j = 2; j <= length + 1; j++)
                {
                    int value = freq.ElementAt(i - 2).Value[j - 2];
                    worksheetValue.Cells[j, i].Value = value;
                    worksheetValue.Cells[j, i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    var cell = worksheetValue.Cells[j, i];
                    var border = cell.Style.Border;
                    var fontWeight = cell.Style.Font;
                    fontWeight.Bold = true;

                    border.Top.Style = ExcelBorderStyle.Thick;
                    border.Top.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                    border.Left.Style = ExcelBorderStyle.Thick;
                    border.Left.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                    border.Bottom.Style = ExcelBorderStyle.Thick;
                    border.Bottom.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));
                    border.Right.Style = ExcelBorderStyle.Thick;
                    border.Right.Color.SetColor(ColorConverter(colors[totalPacket.ElementAt(i - 2).Key[0]]));

                    if (value == 0)
                    {
                        worksheetValue.Cells[j, i].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheetValue.Cells[j, i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 255, 192, 192));
                    }
                }
                dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
            }

            string paket = totalPacket.ElementAt(0).Key[0];
            int chartRow = 0;
            int chartColumn = 0;
            for (int col = 2; col <= worksheetValue.Dimension.Columns; col++)
            {
                if (paket != totalPacket.ElementAt(col - 2).Key[0])
                {
                    chartRow++;
                    chartColumn = 0;
                }
                if ((int)worksheetValue.Cells[2, col, worksheetValue.Dimension.End.Row, col].Max(x => x.Value) != 0)
                {
                    var scale = worksheetValue.ConditionalFormatting.AddTwoColorScale(worksheetValue.Cells[2, col, worksheetValue.Dimension.End.Row, col]);
                    scale.LowValue.Color = System.Drawing.Color.FromArgb(255, 255, 192, 192);
                    scale.HighValue.Color = System.Drawing.Color.Green;
                }
                var chart = (ExcelLineChart)worksheetChart.Drawings.AddChart("ChartFreq" + col, eChartType.LineMarkers);
                chart.SetPosition(chartRow * 15, 0, 1 + chartColumn * 13, 0);
                chart.SetSize(800, 300);
                var series = chart.Series.Add(worksheetValue.Cells[2, col, worksheetValue.Dimension.End.Row, col], worksheetValue.Cells[2, 1, worksheetValue.Dimension.End.Row, 1]);
                series.Header = worksheetMain.Cells[col, 2].Text;
                paket = totalPacket.ElementAt(col - 2).Key[0];
                chartColumn++;
                dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
            }

            dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Maximum += chartRow * 15 + 10));
            int countChartRow = 0;
            for (int i = 0; i <= chartRow; i++)
            {
                for (int j = 1; j <= 15; j++)
                {
                    dataGrid.Dispatcher.Invoke(new Action(() => progressBar.Value += 1));
                    countChartRow++;
                    worksheetChart.Cells[countChartRow, 1].Value = colors.ElementAt(i).Key;
                    worksheetChart.Row(countChartRow).Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheetChart.Row(countChartRow).Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[colors.ElementAt(i).Key]));
                }

            }
            worksheetChart.Cells.AutoFitColumns();
        }

        public void Change_3DPieChart_Color(ExcelPieChart pieChart, Dictionary<string, SolidColorBrush> rowColor)
        {
            const string PIE_PATH = "c:chartSpace/c:chart/c:plotArea/c:pie3DChart/c:ser";

            //Get the nodes
            var ws = pieChart.WorkSheet;
            var nsm = ws.Drawings.NameSpaceManager;
            var nschart = nsm.LookupNamespace("c");
            var nsa = nsm.LookupNamespace("a");
            var node = pieChart.ChartXml.SelectSingleNode(PIE_PATH, nsm);
            var doc = pieChart.ChartXml;

            //Add the node
            var rand = new Random();
            for (var i = 0; i < rowColor.Count; i++)
            {
                //Create the data point node
                var dPt = doc.CreateElement("dPt", nschart);

                var idx = dPt.AppendChild(doc.CreateElement("idx", nschart));
                var valattrib = idx.Attributes.Append(doc.CreateAttribute("val"));
                valattrib.Value = i.ToString(CultureInfo.InvariantCulture);
                node.AppendChild(dPt);

                //Add the solid fill node
                var spPr = doc.CreateElement("spPr", nschart);
                var solidFill = spPr.AppendChild(doc.CreateElement("solidFill", nsa));
                var srgbClr = solidFill.AppendChild(doc.CreateElement("srgbClr", nsa));
                valattrib = srgbClr.Attributes.Append(doc.CreateAttribute("val"));

                //Set the color
                var color = ColorConverter(rowColor.ElementAt(i).Value);
                valattrib.Value = System.Drawing.ColorTranslator.ToHtml(color).Replace("#", String.Empty);
                dPt.AppendChild(spPr);
            }
        }

        public System.Drawing.Color LightenColor(SolidColorBrush color, double factor)
        {
            int r = (int)Math.Min(color.Color.R + 255 * factor, 255);
            int g = (int)Math.Min(color.Color.G + 255 * factor, 255);
            int b = (int)Math.Min(color.Color.B + 255 * factor, 255);
            return System.Drawing.Color.FromArgb(r, g, b);
        }
        public System.Drawing.Color ColorConverter(SolidColorBrush brush)
        {
            byte red = brush.Color.R;
            byte green = brush.Color.G;
            byte blue = brush.Color.B;
            return System.Drawing.Color.FromArgb(red, green, blue);
        }

    }
}
