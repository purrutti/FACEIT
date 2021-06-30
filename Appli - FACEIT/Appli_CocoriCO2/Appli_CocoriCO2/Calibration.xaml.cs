using System;
using System.Collections.Generic;
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
    /// Logique d'interaction pour Calibration.xaml
    /// </summary>
    public partial class Calibration : Window
    {
        MainWindow MW = ((MainWindow)Application.Current.MainWindow);
        CancellationTokenSource cts = new CancellationTokenSource();
        public Calibration()
        {
            InitializeComponent();
            comboBox_Condition.SelectedIndex = 0;
            comboBox_Meso.SelectedIndex = 0;
            comboBox_Sensor.SelectedIndex = 0;
            InitializeAsync();
        }

        private static async Task RunPeriodicAsync(Action onTick,
                                          TimeSpan dueTime,
                                          TimeSpan interval,
                                          CancellationToken token)
        {
            // Initial wait time before we begin the periodic loop.
            if (dueTime > TimeSpan.Zero)
                await Task.Delay(dueTime, token);

            // Repeat this loop until cancelled.
            while (!token.IsCancellationRequested)
            {
                // Call our onTick function.
                onTick?.Invoke();

                // Wait to repeat again.
                if (interval > TimeSpan.Zero)
                    await Task.Delay(interval, token);
            }
        }
        private async Task InitializeAsync()
        {
            int t;
            Int32.TryParse(Properties.Settings.Default["dataLogInterval"].ToString(), out t);
            var dueTime = TimeSpan.FromMinutes(0);
            var interval = TimeSpan.FromSeconds(1);

            // TODO: Add a CancellationTokenSource and supply the token here instead of None.
            await RunPeriodicAsync(RefreshMeasure, dueTime, interval, cts.Token);
        }

        private void RefreshMeasure()
        {
            int condID = comboBox_Condition.SelectedIndex;
            int MesoID = comboBox_Meso.SelectedIndex;
            int sensorID = comboBox_Sensor.SelectedIndex;

            switch (sensorID)
            {
                case 0:
                    label_measure.Content = string.Format("{0:0.00} °C", MW.conditions[condID].Meso[MesoID].temperature);
                    label_measure2.Content = "";
                    label_measure3.Content = "";
                    label_explain_offset.Content = "Ideally a value between 0°C and 5°C";
                    label_explain_slope.Content = "Ideally a value between 20°C and 25°C";
                    tb_Offset.IsEnabled = true;
                    tb_Slope.IsEnabled = true;
                    break;
                case 1:
                    label_measure.Content = string.Format("{0:0.00} %", MW.conditions[condID].Meso[MesoID].oxy_pc);
                    label_measure2.Content = "";
                    label_measure3.Content = "";
                    label_explain_offset.Content = "Has to be 0%";
                    label_explain_slope.Content = "Has to be 100%";
                    tb_Offset.Text = "0";
                    tb_Slope.Text = "100";
                    tb_Offset.IsEnabled = false;
                    tb_Slope.IsEnabled = false;
                    break;
                case 2:
                    label_measure.Content = string.Format("{0:0.00} uS/cm", MW.conditions[condID].Meso[MesoID].cond);
                    label_measure2.Content = string.Format("{0:0.00}", MW.conditions[condID].Meso[MesoID].salinite);
                    label_measure3.Content = string.Format("{0:0.00} °C", MW.conditions[condID].Meso[MesoID].temperature);
                    label_explain_offset.Content = "Ideally a value close to 0 µS/cm";
                    label_explain_slope.Content = "Has to be above 20 000 µS/cm";
                    tb_Offset.IsEnabled = true;
                    tb_Slope.IsEnabled = true;
                    break;

            }
        }


        private void btn_SendOffset_Click(object sender, RoutedEventArgs e)
        {
            int condID = comboBox_Condition.SelectedIndex;
            int mesoID = comboBox_Meso.SelectedIndex;
            int sensorID = comboBox_Sensor.SelectedIndex;
            float value;

            float.TryParse(tb_Offset.Text, out value);
            sendReq(condID, mesoID, sensorID, 0, value);
        }

        private void btn_SendSlope_Click(object sender, RoutedEventArgs e)
        {
            int condID = comboBox_Condition.SelectedIndex;
            int mesoID = comboBox_Meso.SelectedIndex;
            int sensorID = comboBox_Sensor.SelectedIndex;
            float value;

            float.TryParse(tb_Slope.Text, out value);
            sendReq(condID, mesoID, sensorID, 1, value);
        }

        private void sendReq(int condID, int mesoID, int sensorID, int calibParam, float value)
        {
            //{ command: 4, condID: 1,senderID: 4, MesoID: 1,sensorID: 2, calibParam: 1, value: 123,45}
            string msg = "{command:4,condID:" + condID + ",senderID:4,mesoID:" + mesoID + ", sensorID:" + sensorID + ",calibParam:" + calibParam + ",value:" + value + "}";

            ((MainWindow)Application.Current.MainWindow).comDebugWindow.tb1.Text = msg;


            if (((MainWindow)Application.Current.MainWindow).ws.State == WebSocketState.Open)
            {
                Task<string> t2 = Send(((MainWindow)Application.Current.MainWindow).ws, msg, ((MainWindow)Application.Current.MainWindow).comDebugWindow.tb2);
                t2.Wait(50);
            }
        }

        private void comboBox_Sensor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshMeasure();
        }

        private void comboBox_Condition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBox_Sensor_SelectionChanged(sender, e);
        }

        private void comboBox_Meso_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBox_Sensor_SelectionChanged(sender, e);
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

        private void btn_FactoryReset_Click(object sender, RoutedEventArgs e)
        {
            int condID = comboBox_Condition.SelectedIndex;
            int mesoID = comboBox_Meso.SelectedIndex;
            int sensorID = comboBox_Sensor.SelectedIndex;
            float value;

            float.TryParse(tb_Offset.Text, out value);
            sendReq(condID, mesoID, sensorID, 99, value);
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }
}
