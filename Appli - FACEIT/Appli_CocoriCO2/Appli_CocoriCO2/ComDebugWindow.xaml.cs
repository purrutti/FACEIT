using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using LiveCharts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
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
    /// Logique d'interaction pour ComDebugWindow.xaml
    /// </summary>
    public partial class ComDebugWindow : Window
    {
        MainWindow MW = ((MainWindow)Application.Current.MainWindow);
        DateTime lastFileWrite = DateTime.Now.ToUniversalTime();

        string token = Properties.Settings.Default["InfluxDBToken"].ToString();
        string bucket = Properties.Settings.Default["InfluxDBBucket"].ToString();
        string org = Properties.Settings.Default["InfluxDBOrg"].ToString();

        InfluxDBClient client;
        CancellationTokenSource cts = new CancellationTokenSource();

        public ComDebugWindow()
        {
            InitializeComponent();
            InitializeAsync();
            client = InfluxDBClientFactory.Create("http://localhost:8086", token.ToCharArray());
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
            var dueTime = TimeSpan.FromMinutes(t);
            var interval = TimeSpan.FromMinutes(t);

            // TODO: Add a CancellationTokenSource and supply the token here instead of None.
            await RunPeriodicAsync(saveData, dueTime, interval, cts.Token);
        }


        private void tb2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tb2.Text.Length > 0)
            {
                ReadData(tb2.Text);
            }
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
                else if (c.command == 3 && (c.Meso != null))
                {
                    //MW.conditions[c.condID].regulSalinite.consigne = c.regulSalinite.consigne;
                    //MW.conditions[c.condID].regulTemp.consigne = c.regulTemp.consigne;
                    for (int i = 0; i < 3; i++) MW.conditions[c.condID].Meso[i] = c.Meso[i];
                }
                else if (c.command == 6)
                {
                    MW.masterData = JsonConvert.DeserializeObject<MasterData>(data);
                    MW.masterData.lastUpdated = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(MW.masterData.time);
                    MW.statusLabel1.Text = "Last updated: " + MW.masterData.lastUpdated.ToString() + " UTC";
                }

                if (!(bool)MW.ForceInSituWindow.checkBox_ForceInSitu.IsChecked) MW.conditions[0].regulTemp.consigne = MW.inSituData.temperature;
                else {
                    double temp;
                    Double.TryParse(MW.ForceInSituWindow.tb_temperature.Text, out temp);
                    MW.conditions[0].regulTemp.consigne = temp;
                }
                MW.DisplayData(c.command);


            }
            catch (Exception e)
            {

            }
        }

        private void saveData()
        {
            try
            {
                Condition c = MW.conditionData.Last<Condition>();
                if (c.lastUpdated != lastFileWrite)
                {
                    DateTime dt = DateTime.Now;
                    string filePath = Properties.Settings.Default["dataFileBasePath"].ToString() + "_" + dt.ToString("yyyy_MM_dd") + ".csv";
                    filePath = filePath.Replace('\\', '/');

                    saveToFile(filePath, dt);
                    //if (c.lastUpdated.Day != lastFileWrite.Day) ftpTransfer(filePath);
                    if (c.lastUpdated.Hour != lastFileWrite.Hour)// POur tester
                    {
                        if (c.lastUpdated.Hour == 0)
                        {
                            string fp = Properties.Settings.Default["dataFileBasePath"].ToString() + "_" + dt.AddDays(-1).ToString("yyyy_MM_dd") + ".csv";
                            ftpTransfer(fp);
                        }
                        else
                        {
                            ftpTransfer(filePath);
                        }
                        lastFileWrite = c.lastUpdated;
                    }
                    MW.conditionData.Clear();
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("Problem saving data:" + e.Message);
            }
            
            
        }

        private void writeDataPoint(int conditionId, int MesoID, string field, double value, DateTime dt)
        {
            try
            {
                string tag;
                if (MesoID == -1) tag = "AmbientData";
                else tag = MesoID.ToString();
                var point = PointData
                  .Measurement("FACEIT")
                  .Tag("Condition", conditionId.ToString())
                  .Tag("Mesocosm", tag)
                  .Field(field, value)
                  .Timestamp(dt.ToUniversalTime(), WritePrecision.S);

                using (var writeApi = client.GetWriteApi())
                {
                    writeApi.WritePoint(bucket, org, point);
                }
            }catch(Exception e)
            {
                MessageBox.Show("Problem With InfluxDB writing data:" + e.Message);
            }
            
        }


        private void saveToFile(string filePath, DateTime dt)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    //Write headers
                    String header = "Time;Ambient_Time;Ambient_salinity;Ambient_Temperature;Ambient_Oxygen;AmbientWater_Flowrate;ColdWater_Flowrate;HotWater_Flowrate;AmbientWater_Pressure;ColdWater_Pressure;HotWater_Pressure;";

                    for (int i = 0; i < 4; i++)
                    {
                        header += "Condition["; header += i; header += "]_Temperature_setpoint;";
                        if (i > 0)
                        {
                            header += "Condition["; header += i; header += "]_Salinity_setpoint;";
                        }
                        else
                        {
                            header += "Condition["; header += i; header += "]_Pressure_setpoint;";
                        }
                        for (int j = 0; j < 3; j++)//pour chaque Mesocosme
                        {
                            header += "Condition["; header += i; header += "]_Meso["; header += j; header += "]_Temperature;";
                            header += "Condition["; header += i; header += "]_Meso["; header += j; header += "]_oxy;";
                            header += "Condition["; header += i; header += "]_Meso["; header += j; header += "]_cond;";
                            header += "Condition["; header += i; header += "]_Meso["; header += j; header += "]_salinity;";
                            header += "Condition["; header += i; header += "]_Meso["; header += j; header += "]_FlowRate;";
                            header += "Condition["; header += i; header += "]_Meso["; header += j; header += "]_PIDoutput_Temperature;";
                            header += "Condition["; header += i; header += "]_Meso["; header += j; header += "]_PIDoutput_Salinity;";
                        }
                    }
                    header += "incubation;\n";
                    System.IO.File.WriteAllText(filePath, header);
                }

                string data = dt.ToUniversalTime().ToString(); data += ";";

                data += MW.inSituData.time.ToUniversalTime().ToString(); data += ";";
                data += MW.inSituData.salinite.ToString(); data += ";";
                data += MW.inSituData.temperature.ToString(); data += ";";
                data += MW.inSituData.oxygen.ToString(); data += ";";

                data += MW.masterData.debitEA.ToString(); data += ";";
                data += MW.masterData.debitEF.ToString(); data += ";";
                data += MW.masterData.debitEC.ToString(); data += ";";
                data += MW.masterData.pressionEA.ToString(); data += ";";
                data += MW.masterData.pressionEF.ToString(); data += ";";
                data += MW.masterData.pressionEC.ToString(); data += ";";


                writeDataPoint(0, -1, "Ambient_salinity", MW.inSituData.salinite, dt);
                writeDataPoint(0, -1, "Ambient_Temperature", MW.inSituData.temperature, dt);
                writeDataPoint(0, -1, "Ambient_Oxygen", MW.inSituData.oxygen, dt);
                writeDataPoint(0, -1, "AmbientWater_Pressure", MW.masterData.pressionEA, dt);
                writeDataPoint(0, -1, "ColdWater_Pressure", MW.masterData.pressionEF, dt);
                writeDataPoint(0, -1, "HotWater_Pressure", MW.masterData.pressionEC, dt);

                for (int i = 0; i < 4; i++)
                {
                    data += MW.conditions[i].regulTemp.consigne; data += ";";
                    data += MW.conditions[i].regulSalinite.consigne; data += ";";

                    writeDataPoint(i, -1, "Temperature_setpoint", MW.conditions[i].regulTemp.consigne, dt);
                    writeDataPoint(i, -1, "Salinity_setpoint", MW.conditions[i].regulSalinite.consigne, dt);

                    for (int j = 0; j < 3; j++)
                    {

                        data += MW.conditions[i].Meso[j].temperature; data += ";";
                        data += MW.conditions[i].Meso[j].oxy_pc; data += ";";
                        data += MW.conditions[i].Meso[j].cond; data += ";";
                        data += MW.conditions[i].Meso[j].salinite; data += ";";
                        data += MW.conditions[i].Meso[j].debit; data += ";";
                        data += MW.conditions[i].Meso[j].tempSortiePID_pc; data += ";";
                        data += MW.conditions[i].Meso[j].salSortiePID_pc; data += ";";

                        writeDataPoint(i, j, "temperature", MW.conditions[i].Meso[j].temperature, dt);
                        writeDataPoint(i, j, "oxy", MW.conditions[i].Meso[j].oxy_pc, dt);
                        writeDataPoint(i, j, "cond", MW.conditions[i].Meso[j].cond, dt);
                        writeDataPoint(i, j, "salinite", MW.conditions[i].Meso[j].salinite, dt);
                        writeDataPoint(i, j, "debit", MW.conditions[i].Meso[j].debit, dt);
                        writeDataPoint(i, j, "tempSortiePID_pc", MW.conditions[i].Meso[j].tempSortiePID_pc, dt);
                        writeDataPoint(i, j, "salSortiePID_pc", MW.conditions[i].Meso[j].salSortiePID_pc, dt);
                    }
                }
                if (MW.ExperimentState) data += "0";
                else data += "1";
                data += ";\n";
                System.IO.File.AppendAllText(filePath, data);
            }
            catch(Exception e)
            {
                MessageBox.Show("Problem writing data File:" + e.Message);
            }
            
        }

        private void ftpTransfer(string fileName)
        {
            string ftpUsername = Properties.Settings.Default["ftpUsername"].ToString();
            string ftpPassword = Properties.Settings.Default["ftpPassword"].ToString();
            string ftpDir = "ftp://" + Properties.Settings.Default["ftpDir"].ToString();

            string fn = fileName.Substring(fileName.LastIndexOf('/') + 1);
            ftpDir += fn;
            using (var client = new WebClient())
            {
                client.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                client.UploadFile(ftpDir, WebRequestMethods.Ftp.UploadFile, fileName);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }

}
