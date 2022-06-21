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
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;
using SlackAPI;
using System.Net.WebSockets;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Appli_CocoriCO2
{

    public class InSituData
    {

        [JsonProperty(Required = Required.Default)]
        public IList<IList<string>> data { get; set; }

        public DateTime time { get; set; }
        public double salinite { get; set; }
        public double temperature { get; set; }
        public double oxygen { get; set; }
    }

    public class MasterData
    {

        [JsonProperty(Required = Required.Default)]
        public double debitEA { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double debitEF { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double debitEC { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double pressionEA { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double pressionEF { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double pressionEC { get; set; }
        
        public long time { get; set; }
        public DateTime lastUpdated { get; set; }
    }
    public class Mesocosme
    {
        [JsonProperty("MID",Required = Required.Default)]
        public int mesocosmeID{ get; set; }

        [JsonProperty("flow", Required = Required.Default)]
        public double debit { get; set; }
        [JsonProperty("temp", Required = Required.Default)]
        public double temperature { get; set; }
        [JsonProperty("oxy", Required = Required.Default)] 
        public double oxy { get; set; }
        [JsonProperty("oxy_temp", Required = Required.Default)]
        public double oxy_temp { get; set; }
        [JsonProperty("oxy_pc", Required = Required.Default)]
        public double oxy_pc { get; set; }
        [JsonProperty("cond", Required = Required.Default)]
        public double cond { get; set; }
        [JsonProperty("sali", Required = Required.Default)]
        public double salinite { get; set; }

        [JsonProperty("tempSPID_pc", Required = Required.Default)]
        public double tempSortiePID_pc { get; set; }
        [JsonProperty("salSPID_pc", Required = Required.Default)]
        public double salSortiePID_pc { get; set; }

    }
    public class Regul
    {
        [JsonProperty("sortiePID", Required = Required.Default)]
        public double sortiePID { get; set; }
        [JsonProperty("cons", Required = Required.Default)]
        public double consigne { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double Kp { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double Ki { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double Kd { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double sortiePID_pc { get; set; }
        [JsonProperty("autForcage", Required = Required.Default)]
        public bool autorisationForcage { get; set; }
        [JsonProperty("consForcage", Required = Required.Default)]
        public int consigneForcage { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double offset { get; set; }

    }
    public class Condition
    {
        public int command { get; set; }
       
        public int condID { get; set; }
        [JsonProperty("data",Required = Required.Default)]
        public Mesocosme[] Meso { get; set; }
        [JsonProperty("regulT", Required = Required.Default)]
        public Regul regulTemp { get; set; }
        [JsonProperty("regulS", Required = Required.Default)]
        public Regul regulSalinite { get; set; }
        public long time { get; set; }
        public DateTime lastUpdated { get; set; }

    }

    public unsafe class Alarme
    {
        public string libelle { get; set; }
        public DateTime dtTriggered { get; set; }
        public DateTime dtRaised { get; set; }
        public DateTime dtAcknowledged { get; set; }
        public TimeSpan delay { get; set; }
        public double threshold { get; set; }
        public double delta { get; set; }
        public double value { get; set; }
        public bool enabled { get; set; }
        public bool triggered { get; set; }
        public bool raised { get; set; }
        public bool acknowledged { get; set; }
        public int comparaison { get; set; }
        public bool checkAndRaise(double val) // raise alarm if value is upperThan threshold
        {
            value = val;
            if (!enabled) return false;
            bool upperThan, lowerThan;
            switch (comparaison)
            {
                case 0:

                    upperThan = true;
                    lowerThan = false;
                    break;
                case 1:
                    upperThan = false;
                    lowerThan = true;
                    break;
                case 2:
                    upperThan = true;
                    lowerThan = true;
                    break;
                default:
                    upperThan = false;
                    lowerThan = false;
                    break;
            }


            bool t = false;
            if (triggered) t = true;
            if (!triggered && upperThan && value >= (threshold + delta))
            {
                dtTriggered = DateTime.Now;
                t = true;
            }
            if (!triggered && lowerThan && value <= (threshold - delta))
            {
                dtTriggered = DateTime.Now;
                t = true;
            }
            triggered = t;

            if (!raised && triggered && dtTriggered.Add(delay) < DateTime.Now)
            {
                raised = true;
                dtRaised = DateTime.Now;
                sendSlackMessage(dtTriggered.ToString()+":"+this.libelle + string.Format(" Measure = {0:0.00}, ", value) + string.Format("Setpoint = {0:0.00}", threshold));
            }
            if (raised) return true;
            return false;
        }

        public void sendSlackMessage(String msg)
        {
            string TOKEN = Properties.Settings.Default["SlackToken"].ToString();  // token from last step in section above
            var slackClient = new SlackTaskClient(TOKEN);

            slackClient.PostMessageAsync(Properties.Settings.Default["SlackChannelID"].ToString(), msg);
        }

        public bool checkAndRaise(bool val, bool th) // raise alarm if value is upperThan threshold
        {
            if (!enabled) return false;


            if (!triggered && val != th)
            {
                triggered = true;
                dtTriggered = DateTime.Now;
            }
            else triggered = false;

            if (!raised && triggered && dtTriggered.Add(delay) > DateTime.Now)
            {
                raised = true;
                dtRaised = DateTime.Now;
                sendSlackMessage(this.libelle + " triggered at:" + dtTriggered.ToString());

            }
            if (raised) return true;
            return false;
        }


        public unsafe void set(string l, bool ena, int comp, double d, TimeSpan del)
        {
            libelle = l;
            enabled = ena;
            comparaison = comp;
            delta = d;
            delay = del;
        }
    }


    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool ExperimentState = true;
        


        //public List<Condition> conditions;
        public ObservableCollection<Condition> conditions;
        public ObservableCollection<Condition> conditionData;
        public ObservableCollection<Alarme> alarms;
        public MasterData masterData = new MasterData();
        public InSituData inSituData = new InSituData();


        //public List<Condition> conditionData;
        public ExpSettingsWindow expSettingsWindow;
        public Calibration calibrationWindow;
        public ComDebugWindow comDebugWindow;
        public Force ForceInSituWindow;
        public CultureInfo ci;

        public AlarmsListWindow alarmsListWindow;

        public ClientWebSocket ws = new ClientWebSocket();
        bool autoReco;
        public int step;

        CancellationTokenSource cancelToken = new CancellationTokenSource();


        public string[] Labels = new[] {"0"};
        public MainWindow()
        {
            
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show("FACE-IT Application is already running. Only one instance of this application is allowed");
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                InitializeComponent();
                var cts = new CancellationTokenSource();

                btn_stop.Content = "STOP";
                statusLabel2.Text = "Experiment is running normally";
                statusLabel2.Background = Brushes.Green;
                statusLabel2.Foreground = Brushes.White;

                autoReco = false;
                conditions = new ObservableCollection<Condition>();
                conditionData = new ObservableCollection<Condition>();
                for (int i = 0; i < 4; i++)
                {
                    Condition c = new Condition();
                    c.condID = i;
                    c.regulSalinite = new Regul();
                    c.regulTemp = new Regul();
                    c.Meso = new Mesocosme[3];
                    for (int j = 0; j < 3; j++) c.Meso[j] = new Mesocosme();
                    conditions.Add(c);
                }

                expSettingsWindow = new ExpSettingsWindow();
                comDebugWindow = new ComDebugWindow();
                calibrationWindow = new Calibration();
                ForceInSituWindow = new Force();

                alarms = new ObservableCollection<Alarme>();

                InitializeAsync();
                InitializeAsyncSendParams();
                //InitializeAsyncGetInSituData();

                InitializeAsyncAlarms();

                ci = new CultureInfo("en-US");
                ci.NumberFormat.NumberDecimalDigits = 2;
                ci.NumberFormat.NumberDecimalSeparator = ".";
                ci.NumberFormat.NumberGroupSeparator = " ";
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
                CultureInfo.DefaultThreadCurrentCulture = ci;
                CultureInfo.DefaultThreadCurrentUICulture = ci;

                //getInSituData();
                setAlarms();

                alarmsListWindow = new AlarmsListWindow();

                Alarme alarm = new Alarme();


                alarm.sendSlackMessage("APPLICATION STARTED");
            }
            
        }


        private async void getInSituData()
        {
            try
            {
                var client = new HttpClient();

                //https://dashboard.awi.de/data-xxl/rest/data?beginDate=2020-10-01T00:01:00&endDate=2021-10-01T00:01:00&format=application/json&aggregate=DAY&sensors=station:svluwobs:fb_731101:sbe45_awi_0403:salinity&sensors=station:svluwobs:fb_731101:sbe45_awi_0403:temperature

                client.BaseAddress = new Uri("https://dashboard.awi.de");
                client.DefaultRequestHeaders.Add("User-Agent", "C# console program");
                client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                string fromDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss");
                string toDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                var url = "data-xxl/rest/data?beginDate=" + fromDate + "&endDate=" + toDate + "&format=application/json&aggregate=HOUR&sensors=station:svluwobs:fb_731101:sbe45_awi_0403:salinity&sensors=station:svluwobs:fb_731101:sbe45_awi_0403:temperature&sensors=station:svluwobs:fb_731101:oxygen_awi_574:oxygen_saturation";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var resp = await response.Content.ReadAsStringAsync();

                inSituData = JsonConvert.DeserializeObject<InSituData>(resp);
                IList<string> d = inSituData.data.Last<IList<string>>();
                inSituData.time = DateTime.Parse(d.ElementAt<string>(0).ToString());

                double s, t, o;

                bool success = false;

                success = double.TryParse(d.ElementAt<string>(1).ToString(), out s);
                success &= double.TryParse(d.ElementAt<string>(2).ToString(), out t);
                success &= double.TryParse(d.ElementAt<string>(3).ToString(), out o);

                if(success && s>0 && o > 0)
                {                 
                    inSituData.salinite = s;
                    inSituData.temperature = t;
                    inSituData.oxygen = o;
                }


                /*label_IS_Time.Content = "Time: " + inSituData.time.ToString("yyyy-MM-dd HH:mm:ss");
                label_IS_Temp.Content = string.Format(ci, "Temperature: {0:0.00} °C", inSituData.temperature);
                label_IS_Salinity.Content = string.Format(ci, "Salinity:          {0:0.00}", inSituData.salinite);
                label_IS_Oxygen.Content = string.Format(ci, "Oxygen:         {0:0.00}%", inSituData.oxygen);

                ForceInSituWindow.label_IS_Time.Content = label_IS_Time.Content;
                ForceInSituWindow.label_IS_Temp.Content = label_IS_Temp.Content;
                ForceInSituWindow.label_IS_Salinity.Content = label_IS_Salinity.Content;
                ForceInSituWindow.label_IS_Oxygen.Content = label_IS_Oxygen.Content;
                */


                //data.ForEach(Console.WriteLine);
            }
            catch(Exception e)
            {
                MessageBox.Show("Could not retrieve in situ data: " + e.Message + "\nCheck the internet connection.");
            }



        }


        private void checkConnection()
        {
            switch (ws.State)
            {
                case WebSocketState.Open:
                    Connect_btn.Header = "Disconnect";
                    Connect_btn.IsEnabled = true;
                    statusLabel.Text = "Connection Status: Connected";

                    sendRequest();
                    break;
                case WebSocketState.Closed:
                    Connect_btn.Header = "Connect";
                    Connect_btn.IsEnabled = true;
                    statusLabel.Text = "Connection Status: Disconnected";
                    ws = new ClientWebSocket();
                    Connect();
                    break;
                case WebSocketState.Aborted:
                    ws.Dispose();
                    ws = new ClientWebSocket();
                    Connect_btn.Header = "Connect";
                    Connect_btn.IsEnabled = true;
                    statusLabel.Text = "Connection Status: Disconnected";
                    Connect();
                    break;
                case WebSocketState.None:
                    Connect_btn.Header = "Connect";
                    Connect_btn.IsEnabled = true;
                    statusLabel.Text = "Connection Status: Disconnected";
                    Connect();
                    break;
                case WebSocketState.Connecting:
                    Connect_btn.Header = "Connecting";
                    Connect_btn.IsEnabled = false;
                    statusLabel.Text = "Connection Status: Connecting";
                    break;
            }

        }

        private void sendRequest()
        {
            string msg = "";

            var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            switch (step)
            {
                case 0:
                    msg = "{\"command\":1,\"condID\":0,\"senderID\":4}";
                    break;
                case 1:
                    msg = "{\"command\":1,\"condID\":1,\"senderID\":4}";
                    break;
                case 2:
                    msg = "{\"command\":1,\"condID\":2,\"senderID\":4}";
                    break;
                case 3:
                    msg = "{\"command\":1,\"condID\":3,\"senderID\":4}";
                    break;
                case 4:
                    msg = "{\"command\":5,\"condID\":0,\"senderID\":4,\"time\":" + Timestamp + "}";
                    break;
            }
            if (step < 4) step++; else step = 0;

            comDebugWindow.tb1.Text = msg;

                Task<string> t2 = Send(ws, msg, comDebugWindow.tb2);
                t2.Wait(50);
        }
        
        
        private static async Task Connect(ClientWebSocket ws, TextBox tb)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Token.ThrowIfCancellationRequested();
            string address = Properties.Settings.Default["MasterIPAddress"].ToString();
            Uri serverUri = new Uri("ws://"+address+":81");

            try
            {
                await ws.ConnectAsync(serverUri, cts.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            if (ws.State == WebSocketState.Open)
            {
                CancellationTokenSource cts1 = new CancellationTokenSource();
                cts1.Token.ThrowIfCancellationRequested();
                ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1024]);
                
                try
                {
                    WebSocketReceiveResult result = await ws.ReceiveAsync(
                    bytesReceived, cts1.Token);
                    tb.Text = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

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
               
                ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult result = ws.ReceiveAsync(
                        bytesReceived, timeOut).Result;
                string data = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
                tb.Text = data;
                return data;
            }
            return null;

        }



        public void DisplayData(int command)
        {

            
            //expSettingsWindow.tb_pH_setPoint.Text = conditions[0].regulpH.consigne.ToString();
            if (command == 2)//PARAMS
            {
                
            }
            else if (command == 3)//DATA
            {
                double meanTemp = 0;
                for (int i = 0; i < 3; i++)
                {
                    meanTemp += conditions[0].Meso[i].temperature / 3;
                }

                inSituData.temperature = meanTemp;
                label_C0_Temp_mean.Content = string.Format(ci, "T°C:        {0:0.00} °C", meanTemp); ;

                label_C0_pressionEA_setpoint.Content = string.Format(ci, "Pressure setpoint: {0:0.000} bars", conditions[0].regulSalinite.consigne);
                //label_C0_pressionEF_setpoint.Content = string.Format(ci, "Pressure setpoint: {0:0.000} bars", conditions[0].regulSalinite.consigne);
                label_C0_pressionEC_setpoint.Content = string.Format(ci, "Pressure setpoint: {0:0.000} bars", conditions[0].regulSalinite.consigne);
                //label_C0_Temp_setpoint.Content = string.Format(ci, "T°C:        {0:0.00} °C", conditions[0].regulTemp.consigne);
                //label_C1_Cond_setpoint.Content = string.Format(ci, "Salinity: {0:0.00}", conditions[1].regulSalinite.consigne);
                label_C1_Temp_setpoint.Content = string.Format(ci, "T°C:        {0:0.00} °C", conditions[1].regulTemp.consigne);
                //label_C2_Cond_setpoint.Content = string.Format(ci, "Salinity: {0:0.00}", conditions[2].regulSalinite.consigne);
                label_C2_Temp_setpoint.Content = string.Format(ci, "T°C:        {0:0.00} °C", conditions[2].regulTemp.consigne);
                //label_C3_Cond_setpoint.Content = string.Format(ci, "Salinity: {0:0.00}", conditions[3].regulSalinite.consigne);
                label_C3_Temp_setpoint.Content = string.Format(ci, "T°C:        {0:0.00} °C", conditions[3].regulTemp.consigne);


                if (conditions[0].regulSalinite.autorisationForcage)
                {
                    label_C0_PressionEA_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[0].regulSalinite.consigneForcage);
                    //label_C0_PressionEF_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[0].regulSalinite.consigneForcage);
                    label_C0_PressionEC_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[0].regulSalinite.consigneForcage);
                    label_C0_PressionEA_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    //label_C0_PressionEF_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C0_PressionEC_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    label_C0_PressionEA_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[0].Meso[0].salSortiePID_pc);
                    //label_C0_PressionEF_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[0].Meso[1].salSortiePID_pc);
                    label_C0_PressionEC_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[0].Meso[2].salSortiePID_pc);
                    label_C0_PressionEA_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    //label_C0_PressionEF_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C0_PressionEC_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                }

                /*if (conditions[0].regulTemp.autorisationForcage)
                {
                    label_C0M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[0].regulTemp.consigneForcage);
                    label_C0M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[0].regulTemp.consigneForcage);
                    label_C0M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[0].regulTemp.consigneForcage);
                    label_C0M0_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C0M1_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C0M2_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    label_C0M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[0].Meso[0].tempSortiePID_pc);
                    label_C0M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[0].Meso[1].tempSortiePID_pc);
                    label_C0M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[0].Meso[2].tempSortiePID_pc);
                    label_C0M0_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C0M1_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C0M2_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                }



                if (conditions[1].regulSalinite.autorisationForcage)
                {
                    label_C1M0_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[1].regulSalinite.consigneForcage);
                    label_C1M1_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[1].regulSalinite.consigneForcage);
                    label_C1M2_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[1].regulSalinite.consigneForcage);
                    label_C1M0_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C1M1_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C1M2_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    label_C1M0_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[1].Meso[0].salSortiePID_pc);
                    label_C1M1_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[1].Meso[1].salSortiePID_pc);
                    label_C1M2_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[1].Meso[2].salSortiePID_pc);
                    label_C1M0_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C1M1_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C1M2_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                }*/

                if (conditions[1].regulTemp.autorisationForcage)
                {
                    label_C1M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[1].regulTemp.consigneForcage);
                    label_C1M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[1].regulTemp.consigneForcage);
                    label_C1M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[1].regulTemp.consigneForcage);
                    label_C1M0_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C1M1_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C1M2_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    label_C1M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[1].Meso[0].tempSortiePID_pc);
                    label_C1M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[1].Meso[1].tempSortiePID_pc);
                    label_C1M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[1].Meso[2].tempSortiePID_pc);
                    label_C1M0_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C1M1_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C1M2_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                }

                /*if (conditions[2].regulSalinite.autorisationForcage)
                {
                    label_C2M0_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[2].regulSalinite.consigneForcage);
                    label_C2M1_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[2].regulSalinite.consigneForcage);
                    label_C2M2_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[2].regulSalinite.consigneForcage);
                    label_C2M0_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C2M1_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C2M2_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    label_C2M0_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[2].Meso[0].salSortiePID_pc);
                    label_C2M1_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[2].Meso[1].salSortiePID_pc);
                    label_C2M2_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0}%", conditions[2].Meso[2].salSortiePID_pc);
                    label_C2M0_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C2M1_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C2M2_Cond_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                }*/

                if (conditions[2].regulTemp.autorisationForcage)
                {
                    label_C2M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[2].regulTemp.consigneForcage);
                    label_C2M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[2].regulTemp.consigneForcage);
                    label_C2M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[2].regulTemp.consigneForcage);
                    label_C2M0_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C2M1_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C2M2_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    label_C2M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[2].Meso[0].tempSortiePID_pc);
                    label_C2M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[2].Meso[1].tempSortiePID_pc);
                    label_C2M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[2].Meso[2].tempSortiePID_pc);
                    label_C2M0_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C2M1_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C2M2_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                }

               

                if (conditions[3].regulTemp.autorisationForcage)
                {
                    label_C3M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[3].regulTemp.consigneForcage);
                    label_C3M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[3].regulTemp.consigneForcage);
                    label_C3M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[3].regulTemp.consigneForcage);
                    label_C3M0_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C3M1_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                    label_C3M2_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    label_C3M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[3].Meso[0].tempSortiePID_pc);
                    label_C3M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[3].Meso[1].tempSortiePID_pc);
                    label_C3M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0}%", conditions[3].Meso[2].tempSortiePID_pc);
                    label_C3M0_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C3M1_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                    label_C3M2_Temp_sortiePID.Foreground = System.Windows.Media.Brushes.Black;
                }

                label_C0M0_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[0].Meso[0].debit);
                label_C0M1_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[0].Meso[1].debit);
                label_C0M2_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[0].Meso[2].debit);
                label_C0M0_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[0].Meso[0].oxy_pc);
                label_C0M1_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[0].Meso[1].oxy_pc);
                label_C0M2_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[0].Meso[2].oxy_pc);
                label_C0M0_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[0].Meso[0].salinite);
                label_C0M1_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[0].Meso[1].salinite);
                label_C0M2_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[0].Meso[2].salinite);
                label_C0M0_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[0].Meso[0].temperature);
                label_C0M1_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[0].Meso[1].temperature);
                label_C0M2_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[0].Meso[2].temperature);

                label_C1M0_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[1].Meso[0].debit);
                label_C1M1_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[1].Meso[1].debit);
                label_C1M2_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[1].Meso[2].debit);
                label_C1M0_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[1].Meso[0].oxy_pc);
                label_C1M1_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[1].Meso[1].oxy_pc);
                label_C1M2_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[1].Meso[2].oxy_pc);
                label_C1M0_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[1].Meso[0].salinite);
                label_C1M1_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[1].Meso[1].salinite);
                label_C1M2_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[1].Meso[2].salinite);
                label_C1M0_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[1].Meso[0].temperature);
                label_C1M1_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[1].Meso[1].temperature);
                label_C1M2_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[1].Meso[2].temperature);

                label_C2M0_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[2].Meso[0].debit);
                label_C2M1_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[2].Meso[1].debit);
                label_C2M2_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[2].Meso[2].debit);
                label_C2M0_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[2].Meso[0].oxy_pc);
                label_C2M1_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[2].Meso[1].oxy_pc);
                label_C2M2_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[2].Meso[2].oxy_pc);
                label_C2M0_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[2].Meso[0].salinite);
                label_C2M1_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[2].Meso[1].salinite);
                label_C2M2_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[2].Meso[2].salinite);
                label_C2M0_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[2].Meso[0].temperature);
                label_C2M1_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[2].Meso[1].temperature);
                label_C2M2_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[2].Meso[2].temperature);

                label_C3M0_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[3].Meso[0].debit);
                label_C3M1_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[3].Meso[1].debit);
                label_C3M2_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} L/min", conditions[3].Meso[2].debit);
                label_C3M0_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[3].Meso[0].oxy_pc);
                label_C3M1_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[3].Meso[1].oxy_pc);
                label_C3M2_O2.Content = string.Format(ci, "02:           {0:0.00}%", conditions[3].Meso[2].oxy_pc);
                label_C3M0_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[3].Meso[0].salinite);
                label_C3M1_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[3].Meso[1].salinite);
                label_C3M2_Cond.Content = string.Format(ci, "Salinity:   {0:0.00}", conditions[3].Meso[2].salinite);
                label_C3M0_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[3].Meso[0].temperature);
                label_C3M1_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[3].Meso[1].temperature);
                label_C3M2_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[3].Meso[2].temperature);

                double totalFlowrate = 0;

                for(int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 3; j++) totalFlowrate += conditions[i].Meso[j].debit;
                }

                label_total_flowrate.Content = string.Format(ci, "Total Flowrate: {0:0.00} L/min", totalFlowrate);
            }
            else if (command == 6)//MASTER DATA
            {
                //TODO
                //label_C0_debitEA.Content = string.Format(ci, "Flowrate: \t{0:0.00} l/mn", masterData.debitEA);
                //label_C0_debitEF.Content = string.Format(ci, "Flowrate: \t{0:0.00} l/mn", masterData.debitEF);
                //label_C0_debitEC.Content = string.Format(ci, "Flowrate: \t{0:0.00} l/mn", masterData.debitEC);
                label_C0_pressionEA_measure.Content = string.Format(ci, "Pressure measure: {0:0.000} bars", masterData.pressionEA);
                //label_C0_pressionEF_measure.Content = string.Format(ci, "Pressure measure: {0:0.000} bars", masterData.pressionEF);
                label_C0_pressionEC_measure.Content = string.Format(ci, "Pressure measure: {0:0.000} bars", masterData.pressionEC);
            }
        }




        private void sendParams()
        {
            string msg;

            for (int i = 0; i < 4; i++)
            {
                if (conditions[i].regulTemp.Kp == 0) msg = "{command:0,condID:"+i+", senderID:4}";
                else
                {
                    expSettingsWindow.calculConsignes(i);
                    msg = expSettingsWindow.buildJsonParams(conditions[i]);
                    comDebugWindow.tb1.Text = msg;
                }
                

                if (ws.State == WebSocketState.Open)
                {
                    Task<string> t2 = Send(ws, msg, comDebugWindow.tb2);
                    t2.Wait(50);
                }
                else
                {
                    Connect();
                }
            }
        }

        private void Connect()
        {
            
            if (ws.State != WebSocketState.Open)
            {
                Task t = Connect(ws, comDebugWindow.tb2);
                t.Wait(50);
                Connect_btn.Header = "Connecting";
                Connect_btn.IsEnabled = false;
                
            }
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
            while (true)
            {
                // Call our onTick function.
                onTick?.Invoke();

                // Wait to repeat again.
                if (interval > TimeSpan.Zero)
                    await Task.Delay(interval, CancellationToken.None);
            }
        }

        private async Task InitializeAsync()
        {
            int t;
            Int32.TryParse(Properties.Settings.Default["dataQueryInterval"].ToString(), out t);
            var dueTime = TimeSpan.FromSeconds(0);
            var interval = TimeSpan.FromSeconds(t);

            var cancel = new CancellationTokenSource();
            cancel.Token.ThrowIfCancellationRequested();

            // TODO: Add a CancellationTokenSource and supply the token here instead of None.
            try
            {

                await RunPeriodicAsync(checkConnection, dueTime, interval, cancel.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await InitializeAsync();
            }
        }

        private async Task InitializeAsyncGetInSituData()
        {
            int t;
            Int32.TryParse(Properties.Settings.Default["dataQueryInterval"].ToString(), out t);
            var dueTime = TimeSpan.FromSeconds(0);
            var interval = TimeSpan.FromHours(1);

            var cancel = new CancellationTokenSource();
            cancel.Token.ThrowIfCancellationRequested();

            try
            {

                await RunPeriodicAsync(getInSituData, dueTime, interval, cancel.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await InitializeAsyncGetInSituData();
            }
        }

        private async Task InitializeAsyncSendParams()
        {
            int t;
            //Int32.TryParse(Properties.Settings.Default["dataQueryInterval"].ToString(), out t);
            var dueTime = TimeSpan.FromSeconds(5);
            var interval = TimeSpan.FromSeconds(5);

            // TODO: Add a CancellationTokenSource and supply the token here instead of None.

            var cancel = new CancellationTokenSource();
            cancel.Token.ThrowIfCancellationRequested();

            // TODO: Add a CancellationTokenSource and supply the token here instead of None.
            try
            {

                await RunPeriodicAsync(sendParams, dueTime, interval, cancel.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await InitializeAsyncSendParams();
            }
        }

        private async Task InitializeAsyncAlarms()
        {

            var dueTime = TimeSpan.FromSeconds(10);
            var interval = TimeSpan.FromSeconds(5);

            var cancel = new CancellationTokenSource();
            cancel.Token.ThrowIfCancellationRequested();

            // TODO: Add a CancellationTokenSource and supply the token here instead of None.
            try
            {

                await RunPeriodicAsync(checkAlarms, dueTime, interval, cancel.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await InitializeAsyncAlarms();
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            switch (ws.State)
            {
                case WebSocketState.Open:
                    try
                    {
                        ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    break;
                case WebSocketState.Closed:
                case WebSocketState.Aborted:
                    ws.Dispose();
                    ws = new ClientWebSocket();
                    Connect();
                    break;
                case WebSocketState.None:
                    Connect();
                    break;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            CancelEventArgs ce = new CancelEventArgs();
            Window_Closing(sender, ce);
        }

        private void AppSettings_Click(object sender, RoutedEventArgs e)
        {
            AppSettingsWindow appSettingsWindow = new AppSettingsWindow();
            appSettingsWindow.Show();
        }

        private void ExpSettings_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 4; i++) expSettingsWindow.load(i);
            expSettingsWindow.Show();
            expSettingsWindow.Focus();
        }

        private void Calibrate_btn_Click(object sender, RoutedEventArgs e)
        {
            calibrationWindow.Show();
            calibrationWindow.Focus();
        }


        

        private void ComDebug_Click(object sender, RoutedEventArgs e)
        {
            comDebugWindow.Show();
            comDebugWindow.Focus();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Alarme alarm = new Alarme();
            alarm.sendSlackMessage("APPLICATION CLOSED");

            alarmsListWindow.Close();
            comDebugWindow.Close();
            expSettingsWindow.Close();
            Properties.Settings.Default.Save();
            System.Windows.Application.Current.Shutdown();
        }

        private void Ellipse_MouseDown_1(object sender, MouseButtonEventArgs e)
        {

        }

        private void Ellipse_MouseDown_2(object sender, MouseButtonEventArgs e)
        {

        }

        private void Monitoring_btn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Properties.Settings.Default["InfluxDBWebpage"].ToString());
        }

        private void RData_btn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.obs-vlfr.fr/~gazeau/FACE-IT/FACE-IT.html");
        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            ExperimentState = !ExperimentState;

            if (ExperimentState)
            {
                btn_stop.Content = "STOP";
                statusLabel2.Text = "Experiment is running normally";
                statusLabel2.Background = Brushes.Green;
                statusLabel2.Foreground = Brushes.White;

                foreach (Condition c in conditions)
                {
                    c.regulSalinite.autorisationForcage = false;

                    string msg = buildJsonParams(c);

                    ((MainWindow)Application.Current.MainWindow).comDebugWindow.tb1.Text = msg;


                    if (((MainWindow)Application.Current.MainWindow).ws.State == WebSocketState.Open)
                    {
                        Task<string> t2 = Send(((MainWindow)Application.Current.MainWindow).ws, msg, ((MainWindow)Application.Current.MainWindow).comDebugWindow.tb2);
                        t2.Wait(50);
                    }
                }

            }
            else
            {
                btn_stop.Content = "START";
                statusLabel2.Text = "Experiment is stopped: General valves are closed, Data logging is stopped";
                statusLabel2.Background = Brushes.Red;
                statusLabel2.Foreground = Brushes.Black;

                foreach (Condition c in conditions)
                {
                    c.regulSalinite.autorisationForcage = true;
                    c.regulSalinite.consigneForcage = 0;

                    string msg = buildJsonParams(c);

                    ((MainWindow)Application.Current.MainWindow).comDebugWindow.tb1.Text = msg;


                    if (((MainWindow)Application.Current.MainWindow).ws.State == WebSocketState.Open)
                    {
                        Task<string> t2 = Send(((MainWindow)Application.Current.MainWindow).ws, msg, ((MainWindow)Application.Current.MainWindow).comDebugWindow.tb2);
                        t2.Wait(50);
                    }
                }

            }
            //Close the 3 main valves

            

            //stop data logging
        }

        public string buildJsonParams(Condition c)
        {
            string msg = "{command:2,condID:" + c.condID + ",senderID:4,";

            msg += "\"regulT\":{";
            msg += "\"offset\":" + c.regulTemp.offset.ToString() + ",";
            msg += "\"cons\":" + c.regulTemp.consigne.ToString() + ",";
            msg += "\"Kp\":" + c.regulTemp.Kp.ToString() + ",";
            msg += "\"Ki\":" + c.regulTemp.Ki.ToString() + ",";
            msg += "\"Kd\":" + c.regulTemp.Kd.ToString() + ",";
            msg += "\"consForcage\":" + c.regulTemp.consigneForcage + ",";
            msg += "\"autForcage\":\"" + c.regulTemp.autorisationForcage + "\"},";


            msg += "\"regulS\":{\"offset\":" + c.regulSalinite.offset.ToString() + ",";
            msg += "\"cons\":" + c.regulSalinite.consigne.ToString() + ",";
            msg += "\"Kp\":" + c.regulSalinite.Kp.ToString() + ",";
            msg += "\"Ki\":" + c.regulSalinite.Ki.ToString() + ",";
            msg += "\"Kd\":" + c.regulSalinite.Kd.ToString() + ",";
            msg += "\"consForcage\":" + c.regulSalinite.consigneForcage + ",";
            msg += "\"autForcage\":\"" + c.regulSalinite.autorisationForcage + "\"}";

            msg += "}";

            return msg;
        }

        private void Documentation_btn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("C:/Users/FACE-IT/Desktop/FACEIT/Appli - FACEIT/FACE-IT - experimental system documentation.pdf");
        }

        private void ForceInSitu_Click(object sender, RoutedEventArgs e)
        {
            ForceInSituWindow.Show();
            ForceInSituWindow.Focus();
        }

        private void AlarmsSettings_Click(object sender, RoutedEventArgs e)
        {
            Alarms alarmsWindow = new Alarms();
            alarmsWindow.Show();
        }

        private void AlarmsList_Click(object sender, RoutedEventArgs e)
        {
            alarmsListWindow.Show();
            alarmsListWindow.Focus();
        }


        private void checkAlarme(string libelle, double value, double t)
        {
            try
            {
                Alarme a = alarms.Single(alarm => alarm.libelle == libelle);
                a.threshold = t;
                a.checkAndRaise(value);
            }
            catch (Exception e)
            {

            }

        }

        private void checkAlarme(string libelle, bool value, bool threshold)
        {


            try
            {
                Alarme a = alarms.Single(alarm => alarm.libelle == libelle);
                a.checkAndRaise(value, threshold);
            }
            catch (Exception e)
            {

            }
        }

        private void checkAlarms()
        {
            double d;

            string cond, meso;
            checkAlarme("C0_Alarm Pressure Ambiant Water", masterData.pressionEA, conditions[0].regulSalinite.consigne);
            checkAlarme("C0_Alarm Pressure Hot Water", masterData.pressionEC, conditions[0].regulSalinite.consigne);



            for (int i = 0; i < 4; i++) //conditions
            {
                cond = string.Format(ci, "C{0:0}", i);


                for (int j = 0; j < 3; j++)//mesocosmes
                {
                    meso = string.Format(ci, "M{0:0}", j);


                    Double.TryParse(Properties.Settings.Default["FlowrateSetpoint"].ToString(), out d);

                    checkAlarme(cond + meso + "_Alarm Flowrate", conditions[i].Meso[j].debit, d);
                    checkAlarme(cond + meso + "_Alarm Temperature", conditions[i].Meso[j].temperature, conditions[i].regulTemp.consigne);

                }
            }

            if (alarmsListWindow._lastHeaderClicked != null) alarmsListWindow.Sort(alarmsListWindow._lastHeaderClicked.Tag as string, alarmsListWindow._lastDirection);
            else alarmsListWindow.Sort("raised", alarmsListWindow._lastDirection);
        }

        public unsafe void setAlarms()
        {
            alarms.Clear();


            string cond;
            string meso;
            bool e;
            double d;


            cond = "C0";
            Boolean.TryParse(Properties.Settings.Default["AlarmPressure"].ToString(), out e);
            Double.TryParse(Properties.Settings.Default["PressureDelta"].ToString(), out d);
            Alarme a = new Alarme();
            a.set(cond + "_Alarm Pressure Ambiant Water", e, 2, d, TimeSpan.FromSeconds(30));
            alarms.Add(a);
            Alarme b = new Alarme();
            b.set(cond + "_Alarm Pressure Hot Water", e, 2, d, TimeSpan.FromSeconds(30));
            alarms.Add(b);

            for (int i = 0; i < 4; i++) //conditions
            {
                cond = string.Format(ci, "C{0:0}", i);

                for (int j = 0; j < 3; j++)//mesocosmes
                {
                    meso = string.Format(ci, "M{0:0}", j);

                    Double.TryParse(Properties.Settings.Default["FlowrateDelta"].ToString(), out d);
                    Boolean.TryParse(Properties.Settings.Default["AlarmFlowrate"].ToString(), out e);

                    Alarme l = new Alarme();
                    l.set(cond + meso + "_Alarm Flowrate", e, 2, d, TimeSpan.FromSeconds(30));
                    alarms.Add(l);

                    Double.TryParse(Properties.Settings.Default["MesocosmTempDelta"].ToString(), out d);
                    Boolean.TryParse(Properties.Settings.Default["AlarmtempMesocosm"].ToString(), out e);
                    Alarme n = new Alarme();
                    n.set(cond + meso + "_Alarm Temperature", e, 2, d, TimeSpan.FromSeconds(30));
                    alarms.Add(n);

                }
            }
        }
    }
}
