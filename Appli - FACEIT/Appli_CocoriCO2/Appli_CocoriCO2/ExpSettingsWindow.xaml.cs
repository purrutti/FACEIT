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
            //{command:0,condID:0,senderID:4}
            string msg = "{command:0,condID:"+ comboBox_Condition.SelectedIndex+", senderID:4}";
           

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
            if (Double.TryParse(tb_dCond_setPoint.Text, out dTemp)) c.regulSalinite.offset = dTemp;

            /*
             * {"command":2,"condID":0,"time":"1611595972","regulTemp":{"consigne":0,"Kp":0,"Ki":0,"Kd":0},"regulCond":{"consigne":0,"Kp":0,"Ki":0,"Kd":0}}
             * */
            string msg = "{command:2,condID:" + comboBox_Condition.SelectedIndex + ",senderID:4,";

            c.regulTemp = new Regul();
            c.regulTemp.autorisationForcage = (bool)checkBox_Cond_Override.IsChecked;
            if (Int32.TryParse(tb_Temp_consigneForcage.Text, out temp)) c.regulTemp.consigneForcage = temp;
            if (Double.TryParse(tb_Temp_setPoint.Text, out dTemp)) c.regulTemp.consigne = dTemp;
            if (Double.TryParse(tb_Temp_Kp.Text, out dTemp)) c.regulTemp.Kp = dTemp;
            if (Double.TryParse(tb_Temp_Ki.Text, out dTemp)) c.regulTemp.Ki = dTemp;
            if (Double.TryParse(tb_Temp_Kd.Text, out dTemp)) c.regulTemp.Kd = dTemp;
            if (Double.TryParse(tb_dT_setPoint.Text, out dTemp)) c.regulTemp.offset = dTemp;
            msg += "\"regulTemp\":{";
            msg += "\"offset\":" + c.regulTemp.offset.ToString() + ",";
            msg += "\"consigne\":" + c.regulTemp.consigne.ToString() + ",";
            msg += "\"Kp\":" + c.regulTemp.Kp.ToString() + ",";
            msg += "\"Ki\":" + c.regulTemp.Ki.ToString() + ",";
            msg += "\"Kd\":" + c.regulTemp.Kd.ToString() + ",";
            msg += "\"consigneForcage\":" + c.regulTemp.consigneForcage + ",";
            msg += "\"autorisationForcage\":\"" + c.regulTemp.autorisationForcage + "\"},";


            msg += "\"regulSalinite\":{\"offset\":" + c.regulSalinite.offset.ToString() + ",";
            msg += "\"consigne\":" + c.regulSalinite.consigne.ToString() + ",";
            msg += "\"Kp\":" + c.regulSalinite.Kp.ToString() + ",";
            msg += "\"Ki\":" + c.regulSalinite.Ki.ToString() + ",";
            msg += "\"Kd\":" + c.regulSalinite.Kd.ToString() + ",";
            msg += "\"consigneForcage\":" + c.regulSalinite.consigneForcage + ",";
            msg += "\"autorisationForcage\":\"" + c.regulSalinite.autorisationForcage + "\"}";
            
            msg += "}";

            ((MainWindow)Application.Current.MainWindow).comDebugWindow.tb1.Text = msg;


            if (((MainWindow)Application.Current.MainWindow).ws.State == WebSocketState.Open)
            {
                Task<string> t2 = Send(((MainWindow)Application.Current.MainWindow).ws, msg, ((MainWindow)Application.Current.MainWindow).comDebugWindow.tb2);
                t2.Wait(50);
            }
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
            btn_LoadFromPLC_Click(sender, e);
            if (comboBox_Condition.SelectedIndex == 0)
            {
                tb_Cond_setPoint.IsEnabled = true;
                label_Cond_title.Content = "Pressure regulation";
                label_Cond_setPoint.Content = "Pressure setpoint";
                label_dCond.Visibility = Visibility.Hidden;
                tb_dCond_setPoint.Visibility = Visibility.Hidden;
                label_dT.Visibility = Visibility.Hidden;
                tb_dT_setPoint.Visibility = Visibility.Hidden;
            }
            else
            {
                tb_Cond_setPoint.IsEnabled = false;
                label_Cond_title.Content = "Salinity regulation";
                label_Cond_setPoint.Content = "Cond. setpoint";
                label_dCond.Visibility = Visibility.Visible;
                tb_dCond_setPoint.Visibility = Visibility.Visible;
                label_dT.Visibility = Visibility.Visible;
                tb_dT_setPoint.Visibility = Visibility.Visible;
            }
            refreshParams();
          }

        private void refreshParams()
        {
            int condID = comboBox_Condition.SelectedIndex;
            tb_dCond_setPoint.Text = MW.conditions[condID].regulSalinite.offset.ToString(ci);
            tb_Cond_setPoint.Text = MW.conditions[condID].regulSalinite.consigne.ToString(ci);
            tb_Cond_consigneForcage.Text = MW.conditions[condID].regulSalinite.consigneForcage.ToString(ci);
            tb_Cond_Kp.Text = MW.conditions[condID].regulSalinite.Kp.ToString(ci);
            tb_Cond_Ki.Text = MW.conditions[condID].regulSalinite.Ki.ToString(ci);
            tb_Cond_Kd.Text = MW.conditions[condID].regulSalinite.Kd.ToString(ci);
            checkBox_Cond_Override.IsChecked = MW.conditions[condID].regulSalinite.autorisationForcage;

            tb_dT_setPoint.Text = MW.conditions[condID].regulTemp.offset.ToString(ci);
            tb_Temp_setPoint.Text = MW.conditions[condID].regulTemp.consigne.ToString(ci);
            tb_Temp_consigneForcage.Text = MW.conditions[condID].regulTemp.consigneForcage.ToString(ci);
            tb_Temp_Kp.Text = MW.conditions[condID].regulTemp.Kp.ToString(ci);
            tb_Temp_Ki.Text = MW.conditions[condID].regulTemp.Ki.ToString(ci);
            tb_Temp_Kd.Text = MW.conditions[condID].regulTemp.Kd.ToString(ci);
            checkBox_Temp_Override.IsChecked = MW.conditions[condID].regulTemp.autorisationForcage;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }
}
