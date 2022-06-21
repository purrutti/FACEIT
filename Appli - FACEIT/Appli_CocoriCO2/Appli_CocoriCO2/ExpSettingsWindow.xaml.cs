using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
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
    /// Logique d'interaction pour ExpSettingsWindow.xaml
    /// </summary>
    public partial class ExpSettingsWindow : Window
    {
        MainWindow MW = ((MainWindow)Application.Current.MainWindow);
        public CultureInfo ci;
        public ExpSettingsWindow()
        {
            InitializeComponent();
            ci = new CultureInfo("en-US");
                ci.NumberFormat.NumberDecimalDigits = 2;
                ci.NumberFormat.NumberDecimalSeparator = ".";
                ci.NumberFormat.NumberGroupSeparator = " ";
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
                CultureInfo.DefaultThreadCurrentCulture = ci;
                CultureInfo.DefaultThreadCurrentUICulture = ci;

            comboBox_Condition.SelectedIndex = 0;
        }

        private void btn_SaveToFile_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_LoadFromPLC_Click(object sender, RoutedEventArgs e)
        {
            load(comboBox_Condition.SelectedIndex);
        }

        public void load(int index)
        {
            //{command:0,condID:0,senderID:4}
            string msg = "{command:0,condID:" + index + ", senderID:4}";


            if (((MainWindow)Application.Current.MainWindow).ws.State == WebSocketState.Open)
            {
                Task<string> t2 = Send(((MainWindow)Application.Current.MainWindow).ws, msg, ((MainWindow)Application.Current.MainWindow).comDebugWindow.tb2);
                t2.Wait(50);
                refreshParams();
            }
        }

        private void btn_SaveToPLC_Click(object sender, RoutedEventArgs e)
        {
             Condition c = new Condition();

            int temp;
            double dTemp;
            c.condID = comboBox_Condition.SelectedIndex;
            c.command = 0;
            c.regulSalinite = new Regul();
            c.regulSalinite.autorisationForcage = (bool)checkBox_Cond_Override.IsChecked;
            if (Int32.TryParse(tb_Cond_consigneForcage.Text, out temp)) c.regulSalinite.consigneForcage = temp;
            if (Double.TryParse(tb_Cond_setPoint.Text, out dTemp)) c.regulSalinite.consigne = dTemp;
            if (Double.TryParse(tb_Cond_Kp.Text, out dTemp)) c.regulSalinite.Kp = dTemp;
            if (Double.TryParse(tb_Cond_Ki.Text, out dTemp)) c.regulSalinite.Ki = dTemp;
            if (Double.TryParse(tb_Cond_Kd.Text, out dTemp)) c.regulSalinite.Kd = dTemp;

            c.regulTemp = new Regul();
            if (Int32.TryParse(tb_Temp_consigneForcage.Text, out temp)) c.regulTemp.consigneForcage = temp;
            if (Double.TryParse(tb_Temp_setPoint.Text, out dTemp)) c.regulTemp.consigne = dTemp;
            if (Double.TryParse(tb_Temp_Kp.Text, out dTemp)) c.regulTemp.Kp = dTemp;
            if (Double.TryParse(tb_Temp_Ki.Text, out dTemp)) c.regulTemp.Ki = dTemp;
            if (Double.TryParse(tb_Temp_Kd.Text, out dTemp)) c.regulTemp.Kd = dTemp;
            if (Double.TryParse(tb_dT_setPoint.Text, out dTemp)) c.regulTemp.offset = dTemp;

            c.regulTemp.autorisationForcage = (bool)checkBox_Temp_Override.IsChecked;

            /*
             * {"command":2,"condID":0,"time":"1611595972","regulTemp":{"consigne":0,"Kp":0,"Ki":0,"Kd":0},"regulCond":{"consigne":0,"Kp":0,"Ki":0,"Kd":0}}
             * */
            string msg = buildJsonParams(c);

            ((MainWindow)Application.Current.MainWindow).comDebugWindow.tb1.Text = msg;


            if (((MainWindow)Application.Current.MainWindow).ws.State == WebSocketState.Open)
            {
                Task<string> t2 = Send(((MainWindow)Application.Current.MainWindow).ws, msg, ((MainWindow)Application.Current.MainWindow).comDebugWindow.tb2);
                t2.Wait(50);
            }
        }

        public string buildJsonParams(Condition c)
        {
            string msg = "{command:2,condID:" + c.condID + ",senderID:4,";

            msg += "\"regulT\":{";
            msg += "\"offset\":" + string.Format(ci, "{0:0.00}", c.regulTemp.offset) + ",";
            msg += "\"cons\":" + string.Format(ci, "{0:0.00}", c.regulTemp.consigne) + ",";
            msg += "\"Kp\":" + c.regulTemp.Kp.ToString() + ",";
            msg += "\"Ki\":" + c.regulTemp.Ki.ToString() + ",";
            msg += "\"Kd\":" + c.regulTemp.Kd.ToString() + ",";
            msg += "\"consForcage\":" + c.regulTemp.consigneForcage + ",";
            msg += "\"autForcage\":\"" + c.regulTemp.autorisationForcage + "\"},";


            msg += "\"regulS\":{\"offset\":" + string.Format(ci, "{0:0.00}", c.regulSalinite.offset) + ",";
            msg += "\"cons\":" + string.Format(ci, "{0:0.00}", c.regulSalinite.consigne) + ",";
            msg += "\"Kp\":" + c.regulSalinite.Kp.ToString() + ",";
            msg += "\"Ki\":" + c.regulSalinite.Ki.ToString() + ",";
            msg += "\"Kd\":" + c.regulSalinite.Kd.ToString() + ",";
            msg += "\"consForcage\":" + c.regulSalinite.consigneForcage + ",";
            msg += "\"autForcage\":\"" + c.regulSalinite.autorisationForcage + "\"}";

            msg += "}";

            return msg;
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private static async Task<string> Send(ClientWebSocket ws, string msg, TextBox tb)
        {
            var timeOut = new CancellationTokenSource(500).Token;
            if (ws.State == WebSocketState.Open)
            {
                ArraySegment<byte> bytesToSend = new ArraySegment<byte>(
                    Encoding.UTF8.GetBytes(msg));
                await ws.SendAsync(
                    bytesToSend, WebSocketMessageType.Text,
                    true, timeOut);
            }
            if (ws.State == WebSocketState.Open)
            {
                ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[600]);
                WebSocketReceiveResult result = ws.ReceiveAsync(
                    bytesReceived, timeOut).Result;
                string data = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
                tb.Text = data;
                return data;
            }
            return null;

        }

        private void comboBox_Condition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            load(comboBox_Condition.SelectedIndex);
            Visibility V = Visibility.Hidden;
            Visibility V2 = Visibility.Hidden;
            if (comboBox_Condition.SelectedIndex == 0)
            {
                V = Visibility.Hidden;
                V2 = Visibility.Visible;
                tb_Cond_setPoint.IsEnabled = true;
                label_Cond_title.Content = "Pressure regulation";
                label_Cond_setPoint.Content = "Pressure setpoint";
            }
            else
            {
                V = Visibility.Visible;
                V2 = Visibility.Hidden;
                tb_Cond_setPoint.IsEnabled = false;
            }

            label_dT.Visibility = V;
            tb_dT_setPoint.Visibility = V;

            label_Temp_title.Visibility = V;
            label_Temp_setpoint.Visibility = V;
            label_Temp_Kp.Visibility = V;
            label_Temp_Ki.Visibility = V;
            label_Temp_Kd.Visibility = V;
            checkBox_Temp_Override.Visibility = V;
            tb_Temp_consigneForcage.Visibility = V;
            label_pc2.Visibility = V;
            tb_Temp_setPoint.Visibility = V;
            tb_Temp_Ki.Visibility = V;
            tb_Temp_Kp.Visibility = V;
            tb_Temp_Kd.Visibility = V;

            label_Cond_title.Visibility = V2;
            label_Cond_setPoint.Visibility = V2;
            label_Cond_Kp.Visibility = V2;
            label_Cond_Ki.Visibility = V2;
            label_Cond_Kd.Visibility = V2;
            checkBox_Cond_Override.Visibility = V2;
            tb_Cond_setPoint.Visibility = V2;
            tb_Cond_Kp.Visibility = V2;
            tb_Cond_Ki.Visibility = V2;
            tb_Cond_Kd.Visibility = V2;
            tb_Cond_consigneForcage.Visibility = V2;

            refreshParams();
          }

        private void refreshParams()
        {
            int condID = comboBox_Condition.SelectedIndex;
            if (condID == 0)
            {
                tb_Cond_setPoint.Text = string.Format(ci, "{0:0.00}", MW.conditions[condID].regulSalinite.consigne);
            }
            else
            {
                calculConsignes(condID);

                tb_Cond_setPoint.Text = string.Format(ci, "{0:0.00}", MW.conditions[condID].regulSalinite.consigne);
            }
            
            tb_Cond_consigneForcage.Text = string.Format(ci, "{0:0}", MW.conditions[condID].regulSalinite.consigneForcage);
            tb_Cond_Kp.Text = string.Format(ci, "{0:0.00}", MW.conditions[condID].regulSalinite.Kp);
            tb_Cond_Ki.Text = string.Format(ci, "{0:0.00}", MW.conditions[condID].regulSalinite.Ki);
            tb_Cond_Kd.Text = string.Format(ci, "{0:0.00}", MW.conditions[condID].regulSalinite.Kd);
            checkBox_Cond_Override.IsChecked = MW.conditions[condID].regulSalinite.autorisationForcage;

            tb_dT_setPoint.Text = string.Format(ci, "{0:0.00}", MW.conditions[condID].regulTemp.offset);
            tb_Temp_setPoint.Text = string.Format(ci, "{0:0.00}", MW.conditions[condID].regulTemp.consigne);
            tb_Temp_consigneForcage.Text = string.Format(ci, "{0:0}", MW.conditions[condID].regulTemp.consigneForcage);
            tb_Temp_Kp.Text = string.Format(ci, "{0:0.00}", MW.conditions[condID].regulTemp.Kp);
            tb_Temp_Ki.Text = string.Format(ci, "{0:0.00}", MW.conditions[condID].regulTemp.Ki);
            tb_Temp_Kd.Text = string.Format(ci, "{0:0.00}", MW.conditions[condID].regulTemp.Kd);
            checkBox_Temp_Override.IsChecked = MW.conditions[condID].regulTemp.autorisationForcage;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        public void calculConsignes(int condID)
        {

            double meanTemp = 0;
            double meanSal = 0;
            for (int i = 0; i < 3; i++)
            {
                meanTemp += MW.conditions[0].Meso[i].temperature / 3;
                meanSal += MW.conditions[0].Meso[i].salinite / 3;
            }

            if (!(bool)MW.ForceInSituWindow.checkBox_ForceInSitu.IsChecked)
            {
                if (condID == 0) MW.conditions[condID].regulTemp.consigne = MW.inSituData.temperature;
                else MW.conditions[condID].regulTemp.consigne = MW.conditions[condID].regulTemp.offset + MW.inSituData.temperature;
            }
            else
            {
                double temp;
                Double.TryParse(MW.ForceInSituWindow.tb_temperature.Text, out temp);
                if (condID == 0) MW.conditions[condID].regulTemp.consigne = temp;
                else MW.conditions[condID].regulTemp.consigne = MW.conditions[condID].regulTemp.offset + temp;
            }
            //MW.conditions[condID].regulSalinite.offset = a * meanTemp + b;
            
            //if (condID != 0) MW.conditions[condID].regulSalinite.consigne = MW.conditions[condID].regulSalinite.offset + meanSal;
            

           // if (condID == 0) MW.conditions[condID].regulTemp.consigne = MW.inSituData.temperature;
            //else MW.conditions[condID].regulTemp.consigne = MW.conditions[condID].regulTemp.offset + meanTemp;

           
        }

    }
}
