using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Text.RegularExpressions;

namespace Tagger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        XmlDocument doc = new XmlDocument();
        string currentCat = "";
        string[] temp = new string[0];

        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void LoadCategories()
        {       
            XmlNodeList categories = doc.SelectNodes("/Brain/HMM");

            for (int i = 0; i < categories.Count; i++)
            {
                Categories.Items.Add(categories[i].Attributes["Tag"].InnerText);
            }
        }

        private void LoadControls(string tag)
        {
            controlsGrid.Children.Clear();

            try
            {
                XmlNodeList states = doc.SelectNodes("/Brain/HMM[@Tag=\"" + tag + "\"]/Model");
                int offset = 0;
                for (int i = 0; i < states.Count; i++)
                {
                    RadioButton rb = new RadioButton();
                    rb.Content = states[i].Attributes["State"].InnerText;
                    rb.Margin = new Thickness(0, offset, 0, 0);
                    rb.Checked += rb_Checked;
                    offset += 20;
                    controlsGrid.Children.Add(rb);
                }
            }
            catch
            {

            }
        }

        void rb_Checked(object sender, RoutedEventArgs e)
        {
            currentCat = ((RadioButton)sender).Content.ToString();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            doc.Load("Brain.xml");
            LoadCategories();
            //LoadControls();
        }

        private void Categories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadControls(Categories.SelectedValue.ToString());
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextRange textRange = new TextRange(
        // TextPointer to the start of content in the RichTextBox.
        ((RichTextBox)sender).Document.ContentStart, 
        // TextPointer to the end of content in the RichTextBox.
        ((RichTextBox)sender).Document.ContentEnd
    );
            
            string[] s = textRange.Text.Replace("\"","").Replace(",","").Replace(".","").Replace("-","").Split(' ');
            ToButtons(s);
        }

        private void ToButtons(string[] s)
        {
            workGrid.Children.Clear();
            workGrid.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            workGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            temp = s;
            Point p = new Point(0, 0);

            for (int i = 0; i < s.Length; i++)
            {
                Button b = new Button();
                b.Content = s[i];
                b.Margin = new Thickness(p.X, p.Y, 0, 0);
                b.Width = 69;
                b.Click += b_Click;
                b.Height = 25;
                b.Name = "b" + i.ToString();
                b.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                b.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                if (p.X+ 100 > sourceGrid.ActualWidth)
                {
                    p.X = 0;
                    p.Y += 30;
                }
                else
                    p.X += 75;

                workGrid.Children.Add(b);
            }

            sourceGrid.Visibility = System.Windows.Visibility.Hidden;
            workGrid.Visibility = System.Windows.Visibility.Visible;
        }

        void b_Click(object sender, RoutedEventArgs e)
        {
            int i = Convert.ToInt32(((Button)sender).Name.Substring(1));
            temp[i] = "/" + temp[i] + "/" + currentCat;
            ((Button)sender).Background = Brushes.Green;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            fillBG();
            string s = "";
            for(int i=0; i<temp.Length; i++)
            {
                s+=temp[i]+" ";
            }

            s=s.Substring(0, s.Length-1);

            XmlNode node = doc.CreateNode(XmlNodeType.Element, "Data", null);
            node.InnerText = s;

            doc.SelectSingleNode("/Brain/HMM[@Tag=\"" + Categories.SelectedValue + "\"]/TrainingData").AppendChild(node);

            doc.Save("Brain.xml");

            workGrid.Visibility = System.Windows.Visibility.Hidden;
            sourceGrid.Visibility = System.Windows.Visibility.Visible;
        }

        private void fillBG()
        {
            Regex r = new Regex(@"/((\w|\s)+)/(\w+)", RegexOptions.Compiled);
            for (int i = 0; i < temp.Length; i++)
            {
                if (!r.IsMatch(temp[i]))
                    temp[i] = "/" + temp[i] + "/bg";
            }
        }
    }
}
