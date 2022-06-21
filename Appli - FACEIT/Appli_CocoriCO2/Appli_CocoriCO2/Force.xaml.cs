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
using System.Windows.Shapes;

namespace Appli_CocoriCO2
{
    /// <summary>
    /// Logique d'interaction pour Force.xaml
    /// </summary>
    public partial class Force : Window
    {
        public Force()
        {
            InitializeComponent();
            tb_temperature.Text = Properties.Settings.Default["ForceInSituTemp"].ToString();
            if((bool)Properties.Settings.Default["ForceInSituCheckbox"]) checkBox_ForceInSitu.IsChecked = true;
            else checkBox_ForceInSitu.IsChecked = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.ForceInSituTemp = tb_temperature.Text;
            Properties.Settings.Default.ForceInSituCheckbox = (bool)checkBox_ForceInSitu.IsChecked;
            Properties.Settings.Default.Save();
            this.Hide();
            e.Cancel = true;
        }
    }
}
