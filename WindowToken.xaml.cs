using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Shapes;

namespace MFIWPF
{
    /// <summary>
    /// Interaction logic for WindowToken.xaml
    /// </summary>
    public partial class WindowToken : Window
    {
        public WindowToken()
        {
            InitializeComponent();
            textboxSourceArenaToken.Text = WindowToken.ReadToken();
        }

        private void hyperlinkSourceArena_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            string navigateUri = hl.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            BinaryWriter binarywriteDefaults = new BinaryWriter(File.Open("Defaults.sps", FileMode.OpenOrCreate));
            binarywriteDefaults.Write(textboxSourceArenaToken.Text.Trim());
            binarywriteDefaults.Close();
            Close();
        }

        public static string ReadToken()
        {
            string Token = "";
            try
            {
                BinaryReader binaryreaderDefaults = new BinaryReader(File.Open("Defaults.sps", FileMode.Open));
                Token = binaryreaderDefaults.ReadString();
                binaryreaderDefaults.Close();
            }
            catch (Exception exp)
            {

            }
            return Token;
        }
    }
}
