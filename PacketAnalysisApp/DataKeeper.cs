using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using System.Threading;
using System.Globalization;

namespace PacketAnalysisApp
{
    public class DataKeeper
    {
        string extention = ".txt";
        string mainPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PacketAnalysis");
        string packetPath = null;
        string dataPath = null;
        string freqPath = null;
        string dimPath = null;

        public string nowDate = null;
        public string packetName = null;        

        public Dictionary<string, SolidColorBrush> colors;
        public List<string[]> fileNames = new List<string[]>();

        public bool writeFinished = true;

        public void createFile(string path, string header)
        {
            if (fileNames.Count > 0)
            {
                foreach (string[] fileNameArr in fileNames)
                {
                    string fileName = fileNameArr[0] + "_" + fileNameArr[1];
                    using (StreamWriter sw = File.CreateText(Path.Combine(path, fileName + extention)))
                    {
                        sw.WriteLine("ZAMAN," + header);
                    }
                }
            }
        }
        public void CreateDir()
        {
            nowDate = DateTime.Now.ToString("dd/MM/yy") + "--" + DateTime.Now.ToString("HH/mm");
            
            if(!Directory.Exists(mainPath)) 
            {
                Directory.CreateDirectory(mainPath);
            }

            dataPath = Path.Combine(mainPath, "DATA");
            packetPath = Path.Combine(dataPath, packetName);

            string datePath = Path.Combine(packetPath, nowDate);

            freqPath = Path.Combine(datePath, "FREKANS");
            dimPath = Path.Combine(datePath, "BOYUT");

            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(packetPath);
            Directory.CreateDirectory(datePath);
            Directory.CreateDirectory(freqPath);
            Directory.CreateDirectory(dimPath);

            Task.Run(() =>
            {
                createFile(freqPath, "FREKANS");
                createFile(dimPath, "BOYUT");
            });

        }

        public void savedDataMode(string date)
        {
            nowDate = date;
            if (!Directory.Exists(mainPath))
            {
                Directory.CreateDirectory(mainPath);
            }

            dataPath = Path.Combine(mainPath, "DATA");
            packetPath = Path.Combine(dataPath, packetName);

            string datePath = Path.Combine(packetPath, nowDate);

            freqPath = Path.Combine(datePath, "FREKANS");
            dimPath = Path.Combine(datePath, "BOYUT");
        }

        public void writeData(string type, string fileName, List<int> YValues, List<string> XValues)
        {
            var concatList = XValues.Zip(YValues, (time, value) => $"{time}, {value}");
            if (type == "FREKANS")
            {                
                File.AppendAllLines(Path.Combine(freqPath, fileName + extention), concatList);
            }
            else if (type == "BOYUT")
            {
                File.AppendAllLines(Path.Combine(dimPath, fileName + extention), concatList);
            }
        }
        public void writeOneData(string type, string fileName, int YValues, string XValues)
        {
            var concatList = $"{XValues}, {YValues}\n";
            if (type == "FREKANS")
            {
                File.AppendAllText(Path.Combine(freqPath, fileName + extention), concatList);
            }
            else if (type == "BOYUT")
            {                
                File.AppendAllText(Path.Combine(dimPath, fileName + extention), concatList);
            }
        }
        public void setCellStyle(ExcelWorksheet sheet, int row, int col, object value, SolidColorBrush background = null, 
                                 bool fontBold = false, SolidColorBrush borderBrush= null) 
        {            
            sheet.Cells[row, col].Value = value;
            sheet.Cells[row,col].Style.Font.Bold = fontBold;
            sheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            if (background != null)
            {
                sheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(ColorConverter(background));
            }

            if (borderBrush != null)
            {
                var border = sheet.Cells[row, col].Style.Border;

                border.Top.Style = ExcelBorderStyle.Thick;
                border.Top.Color.SetColor(ColorConverter(borderBrush));
                border.Left.Style = ExcelBorderStyle.Thick;
                border.Left.Color.SetColor(ColorConverter(borderBrush));
                border.Bottom.Style = ExcelBorderStyle.Thick;
                border.Bottom.Color.SetColor(ColorConverter(borderBrush));
                border.Right.Style = ExcelBorderStyle.Thick;
                border.Right.Color.SetColor(ColorConverter(borderBrush));
            }
        }

        public void initMainExport(ExcelWorksheet sheet)
        {
        } 

        public delegate void ExportFinishedEventHandler();
        public event ExportFinishedEventHandler ExportFinished;
        public async void mainExport(Dictionary<string[], int[]> totalPacket, string savePath, Image loading,
                                     Button exportButton, Label exportLabel)
        {
            writeFinished = false;
            string initfileName = totalPacket.ElementAt(0).Key[0] + "_" + totalPacket.ElementAt(0).Key[1];
            //progressBar.Maximum = File.ReadAllLines(Path.Combine(dimPath, initfileName + extention)).Length*totalPacket.Count*1.2;
            //progressBar.Value = 0;

            var package = new ExcelPackage();
            var worksheetTable = package.Workbook.Worksheets.Add("Genel Tablo");
            var worksheetFrekans = package.Workbook.Worksheets.Add("Frakans Tablosu");
            var worksheetChart = package.Workbook.Worksheets.Add("Frekans Grafikleri");
            var worksheetBoyut = package.Workbook.Worksheets.Add("Boyut Tablosu");
            var worksheetBoyutChart = package.Workbook.Worksheets.Add("Boyut Grafikleri");

            ExcelPieChart pieExcelChart = (ExcelPieChart)worksheetTable.Drawings.AddChart("pieChart", eChartType.PieExploded3D);
            var pieExcelSeries = pieExcelChart.Series;
            pieExcelChart.DataLabel.ShowPercent = true;
            pieExcelChart.DataLabel.ShowValue = true;
            pieExcelChart.DataLabel.ShowLegendKey = true;

            setCellStyle(worksheetTable, 1, 1, "PAKET ADI", Brushes.LightGray);
            setCellStyle(worksheetTable, 1, 2, "PROJE ADI", Brushes.LightGray);
            setCellStyle(worksheetTable, 1, 3, "TOPLAM GELEN PAKET", Brushes.LightGray);
            setCellStyle(worksheetTable, 1, 4, "TOPLAM GELEN BOYUT", Brushes.LightGray);

            setCellStyle(worksheetTable, 1, 19, "PAKET ADI", Brushes.LightGray);
            setCellStyle(worksheetTable, 1, 20, "TOPLAM GELEN PAKET SAYISI", Brushes.LightGray);


            int rowTable = 2;
            int columnFreq = 2;
            int barChartPos = 2;
            string paket = "";
            int pieRow = 1;

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

                    var barChart = worksheetTable.Drawings.AddChart(item.Key[0], eChartType.ColumnClustered);
                    barChart.SetPosition(barChartPos - 1, 0, 4, 0);
                    var series = barChart.Series.Add(worksheetTable.Cells[barChartPos, 3, rowTable + count - 1, 3], worksheetTable.Cells[barChartPos, 2, rowTable + count - 1, 2]);
                    series.Fill.Color = ColorConverter(colors[item.Key[0]]);
                    series.Header = item.Key[0];
                    barChart.SetSize(800, (rowTable - barChartPos + count) * 20);

                    setCellStyle(worksheetTable, pieRow, 19, item.Key[0], colors[item.Key[0]], true);
                    setCellStyle(worksheetTable, pieRow, 20, null, colors[item.Key[0]], true);

                    worksheetTable.Cells[pieRow, 20].Formula = "Sum(" + worksheetTable.Cells[barChartPos, 3].Address +
                                                            ":" + worksheetTable.Cells[rowTable + count - 1, 3].Address + ")";                   

                    barChartPos = rowTable + count;
                }

                string keyConcatenated = item.Key[0] + "_" + item.Key[1];

                setCellStyle(worksheetFrekans, 1, columnFreq, keyConcatenated, colors[item.Key[0]], true);
                setCellStyle(worksheetBoyut, 1, columnFreq * 3 - 4, keyConcatenated, colors[item.Key[0]], true);

                setCellStyle(worksheetTable, rowTable, 1, item.Key[0], colors[item.Key[0]],true);
                setCellStyle(worksheetTable, rowTable, 2, item.Key[1], colors[item.Key[0]], true);
                setCellStyle(worksheetTable, rowTable, 3, item.Value[1], colors[item.Key[0]], true);
                setCellStyle(worksheetTable, rowTable, 4, item.Value[3], colors[item.Key[0]], true);

                rowTable++;
                columnFreq++;
                paket = item.Key[0];
                
                //progressBar.Value += 1;
            }

            pieExcelChart.SetSize(300, 500);
            pieExcelChart.SetPosition((pieRow + colors.Count + 3) * 10, 19 * 15 + 800);
            pieExcelSeries.Add(worksheetTable.Cells[2, 20, pieRow, 20], worksheetTable.Cells[2, 19, pieRow, 19]);
            Change_3DPieChart_Color(pieExcelChart, colors);
          
            await Task.Run(() =>
            {
                setTable("BOYUT", worksheetBoyut, worksheetBoyutChart, totalPacket);
                setTable("FREKANS", worksheetFrekans, worksheetChart, totalPacket);
            });

            worksheetBoyut.Cells.AutoFitColumns();
            worksheetTable.Cells.AutoFitColumns();
            worksheetFrekans.Cells.AutoFitColumns();
            worksheetBoyutChart.Cells.AutoFitColumns();
            worksheetChart.Cells.AutoFitColumns();

            if(savePath != null)
            {
                if (new FileInfo(@savePath) != null)
                {
                    package.SaveAs(new FileInfo(@savePath));
                }
            }


            package.Dispose();

            //await Task.Delay(1000);
            loading.Visibility = Visibility.Collapsed;
            //progressBar.Value = 0;
            exportLabel.Visibility = Visibility.Visible;
            await Task.Delay(1000);

            exportLabel.Visibility = Visibility.Collapsed;
            exportButton.Visibility = Visibility.Visible;

            writeFinished = true;            
            ExportFinished?.Invoke();
        }

        public void setTable(string type, ExcelWorksheet valueSheet, ExcelWorksheet chartSheet, Dictionary<string[], int[]> totalPacket)
        {

            string typePath = (type == "BOYUT") ? dimPath : freqPath;
            string paket = totalPacket.ElementAt(0).Key[0];
            int chartRow = 0;
            int chartColumn = 0;

            int timeCol = 1;
            int valueCol = 1;
            for (int i = 2; i < totalPacket.Count + 2; i++)
            {
                int row = 2;
                if (paket != totalPacket.ElementAt(i - 2).Key[0])
                {
                    chartRow++;
                    chartColumn = 0;
                }

                string fileName = totalPacket.ElementAt(i - 2).Key[0] + "_" + totalPacket.ElementAt(i - 2).Key[1];

                using (StreamReader sr = new StreamReader(Path.Combine(typePath, fileName + extention)))
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

                            timeCol = (type == "BOYUT") ? i * 3 - 5 : 1;
                            valueCol = (type == "BOYUT") ? i * 3 - 4 : i;
                            setCellStyle(valueSheet, row, timeCol, data[0], null, true, colors[totalPacket.ElementAt(i - 2).Key[0]]);
                            setCellStyle(valueSheet, row, valueCol, value, null, true, colors[totalPacket.ElementAt(i - 2).Key[0]]);

                            if ((int)value == 0)
                            {
                                valueSheet.Cells[row, valueCol].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                valueSheet.Cells[row, valueCol].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 255, 192, 192));
                            }
                        }

                        row++;
                    }
                }
                var chart = (ExcelLineChart)chartSheet.Drawings.AddChart("ChartDim" + i, eChartType.LineMarkers);
                chart.SetPosition(chartRow * 15, 0, 1 + chartColumn * 13, 0);
                chart.SetSize(800, 300);
                var series = chart.Series.Add(valueSheet.Cells[2, valueCol, row + 1, valueCol], valueSheet.Cells[2, timeCol, row + 1, timeCol]);
                series.Header = totalPacket.ElementAt(i - 2).Key[1];
                paket = totalPacket.ElementAt(i - 2).Key[0];
                chartColumn++;
            }

            int countChartRow = 0;
            for (int i = 0; i <= chartRow; i++)
            {
                for (int j = 1; j <= 15; j++)
                {
                    //progressBar.Value += 1;
                    countChartRow++;
                    chartSheet.Cells[countChartRow, 1].Value = colors.ElementAt(i).Key;
                    chartSheet.Row(countChartRow).Style.Fill.PatternType = ExcelFillStyle.Solid;
                    chartSheet.Row(countChartRow).Style.Fill.BackgroundColor.SetColor(ColorConverter(colors[colors.ElementAt(i).Key]));
                }
            }

        }

        public void ChartExport(string type, string fileName, string savePath)
        {

            //************ EXCEL TANIMLAMALARI ********************
            string typePath = (type == "FREKANS") ? freqPath : dimPath;
            var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(type + " GRAFİĞİ");

            //************ EXPORT İÇİN KOMPONENETLERİN TANIMLANMASI ********************

            //************ EXCEL CHART TANIMLAMALARI ********************
            var chart = (ExcelLineChart)worksheet.Drawings.AddChart("Chart", eChartType.LineMarkers);
            chart.SetPosition(3 * 20, 9 * 20);
            chart.SetSize(1500, 300);
            chart.Name = fileName;

            //************ DOSYA OKUNMASI VE EXCELE YAZILMASI ********************
            setCellStyle(worksheet, 1, 4, "TOPLAM", Brushes.LightGray);
            using (StreamReader sr = new StreamReader(Path.Combine(typePath, fileName + extention)))
            {
                int row = 1;
                while (!sr.EndOfStream)
                {
                    string[] data = sr.ReadLine().Split(',');
                    object value = null;
                    try
                    {
                        value = int.Parse(data[1]);
                    }
                    catch
                    {
                        value = data[1];
                    }
                    setCellStyle(worksheet, row, 1, data[0]);
                    setCellStyle(worksheet, row, 2, value);

                    row++;
                }
            }

            worksheet.Cells.AutoFitColumns();
            int end = (2 < worksheet.Dimension.End.Row) ? worksheet.Dimension.End.Row : 2;
            var scale = worksheet.ConditionalFormatting.AddTwoColorScale(worksheet.Cells[2, 2, end, 2]);
            scale.LowValue.Color = System.Drawing.Color.FromArgb(255, 255, 192, 192);
            scale.HighValue.Color = System.Drawing.Color.Green;

            //************ EXCEL CHART DEĞER GİRİMİ ********************
            chart.Series.Add(worksheet.Cells[2, 2, worksheet.Dimension.End.Row, 2], worksheet.Cells[2, 1, end, 1]);
            chart.Series[0].Header = fileName;

            //************ EXCEL TOPLAM SÜTUNU AYARLANMASI ********************
            worksheet.Cells[2, 4].Formula = "Sum(" + worksheet.Cells[2, 2].Address +
                ":" + worksheet.Cells[worksheet.Dimension.End.Row, 2].Address + ")";
            worksheet.Cells[2, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[2, 4].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.Green));

            //************ EXCEL DOSYANIN KAYDEDİLMESİ ********************
            if (new FileInfo(@savePath) != null)
            {
                package.SaveAs(new FileInfo(@savePath));
            }

            //************ EXCEL DOSYANIN KAYDEDİLDİĞİNİ GÖSTEREN ANİMASYON ********************

            package.Dispose();

        }

        public async void readData(string type, string fileName, string savePath, StackPanel ExportPanel)
        {
            //************ EXCEL TANIMLAMALARI ********************
            string typePath = (type == "FREKANS") ? freqPath : dimPath;
            var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(type + " GRAFİĞİ");

            //************ EXPORT İÇİN KOMPONENETLERİN TANIMLANMASI ********************
            Label title = (Label)ExportPanel.Children[0];
            title.Visibility = Visibility.Collapsed;

            ProgressBar chartExportProgress = (ProgressBar)ExportPanel.Children[1];
            chartExportProgress.Visibility = Visibility.Visible;            
            chartExportProgress.Minimum = 0;
            chartExportProgress.Value = 0;
            chartExportProgress.Height = 5;
            chartExportProgress.Margin = new Thickness(20, 5, 20, 0);
            chartExportProgress.Maximum = File.ReadAllLines(Path.Combine(typePath, fileName + extention)).Length;

            Label chartExportLabel = (Label)ExportPanel.Children[2];

            //************ EXCEL CHART TANIMLAMALARI ********************
            var chart = (ExcelLineChart)worksheet.Drawings.AddChart("Chart", eChartType.LineMarkers);
            chart.SetPosition(3 * 20, 9 * 20);
            chart.SetSize(1500, 300);
            chart.Name = fileName;

            //************ DOSYA OKUNMASI VE EXCELE YAZILMASI ********************
            setCellStyle(worksheet, 1, 4, "TOPLAM", Brushes.LightGray);
            using (StreamReader sr = new StreamReader(Path.Combine(typePath, fileName + extention)))
            {
                int row = 1;
                while (!sr.EndOfStream)
                {
                    chartExportProgress.Value = row;
                    string[] data = sr.ReadLine().Split(',');
                    object value = null;
                    try
                    {
                        value = int.Parse(data[1]);
                    }
                    catch
                    {
                        value = data[1];
                    } 
                    setCellStyle(worksheet, row, 1, data[0]);
                    setCellStyle(worksheet, row, 2, value);

                    row++;
                }
            }

            worksheet.Cells.AutoFitColumns();
            var scale = worksheet.ConditionalFormatting.AddTwoColorScale(worksheet.Cells[2, 2, worksheet.Dimension.End.Row, 2]);
            scale.LowValue.Color = System.Drawing.Color.FromArgb(255, 255, 192, 192);
            scale.HighValue.Color = System.Drawing.Color.Green;

            //************ EXCEL CHART DEĞER GİRİMİ ********************
            chart.Series.Add(worksheet.Cells[2, 2, worksheet.Dimension.End.Row, 2], worksheet.Cells[2, 1, worksheet.Dimension.End.Row, 1]);
            chart.Series[0].Header = fileName;

            //************ EXCEL TOPLAM SÜTUNU AYARLANMASI ********************
            worksheet.Cells[2, 4].Formula = "Sum(" + worksheet.Cells[2, 2].Address +
                ":" + worksheet.Cells[worksheet.Dimension.End.Row , 2].Address + ")";
            worksheet.Cells[2, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[2, 4].Style.Fill.BackgroundColor.SetColor(ColorConverter(Brushes.Green));

            //************ EXCEL DOSYANIN KAYDEDİLMESİ ********************
            if (new FileInfo(@savePath) != null)
            {
                package.SaveAs(new FileInfo(@savePath));
            }

            //************ EXCEL DOSYANIN KAYDEDİLDİĞİNİ GÖSTEREN ANİMASYON ********************
            await Task.Delay(1000);
            chartExportProgress.Visibility = Visibility.Collapsed;
            chartExportProgress.Value = 0;
            chartExportLabel.Visibility = Visibility.Visible;
            await Task.Delay(1000);
            chartExportLabel.Visibility = Visibility.Collapsed;
            title.Visibility = Visibility.Visible;

            package.Dispose();
        }

        public System.Drawing.Color ColorConverter(SolidColorBrush brush)
        {
            if (brush == null)
            {
               return ColorConverter(Brushes.White);
            }
            else
            {
                byte red = brush.Color.R;
                byte green = brush.Color.G;
                byte blue = brush.Color.B;
                return System.Drawing.Color.FromArgb(red, green, blue);
            }
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
    }
}
