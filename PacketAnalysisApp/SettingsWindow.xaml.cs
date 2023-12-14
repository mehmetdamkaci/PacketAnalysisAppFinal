using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LiveCharts.Wpf;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Security.Principal;
using System.Threading;
using static NetMQ.NetMQSelector;

namespace PacketAnalysisApp
{
    /// <summary>
    /// SettingsWindow.xaml etkileşim mantığı
    /// </summary>
    /// 
    public class CONFIG
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int chartLength { get; set; }
        public int bufferLength { get; set; }
        public Dictionary<string, int> Freq { get; set; }
        public Dictionary<string, int> Dim { get; set; }
    }

    public partial class SettingsWindow : Window
    {
        public bool disconnect = false;

        public string[] _packetProje;
        public string packetName;
        public string packetPath;
        public int lenChart;
        public int lenBuffer;
        public string jsonPath = "PacketConfig.json";
        public string nowDate;
        public CONFIG configData;
        
        public Dictionary<string, Dictionary<int, string>> enumStruct;
        public Dictionary<string[], int> expectedFreq = new Dictionary<string[], int>(new StringArrayComparer());
        public Dictionary<string[], int> expectedDim = new Dictionary<string[], int>(new StringArrayComparer());
        public Dictionary<string[], int[]> mergeExpected;
        public Dictionary<string, SolidColorBrush> colors;


        Dictionary<string[], int[]> mergedExpectedDict;
        Dictionary<string, string> matchedEnums;
        string enumPath = string.Empty;
        string enumText = null;

        TextBox freqBox;
        TextBox dimBox;

        string[] iconNames = { "edit" , "tick"};
        string initIconName = "edit";
        
        
        public SettingsWindow(string jsonPath = "PacketConfig.json")
        {
            InitializeComponent();
            this.jsonPath = jsonPath;
            string json = File.ReadAllText(jsonPath);
            configData = JsonConvert.DeserializeObject<CONFIG>(json);
            packetName = configData.Name;
            packetPath = configData.Path;
            lenChart = configData.chartLength;
            lenBuffer = configData.bufferLength;
            enumText = File.ReadAllText(packetPath);

            packetNameLabel.Content = packetName;
            chartLengthBox.Text = lenChart.ToString();
            bufferLengthBox.Text = lenBuffer.ToString();
            ProcessEnumCode(enumText, false);
            InitExpectedValue();

        }

        public void CopyConfigFile() 
        {
            string configPath = Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"), "PacketAnalysis\\DATA\\" + packetName + "\\" + nowDate + "\\PacketConfig.json");
            if (File.Exists(configPath)) File.Copy(jsonPath, configPath, true);
            else File.Copy(jsonPath, configPath);
        }

        public void InitIcon()
        {
            BitmapImage addEnumSource = new BitmapImage();
            addEnumSource.BeginInit();
            addEnumSource.UriSource = new Uri((Path.Combine(Environment.CurrentDirectory, "download2.png")));
            addEnumSource.EndInit();
            addEnumIcon.Source = addEnumSource;

            foreach (var listViewItem in projectListView.Items.Cast<object>().Select(b =>
                     projectListView.ItemContainerGenerator.ContainerFromItem(b) as ListViewItem))
            {
                Image editImage = FindVisualChild<Image>(listViewItem, "edit");
                BitmapImage editSource = new BitmapImage();
                editSource.BeginInit();
                editSource.UriSource = new Uri((Path.Combine(Environment.CurrentDirectory, "edit.png")));
                editSource.EndInit();
                editImage.Source = editSource;
            }

        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            GridView gView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;
            var col1 = 0.28;
            var col2 = 0.28;
            var col3 = 0.17;
            var col4 = 0.16;
            var col5 = 0.11;

            if(workingWidth > 0)
            {
                gView.Columns[0].Width = workingWidth * col1;
                gView.Columns[1].Width = workingWidth * col2;
                gView.Columns[2].Width = workingWidth * col3;
                gView.Columns[3].Width = workingWidth * col4;
                gView.Columns[4].Width = workingWidth * col5;
            }
            //InitIcon();
            //setColor();
            InitIcon();
        }


        private void ProjectsView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            GridView gView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth; // take into account vertical scrollbar

            if(workingWidth>0) gView.Columns[0].Width = workingWidth;
        }

        //public delegate void UpdateExpectedHandler();
        //public event UpdateExpectedHandler UpdateExpected;        
        public void showUpdateListView()
        {
            for(int i = 0; i<mergedExpectedDict.Count; i++) 
            {
                string freqName = configData.Freq.ElementAt(i).Key;
                int freqValue = configData.Freq.ElementAt(i).Value;
                int dimValue = configData.Dim.ElementAt(i).Value;
                mergedExpectedDict.ElementAt(i).Value[0] = freqValue;
                mergedExpectedDict.ElementAt(i).Value[1] = dimValue;                
            }
            projectListView.Items.Refresh();            
        }

        public void InitExpectedValue()
        {
            expectedFreq.Clear();
            expectedDim.Clear();
            for (int i = 0; i < configData.Freq.Count; i++)
            {
                string freqName = configData.Freq.ElementAt(i).Key;
                int freqValue = configData.Freq.ElementAt(i).Value;
                int dimValue = configData.Dim.ElementAt(i).Value;

                expectedDim.Add(freqName.Split('.'), dimValue);
                expectedFreq.Add(freqName.Split('.'), freqValue);
            }

            mergedExpectedDict = expectedFreq.Zip(expectedDim, (kv1, kv2) => new KeyValuePair<string[], int[]>(kv1.Key, new int[] { kv1.Value, kv2.Value }))
                                             .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, new StringArrayComparer());

            ((GridView)projectListView.View).Columns[0].DisplayMemberBinding = new Binding("Key[0]");
            ((GridView)projectListView.View).Columns[1].DisplayMemberBinding = new Binding("Key[1]");

            projectListView.ItemsSource = mergedExpectedDict.ToList();
            projectsList.ItemsSource = enumStruct[packetName].Values.ToList();

        }

        public delegate void ChartUpdatingEventHandler (object sender, RoutedEventArgs e);
        public event ChartUpdatingEventHandler ChartUpdating;
        private void ChartSettingButtonClicked(object sender, RoutedEventArgs e)
        {
            lenBuffer = Convert.ToInt32(bufferLengthBox.Text);
            lenChart = Convert.ToInt32(chartLengthBox.Text);

            configData.chartLength = lenChart;
            configData.bufferLength = lenBuffer;

            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(configData, Formatting.Indented));
            string configPath = Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"), "PacketAnalysis\\DATA\\" + packetName + "\\" + nowDate + "\\PackageConfig.json");
            File.Copy(jsonPath, configPath, true);

            ChartUpdating?.Invoke(sender, e);

        }

        public delegate void DisconnectEventHandler(object sender, RoutedEventArgs e);
        public event DisconnectEventHandler DisconnectEvent;
        private void AddEnumFile_Clicked(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
            openFileDlg.Filter = "C# File|*.cs";

            Nullable<bool> result = openFileDlg.ShowDialog();

            if (result == true & disconnect)
            {
                enumPath = openFileDlg.FileName;
                enumText = File.ReadAllText(enumPath);
                ProcessEnumCode(enumText, true);
                expectedGrid.Visibility = Visibility.Collapsed;
            }
            else 
            {
                if (result == true) 
                {
                    DisconnectEvent?.Invoke(sender, e);
                    enumPath = openFileDlg.FileName;
                    enumText = File.ReadAllText(enumPath);
                    ProcessEnumCode(enumText, true);
                    expectedGrid.Visibility = Visibility.Collapsed;
                } 
                return;
            } 
        }        

        public void ProcessEnumCode(string enumFileText, bool privMatched)
        {
            if (enumStruct != null) enumStruct.Clear();
            else enumStruct = new Dictionary<string, Dictionary<int, string>>();

            if (expectedFreq != null) enumStruct.Clear();
            else expectedFreq = new Dictionary<string[], int>();

            if (expectedFreq != null) enumStruct.Clear();
            else expectedDim = new Dictionary<string[], int>();

            var syntaxTree = CSharpSyntaxTree.ParseText(enumFileText);
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
            };

            var compilation = CSharpCompilation.Create("DynamicEnumCompilation")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTree);

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    MessageBox.Show("Derleme hatası:\n" + string.Join("\n", result.Diagnostics));
                    return;
                }

                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());

                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsEnum)
                    {
                        enumStruct[type.Name] = new Dictionary<int, string>();

                        List<string> enumValue = new List<string>();
                        foreach (var value in Enum.GetValues(type))
                        {
                            enumStruct[type.Name][(int)value] = value.ToString();
                            enumValue.Add(value.ToString());
                        }
                    }
                }    
            }

            projectsList.ItemsSource = null;

            if (privMatched)
            {
                CreateEnumMatchGrid();                
            }
            
        }

        public void CreateEnumMatchGrid()
        {
            bool completed = false;
            StackPanel matchPanel = new StackPanel();
            matchPanel.Orientation = Orientation.Vertical;
            matchPanel.HorizontalAlignment = HorizontalAlignment.Center;

            Label messageLabel = new Label();
            messageLabel.Content = "Paketlerin Bulunduğu Enumı, Üzerine Tıklayarak Seçiniz";
            messageLabel.FontSize = 16;
            messageLabel.Margin = new Thickness(0, 10, 0, 30);
            messageLabel.HorizontalAlignment = HorizontalAlignment.Center;
            messageLabel.Foreground = Brushes.White;
            messageLabel.Background = Brushes.Black;
            matchPanel.Children.Add(messageLabel);

            ScrollViewer scrollViewer = new ScrollViewer();
            StackPanel mainEnumPanel = new StackPanel();
            mainEnumPanel.Orientation = Orientation.Horizontal;
            for (int i = 0; i< 3; i++)
            {
                StackPanel enumPanel = new StackPanel();
                enumPanel.Orientation = Orientation.Vertical;
                for(int j = 0; j < enumStruct.Count / 3 + 1 ; j++)
                {
                    Border enumPanelBorder = new Border();
                    enumPanelBorder.Margin = new Thickness(0, 0, 40, 40);
                    enumPanelBorder.BorderThickness = new Thickness(4);
                    enumPanelBorder.BorderBrush = Brushes.Transparent;
                    enumPanelBorder.Width = 200;
                    enumPanelBorder.Background = Brushes.Transparent;
                    enumPanelBorder.HorizontalAlignment = HorizontalAlignment.Center;

                    StackPanel listBoxPanel = new StackPanel();
                    listBoxPanel.Orientation = Orientation.Vertical;
                    listBoxPanel.Margin = new Thickness(0, 0, 0, 0);
                    enumPanelBorder.Child = listBoxPanel;

                    Button enumButton = new Button();
                    enumButton.Content = enumStruct.ElementAt(i*(enumStruct.Count / 3 + 1) +j).Key;
                    enumButton.Name = (string)enumButton.Content;
                    enumButton.BorderThickness = new Thickness(2.5);
                    enumButton.BorderBrush = Brushes.Black;
                    enumButton.Background = Brushes.LightGray;
                    enumButton.Foreground = Brushes.Black;
                    enumButton.FontWeight = FontWeights.Bold;                    
                    enumButton.Width = 200;
                    enumButton.Height = 30;
                    enumButton.Click += EnumButtonClicked;
                        
                    ListBox enumElementList = new ListBox();
                    enumElementList.Margin = new Thickness(0, 10, 0, 0);
                    enumElementList.FontWeight = FontWeights.Bold;
                    enumElementList.Background = Brushes.LightGray;
                    enumElementList.BorderThickness = BorderThickness = new Thickness(0,4,0,4);
                    enumElementList.BorderBrush = Brushes.LightGreen;
                    enumElementList.ItemsSource = enumStruct.ElementAt(i * (enumStruct.Count / 3 + 1) + j).Value;
                    enumElementList.Visibility = Visibility.Collapsed;                    

                    Button selectedPacketEnum = new Button();
                    selectedPacketEnum.Margin = new Thickness(0, 10, 0, 20);
                    selectedPacketEnum.Content = "Seç";
                    selectedPacketEnum.BorderThickness = new Thickness(2);
                    selectedPacketEnum.HorizontalAlignment = HorizontalAlignment.Center;
                    selectedPacketEnum.FontWeight = FontWeights.Bold;
                    selectedPacketEnum.Width = 50;
                    selectedPacketEnum.Visibility = Visibility.Collapsed;
                    selectedPacketEnum.Click += SelectedPacketEnumClicked;

                    listBoxPanel.Children.Add(enumButton);
                    listBoxPanel.Children.Add(enumElementList);
                    listBoxPanel.Children.Add(selectedPacketEnum);
                    enumPanelBorder.Child = listBoxPanel;
                    enumPanel.Children.Add(enumPanelBorder);

                    if (i * (enumStruct.Count / 3 + 1) + j == enumStruct.Count - 1) 
                    {
                        completed = true;
                        break;
                    } 

                }                
                mainEnumPanel.Children.Add(enumPanel);
                if(completed) break;
            }
            matchPanel.Children.Add(mainEnumPanel);
            scrollViewer.Content = matchPanel;
            matchAndExpectedGrid.Children.Add(scrollViewer);

        }

        private void BackButtonClicked(object sender, RoutedEventArgs e)
        {          
            
            if (((Button)sender).Name == "backGrid")
            {
                backButton.Name = "backMatch";
                matchAndExpectedGrid.Children[3].Visibility = Visibility.Collapsed;
                matchAndExpectedGrid.Children[2].Visibility = Visibility.Visible;
            }
            else
            {
                matchAndExpectedGrid.Children[2].Visibility = Visibility.Collapsed;
                matchAndExpectedGrid.Children[1].Visibility = Visibility.Visible;
                backButton.Visibility = Visibility.Collapsed;
            }

        }

        private void SelectedPacketEnumClicked(object sender, RoutedEventArgs e)
        {
            if (matchAndExpectedGrid.Children.Count >= 3) matchAndExpectedGrid.Children.RemoveAt(2);

            Button clickedSelectedButton = (Button)sender;
            ScrollViewer scrollViewer = FindVisualParent<ScrollViewer>(clickedSelectedButton);
            StackPanel stackPanel = FindVisualParent<StackPanel>(clickedSelectedButton);
            packetName =  ((Button)stackPanel.Children[0]).Name;
            packetNameLabel.Content = packetName;

            ScrollViewer matchScroll = new ScrollViewer();            

            StackPanel enumMatchingPanel = new StackPanel();            
            enumMatchingPanel.Orientation = Orientation.Vertical;
            enumMatchingPanel.HorizontalAlignment = HorizontalAlignment.Center;

            Label msgPacketNameLabel = new Label();
            msgPacketNameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            msgPacketNameLabel.VerticalAlignment = VerticalAlignment.Top;
            msgPacketNameLabel.Foreground = Brushes.White;
            msgPacketNameLabel.FontSize = 16;
            msgPacketNameLabel.Margin = new Thickness(0, 10, 0, 20);
            msgPacketNameLabel.Content = "Paket Enumı Olarak " + packetName + " Seçildi. Enum Eşleştirmelerini Yapınız.";
            msgPacketNameLabel.Background = Brushes.Black;            

            enumMatchingPanel.Children.Add(msgPacketNameLabel);

            scrollViewer.Visibility = Visibility.Collapsed;
            backButton.Visibility= Visibility.Visible;

            for (int i = 0; i < enumStruct[packetName].Count; i++)
            {
                StackPanel comboBoxPanel = new StackPanel();
                comboBoxPanel.Orientation = Orientation.Horizontal;
                comboBoxPanel.HorizontalAlignment= HorizontalAlignment.Center;

                Label enumNameLabel = new Label();
                enumNameLabel.Content = enumStruct[packetName].Values.ElementAt(i).ToString();
                enumNameLabel.Background = Brushes.LightGray;
                enumNameLabel.Foreground = Brushes.Black;
                enumNameLabel.BorderThickness = new Thickness(1);
                enumNameLabel.BorderBrush = Brushes.Black;
                enumNameLabel.FontSize = 14;
                enumNameLabel.FontWeight = FontWeights.Bold;
                enumNameLabel.Width = 250;

                comboBoxPanel.Children.Add(enumNameLabel);

                ComboBox enumBox = new ComboBox();
                enumBox.Name = enumStruct[packetName].Values.ElementAt(i).ToString();
                enumBox.VerticalContentAlignment = VerticalAlignment.Center;
                enumBox.FontWeight = FontWeights.Bold;
                enumBox.BorderBrush = Brushes.Red;
                enumBox.BorderThickness = new Thickness(2);                
                enumBox.Width = 250;
                enumBox.SelectionChanged += EnumBoxSelectionChanged;                

                for(int j = 0; j< enumStruct.Count; j++)
                {
                    if (enumStruct.ElementAt(j).Key != packetName)
                    {
                        enumBox.Items.Add(enumStruct.ElementAt(j).Key);
                    }
                }

                enumBox.Items.Add("");
                comboBoxPanel.Children.Add(enumBox);

                enumMatchingPanel.Children.Add(comboBoxPanel);
            }


            Button matchingSaveButton = new Button();
            matchingSaveButton.Content = "Kaydet";
            matchingSaveButton.VerticalAlignment = VerticalAlignment.Bottom;
            matchingSaveButton.Width = 80;
            matchingSaveButton.Height = 25;
            matchingSaveButton.Margin = new Thickness(0, 20, 0, 30);
            matchingSaveButton.Click += MatchingSaveButtonClicked;

            enumMatchingPanel.Children.Add(matchingSaveButton);
            matchScroll.Content = enumMatchingPanel;

            backButton.Name = "backMatch";

            if (matchAndExpectedGrid.Children.Count == 3) matchAndExpectedGrid.Children.Insert(2, matchScroll);
            else matchAndExpectedGrid.Children.Add(matchScroll);
        }

        private void MatchingSaveButtonClicked(object sender, RoutedEventArgs e)
        {
            if (matchAndExpectedGrid.Children.Count == 4) matchAndExpectedGrid.Children.RemoveAt(3);
            matchAndExpectedGrid.Children[2].Visibility = Visibility.Collapsed;

            Button saveButton = sender as Button;
            StackPanel stackPanel = FindVisualParent<StackPanel>(saveButton);

            StackPanel dataGridPanel = new StackPanel();
            dataGridPanel.Orientation = Orientation.Vertical;

            Label gridLabel = new Label();
            gridLabel.Content = "ENUM EŞLTEŞTİRMELERİ";
            gridLabel.FontSize = 16;
            gridLabel.Margin = new Thickness(0, 10, 0, 20);
            gridLabel.HorizontalAlignment = HorizontalAlignment.Center;
            gridLabel.Foreground = Brushes.White;
            gridLabel.Background = Brushes.Black;
            gridLabel.FontWeight = FontWeights.Bold;
            dataGridPanel.Children.Add(gridLabel);

            DataGrid matchGrid = new DataGrid();
            matchGrid.HorizontalAlignment = HorizontalAlignment.Center;
            matchGrid.VerticalAlignment = VerticalAlignment.Center;
            matchGrid.AutoGenerateColumns = false;
            matchGrid.AlternatingRowBackground = Brushes.LightGray;
            matchGrid.Margin = new Thickness(50,0,50,0);
            matchGrid.RowHeight = 30;
            matchGrid.FontWeight = FontWeights.Bold;
            matchGrid.FontSize = 14;

            DataGridTextColumn enumName = new DataGridTextColumn
            {
                Header = "ENUM İSMİ",
                Binding = new Binding("Key")
                {
                    Converter = new EnumMatchedGridConverter(packetName)
                }                
            };
            DataGridTextColumn enumValue = new DataGridTextColumn
            {
                Header = "EŞLEŞTİĞİ ENUM",
                Binding = new Binding("Value")
            };

            enumName.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            enumValue.Width = new DataGridLength(1 * 1, DataGridLengthUnitType.Star);

            enumName.HeaderStyle = new Style(typeof(DataGridColumnHeader));
            enumName.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, Brushes.LightGray));
            enumName.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            enumName.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center)); 


            enumValue.HeaderStyle = new Style(typeof(DataGridColumnHeader));
            enumValue.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, Brushes.LightGray));
            enumValue.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            enumValue.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));

            matchGrid.Columns.Add(enumName);
            matchGrid.Columns.Add(enumValue);

            matchedEnums = new Dictionary<string, string>();
            for (int i = 1; i < stackPanel.Children.Count - 1; i++)
            {                
                ComboBox comboBox = ((ComboBox)((StackPanel)stackPanel.Children[i]).Children[1]);
                matchedEnums.Add(comboBox.Name.Replace("\n", ""), comboBox.Text.Replace("\n", ""));                
            }

            matchGrid.ItemsSource = matchedEnums.ToList();

            Button saveGridButton = new Button();
            saveGridButton.Content = "Kaydet";
            saveGridButton.VerticalAlignment = VerticalAlignment.Bottom;
            saveGridButton.Width = 80;
            saveGridButton.Height = 25;
            saveGridButton.Margin = new Thickness(0, 20, 0, 30);
            saveGridButton.Click += saveGridButtonClicked;

            dataGridPanel.Children.Add(matchGrid);
            dataGridPanel.Children.Add(saveGridButton);

            if (matchAndExpectedGrid.Children.Count == 4) 
            {
                matchAndExpectedGrid.Children.Insert(3, dataGridPanel);
            } 
            else matchAndExpectedGrid.Children.Add(dataGridPanel);

            backButton.Name = "backGrid";
        }

        public delegate void SaveClickedEventHandler(object sender, RoutedEventArgs e);
        public event SaveClickedEventHandler SaveClickedEvent;
        public void saveGridButtonClicked(object sender, RoutedEventArgs e)
        {    
            string matchedEnumText = enumText;
            for (int i = 0; i < matchedEnums.Count; i++)
            {
                if (matchedEnums.Values.ElementAt(i) != "")
                {
                    matchedEnumText = matchedEnumText.Replace("enum " + matchedEnums.Values.ElementAt(i), "enum " + matchedEnums.Keys.ElementAt(i));
                }
            }
            string path;

            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PacketAnalysis")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PacketAnalysis"));
            }

            path = Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"), "PacketAnalysis") + "\\Matched_" + enumPath.Substring(enumPath.LastIndexOf('\\') + 1);

            if (File.Exists(path))
            {
                File.Delete(path);
                File.WriteAllText(path, matchedEnumText);
            }
            else
            {
                File.WriteAllText(path, matchedEnumText);
            }

            ProcessEnumCode(matchedEnumText, false);

            configData.Path = path;
            configData.Name = packetName;
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(configData, Formatting.Indented));
            string configPath = Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"), "PacketAnalysis\\DATA\\" + packetName + "\\" + nowDate + "\\PacketConfig.json");
            if(File.Exists(configPath)) File.Copy(jsonPath, configPath, true);
            else File.Copy(jsonPath, configPath);

            NewExpectedValue(expectedFreq, "FREQ");
            NewExpectedValue(expectedDim, "DIM");

            mergedExpectedDict = expectedFreq.Zip(expectedDim, (kv1, kv2) => new KeyValuePair<string[], int[]>(kv1.Key, new int[] { kv1.Value, kv2.Value }))
                                 .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, new StringArrayComparer());

            ((GridView)projectListView.View).Columns[0].DisplayMemberBinding = new Binding("Key[0]");
            ((GridView)projectListView.View).Columns[1].DisplayMemberBinding = new Binding("Key[1]");

            projectListView.ItemsSource = mergedExpectedDict.ToList();
            projectsList.ItemsSource = enumStruct[packetName].Values.ToList();

            matchAndExpectedGrid.Children[3].Visibility = Visibility.Collapsed;
            matchAndExpectedGrid.Children[1].Visibility = Visibility.Collapsed;
            backButton.Visibility = Visibility.Collapsed;
            expectedGrid.Visibility = Visibility.Visible;            

            SaveClickedEvent?.Invoke(sender, e);
        }

        private void EnumBoxSelectionChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            string comboContent = comboBox.Text;
            string comboName = comboBox.Name;

            StackPanel tempStackPanel = FindVisualParent<StackPanel>(comboBox);
            StackPanel stackPanel = FindVisualParent<StackPanel>(tempStackPanel);

            var selectedItem = comboBox.SelectedItem;

            ComboBoxıtemsControl(comboName, stackPanel, selectedItem, comboContent);
        }

        public void ComboBoxıtemsControl(string name, StackPanel stackPanel, object selectedItem, string content)
        {
            Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    for (int i = 1; i < stackPanel.Children.Count - 1; i++)
                    {
                        ComboBox comboBox = ((ComboBox)((StackPanel)stackPanel.Children[i]).Children[1]);
                        if (comboBox.Name != name)
                        {
                            if ((string)selectedItem == "")
                            {
                                if (content != "")
                                {
                                    comboBox.Items.Remove("");
                                    comboBox.Items.Add(content);
                                    comboBox.Items.Add("");
                                }
                            }
                            else
                            {
                                comboBox.Items.Remove(selectedItem);
                                if (content != "")
                                {
                                    comboBox.Items.Remove("");
                                    comboBox.Items.Add(content);
                                    comboBox.Items.Add("");
                                }
                            }
                        }
                    }
                });

            });

        }

        private void EnumButtonClicked(object sender, RoutedEventArgs e)
        {
            Button clickedEnumButton = (Button)sender;
            StackPanel stackPanel = FindVisualParent <StackPanel> (clickedEnumButton);

            Border border = FindVisualParent<Border>(stackPanel);
            if (border.BorderBrush == Brushes.Transparent) border.BorderBrush = Brushes.LightGreen;
            else border.BorderBrush = Brushes.Transparent;

            if (stackPanel.Children[1].Visibility == Visibility.Collapsed) stackPanel.Children[1].Visibility = Visibility.Visible;
            else stackPanel.Children[1].Visibility = Visibility.Collapsed;

            if (stackPanel.Children[2].Visibility == Visibility.Collapsed) stackPanel.Children[2].Visibility = Visibility.Visible;
            else stackPanel.Children[2].Visibility = Visibility.Collapsed;            
        }

        public void NewExpectedValue(Dictionary<string[], int> expectedValues, string key)
        {
            expectedValues.Clear();
            for (int i = 0; i < enumStruct[packetName].Count; i++)
            {
                for (int j = 0; j < enumStruct[enumStruct[packetName].Values.ElementAt(i)].Values.Count; j++)
                {
                    string[] paket_proje = { enumStruct[packetName].Values.ElementAt(i),
                                                            enumStruct[enumStruct[packetName].Values.ElementAt(i)].Values.ElementAt(j)};
                    expectedValues.Add(paket_proje, 0);
                }
            }

            Dictionary<string, int> tempExpectedValues = new Dictionary<string, int>();
            for (int i = 0; i < expectedValues.Count; i++)
            {
                tempExpectedValues.Add(expectedValues.ElementAt(i).Key[0] + "." + expectedValues.ElementAt(i).Key[1], expectedValues.ElementAt(i).Value);
            }

            if(key == "FREQ")
            {
                configData.Freq = tempExpectedValues;
                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(configData, Formatting.Indented));
                string configPath = Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"), "PacketAnalysis\\DATA\\" + packetName + "\\" + nowDate + "\\PacketConfig.json");
                File.Copy(jsonPath, configPath, true);
            }
            else if(key == "DIM")
            {
                configData.Dim = tempExpectedValues;
                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(configData, Formatting.Indented));
                string configPath = Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"), "PacketAnalysis\\DATA\\" + packetName + "\\" + nowDate + "\\PacketConfig.json");
                File.Copy(jsonPath, configPath, true);
            }

        }


        private T FindVisualChild2<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                    return (T)child;

                T childOfChild = FindVisualChild2<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        string clickedProject = "";
        private void projectsList_SelectionChanged(object sender, MouseButtonEventArgs e)
        {
            bool control = false;
            //MessageBox.Show("Changed");
            string selectedItem = projectsList.SelectedItem as string;
            
            foreach (var listViewItem in projectListView.Items.Cast<object>().Select(b =>
                    projectListView.ItemContainerGenerator.ContainerFromItem(b) as ListViewItem))
            {
                if (listViewItem != null)
                {
                    KeyValuePair<string[], int[]> pair = (KeyValuePair<string[], int[]>)listViewItem.DataContext;

                    if(clickedProject == selectedItem)
                    {
                        listViewItem.Visibility = Visibility.Visible;
                        control = true;
                    }
                    else
                    {
                        if (pair.Key[0] == selectedItem)
                        {
                            listViewItem.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            listViewItem.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }

            if (control) clickedProject = "";
            else clickedProject = selectedItem;

        }

        public void setColor()
        {

            if (colors != null)
            {
                foreach (var listViewItem in projectListView.Items.Cast<object>().Select(b =>
                        projectListView.ItemContainerGenerator.ContainerFromItem(b) as ListViewItem))
                {
                    if (listViewItem != null)
                    {
                        KeyValuePair<string[], int[]> pair = (KeyValuePair<string[], int[]>)listViewItem.DataContext;

                        SolidColorBrush newSolidColorBrush = new SolidColorBrush(Color.FromArgb((byte)200, colors[pair.Key[0]].Color.R,
                                                            colors[pair.Key[0]].Color.G, colors[pair.Key[0]].Color.B));
                        listViewItem.BorderBrush = newSolidColorBrush;
                    }
                }

                foreach (var listViewItem in projectsList.Items.Cast<object>().Select(b =>
                        projectsList.ItemContainerGenerator.ContainerFromItem(b) as ListViewItem))
                {
                    if (listViewItem != null)
                    {
                        string pair = (string)listViewItem.DataContext;

                        SolidColorBrush newSolidColorBrush = new SolidColorBrush(Color.FromArgb((byte)200, colors[pair].Color.R,
                                                            colors[pair].Color.G, colors[pair].Color.B));
                        listViewItem.BorderBrush = newSolidColorBrush;
                    }
                }
            }
            
        }

        public void updateExpetedBox(string[] name)
        {
            foreach (var listViewItem in projectListView.Items.Cast<object>().Select(b =>
                                         projectListView.ItemContainerGenerator.ContainerFromItem(b) as ListViewItem))
            {
                if (listViewItem != null)
                {                   
                    KeyValuePair<string[], int[]> pair = (KeyValuePair<string[], int[]>)listViewItem.DataContext;

                    if (pair.Key[0] == name[0] & pair.Key[1] == name[1])
                    {
                        TextBox updateFreqBox = FindVisualChild<TextBox>(listViewItem, "freqBox");
                        TextBox updateDimBox = FindVisualChild<TextBox>(listViewItem, "dimBox");

                        updateFreqBox.Text = expectedFreq[name].ToString();
                        updateDimBox.Text = expectedDim[name].ToString();
                    }
                }
            }
        }

        public delegate void UpdateClickedEventHandler(object sender, RoutedEventArgs e);
        public event UpdateClickedEventHandler UpdateClickedEvent;
        private void UpdateClick(object sender, RoutedEventArgs e)
        {
            bool dataControl = false;
            Button clickedButton = (Button)sender;            

            Image buttonIcon = FindVisualChild<Image>(clickedButton, iconNames[0]);

            if (buttonIcon == null) 
            {
                buttonIcon = FindVisualChild<Image>(clickedButton, iconNames[1]);
                initIconName = iconNames[1];
            } 
            else initIconName = iconNames[0];

            string editName = Environment.CurrentDirectory +  "\\edit.png";
            string tickName = Environment.CurrentDirectory + "\\icon.png";

            BitmapImage bitmapImageEdit = new BitmapImage(new Uri(editName, UriKind.Absolute));
            BitmapImage bitmapImageTick = new BitmapImage(new Uri(tickName, UriKind.Absolute));

            ListViewItem item = FindVisualParent<ListViewItem>(clickedButton);

            freqBox = FindVisualChild<TextBox>(item, "freqBox");
            dimBox = FindVisualChild<TextBox>(item, "dimBox");



            if (buttonIcon.Name == "edit")
            {
                buttonIcon.Source = bitmapImageTick;
                buttonIcon.Name = "tick";
                initIconName = "tick";

                if (freqBox != null)
                {
                    freqBox.IsReadOnly = false;
                    freqBox.BorderThickness = new Thickness(2);
                    freqBox.BorderBrush = Brushes.DimGray;
                    freqBox.Background = Brushes.White;
                    freqBox.Focus();
                }
                if (dimBox != null)
                {
                    dimBox.IsReadOnly = false;
                    dimBox.BorderThickness = new Thickness(2);
                    dimBox.Background = Brushes.White;
                    dimBox.BorderBrush = Brushes.DimGray;
                }

            }

            else if(buttonIcon.Name == "tick")
            {
                if (freqBox != null)
                {
                    try
                    {
                        Convert.ToInt32(freqBox.Text);
                    }
                    catch 
                    {
                        MessageBox.Show("Beklenen Frekans Değeri Pozitif Bir Tamsayı Olmalıdır.");
                        dataControl = false;
                        return;
                    }

                    if (Convert.ToInt32(freqBox.Text) < 0)
                    {
                        MessageBox.Show("Beklenen Frekans Değeri Sıfırdan Küçük Olamaz.");
                        dataControl = false;
                        return;
                    }



                    freqBox.IsReadOnly = true;
                    freqBox.BorderBrush = Brushes.LightGray;
                    freqBox.Background = Brushes.Transparent;
                    freqBox.BorderThickness = new Thickness(0);
                    dataControl = true;
                }
                if (dimBox != null)
                {
                    try
                    {
                        Convert.ToInt32(dimBox.Text);
                    }
                    catch
                    {
                        MessageBox.Show("Beklenen Boyut Değeri Pozitif Bir Tamsayı Olmalıdır.");
                        dataControl = false;
                        return;
                    }

                    if (Convert.ToInt32(dimBox.Text) < 0)
                    {
                        MessageBox.Show("Beklenen Boyut Değeri Sıfırdan Küçük Olamaz.");
                        dataControl = false;
                        return;
                    }
                    dimBox.IsReadOnly = true;
                    dimBox.BorderBrush = Brushes.LightGray;
                    dimBox.Background = Brushes.Transparent;
                    dimBox.BorderThickness = new Thickness(0);
                    dataControl = true;
                }

                if (dataControl)
                {
                    buttonIcon.Source = bitmapImageEdit;
                    buttonIcon.Name = "edit";
                    initIconName = "edit";

                    string[] packetProje = ((KeyValuePair<string[], int[]>)item.DataContext).Key;
                    string name = packetProje[0] + "." + packetProje[1];

                    expectedFreq[packetProje] = Convert.ToInt32(freqBox.Text);
                    configData.Freq[name] = expectedFreq[packetProje];

                    expectedDim[packetProje] = Convert.ToInt32(dimBox.Text);
                    configData.Dim[name] = expectedDim[packetProje];

                    _packetProje = packetProje;

                    File.WriteAllText(jsonPath, JsonConvert.SerializeObject(configData, Formatting.Indented));
                    string configPath = Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"), "PacketAnalysis\\DATA\\" + packetName + "\\" + nowDate + "\\PackageConfig.json");
                    File.Copy(jsonPath, configPath, true);

                    UpdateClickedEvent?.Invoke(sender, e);
                }

            }
        }

        private void FreqBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                dimBox.Focus();
            }
        }

        private void DimBoxKeyDown(object sender, KeyEventArgs e)
        {
        }

        private T FindVisualChild<T>(DependencyObject depObj, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                if (child is T && (child as FrameworkElement).Name == childName)
                {
                    return (T)child;
                }

                T childItem = FindVisualChild<T>(child, childName);
                if (childItem != null)
                {
                    return childItem;
                }
            }
            return null;
        }

        private T FindVisualParent<T>(DependencyObject depObj) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(depObj);

            if (parent == null)
                return null;

            T parentObject = parent as T;
            return parentObject ?? FindVisualParent<T>(parent);
        }
    }
}
