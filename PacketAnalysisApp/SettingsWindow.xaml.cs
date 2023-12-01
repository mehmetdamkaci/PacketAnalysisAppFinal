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
using System.Windows.Shapes;
using LiveCharts.Wpf;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

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
        public Dictionary<string, int> Freq { get; set; }
        public Dictionary<string, int> Dim { get; set; }
    }

    public partial class SettingsWindow : Window
    {
        public string paketName;
        public string paketPath;
        public string jsonPath = "PacketConfig.json";
        public CONFIG configData;
        
        public Dictionary<string, Dictionary<int, string>> enumStruct;
        public Dictionary<string[], int> expectedFreq;
        public Dictionary<string[], int> expectedDim;
        public Dictionary<string[], int[]> mergeExpected;


        string enumPath = string.Empty;

        TextBox freqBox;
        TextBox dimBox;

        string[] iconNames = { "edit" , "tick"};
        string initIconName = "edit";


        public Dictionary<string, SolidColorBrush> colors;

        public SettingsWindow()
        {
            string json = File.ReadAllText(jsonPath);
            configData = JsonConvert.DeserializeObject<CONFIG>(json);
            paketName = configData.Name;
            paketPath = configData.Path;

            InitializeComponent();
            packetNameLabel.Content = paketName;

        }

        private void AddEnumFile_Clicked(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
            openFileDlg.Filter = "C# File|*.cs";

            Nullable<bool> result = openFileDlg.ShowDialog();

            if (result == true)
            {
                enumPath = openFileDlg.FileName;
                string enumText =  File.ReadAllText(enumPath);
                ProcessEnumCode(enumText);
                expectedGrid.Visibility = Visibility.Collapsed;
                
            }
        }
        

        public void ProcessEnumCode(string enumFileText)
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

            //InitExpectedValue(expectedFreq);
            //InitExpectedValue(expectedDim);

            CreateEnumMatchGrid();
        }

        public void CreateEnumMatchGrid()
        {            
            StackPanel matchPanel = new StackPanel();
            matchPanel.Orientation = Orientation.Vertical;
            matchPanel.HorizontalAlignment = HorizontalAlignment.Center;

            Label messageLabel = new Label();
            messageLabel.Content = "Paketlerin Bulunduğu Enumı, Üzerine Tıklayarak Seçiniz";
            messageLabel.FontSize = 16;
            messageLabel.Margin = new Thickness(0, 10, 0, 30);
            messageLabel.HorizontalAlignment = HorizontalAlignment.Center;
            messageLabel.Foreground = Brushes.White;
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

                    listBoxPanel.Children.Add(enumButton);
                    listBoxPanel.Children.Add(enumElementList);
                    listBoxPanel.Children.Add(selectedPacketEnum);
                    enumPanelBorder.Child = listBoxPanel;
                    enumPanel.Children.Add(enumPanelBorder);

                    

                    if (i * (enumStruct.Count / 3 + 1) + j == enumStruct.Count - 1) break;

                }
                mainEnumPanel.Children.Add(enumPanel);
            }
            matchPanel.Children.Add(mainEnumPanel);
            scrollViewer.Content = matchPanel;
           // matchPanel.Children.Add(scrollViewer);
            matchAndExpectedGrid.Children.Add(scrollViewer);
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

        public void InitExpectedValue(Dictionary<string[], int> expectedValues)
        {
            for (int i = 0; i < enumStruct[paketName].Count; i++)
            {
                for (int j = 0; j < enumStruct[enumStruct[paketName].Values.ElementAt(i)].Values.Count; j++)
                {
                    string[] paket_proje = { enumStruct[paketName].Values.ElementAt(i),
                                                            enumStruct[enumStruct[paketName].Values.ElementAt(i)].Values.ElementAt(j)};
                    expectedValues.Add(paket_proje, 0);
                }
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
        private void UpdateClick(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;            

            Image buttonIcon = FindVisualChild<Image>(clickedButton, iconNames[0]);

            if (buttonIcon == null) 
            {
                buttonIcon = FindVisualChild<Image>(clickedButton, iconNames[1]);
                initIconName = iconNames[1];
            } 
            else initIconName = iconNames[0];

            string editName = "C:\\Users\\PC_4232\\Desktop\\Mehmet\\edit.png";
            string tickName = "C:\\Users\\PC_4232\\Desktop\\Mehmet\\icon.png";

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
                buttonIcon.Source = bitmapImageEdit;
                buttonIcon.Name= "edit";
                initIconName = "edit";

                if (freqBox != null)
                {
                    freqBox.IsReadOnly = true;
                    freqBox.BorderBrush = Brushes.LightGray;
                    freqBox.Background = Brushes.Transparent;
                    freqBox.BorderThickness = new Thickness(0);
                }
                if (dimBox != null)
                {
                    dimBox.IsReadOnly = true;
                    dimBox.BorderBrush = Brushes.LightGray;
                    dimBox.Background = Brushes.Transparent;
                    dimBox.BorderThickness = new Thickness(0);
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
