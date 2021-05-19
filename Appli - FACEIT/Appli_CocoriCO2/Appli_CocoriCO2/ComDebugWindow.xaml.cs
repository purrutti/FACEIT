using LiveCharts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Logique d'interaction pour ComDebugWindow.xaml
    /// </summary>
    public partial class ComDebugWindow : Window
    {
        MainWindow MW = ((MainWindow)Application.Current.MainWindow);
        
        public ComDebugWindow()
        {
            InitializeComponent();
        }

        private void tb2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tb2.Text.Length > 0)
            {
                ReadData(tb2.Text);
                // tb2.Text = "";
                Sort("lastUpdated", ListSortDirection.Descending);
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lv_data.ItemsSource);
            view.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            view.SortDescriptions.Add(sd);
            view.Refresh();
        }

        public void ReadData(string data)
        {

            try
            {
                Condition c = JsonConvert.DeserializeObject<Condition>(data);
                c.lastUpdated = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(c.time);
                
                
                MW.statusLabel1.Text = "Last updated: " + c.lastUpdated.ToString() + " UTC";
                MW.conditionData.Add(c);
                if (c.command == 2 && (c.regulSalinite != null)) //SEND_PARAMS
                {
                    MW.conditions[c.condID].lastUpdated = c.lastUpdated;
                    
                   // MW.Labels[MW.Labels.Length-1] = c.lastUpdated.ToString();
                    //MW.seriesCollection[0].Values.Add(c.command);

                    MW.conditions[c.condID].regulSalinite = c.regulSalinite;
                    MW.conditions[c.condID].regulTemp = c.regulTemp;

                    
                }
                else if (c.command == 3  && (c.Meso != null))
                {
                    ///MW.monitoringWindow.Labels.Add(c.lastUpdated.ToString());
                    MW.monitoringWindow.seriesCollection[0].Values.Add(new DateModel
                    {
                        DateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(c.time),
                        Value = c.condID
                    });
                    MW.conditions[c.condID].regulSalinite.consigne = c.regulSalinite.consigne;
                    MW.conditions[c.condID].regulTemp.consigne = c.regulTemp.consigne;
                    for (int i = 0; i < 3; i++) MW.conditions[c.condID].Meso[i] = c.Meso[i];
                }
                else if (c.command == 6)
                {
                    MW.masterData = JsonConvert.DeserializeObject<MasterData>(data);
                    MW.masterData.lastUpdated = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(MW.masterData.time);
                    MW.statusLabel1.Text = "Last updated: " + MW.masterData.lastUpdated.ToString() +" UTC";
                }

                MW.DisplayData(c.command);
                
                
            }
            catch (Exception e)
            {

            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }

}
