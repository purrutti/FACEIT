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
using System.Net.WebSockets;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using LiveCharts;
using LiveCharts.Configurations;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Appli_CocoriCO2
{

    public class InSituData
    {

        [JsonProperty(Required = Required.Default)]
        public IList<IList<string>> data { get; set; }

        public DateTime time { get; set; }
        public double salinite { get; set; }
        public double temperature { get; set; }
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
        [JsonProperty("MesoID",Required = Required.Default)]
        public int mesocosmeID{ get; set; }

        [JsonProperty(Required = Required.Default)]
        public double debit { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double temperature { get; set; }
        [JsonProperty(Required = Required.Default)] 
        public double oxy { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double cond { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double salinite { get; set; }

        [JsonProperty(Required = Required.Default)]
        public double tempSortiePID_pc { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double salSortiePID_pc { get; set; }

    }
    public class Regul
    {
        [JsonProperty(Required = Required.Default)]
        public double sortiePID { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double consigne { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double Kp { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double Ki { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double Kd { get; set; }
        [JsonProperty(Required = Required.Default)]
        public double sortiePID_pc { get; set; }
        [JsonProperty(Required = Required.Default)]
        public bool autorisationForcage { get; set; }
        [JsonProperty(Required = Required.Default)]
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
        [JsonProperty("regulTemp", Required = Required.Default)]
        public Regul regulTemp { get; set; }
        [JsonProperty("regulSalinite", Required = Required.Default)]
        public Regul regulSalinite { get; set; }
        public long time { get; set; }
        public DateTime lastUpdated { get; set; }

    }

    
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //public List<Condition> conditions;
        public ObservableCollection<Condition> conditions;
        public ObservableCollection<Condition> conditionData;
        public MasterData masterData = new MasterData();
        public InSituData inSituData = new InSituData();
        

        //public List<Condition> conditionData;
        public ExpSettingsWindow expSettingsWindow;
        public ComDebugWindow comDebugWindow;
        public CultureInfo ci;

        public ClientWebSocket ws = new ClientWebSocket();
        bool autoReco;
        public int step;

        CancellationTokenSource cancelToken = new CancellationTokenSource();


        public string[] Labels = new[] {"0"};
        public MainWindow()
        {
            InitializeComponent();
            var cts = new CancellationTokenSource();

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

            

            InitializeAsync();
            InitializeAsyncSendParams();
            

            comDebugWindow.lv_data.ItemsSource = conditionData;
            ci = new CultureInfo("en-US");
            ci.NumberFormat.NumberDecimalDigits = 2;
            ci.NumberFormat.NumberDecimalSeparator = ".";
            ci.NumberFormat.NumberGroupSeparator = " ";
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;

            getInSituData();
        }


        private async void getInSituData()
        {
            var client = new HttpClient();

            //https://dashboard.awi.de/data-xxl/rest/data?beginDate=2020-10-01T00:01:00&endDate=2021-10-01T00:01:00&format=application/json&aggregate=DAY&sensors=station:svluwobs:fb_731101:sbe45_awi_0403:salinity&sensors=station:svluwobs:fb_731101:sbe45_awi_0403:temperature

            client.BaseAddress = new Uri("https://dashboard.awi.de");
            client.DefaultRequestHeaders.Add("User-Agent", "C# console program");
            client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

            string fromDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss");
            string toDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            var url = "data-xxl/rest/data?beginDate="+fromDate+"&endDate="+toDate+"&format=application/json&aggregate=HOUR&sensors=station:svluwobs:fb_731101:sbe45_awi_0403:salinity&sensors=station:svluwobs:fb_731101:sbe45_awi_0403:temperature";
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var resp = await response.Content.ReadAsStringAsync();

            inSituData = JsonConvert.DeserializeObject<InSituData>(resp);
            IList<string> d = inSituData.data.Last<IList<string>>();
            inSituData.time = DateTime.Parse(d.ElementAt<string>(0).ToString());
            
            double s, t;

            double.TryParse(d.ElementAt<string>(1).ToString(), out s);
            double.TryParse(d.ElementAt<string>(2).ToString(), out t);

            inSituData.salinite = s;
            inSituData.temperature = t;

            label_IS_Time.Content = "Time: " + inSituData.time.ToString("yyyy-MM-dd HH:mm:ss");
            label_IS_Temp.Content = string.Format(ci, "Temperature: {0:0.00}°C", inSituData.temperature);
            label_IS_Salinity.Content = string.Format(ci, "Salinity: {0:0.00}g/kg", inSituData.salinite);
            //data.ForEach(Console.WriteLine);
        }



        
        private static async Task Connect(ClientWebSocket ws, TextBox tb)
        {
            string address = Properties.Settings.Default["MasterIPAddress"].ToString();
            Uri serverUri = new Uri("ws://"+address+":81");
                await ws.ConnectAsync(serverUri, CancellationToken.None);
            if (ws.State == WebSocketState.Open)
            {
                ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1024]);
                WebSocketReceiveResult result = await ws.ReceiveAsync(
                    bytesReceived, CancellationToken.None);
                tb.Text = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
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
            label_time.Content = "Time: " +DateTime.Now.ToUniversalTime().ToString("dd/MM/yyyy hh:mm")+ " UTC";
            
            //expSettingsWindow.tb_pH_setPoint.Text = conditions[0].regulpH.consigne.ToString();
            if (command == 2)//PARAMS
            {
                
            }
            else if (command == 3)//DATA
            {
                label_C0_PressionEA_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[0].Meso[0].salSortiePID_pc);
                label_C0_PressionEF_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[0].Meso[1].salSortiePID_pc);
                label_C0_PressionEC_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[0].Meso[2].salSortiePID_pc);
                label_C0M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[0].Meso[0].tempSortiePID_pc);
                label_C0M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[0].Meso[1].tempSortiePID_pc);
                label_C0M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[0].Meso[2].tempSortiePID_pc);

                label_C0_pressionEA_setpoint.Content = string.Format(ci, "Pressure setpoint: {0:0.000} bars", conditions[0].regulSalinite.consigne);
                label_C0_pressionEF_setpoint.Content = string.Format(ci, "Pressure setpoint: {0:0.000} bars", conditions[0].regulSalinite.consigne);
                label_C0_pressionEC_setpoint.Content = string.Format(ci, "Pressure setpoint: {0:0.000} bars", conditions[0].regulSalinite.consigne);

                label_C0_Temp_setpoint.Content = string.Format(ci, "T°C: {0:0.00} °C", conditions[0].regulTemp.consigne);

                label_C1_Cond_setpoint.Content = string.Format(ci, "Salinity: {0:0.00} g/kg", conditions[1].regulSalinite.consigne);
                label_C1M0_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[1].Meso[0].salSortiePID_pc);
                label_C1M1_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[1].Meso[1].salSortiePID_pc);
                label_C1M2_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[1].Meso[2].salSortiePID_pc);
                label_C1_Temp_setpoint.Content = string.Format(ci, "T°C: {0:0.00} °C", conditions[1].regulTemp.consigne);
                label_C1M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[1].Meso[0].tempSortiePID_pc);
                label_C1M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[1].Meso[1].tempSortiePID_pc);
                label_C1M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[1].Meso[2].tempSortiePID_pc);

                label_C2_Cond_setpoint.Content = string.Format(ci, "Salinity: {0:0.00} g/kg", conditions[2].regulSalinite.consigne);
                label_C2M0_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[2].Meso[0].salSortiePID_pc);
                label_C2M1_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[2].Meso[1].salSortiePID_pc);
                label_C2M2_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[2].Meso[2].salSortiePID_pc);
                label_C2_Temp_setpoint.Content = string.Format(ci, "T°C: {0:0.00} °C", conditions[2].regulTemp.consigne);
                label_C2M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[2].Meso[0].tempSortiePID_pc);
                label_C2M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[2].Meso[1].tempSortiePID_pc);
                label_C2M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[2].Meso[2].tempSortiePID_pc);

                label_C3_Cond_setpoint.Content = string.Format(ci, "Salinity: {0:0.00} g/kg", conditions[3].regulSalinite.consigne);
                label_C3M0_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[3].Meso[0].salSortiePID_pc);
                label_C3M1_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[3].Meso[1].salSortiePID_pc);
                label_C3M2_Cond_sortiePID.Content = string.Format(ci, "Valve: {0:0} %", conditions[3].Meso[2].salSortiePID_pc);
                label_C3_Temp_setpoint.Content = string.Format(ci, "T°C: {0:0.00} °C", conditions[3].regulTemp.consigne);
                label_C3M0_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[3].Meso[0].tempSortiePID_pc);
                label_C3M1_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[3].Meso[1].tempSortiePID_pc);
                label_C3M2_Temp_sortiePID.Content = string.Format(ci, "V3V: {0:0} %", conditions[3].Meso[2].tempSortiePID_pc);

                label_C0M0_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[0].Meso[0].debit);
                label_C0M1_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[0].Meso[1].debit);
                label_C0M2_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[0].Meso[2].debit);
                label_C0M0_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[0].Meso[0].oxy);
                label_C0M1_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[0].Meso[1].oxy);
                label_C0M2_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[0].Meso[2].oxy);
                label_C0M0_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[0].Meso[0].cond);
                label_C0M1_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[0].Meso[1].cond);
                label_C0M2_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[0].Meso[2].cond);
                label_C0M0_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[0].Meso[0].temperature);
                label_C0M1_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[0].Meso[1].temperature);
                label_C0M2_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[0].Meso[2].temperature);

                label_C1M0_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[1].Meso[0].debit);
                label_C1M1_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[1].Meso[1].debit);
                label_C1M2_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[1].Meso[2].debit);
                label_C1M0_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[1].Meso[0].oxy);
                label_C1M1_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[1].Meso[1].oxy);
                label_C1M2_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[1].Meso[2].oxy);
                label_C1M0_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[1].Meso[0].cond);
                label_C1M1_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[1].Meso[1].cond);
                label_C1M2_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[1].Meso[2].cond);
                label_C1M0_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[1].Meso[0].temperature);
                label_C1M1_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[1].Meso[1].temperature);
                label_C1M2_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[1].Meso[2].temperature);

                label_C2M0_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[2].Meso[0].debit);
                label_C2M1_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[2].Meso[1].debit);
                label_C2M2_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[2].Meso[2].debit);
                label_C2M0_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[2].Meso[0].oxy);
                label_C2M1_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[2].Meso[1].oxy);
                label_C2M2_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[2].Meso[2].oxy);
                label_C2M0_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[2].Meso[0].cond);
                label_C2M1_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[2].Meso[1].cond);
                label_C2M2_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[2].Meso[2].cond);
                label_C2M0_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[2].Meso[0].temperature);
                label_C2M1_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[2].Meso[1].temperature);
                label_C2M2_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[2].Meso[2].temperature);

                label_C3M0_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[3].Meso[0].debit);
                label_C3M1_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[3].Meso[1].debit);
                label_C3M2_Flowrate.Content = string.Format(ci, "Flowrate: {0:0.00} l/s", conditions[3].Meso[2].debit);
                label_C3M0_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[3].Meso[0].oxy);
                label_C3M1_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[3].Meso[1].oxy);
                label_C3M2_O2.Content = string.Format(ci, "02:           {0:0.00} mg/L", conditions[3].Meso[2].oxy);
                label_C3M0_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[3].Meso[0].cond);
                label_C3M1_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[3].Meso[1].cond);
                label_C3M2_Cond.Content = string.Format(ci, "Salinity:   {0:0.00} g/kg", conditions[3].Meso[2].cond);
                label_C3M0_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[3].Meso[0].temperature);
                label_C3M1_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[3].Meso[1].temperature);
                label_C3M2_Temp.Content = string.Format(ci, "T°C:         {0:0.00} °C", conditions[3].Meso[2].temperature);
            }
            else if (command == 6)//MASTER DATA
            {
                //TODO
                label_C0_debitEA.Content = string.Format(ci, "Flowrate: \t{0:0.00} l/s", masterData.debitEA);
                label_C0_debitEF.Content = string.Format(ci, "Flowrate: \t{0:0.00} l/s", masterData.debitEF);
                label_C0_debitEC.Content = string.Format(ci, "Flowrate: \t{0:0.00} l/s", masterData.debitEC);
                label_C0_pressionEA_measure.Content = string.Format(ci, "Pressure measure: {0:0.000} bars", masterData.pressionEA);
                label_C0_pressionEF_measure.Content = string.Format(ci, "Pressure measure: {0:0.000} bars", masterData.pressionEF);
                label_C0_pressionEC_measure.Content = string.Format(ci, "Pressure measure: {0:0.000} bars", masterData.pressionEC);
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
                    break;
                case WebSocketState.Closed:
                case WebSocketState.Aborted:
                case WebSocketState.None:
                    Connect_btn.Header = "Connect";
                    Connect_btn.IsEnabled = true;
                    statusLabel.Text = "Connection Status: Disconnected";
                    break;
                case WebSocketState.Connecting:
                    Connect_btn.Header = "Connecting";
                    Connect_btn.IsEnabled = false;
                    statusLabel.Text = "Connection Status: Connecting";
                    break;
            }

            string msg = "";

            var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                switch (step)
                {
                    case 0:
                        msg = "{command:1,condID:0, senderID:4}";
                        break;
                    case 1:
                        msg = "{command:1,condID:1, senderID:4}";
                        break;
                    case 2:
                        msg = "{command:1,condID:2, senderID:4}";
                        break;
                    case 3:
                        msg = "{command:1,condID:3, senderID:4}";
                        break;
                    case 4:
                        msg = "{command:5,condID:0, senderID:4, time:" + Timestamp + "}";
                        break;
                }
                if (step < 4) step++; else step = 0;
            
            

            comDebugWindow.tb1.Text = msg;

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
            var dueTime = TimeSpan.FromSeconds(t);
            var interval = TimeSpan.FromSeconds(t);

            // TODO: Add a CancellationTokenSource and supply the token here instead of None.
            await RunPeriodicAsync(checkConnection, dueTime, interval, CancellationToken.None);
        }

        private async Task InitializeAsyncSendParams()
        {
            int t;
            //Int32.TryParse(Properties.Settings.Default["dataQueryInterval"].ToString(), out t);
            var dueTime = TimeSpan.FromSeconds(5);
            var interval = TimeSpan.FromSeconds(5);

            // TODO: Add a CancellationTokenSource and supply the token here instead of None.
            await RunPeriodicAsync(sendParams, dueTime, interval, CancellationToken.None);
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

        }

        private void CleanUp_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ManualOverride_Click(object sender, RoutedEventArgs e)
        {

        }

        

        private void ComDebug_Click(object sender, RoutedEventArgs e)
        {
            comDebugWindow.Show();
            comDebugWindow.Focus();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            comDebugWindow.Close();
            expSettingsWindow.Close();
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
    }
}
