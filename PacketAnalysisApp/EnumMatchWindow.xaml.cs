using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using System.Windows.Markup;
using System.Runtime.CompilerServices;

namespace PacketAnalysisApp
{
    public class CONFIG
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public partial class EnumMatchWindow : Window
    {

        public Dictionary<string, Dictionary<int, string>> enumStruct = new Dictionary<string, Dictionary<int, string>>();
        public Dictionary<string, Dictionary<int, string>> enumStructMain = new Dictionary<string, Dictionary<int, string>>();


        public event Action<Dictionary<string, Dictionary<int, string>>> UpdatedList;

        public Button OkKaydetLog = new Button();


        public List<ComboBox> comboBoxs = new List<ComboBox>();
        public Dictionary<string, ComboBox> comboLabel = new Dictionary<string, ComboBox>();
        public List<Label> labels = new List<Label>();
        public string csText = string.Empty;
        Dictionary<string, List<string>> enums;

        string buttonContent = string.Empty;
        List<TextBox> textBoxes = new List<TextBox>();
        List<Button> buttons = new List<Button>();

        List<string> Log = new List<string>();

        List<string> matchedEnums = new List<string>();

        List<string> comboList = new List<string>();

        Dictionary<string, string> comboTextItem = new Dictionary<string, string>();

        List<string> comboText;
        List<string> comboNames;

        Window enumWindow;
        Window kaydetWindow;
        Button clickedButton;
        ScrollViewer scrollViewer;
        StackPanel kaydetStack;

        List<ComboBox> updatedComboBoxs;
        List<Button> updatedButtons;

        Dictionary<string, string> gridItem = new Dictionary<string, string>();
        List<ComboBox> gridItemComboList = new List<ComboBox>();

        StackPanel kaydetAnaSayfaButon;
        Button KaydetButton;

        string newPath;

        public string paketName;
        public string paketPath;
        CONFIG configData;

        public string jsonPath = "PacketConfig.json";
        public EnumMatchWindow()
        {
            string json = File.ReadAllText(jsonPath);


            configData = JsonConvert.DeserializeObject<CONFIG>(json);

            paketName = configData.Name;
            paketPath = configData.Path;

            InitializeComponent();
            FileNameTextBox.Text = paketPath;
            viewEnums();
            enumStructMain = enumStruct;
        }

        public void viewEnums(string path = null)
        {
            comboBoxs.Clear();
            labels.Clear();
            stackPanel.Children.Clear();
            Log.Clear();
            buttons.Clear();

            if (path == null)
            {
                csText = File.ReadAllText(paketPath);
            }
            else
            {
                csText = File.ReadAllText(path);
            }

            comboList = new List<string>();

            stackPanel.HorizontalAlignment = HorizontalAlignment.Center;

            enums = ProcessEnumCode(csText);

            foreach (var key in enums.Keys)
            {
                comboList.Add(key);
            }

            foreach (var kvp in enums)
            {

                StackPanel comboTextBoxPanel = new StackPanel();
                comboTextBoxPanel.Orientation = Orientation.Horizontal;

                kaydetAnaSayfaButon = new StackPanel();
                kaydetAnaSayfaButon.Orientation = Orientation.Horizontal;

                Button enumButton = new Button();
                enumButton.Background = Brushes.DimGray;
                enumButton.Foreground = Brushes.White;
                enumButton.Content = "     " + kvp.Key + "     ";
                enumButton.FontWeight = FontWeights.Bold;
                enumButton.Name = kvp.Key;
                buttons.Add(enumButton);
                enumButton.FontSize = 16;
                enumButton.Width = 200;
                enumButton.Margin = new Thickness(0, 10, 0, 0);

                enumButton.Click += enumButtonClick;
                enumButton.MouseLeave += buttonMouseLeave;
                enumButton.MouseEnter += buttonMouseMove;
                //enumButton.MouseMove += buttonMouseMove;

                TextBox enumTextBox = new TextBox();
                enumTextBox.HorizontalAlignment = HorizontalAlignment.Left;
                enumTextBox.TextWrapping = TextWrapping.NoWrap;
                enumTextBox.Name = kvp.Key;
                enumTextBox.FontWeight = FontWeights.Bold;
                enumTextBox.Width = 200;

                textBoxes.Add(enumTextBox);

                comboTextBoxPanel.Margin = new Thickness(0, 10, 0, 0);
                stackPanel.Children.Add(enumButton);

                //buttons.Add(enumButton);


                if (buttons.Count > 0)
                {
                    foreach (Button btn in buttons)
                    {
                        if (enumButton.Name == btn.Name)
                        {
                            //comboBoxs.Remove(box);
                            if (updatedButtons != null) updatedButtons.Add(btn);
                        }
                    }
                }

            }


            KaydetButton = new Button();
            KaydetButton.Content = "Kaydet";
            KaydetButton.Name = "Kaydet";
            KaydetButton.Background = Brushes.DimGray;
            KaydetButton.Foreground = Brushes.White;
            KaydetButton.FontWeight = FontWeights.Bold;
            KaydetButton.FontSize = 13;
            KaydetButton.Margin = new Thickness(30, 20, 0, 0);
            KaydetButton.Click += KaydetClick;
            KaydetButton.Width = 100;
            kaydetAnaSayfaButon.Children.Add(KaydetButton);
            //stackPanel.Children.Add(KaydetButton);
            buttons.Add(KaydetButton);

        }

        private void buttonMouseLeave(object sender, MouseEventArgs e)
        {
            Button button = sender as Button;

            button.Background = Brushes.DimGray;
            button.Width = 200;
            button.Foreground = Brushes.White;

        }

        private void buttonMouseMove(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;
            button.Width = 220;
            button.Foreground = Brushes.Black;
        }

        private void enumButtonClick(object sender, RoutedEventArgs e)
        {


            if (gridItemComboList != null) gridItemComboList.Clear();
            updatedComboBoxs = new List<ComboBox>();
            enumWindow = new Window();
            enumWindow.Width = 400;
            enumWindow.Height = 400;
            enumWindow.Background = Brushes.DimGray;

            foreach (Button button in buttons)
            {
                button.Visibility = Visibility.Collapsed;
            }


            StackPanel enumStack = new StackPanel();

            enumStack.VerticalAlignment = VerticalAlignment.Center;

            clickedButton = (Button)sender;

            messageLabel.Content = "Paket Enum'ı Olarak " + clickedButton.Content.ToString().Trim() + " Seçildi.";

            clickedButton.IsEnabled = false;
            clickedButton.MouseLeave -= buttonMouseLeave;

            clickedButton.Visibility = Visibility.Visible;

            buttonContent = clickedButton.Content.ToString().Trim();
            List<string> Value = enums[buttonContent];

            int index = 0;
            foreach (var value in Value)
            {
                StackPanel valueAndComboPanel = new StackPanel();
                valueAndComboPanel.Background = Brushes.LightBlue;
                valueAndComboPanel.Orientation = Orientation.Horizontal;
                valueAndComboPanel.HorizontalAlignment = HorizontalAlignment.Center;

                Label valueLabel = new Label();
                valueLabel.Content = value;
                valueLabel.Width = 150;
                valueLabel.FontWeight = FontWeights.Bold;

                ComboBox comboBox = new ComboBox();
                comboBox.Foreground = Brushes.Black;
                comboBox.FontWeight = FontWeights.Bold;
                comboBox.Width = 150;
                comboBox.Name = value;


                comboBox.SelectionChanged += comboBoxItemControl;

                if (comboBoxs.Count > 0)
                {
                    foreach (ComboBox box in comboBoxs)
                    {
                        if (box.Name == comboBox.Name)
                        {
                            //comboBoxs.Remove(box);
                            updatedComboBoxs.Add(box);
                        }
                    }
                }

                comboBoxs.Add(comboBox);
                gridItemComboList.Add(comboBox);
                labels.Add(valueLabel);

                //comboLabel.Add(valueLabel.Content.ToString(), comboBox);

                foreach (var comboValue in comboList)
                {
                    if (buttonContent != comboValue)
                    {
                        comboBox.Items.Add(comboValue);
                    }
                }

                if (!comboBox.Items.Contains(string.Empty)) comboBox.Items.Add(string.Empty);

                valueAndComboPanel.Children.Add(valueLabel);
                valueAndComboPanel.Children.Add(comboBox);

                enumStack.Children.Add(valueAndComboPanel);

                if (comboTextItem.Keys.Contains(comboBox.Name))
                {
                    comboBox.Items.Add(comboTextItem[comboBox.Name]);
                    comboBox.SelectedItem = comboTextItem[comboBox.Name];
                }
                else
                {
                    comboTextItem[comboBox.Name] = "";
                }

                index += 1;
            }


            StackPanel ButtonOkandBack = new StackPanel();
            ButtonOkandBack.Orientation = Orientation.Horizontal;
            ButtonOkandBack.Margin = new Thickness(0, 20, 0, 0);

            Button back = new Button();

            back.Click += backButtonClick;
            back.Margin = new Thickness(0, 0, 0, 0);

            Image image = new Image();
            var source = new BitmapImage(new Uri( Environment.CurrentDirectory + "\\backButton-removebg-preview.png"));
            source.Freeze();
            image.Source = source;
            ImageBrush brush = new ImageBrush();
            brush.ImageSource = image.Source;
            brush.Stretch = Stretch.UniformToFill;

            back.Width = 30;
            back.Height = 30;

            back.MouseEnter += backMouseEnter;
            back.MouseLeave += backMouseLive;
            back.Background = brush;

            Button enumOK = new Button();
            enumOK.Content = "OK";
            enumOK.Width = 40;
            enumOK.Click += enumOKClick;
            enumOK.Margin = new Thickness(100, 0, 0, 0);
            enumOK.FontWeight = FontWeights.Bold;

            ButtonOkandBack.Children.Add(back);
            ButtonOkandBack.Children.Add(enumOK);

            enumStack.Children.Add(ButtonOkandBack);
            //enumStack.Children.Add(enumOK);


            scrollViewer = new ScrollViewer();
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.Content = enumStack;

            scrollViewer.Margin = clickedButton.Margin;

            stackPanel.Children.Add(scrollViewer);

            //enumWindow.Content = scrollViewer;
            //enumWindow.Show();            
        }

        private void backMouseLive(object sender, MouseEventArgs e)
        {
            Button back = sender as Button;
            back.Content = "";

            Image image = new Image();
            var source = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\backButton-removebg-preview.png"));
            source.Freeze();
            image.Source = source;
            ImageBrush brush = new ImageBrush();
            brush.ImageSource = image.Source;
            brush.Stretch = Stretch.UniformToFill;


        }

        private void backMouseEnter(object sender, MouseEventArgs e)
        {
            Button back = sender as Button;
            back.Foreground = Brushes.Black;
            back.FontWeight = FontWeights.Bold;
            back.Content = "Geri";
        }

        private void backButtonClick(object sender, RoutedEventArgs e)
        {
            messageLabel.Content = "Paketlerin Bulunduğu Enum'ı Seçiniz.";
            foreach (ComboBox combo in updatedComboBoxs)
            {
                comboBoxs.Remove(combo);
            }
            if (updatedButtons != null)
            {
                foreach (Button btn in updatedButtons)
                {
                    //MessageBox.Show(btn.Name);
                    buttons.Remove(btn);
                }
            }

            if (comboText != null) comboText.Clear();
            if (comboNames != null) comboNames.Clear();


            scrollViewer.Visibility = Visibility.Collapsed;
            clickedButton.MouseLeave += buttonMouseLeave;
            clickedButton.Width = 200;
            clickedButton.Foreground = Brushes.White;

            foreach (Button button in buttons)
            {
                button.Visibility = Visibility.Visible;
                //button.IsEnabled = true;
                clickedButton.IsEnabled = true;
            }
        }

        private void comboBoxItemControl(object sender, SelectionChangedEventArgs e)
        {
            ComboBox selectedBox = (ComboBox)sender;

            var duplicateItems = selectedBox.Items.Cast<string>()
                .GroupBy(item => item)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            foreach (var item in duplicateItems)
            {
                selectedBox.Items.Remove(item);
            }

            if (selectedBox.SelectedItem != null)
            {
                string selectedItem = selectedBox.SelectedItem.ToString();
                if (selectedItem != "")
                {
                    foreach (ComboBox box in comboBoxs)
                    {

                        for (int i = 0; i < buttons.Count; i++)
                        {
                            if (buttons[i].Name == selectedBox.Text) buttons[i].IsEnabled = true;
                        }

                        if (box.Name != selectedBox.Name) box.Items.Remove(selectedItem);
                        string changedItem = selectedBox.Text;
                        if (!box.Items.Contains(changedItem))
                        {
                            box.Items.Remove("");
                            box.Items.Add(changedItem);
                            comboList.Add(changedItem);
                            box.Items.Add("");
                        }
                    }
                }
                else
                {

                    string addItem = selectedBox.Text;
                    //MessageBox.Show(addItem);
                    foreach (ComboBox box in comboBoxs)
                    {
                        if (!box.Items.Contains(addItem)) box.Items.Add(addItem);

                        if (box.Items.Contains(""))
                        {
                            box.Items.Remove("");
                        }

                        box.Items.Add("");
                    }

                }
            }

            comboList = comboList.Distinct().ToList();
            comboList.Clear();
            foreach (var item in selectedBox.Items)
            {
                comboList.Add(item.ToString());
            }
            comboList = comboList.Distinct().ToList();
        }

        private void enumOKClick(object sender, RoutedEventArgs e)
        {

            if (gridItem != null)
            {
                foreach (ComboBox combo in gridItemComboList)
                {
                    if (combo.Text != "")
                    {
                        if (gridItem.Keys.Contains(clickedButton.Content.ToString().Trim() + "." + combo.Name))
                        {

                            gridItem.Remove(clickedButton.Content.ToString().Trim() + "." + combo.Name);
                            gridItem[clickedButton.Content.ToString().Trim() + "." + combo.Name] = combo.Text;
                        }
                        else
                        {
                            if (combo.Text != "") gridItem[clickedButton.Content.ToString().Trim() + "." + combo.Name] = combo.Text;

                        }
                    }
                    else
                    {
                        gridItem.Remove(clickedButton.Content.ToString().Trim() + "." + combo.Name);
                    }

                }
            }


            foreach (ComboBox combo in updatedComboBoxs)
            {
                //MessageBox.Show(combo.Name);
                comboBoxs.Remove(combo);
            }

            if (updatedButtons != null)
            {
                foreach (Button btn in updatedButtons)
                {
                    //MessageBox.Show(btn.Name);
                    buttons.Remove(btn);
                }
            }


            List<string> items = new List<string>();

            foreach (ComboBox combo in comboBoxs)
            {
                if (combo.Text != "")
                {
                    items.Add(combo.Text);
                }
            }

            foreach (ComboBox combo in comboBoxs)
            {
                foreach (string text in comboList)
                {
                    if (!items.Contains(text))
                    {
                        combo.Items.Add(text);
                    }
                }
            }



            scrollViewer.Visibility = Visibility.Collapsed;

            foreach (Button button in buttons)
            {
                if (button.IsEnabled)
                    button.Visibility = Visibility.Visible;
            }


            clickedButton.Background = Brushes.White;
            clickedButton.Foreground = Brushes.Black;

            comboText = new List<string>();
            comboNames = new List<string>();




            foreach (ComboBox combo in comboBoxs)
            {
                //MessageBox.Show(combo.Text);
                comboList.Remove(combo.Text);
                comboTextItem[combo.Name] = combo.Text;
                if (combo.Text != "") comboText.Add(combo.Text);
                comboNames.Add(combo.Name);
            }


            foreach (var text in comboText)
            {

                if (text != "" & comboText.Where(x => x.Equals(text)).Count() > 2)
                {
                    MessageBox.Show(text + " elemanı birden fazla enum ile eşleşemez.");
                    clickedButton.IsEnabled = true;
                    return;
                }
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                if (comboText.Contains(buttons[i].Name) || buttons[i].Name == "Kaydet")
                {
                    if (buttons[i].Name != "Kaydet")
                    {
                        buttons[i].IsEnabled = false;
                        buttons[i].Foreground = Brushes.Black;
                    }
                }
                else
                {
                    buttons[i].Visibility = Visibility.Collapsed;
                    buttons[i].IsEnabled = true;
                }

                //comboList.Remove(text);

            }

            clickedButton.Visibility = Visibility.Visible;
            clickedButton.Width = clickedButton.Width * 1.5;
            clickedButton.IsEnabled = false;
            clickedButton.MouseLeave -= buttonMouseLeave;
            clickedButton.MouseLeave -= buttonMouseMove;

            Button anaSayfa = new Button();
            anaSayfa.Content = "Ana Sayfa";
            anaSayfa.Click += clickedAnaSayfa;
            anaSayfa.Background = Brushes.DimGray;
            anaSayfa.FontWeight = FontWeights.Bold;
            anaSayfa.Foreground = Brushes.White;
            anaSayfa.Width = 100;
            anaSayfa.Margin = new Thickness(100, 20, 0, 0);
            anaSayfa.Height = 25;
            kaydetAnaSayfaButon.Children.Add(anaSayfa);
            if (!stackPanel.Children.Contains(kaydetAnaSayfaButon)) stackPanel.Children.Add(kaydetAnaSayfaButon);


            //MessageBox.Show(buttons.Count.ToString());

            //for(int i = 0; i < buttons.Count; i++)
            //{

            //    foreach (var text in comboText)
            //    {
            //        if (text != "")
            //        {
            //            if (buttons[i].Name == text) 
            //            {

            //                buttons[i].IsEnabled = false;
            //                buttons[i].Foreground = Brushes.Black;
            //                //stackPanel.Children.Remove(buttons[i]);
            //            } 
            //            else
            //            {
            //                MessageBox.Show(buttons[i].Name);
            //                //buttons[i].Visibility = Visibility.Collapsed;
            //            }
            //            comboList.Remove(text);
            //        }

            //    }

            //}

            foreach (TextBox textBox in textBoxes)
            {
                if (textBox.Name == buttonContent)
                {
                    for (int i = 0; i < comboText.Count; i++)
                    {
                        if (comboText[i] != "" & !matchedEnums.Contains(comboNames[i]))
                        {
                            matchedEnums.Add(comboNames[i]);
                            string log = comboText[i] + " => " + textBox.Name + "." + comboNames[i] + "  ";
                            textBox.Text += log;
                            Log.Add(log);
                        }
                    }
                }
            }


            paketName = clickedButton.Content.ToString().Trim();
            configData.Name = paketName;
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(configData, Formatting.Indented));

            //clickedButton.IsEnabled = true;
            //clickedButton.Width = 200;
            //clickedButton.MouseLeave += buttonMouseLeave;
            //clickedButton.MouseMove -= buttonMouseMove;
            enumWindow.Close();
        }

        private void clickedAnaSayfa(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            button.Margin = new Thickness(0, 20, 0, 0);
            button.Visibility = Visibility.Collapsed;

            KaydetButton.Margin = new Thickness(50, 20, 0, 0);

            clickedButton.IsEnabled = true;

            if (kaydetStack != null) kaydetStack.Visibility = Visibility.Collapsed;

            foreach (Button btn in buttons)
            {
                btn.Visibility = Visibility.Visible;
            }
        }

        public Dictionary<string, List<string>> ProcessEnumCode(string enumCode)
        {
            if (enumStruct != null) enumStruct.Clear();
            enumStruct = new Dictionary<string, Dictionary<int, string>>();
            Dictionary<string, List<string>> enums = new Dictionary<string, List<string>>();

            var syntaxTree = CSharpSyntaxTree.ParseText(enumCode);
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
                    return enums;
                }

                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());

                //List<string> enumOutput = new List<string>();

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

                        enums.Add(type.Name, enumValue);
                    }
                }

                return enums;
            }
        }

        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();

            Nullable<bool> result = openFileDlg.ShowDialog();

            if (result == true)
            {
                FileNameTextBox.Text = openFileDlg.FileName;
            }
        }

        private void OKClick(object sender, RoutedEventArgs e)
        {
            viewEnums(FileNameTextBox.Text);
        }

        private void KaydetClick(object sender, RoutedEventArgs e)
        {

            foreach (ComboBox combo in updatedComboBoxs)
            {
                comboBoxs.Remove(combo);
            }

            kaydetWindow = new Window();

            kaydetWindow.Width = 640;
            kaydetWindow.Height = 480;

            Color color = (Color)ColorConverter.ConvertFromString("#333333");
            SolidColorBrush backgroundBrush = new SolidColorBrush(color);

            kaydetWindow.Background = backgroundBrush;

            kaydetWindow.Closed += kaydetWindowClosed;
            kaydetStack = new StackPanel();

            kaydetStack.VerticalAlignment = VerticalAlignment.Center;
            kaydetStack.HorizontalAlignment = HorizontalAlignment.Center;
            kaydetStack.Margin = new Thickness(50, 0, 50, 0);

            Label logLabel = new Label();
            logLabel.Content = "ENUM EŞLEŞMELERİ";
            logLabel.Foreground = Brushes.White;
            logLabel.Background = backgroundBrush;
            logLabel.HorizontalAlignment = HorizontalAlignment.Center;
            logLabel.FontSize = 18;
            logLabel.FontWeight = FontWeights.Bold;
            logLabel.Margin = new Thickness(0, 0, 0, 20);
            kaydetStack.Children.Add(logLabel);

            TextBox kaydetTextBox = new TextBox();
            kaydetTextBox.Width = kaydetWindow.Width - 50;
            kaydetTextBox.Height = kaydetWindow.Height - 150;
            kaydetTextBox.Margin = new Thickness(0, 0, 0, 10);
            kaydetTextBox.FontWeight = FontWeights.Bold;

            foreach (var log in Log)
            {
                kaydetTextBox.Text += log + "\r\n";
            }

            //kaydetStack.Children.Add(kaydetTextBox);

            //------------------ Data Grid ----------------

            DataGrid matchGrid = new DataGrid();
            matchGrid.HorizontalAlignment = HorizontalAlignment.Center;
            matchGrid.VerticalAlignment = VerticalAlignment.Center;
            matchGrid.AutoGenerateColumns = false;
            matchGrid.AlternatingRowBackground = Brushes.LightGray;

            DataGridTextColumn enumName = new DataGridTextColumn
            {
                Header = "Enum İsmi",
                Binding = new Binding("Value"),
            };
            DataGridTextColumn enumValue = new DataGridTextColumn
            {
                Header = "Eşleştiği Enum",
                Binding = new Binding("Key")
            };

            enumName.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            enumValue.Width = new DataGridLength(1 * 1, DataGridLengthUnitType.Star);

            enumName.HeaderStyle = new Style(typeof(DataGridColumnHeader));
            enumName.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, Brushes.LightGray));
            enumName.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            enumName.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center)); // Sütun başlığının yatay hizalama
            enumName.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)); // Sütun başlığının genişliği


            enumValue.HeaderStyle = new Style(typeof(DataGridColumnHeader));
            enumValue.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, Brushes.LightGray));
            enumValue.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            enumValue.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center)); // Sütun başlığının yatay hizalama
            enumValue.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)); // Sütun başlığının genişliği


            matchGrid.Columns.Add(enumName);
            matchGrid.Columns.Add(enumValue);

            //OkKaydetLog = new Button();
            OkKaydetLog.Content = "OK";
            OkKaydetLog.Width = 50;
            OkKaydetLog.Margin = new Thickness(20);
            OkKaydetLog.Click += OkKaydetLog_Click;

            //kaydetWindow.Content = kaydetStack;


            string newCsText = csText;
            List<string> comboText = new List<string>();


            if (gridItem != null)
            {
                for (int i = 0; i < gridItem.Count; i++)
                {
                    if (gridItem.Values.ElementAt(i).Trim() == "" || gridItem.Keys.ElementAt(i).Trim() == "")
                    {
                        gridItem.Remove(gridItem.Keys.ElementAt(i));
                    }
                }
            }


            matchGrid.ItemsSource = gridItem.ToList();
            kaydetStack.Children.Add(matchGrid);
            kaydetStack.Children.Add(OkKaydetLog);

            foreach (ComboBox combo in comboBoxs)
            {
                comboText.Add(combo.Text);
            }

            foreach (var text in comboText)
            {

                if (text != "" & comboText.Where(x => x.Equals(text)).Count() > 1)
                {

                    MessageBox.Show(text + " elemanı birden fazla enum ile eşleşemez.");
                    return;
                }
            }

            for (int i = 0; i < comboBoxs.Count; i++)
            {
                if (comboBoxs[i].Text != "")
                {
                    newCsText = newCsText.Replace("enum " + comboBoxs[i].Text, "enum " + comboBoxs[i].Name);
                }
            }

            string path = FileNameTextBox.Text;

            if (!Directory.Exists("C:\\PacketAnalysis"))
            {
                Directory.CreateDirectory("C:\\PacketAnalysis");
            }
            newPath = "C:\\PacketAnalysis\\Matched_" + path.Substring(path.LastIndexOf('\\') + 1);
            //newPath = path.Substring(0, path.LastIndexOf("\\")) + "\\" + "new" + path.Substring(path.LastIndexOf('\\') + 1);
            if (File.Exists(newPath))
            {
                File.Delete(newPath);
                File.WriteAllText(newPath, newCsText);
            }
            else
            {
                File.WriteAllText(newPath, newCsText);
            }

            foreach (Button btn in buttons)
            {
                btn.Visibility = Visibility.Collapsed;
            }

            ProcessEnumCode(newCsText);

            stackPanel.Children.Add(kaydetStack);

            //kaydetWindow.Show();


        }

        private void OkKaydetLog_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(newPath + " Dosyası Kaydedildi.");

            foreach (Button btn in buttons)
            {
                btn.Visibility = Visibility.Visible;
            }

            kaydetStack.Visibility = Visibility.Collapsed;

            //MessageBox.Show(Environment.CurrentDirectory);

            paketName = clickedButton.Content.ToString().Trim();
            configData.Name = paketName;
            configData.Path = newPath;
            File.WriteAllText(jsonPath,JsonConvert.SerializeObject(configData, Formatting.Indented));

            enumStructMain = enumStruct;

            UpdatedList?.Invoke(enumStruct);
            this.Close();

            //kaydetWindow.Close();
        }

        private void kaydetWindowClosed(object sender, EventArgs e)
        {
            //MessageBox.Show(newPath + " Dosyası Kaydedildi.");
        }

        private void ClosedWindow(object sender, EventArgs e)
        {

        }
    }
}