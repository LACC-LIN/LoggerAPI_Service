using SimpleTCP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Deployment.Application;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace LoggerAPI_Service
{
    public partial class Form1 : Form
    {

        private SimpleTcpClient client;

        string sqlsrc,
        sqlqry,
        IP_Det,
        Perf_data,
        Perf_data_log,
        Perf_data2,
        Perf_data2_log,
        RR_data,
        RR_data_log,
        sqlqry_rr;

        int Cnt = 1,
            cnt2 = 1,
        Cnt_Runrate = 1;

        string Shift,
        Event,
        ProductionMode_Name,
        DownTime_Wa_Er,
        EndTime,
        PartType,
        UnitProduct,
        UnitSpeed;

        bool connected = false;
        private Timer timer1;
        private Timer timer2;


        private Timer timer_RR_1;
        private Timer timer_local;

        bool DowntimeSummary,
        Warning,
        Error;

        // <----- RT_Performance Table Vars
        int
        RecordID,
        PLC_TotalPartsOK,
        QuantityTarget,
        TechnicalCycleTime,
        ProductionMode,
        ProgressiveTarget,
        NonProductiveTime;

        float OEErt,
        Availability,
        Performance,
        Quality,
        TEEP,
        Downtime,
        QuantityOK,
        QuantityNOK,
        OEETarget,
        OEEtr,
        LastCycleTime;

        // ----->

        // <----- Run_Rate Table Vars
        int QuantityOK_RR,
        PartCnt_RR,
        StationID_RR;


        float IntervalTarget_RR;

        string time_rr;

        // ----->

        // <----- For txt file
        string IP_R,
        IP_S,
        SQL_Conn,
        path_config,
        path_record,
        path_log,
        path_data,
        path_lastdata,
        path_chk,
        _Shift,
        _Event,
        _PartType,
        StationID_str1,
        StationID_str2;

        string[] StationIDs,
        RecordIDs_prev;

        int Port,
        StationID,
        RecordID_prev,
        Pwd,
        _OEETarget,
        _TargetQty,
        _TechCycleTime,
        _ProdModeVal,
        chk_val,
        chk_cloud;
        //----->

        IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();

        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {

            CheckAdminRights();

            //this.WindowState = FormWindowState.Minimized;

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            lblStatus.Text = "DisConnected";
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            lblStatus.BackColor = Color.Red;

            path_log = Application.StartupPath + "\\LoggerAPIService-Config\\logs\\Logs.txt";
            path_config = Application.StartupPath + "\\LoggerAPIService-Config\\Config.txt";
            path_record = Application.StartupPath + "\\LoggerAPIService-Config\\RecordID_prev.txt";
            path_data = Application.StartupPath + "\\LoggerAPIService-Config\\datasent.txt";
            path_lastdata = Application.StartupPath + "\\LoggerAPIService-Config\\lastdata.txt";
            path_chk = Application.StartupPath + "\\LoggerAPIService-Config\\chk.txt";

            ApplVersion();
            ReadTxt();
            CheckBoxState();
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            //ClientConnect();
            //Startupdata();

        }
        //minimize to taskbar
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            bool MousePointerOnTaskBar = Screen.GetWorkingArea(this).Contains(Cursor.Position);

            if (this.WindowState == FormWindowState.Minimized && MousePointerOnTaskBar)
            {
                Hide();
                notifyIcon1.Visible = true;
                notifyIcon1.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                notifyIcon1.ShowBalloonTip(100);
                this.ShowInTaskbar = false;
            }
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        //Menu for taskbar icon
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Close Application 
            notifyIcon1.Dispose();
            Application.Exit();
        }

        private void restoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
            this.ShowInTaskbar = true;
        }

        //resize to normal size
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
            this.ShowInTaskbar = true;
        }

        // <-- Tab 1
        private void CheckAdminRights()
        {
            if (!Program.IsAdministrator())
            {
                // Restart and run as admin
                var exeName = Process.GetCurrentProcess().MainModule.FileName;
                ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                startInfo.Verb = "runas";
                startInfo.Arguments = "restart";
                Process.Start(startInfo);
                Application.Exit();
            }
        }

        //checkbox state
        private void CheckBoxState()
        {
            if (File.Exists(path_chk))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(path_chk))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        char[] spearator = {
                            ','
                        };
                        string[] split = line.ToString().Split(spearator, StringSplitOptions.None);

                        chk_val = Convert.ToInt32(split[0].ToString().Trim());
                        chk_cloud = Convert.ToInt32(split[1].ToString().Trim());

                        //chk_val = Convert.ToInt32(line);
                    }

                    if (new FileInfo(path_chk).Length == 0)
                    {
                        chk_val = 0;
                        chk_cloud = 0;
                    }
                }
            }
            else
            {
                chk_val = 0;
                chk_cloud = 0;
            }

            if (chk_val == 1)
            {
                cbLocal.Checked = true;
            }
            else
            {
                cbLocal.Checked = false;
            }

            if (chk_cloud == 1)
            {
                cbCloud.Checked = true;
                ClientConnect();
            }

        }
        //Read txt file
        private void ReadTxt()
        {

            if (File.Exists(path_config))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(path_config))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        char[] spearator = {
                            ',',
                            ':'
                        };
                        string[] split = line.ToString().Split(spearator, StringSplitOptions.None);

                        IP_R = split[1].ToString();
                        Port = Convert.ToInt32(split[3].Trim());
                        IP_S = split[5].ToString().Trim();
                        Pwd = Convert.ToInt32(split[7].Trim());
                        SQL_Conn = split[9].ToString().Trim();
                        StationID_str1 = split[11].ToString().Trim();
                        //StationID = Convert.ToInt32(split[11]);
                        //StationID_RR = Convert.ToInt32(split[11]);
                    }
                }
                StationID_str2 = StationID_str1.Split('{', '}')[1]; //getting the Ids array --> {1;2;3}

                StationIDs = StationID_str2.ToString().Split(';');// saving only IDs in array
            }
            else
            {
                txtStatus.Text = "";
                txtStatus.Text = "Config File not found";

            }

            //Read last RecordID - RunRate
            //if (File.Exists(path_record))
            //{
            //    using (System.IO.StreamReader sr = new System.IO.StreamReader(path_record))
            //    {
            //        string line;
            //        while ((line = sr.ReadLine()) != null)
            //        {

            //            RecordIDs_prev = line.ToString().Split(';');
            //        }


            //    }
            //}
            //else
            //{
            //    RecordIDs_prev = null;
            //}

            if (File.Exists(path_record))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(path_record))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {

                        RecordID_prev = Convert.ToInt32(line);
                    }

                    if (new FileInfo(path_record).Length == 0)
                    {
                        RecordID_prev = 0;

                    }
                }
            }
            else
            {
                RecordID_prev = 0;
            }


            txtHostIP.Text = (IP_R).ToString().Trim();
            txtPort.Text = (Port).ToString();
            txtSQL.Text = (SQL_Conn).ToString();
            //Read last Trigger based data - RT-Performance
            ReadFile();
        }

        //Read lastdata txt file
        private void ReadFile()
        {

            //Read last Trigger based data - RT-Performance
            if (File.Exists(path_lastdata))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(path_lastdata))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        char[] spearator = {
                            ','
                        };
                        string[] split = line.ToString().Split(spearator, StringSplitOptions.None);

                        _Shift = split[0].ToString();
                        _Event = split[1].ToString().Trim();
                        _OEETarget = Convert.ToInt32(split[2].Trim());
                        _TargetQty = Convert.ToInt32(split[3].Trim());
                        _TechCycleTime = Convert.ToInt32(split[4].Trim());
                        _ProdModeVal = Convert.ToInt32(split[5].Trim());
                        _PartType = split[6].ToString().Trim();
                    }

                    if (new FileInfo(path_record).Length == 0)
                    {
                        _Shift = "";
                        _Event = "";
                        _OEETarget = 0;
                        _TargetQty = 0;
                        _TechCycleTime = 0;
                        _ProdModeVal = 0;
                        _PartType = "";

                    }
                }
            }
            else
            {
                _Shift = "";
                _Event = "";
                _OEETarget = 0;
                _TargetQty = 0;
                _TechCycleTime = 0;
                _ProdModeVal = 0;
                _PartType = "";
            }
        }

        private void ReadLastRecordID()
        {
            //Read last RecordID - RunRate
            //if (File.Exists(path_record))
            //{
            //    using (System.IO.StreamReader sr = new System.IO.StreamReader(path_record))
            //    {
            //        string line;
            //        while ((line = sr.ReadLine()) != null)
            //        {

            //            RecordIDs_prev = line.ToString().Split(';');
            //        }


            //    }
            //}
            //if (RecordID_prev?.Length > 0)
            //{
            //    for (int i = 0; i < RecordID_prev.Length; i++)
            //    {
            //        MessageBox.Show(RecordID_prev[i].ToString());
            //    }
            //}

            if (File.Exists(path_record))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(path_record))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {

                        RecordID_prev = Convert.ToInt32(line);
                    }

                    if (new FileInfo(path_record).Length == 0)
                    {
                        RecordID_prev = 0;

                    }
                }
            }
            else
            {
                RecordID_prev = 0;
            }
        }
        //Startup data sending func
        private void Startupdata()
        {
            try
            {
                //sqlsrc = @"Data Source=.\SQLEXPRESS;Initial Catalog=PA;Persist Security Info=True;User ID=sa;Password=111;";
                sqlsrc = @"" + SQL_Conn + "";
                SqlConnection scon = new SqlConnection(sqlsrc);
                scon.Open();

                sqlqry = "select RT_Performance. OEETarget ,RT_Performance. TargetQuantity ,RT_Performance. TechnicalCycleTime ,RT_Performance. PartType,RT_Performance.Shift ,RT_Performance.AccumType,RT_Performance.InProduction, Conf_Station.UnitProduct, Conf_Station.UnitSpeed " +
                        " from RT_Performance Inner Join RT_Status ON RT_Status.StationID = RT_Performance.StationID Inner Join Conf_Station ON RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active = '1' and RT_Performance.AccumType = 'shift'  and RT_Performance.StationID = '" + StationID + "'";
                SqlCommand cmd = new SqlCommand(sqlqry, scon);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    //Get data from SQL table
                    OEETarget = float.Parse(dr["OEETarget"].ToString());
                    QuantityTarget = Convert.ToInt32(dr["TargetQuantity"].ToString());
                    TechnicalCycleTime = Convert.ToInt32(dr["TechnicalCycleTime"].ToString());
                    PartType = dr["PartType"].ToString();

                    Shift = dr["Shift"].ToString();
                    Event = dr["AccumType"].ToString();
                    ProductionMode = Convert.ToInt32(dr["InProduction"].ToString());

                    UnitProduct = dr["UnitProduct"].ToString();
                    UnitSpeed = dr["UnitSpeed"].ToString();


                    string startupdata = "";
                    startupdata = "\n@11 = " + OEETarget + " " + System.Environment.NewLine + "@12 =" + QuantityTarget + " " + System.Environment.NewLine + "@13 = " + TechnicalCycleTime + " " + System.Environment.NewLine + "@16= " + PartType.Length + "," + PartType + System.Environment.NewLine + "@9 = " + Shift.Length + "," + Shift + System.Environment.NewLine + "@10= " + Event.Length + "," + Event + System.Environment.NewLine + "@28 = " + UnitProduct.Length + "," + UnitProduct + System.Environment.NewLine + "@29= " + UnitSpeed.Length + "," + UnitSpeed + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + " ";
                    string startupdata_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType + "," + UnitProduct + "," + UnitSpeed;

                    if (client.TcpClient.Connected)
                    {
                        string trigger = "\n@27 = 1 " + System.Environment.NewLine;
                        client.WriteLine(trigger);

                        client.WriteLineAndGetReply(startupdata, TimeSpan.FromMilliseconds(100));
                        if (File.Exists(path_data))
                        {
                            File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t" + startupdata_log + Environment.NewLine);
                        }
                        trigger = "\n@27 = 0 " + System.Environment.NewLine;
                        client.WriteLine(trigger);
                    }

                }

                scon.Close();
            }
            catch (Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                txtStatus.Text = "";
                txtStatus.Text = exceptionMessage;

                //log file
                if (File.Exists(path_log))
                {
                    File.AppendAllText(path_log, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during StartupData() --\t" + exceptionMessage + Environment.NewLine);
                }
                else
                {
                    File.Create(path_log);
                    TextWriter tw = new StreamWriter(path_log);
                    tw.WriteLine(DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during StartupData() --\t" + exceptionMessage + Environment.NewLine);
                    tw.Close();
                }

                //checking if connection was closed by server
                if (exceptionMessage == "An existing connection was forcibly closed by the remote host")
                {
                    Application.Restart();
                }

                if (exceptionMessage == "An established connection was aborted by the software in your host machine")
                {
                    Application.Restart();
                }
            }
        }

        private void ClientConnect()
        {
            //Get data from Config file
            ReadTxt();


            //Create new client
            if (client == null)
            {
                client = new SimpleTcpClient
                {
                    StringEncoder = Encoding.UTF8
                };

                client.DataReceived += Client_DataReceived;
            }

            while (!connected)
            {
                try
                {
                    lblStatus.Text = ("Trying connecting to the server...");
                    client.Connect(txtHostIP.Text, Convert.ToInt32(Port));

                    IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                    TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();

                    if (tcpConnections != null && tcpConnections.Length > 0)
                    {
                        TcpState stateOfConnection = tcpConnections.First().State;
                        if (stateOfConnection == TcpState.Established)
                        {
                            // Connection is OK then send id & pwd for validation
                            IP_Det = "DEVC " + IP_S + " " + Pwd + " " + System.Environment.NewLine;
                            client.WriteLineAndGetReply(IP_Det, TimeSpan.FromSeconds(1));

                            btnConnect.Enabled = false;
                            btnDisconnect.Enabled = true;

                            lblStatus.Text = "Connected";
                            lblStatus.BackColor = Color.Green;
                            connected = true;

                            //log file
                            if (File.Exists(path_log))
                            {
                                File.AppendAllText(path_log, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t Connected" + Environment.NewLine);
                            }
                            else
                            {
                                File.Create(path_log);
                                TextWriter tw = new StreamWriter(path_log);
                                tw.WriteLine(DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t Connected" + Environment.NewLine);
                                tw.Close();
                            }

                            //Calling Timer fn to Get data from SQL & send it to X520
                            InitTimer();
                            InitTimer2();
                            InitTimer_RR();
                        }
                        else
                        {
                            // No active tcp Connection to hostName:port

                            btnConnect.Enabled = true;
                            btnDisconnect.Enabled = false;
                            lblStatus.Text = "Connection In-Active";
                            lblStatus.BackColor = Color.Red;
                            client.TcpClient.GetStream().Close();
                            client.Disconnect();
                            connected = false;
                        }

                    }
                }
                catch (Exception ex)
                {
                    txtStatus.Text = "";
                    txtStatus.Text = ("Failed to connect to server \n");
                    txtStatus.Text += ex.ToString();

                    //log file
                    if (File.Exists(path_log))
                    {
                        File.AppendAllText(path_log, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t Failed to connect to server -----" + ex + Environment.NewLine);
                    }
                    else
                    {
                        File.Create(path_log);
                        TextWriter tw = new StreamWriter(path_log);
                        tw.WriteLine(DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t Failed to connect to server -----" + ex + Environment.NewLine);
                        tw.Close();
                    }

                    //Thread.Sleep(5000);

                }
            }

        }

        private void Client_DataReceived(object sender, SimpleTCP.Message e)
        {
            txtStatus.Invoke((MethodInvoker)delegate ()
            {
                txtStatus.Text = e.MessageString + System.Environment.NewLine;
            });
        }

        //RT_Performance - Interval
        public void InitTimer()
        {
            timer1 = new Timer();
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = 60000; //  (1 mins)
            timer1.Start();

        }

        //RT_Performance - Trigger
        public void InitTimer2()
        {
            timer2 = new Timer();
            timer2.Tick += new EventHandler(timer2_Tick);
            timer2.Interval = 3600000; //  (60 mins)
            timer2.Start();

        }

        //RT_RunRate
        public void InitTimer_RR()
        {
            timer_RR_1 = new Timer();
            timer_RR_1.Tick += new EventHandler(timer_RR_1_Tick);
            timer_RR_1.Interval = 900000; // 900000 (15 mins)
            timer_RR_1.Start();
        }

        //RT_Dashboard - localdb
        public void InitTimer_local()
        {

            timer_local = new Timer();
            timer_local.Tick += new EventHandler(timer_local_Tick);
            timer_local.Interval = 60000; //  (1 mins)
            timer_local.Start();

        }
        private void timer_local_Tick(object sender, EventArgs e)
        {

            SaveLocalSqlData_Performance();

        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            GetSqlData_Performance();
            CheckSqlData_Performance();
            Cnt = Cnt + 1;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {

            GetSqlData_Performance2();
            cnt2 = cnt2 + 1;
        }

        private void timer_RR_1_Tick(object sender, EventArgs e)
        {
            GetSqlData_Performance3();
            GetSqlData_RunRate();
            Cnt_Runrate = Cnt_Runrate + 1;
        }


        private void GetSqlData_Performance()
        {
            try
            {
                //sqlsrc = @"Data Source=.\SQLEXPRESS;Initial Catalog=PA;Persist Security Info=True;User ID=sa;Password=111;";
                sqlsrc = @"" + SQL_Conn + "";
                for (int i = 0; i < StationIDs.Length; i++)
                {
                    switch (i)
                    {
                        case 0:


                            SqlConnection scon = new SqlConnection(sqlsrc);
                            scon.Open();

                            //sqlqry = "select RT_Performance.* from RT_Performance, Conf_Station where RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active='1' and RT_Performance.AccumType='shift'  and RT_Performance.StationID='"+StationID+"'";
                            sqlqry = "select RT_Performance.OEErt ,RT_Performance.AvailabilityPercent ,RT_Performance.ThroughputPercent ,RT_Performance. QualityPercent ,RT_Performance.TEEP ,RT_Performance.Downtime ,RT_Performance.QuantityOK ,RT_Performance. QuantityNotOK ,RT_Performance.Shift ,RT_Performance.EndTimeStamp ,RT_Performance. OEETarget ,RT_Performance. TargetQuantity ,RT_Performance. TechnicalCycleTime ,RT_Performance. InProduction ,RT_Performance. ProgressiveTarget ,RT_Performance. OEEtr ,RT_Performance. PartType ,RT_Performance. LastCycleTime ,RT_Performance. NonProductiveTime, RT_Status.PartCycleTime, Conf_ProductionMode.ProductionMode, Conf_ProductionMode.DowntimeSummary,Conf_ProductionMode.Warning,Conf_ProductionMode.Error" +
                                    " from RT_Performance Inner Join RT_Status ON RT_Status.StationID = RT_Performance.StationID Inner Join Conf_ProductionMode ON RT_Performance.InProduction = Conf_ProductionMode.RecordID Inner Join Conf_Station ON RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active = '1' and RT_Performance.AccumType = 'shift'  and RT_Performance.StationID = '" + StationIDs[i] + "'";
                            SqlCommand cmd = new SqlCommand(sqlqry, scon);
                            SqlDataReader dr = cmd.ExecuteReader();
                            if (dr.Read())
                            {
                                //Get data from SQL table
                                OEErt = float.Parse(dr["OEErt"].ToString());
                                Availability = float.Parse(dr["AvailabilityPercent"].ToString());
                                Performance = float.Parse(dr["ThroughputPercent"].ToString());
                                Quality = float.Parse(dr["QualityPercent"].ToString());
                                TEEP = float.Parse(dr["TEEP"].ToString());
                                Downtime = (float.Parse(dr["Downtime"].ToString())) / 1000;
                                QuantityOK = float.Parse(dr["QuantityOK"].ToString());
                                QuantityNOK = float.Parse(dr["QuantityNotOK"].ToString());
                                ProgressiveTarget = Convert.ToInt32(dr["ProgressiveTarget"].ToString());
                                OEEtr = float.Parse(dr["OEEtr"].ToString());
                                LastCycleTime = float.Parse(dr["PartCycleTime"].ToString());
                                NonProductiveTime = Convert.ToInt32(dr["NonProductiveTime"].ToString());
                                ProductionMode_Name = dr["ProductionMode"].ToString();
                                DowntimeSummary = Convert.ToBoolean(dr["DowntimeSummary"].ToString());
                                Warning = Convert.ToBoolean(dr["Warning"].ToString());
                                Error = Convert.ToBoolean(dr["Error"].ToString());
                                EndTime = dr["EndTimeStamp"].ToString();


                                //checking whether it's downtime,error,warning,etc -->
                                if (DowntimeSummary && Warning && Error)
                                {
                                    DownTime_Wa_Er = "True_True_True";
                                }
                                else if (DowntimeSummary && Warning && !Error)
                                {
                                    DownTime_Wa_Er = "True_True_False";
                                }
                                else if (DowntimeSummary && !Warning && Error)
                                {
                                    DownTime_Wa_Er = "True_False_True";
                                }
                                else if (DowntimeSummary && !Warning && !Error)
                                {
                                    DownTime_Wa_Er = "True_False_False";
                                }
                                else
                                {
                                    DownTime_Wa_Er = "False_False_False";
                                }
                                //  --->
                                Perf_data = "";
                                Perf_data = "\n@1 =" + OEErt + " " + System.Environment.NewLine + "@2 = " + Availability + " " + System.Environment.NewLine + "@3 = " + Performance + " " + System.Environment.NewLine + "@4 = " + Quality + " " + System.Environment.NewLine + "@5 = " + TEEP + " " + System.Environment.NewLine + "@6 = " + Downtime + " " + System.Environment.NewLine + "@7 =" + QuantityOK + " " + System.Environment.NewLine + "@8 = " + QuantityNOK + " " + System.Environment.NewLine + "@15 = " + ProgressiveTarget + " " + System.Environment.NewLine + "@17 = " + LastCycleTime + " " + System.Environment.NewLine + "@18 = " + NonProductiveTime + " " + System.Environment.NewLine + "@19 = " + ProductionMode_Name.Length + "," + ProductionMode_Name + System.Environment.NewLine + "@23 = " + DownTime_Wa_Er.Length + "," + DownTime_Wa_Er + System.Environment.NewLine + "@24 = " + EndTime.Length + "," + EndTime + System.Environment.NewLine;
                                Perf_data_log = "\nStation-" + StationIDs[i] + " -> @1 =" + OEErt + " ,  " + "@2 = " + Availability + " ,  " + "@3 = " + Performance + " ,  " + "@4 = " + Quality + " ,  " + "@5 = " + TEEP + " ,  " + "@6 = " + Downtime + " ,  " + "@7 =" + QuantityOK + " ,  " + "@8 = " + QuantityNOK + " ,  " + "@15 = " + ProgressiveTarget + ", @17 = " + LastCycleTime + " , @18 = " + NonProductiveTime + " , @19 = " + ProductionMode_Name + " , @23 = " + DownTime_Wa_Er + " , @24 = " + EndTime;

                                //txtMessage.Text = Perf_data_log;
                                if (client.TcpClient.Connected)
                                {
                                    client.WriteLineAndGetReply(Perf_data, TimeSpan.FromMilliseconds(200));
                                    if (File.Exists(path_data))
                                    {
                                        File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t" + StationIDs[i] + " - " + Perf_data_log + Environment.NewLine);
                                    }
                                }
                                //else
                                //{
                                //    ClientConnect();
                                //}

                                //MessageBox.Show(Perf_data_log.ToString());

                            }

                            scon.Close();
                            break;

                        case 1:

                            SqlConnection scon1 = new SqlConnection(sqlsrc);
                            scon1.Open();

                            //sqlqry = "select RT_Performance.* from RT_Performance, Conf_Station where RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active='1' and RT_Performance.AccumType='shift'  and RT_Performance.StationID='"+StationID+"'";
                            sqlqry = "select RT_Performance.OEErt ,RT_Performance.AvailabilityPercent ,RT_Performance.ThroughputPercent ,RT_Performance. QualityPercent ,RT_Performance.TEEP ,RT_Performance.Downtime ,RT_Performance.QuantityOK ,RT_Performance. QuantityNotOK ,RT_Performance.Shift ,RT_Performance.EndTimeStamp ,RT_Performance. OEETarget ,RT_Performance. TargetQuantity ,RT_Performance. TechnicalCycleTime ,RT_Performance. InProduction ,RT_Performance. ProgressiveTarget ,RT_Performance. OEEtr ,RT_Performance. PartType ,RT_Performance. LastCycleTime ,RT_Performance. NonProductiveTime, RT_Status.PartCycleTime, Conf_ProductionMode.ProductionMode, Conf_ProductionMode.DowntimeSummary,Conf_ProductionMode.Warning,Conf_ProductionMode.Error" +
                                    " from RT_Performance Inner Join RT_Status ON RT_Status.StationID = RT_Performance.StationID Inner Join Conf_ProductionMode ON RT_Performance.InProduction = Conf_ProductionMode.RecordID Inner Join Conf_Station ON RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active = '1' and RT_Performance.AccumType = 'shift'  and RT_Performance.StationID = '" + StationIDs[i] + "'";
                            SqlCommand cmd1 = new SqlCommand(sqlqry, scon1);
                            SqlDataReader dr1 = cmd1.ExecuteReader();
                            if (dr1.Read())
                            {
                                //Get data from SQL table
                                OEErt = float.Parse(dr1["OEErt"].ToString());
                                Availability = float.Parse(dr1["AvailabilityPercent"].ToString());
                                Performance = float.Parse(dr1["ThroughputPercent"].ToString());
                                Quality = float.Parse(dr1["QualityPercent"].ToString());
                                TEEP = float.Parse(dr1["TEEP"].ToString());
                                Downtime = (float.Parse(dr1["Downtime"].ToString())) / 1000;
                                QuantityOK = float.Parse(dr1["QuantityOK"].ToString());
                                QuantityNOK = float.Parse(dr1["QuantityNotOK"].ToString());
                                ProgressiveTarget = Convert.ToInt32(dr1["ProgressiveTarget"].ToString());
                                OEEtr = float.Parse(dr1["OEEtr"].ToString());
                                LastCycleTime = float.Parse(dr1["PartCycleTime"].ToString());
                                NonProductiveTime = Convert.ToInt32(dr1["NonProductiveTime"].ToString());
                                ProductionMode_Name = dr1["ProductionMode"].ToString();
                                DowntimeSummary = Convert.ToBoolean(dr1["DowntimeSummary"].ToString());
                                Warning = Convert.ToBoolean(dr1["Warning"].ToString());
                                Error = Convert.ToBoolean(dr1["Error"].ToString());
                                EndTime = dr1["EndTimeStamp"].ToString();


                                //checking whether it's downtime,error,warning,etc -->
                                if (DowntimeSummary && Warning && Error)
                                {
                                    DownTime_Wa_Er = "True_True_True";
                                }
                                else if (DowntimeSummary && Warning && !Error)
                                {
                                    DownTime_Wa_Er = "True_True_False";
                                }
                                else if (DowntimeSummary && !Warning && Error)
                                {
                                    DownTime_Wa_Er = "True_False_True";
                                }
                                else if (DowntimeSummary && !Warning && !Error)
                                {
                                    DownTime_Wa_Er = "True_False_False";
                                }
                                else
                                {
                                    DownTime_Wa_Er = "False_False_False";
                                }
                                //  --->
                                Perf_data = "";
                                Perf_data_log = "";
                                Perf_data = "\n@31 =" + OEErt + " " + System.Environment.NewLine + "@32 = " + Availability + " " + System.Environment.NewLine + "@33 = " + Performance + " " + System.Environment.NewLine + "@34 = " + Quality + " " + System.Environment.NewLine + "@35 = " + TEEP + " " + System.Environment.NewLine + "@36 = " + Downtime + " " + System.Environment.NewLine + "@37 =" + QuantityOK + " " + System.Environment.NewLine + "@38 = " + QuantityNOK + " " + System.Environment.NewLine + "@45 = " + ProgressiveTarget + " " + System.Environment.NewLine + "@47 = " + LastCycleTime + " " + System.Environment.NewLine + "@48 = " + NonProductiveTime + " " + System.Environment.NewLine + "@49 = " + ProductionMode_Name.Length + "," + ProductionMode_Name + System.Environment.NewLine + "@53 = " + DownTime_Wa_Er.Length + "," + DownTime_Wa_Er + System.Environment.NewLine + "@54 = " + EndTime.Length + "," + EndTime + System.Environment.NewLine;
                                Perf_data_log = "\nStation-" + StationIDs[i] + " -> @31 =" + OEErt + " ,  " + "@32 = " + Availability + " ,  " + "@33 = " + Performance + " ,  " + "@34 = " + Quality + " ,  " + "@35 = " + TEEP + " ,  " + "@36 = " + Downtime + " ,  " + "@37 =" + QuantityOK + " ,  " + "@38 = " + QuantityNOK + " ,  " + "@45 = " + ProgressiveTarget + ", @47 = " + LastCycleTime + " , @48 = " + NonProductiveTime + " , @49 = " + ProductionMode_Name + " , @53 = " + DownTime_Wa_Er + " , @54 = " + EndTime;

                                //txtMessage.Text = Perf_data_log;
                                if (client.TcpClient.Connected)
                                {
                                    client.WriteLineAndGetReply(Perf_data, TimeSpan.FromMilliseconds(200));
                                    if (File.Exists(path_data))
                                    {
                                        File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t" + StationIDs[i] + " - " + Perf_data_log + Environment.NewLine);
                                    }
                                }
                                //else
                                //{
                                //    ClientConnect();
                                //}

                               // MessageBox.Show(Perf_data_log.ToString());

                            }

                            scon1.Close();
                            break;
                    }
                }
            }

            catch (Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                txtStatus.Text = "";
                txtStatus.Text = exceptionMessage;

                //log file
                if (File.Exists(path_log))
                {
                    File.AppendAllText(path_log, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during GetSqlData_Performance() --\t" + exceptionMessage + Environment.NewLine);
                }
                else
                {
                    File.Create(path_log);
                    TextWriter tw = new StreamWriter(path_log);
                    tw.WriteLine(DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during GetSqlData_Performance() --\t" + exceptionMessage + Environment.NewLine);
                    tw.Close();
                }

                //checking if connection was closed by server
                if (exceptionMessage == "An existing connection was forcibly closed by the remote host")
                {
                    Application.Restart();
                }

                if (exceptionMessage == "An established connection was aborted by the software in your host machine")
                {
                    Application.Restart();
                }
            }
        }

        //60mins interval
        private void GetSqlData_Performance2()
        {

            try
            {
                sqlsrc = @"" + SQL_Conn + "";
                for (int i = 0; i < StationIDs.Length; i++)
                {
                    switch (i)
                    {
                        case 0:


                            SqlConnection scon = new SqlConnection(sqlsrc);
                            scon.Open();

                            //sqlqry = "select RT_Performance.* from RT_Performance, Conf_Station where RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active='1' and RT_Performance.AccumType='shift'  and RT_Performance.StationID='"+StationID+"'";
                            sqlqry = "select RT_Performance. OEETarget ,RT_Performance. TargetQuantity ,RT_Performance. TechnicalCycleTime ,RT_Performance. PartType,RT_Performance.Shift ,RT_Performance.AccumType,RT_Performance.InProduction, Conf_Station.UnitProduct, Conf_Station.UnitSpeed " +
                        " from RT_Performance Inner Join RT_Status ON RT_Status.StationID = RT_Performance.StationID Inner Join Conf_Station ON RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active = '1' and RT_Performance.AccumType = 'shift'  and RT_Performance.StationID = '" + StationIDs[i] + "'";
                            SqlCommand cmd = new SqlCommand(sqlqry, scon);
                            SqlDataReader dr = cmd.ExecuteReader();
                            if (dr.Read())
                            {
                                //Get data from SQL table
                                OEETarget = float.Parse(dr["OEETarget"].ToString());
                                QuantityTarget = Convert.ToInt32(dr["TargetQuantity"].ToString());
                                TechnicalCycleTime = Convert.ToInt32(dr["TechnicalCycleTime"].ToString());
                                PartType = dr["PartType"].ToString();

                                Shift = dr["Shift"].ToString();
                                Event = dr["AccumType"].ToString();
                                ProductionMode = Convert.ToInt32(dr["InProduction"].ToString());

                                UnitProduct = dr["UnitProduct"].ToString();
                                UnitSpeed = dr["UnitSpeed"].ToString();

                                Perf_data2 = "";
                                Perf_data2 = "\n@11 = " + OEETarget + " " + System.Environment.NewLine + "@12 =" + QuantityTarget + " " + System.Environment.NewLine + "@13 = " + TechnicalCycleTime + " " + System.Environment.NewLine + "@16= " + PartType.Length + "," + PartType + System.Environment.NewLine + "@28 = " + UnitProduct.Length + "," + UnitProduct + System.Environment.NewLine + "@29= " + UnitSpeed.Length + "," + UnitSpeed + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + " ";
                                Perf_data2_log = OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType + "," + UnitProduct + "," + UnitSpeed;

                                if (client.TcpClient.Connected)
                                {
                                    string trigger = "\n@27 = 1 " + System.Environment.NewLine;
                                    //client.WriteLineAndGetReply(trigger, TimeSpan.FromMilliseconds(100));
                                    client.WriteLine(trigger);

                                    client.WriteLineAndGetReply(Perf_data2, TimeSpan.FromMilliseconds(100));
                                    if (File.Exists(path_data))
                                    {
                                        File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "-- GetSqlData_Performance2() --\t Station-> " + StationIDs[i] + " - " + Perf_data2_log + Environment.NewLine);
                                    }
                                    trigger = "\n@27 = 0 " + System.Environment.NewLine;
                                    //client.WriteLineAndGetReply(trigger, TimeSpan.FromMilliseconds(100));
                                    client.WriteLine(trigger);
                                }
                                else
                                {
                                    ClientConnect();
                                }

                                File.WriteAllText(path_lastdata, String.Empty);
                                Perf_data2_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType;

                                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_lastdata, true))
                                {
                                    file.WriteLine(Perf_data2_log);
                                    // txtMessage.Text = Perf_data2_log;
                                }

                            }
                            scon.Close();
                            break;

                        case 1:

                            SqlConnection scon1 = new SqlConnection(sqlsrc);
                            scon1.Open();

                            sqlqry = "select RT_Performance. OEETarget ,RT_Performance. TargetQuantity ,RT_Performance. TechnicalCycleTime ,RT_Performance. PartType,RT_Performance.Shift ,RT_Performance.AccumType,RT_Performance.InProduction, Conf_Station.UnitProduct, Conf_Station.UnitSpeed " +
                        " from RT_Performance Inner Join RT_Status ON RT_Status.StationID = RT_Performance.StationID Inner Join Conf_Station ON RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active = '1' and RT_Performance.AccumType = 'shift'  and RT_Performance.StationID = '" + StationIDs[i] + "'";
                            SqlCommand cmd1 = new SqlCommand(sqlqry, scon1);
                            SqlDataReader dr1 = cmd1.ExecuteReader();
                            if (dr1.Read())
                            {
                                //Get data from SQL table
                                OEETarget = float.Parse(dr1["OEETarget"].ToString());
                                QuantityTarget = Convert.ToInt32(dr1["TargetQuantity"].ToString());
                                TechnicalCycleTime = Convert.ToInt32(dr1["TechnicalCycleTime"].ToString());
                                PartType = dr1["PartType"].ToString();

                                Shift = dr1["Shift"].ToString();
                                Event = dr1["AccumType"].ToString();
                                ProductionMode = Convert.ToInt32(dr1["InProduction"].ToString());

                                UnitProduct = dr1["UnitProduct"].ToString();
                                UnitSpeed = dr1["UnitSpeed"].ToString();

                                Perf_data2 = "";
                                Perf_data2 = "\n@41 = " + OEETarget + " " + System.Environment.NewLine + "@42 =" + QuantityTarget + " " + System.Environment.NewLine + "@43 = " + TechnicalCycleTime + " " + System.Environment.NewLine + "@46= " + PartType.Length + "," + PartType + System.Environment.NewLine + "@58 = " + UnitProduct.Length + "," + UnitProduct + System.Environment.NewLine + "@59= " + UnitSpeed.Length + "," + UnitSpeed + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + " ";
                                Perf_data2_log = OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType + "," + UnitProduct + "," + UnitSpeed;

                                if (client.TcpClient.Connected)
                                {
                                    string trigger = "\n@27 = 1 " + System.Environment.NewLine;
                                    //client.WriteLineAndGetReply(trigger, TimeSpan.FromMilliseconds(100));
                                    client.WriteLine(trigger);

                                    client.WriteLineAndGetReply(Perf_data2, TimeSpan.FromMilliseconds(100));

                                    if (File.Exists(path_data))
                                    {
                                        File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "-- GetSqlData_Performance2() --\t Station-> " + StationIDs[i] + " - " + Perf_data2_log + Environment.NewLine);
                                    }
                                    trigger = "\n@27 = 0 " + System.Environment.NewLine;
                                    //client.WriteLineAndGetReply(trigger, TimeSpan.FromMilliseconds(100));
                                    client.WriteLine(trigger);
                                }
                                else
                                {
                                    ClientConnect();
                                }

                                File.WriteAllText(path_lastdata, String.Empty);
                                Perf_data2_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType;

                                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_lastdata, true))
                                {
                                    file.WriteLine(Perf_data2_log);
                                    // txtMessage.Text = Perf_data2_log;
                                }

                            }

                            scon1.Close();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                txtStatus.Text = "";
                txtStatus.Text = exceptionMessage;

                //log file
                if (File.Exists(path_log))
                {
                    File.AppendAllText(path_log, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during GetSqlData_Performance2() --\t" + exceptionMessage + Environment.NewLine);
                }
                else
                {
                    File.Create(path_log);
                    TextWriter tw = new StreamWriter(path_log);
                    tw.WriteLine(DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during GetSqlData_Performance2() --\t" + exceptionMessage + Environment.NewLine);
                    tw.Close();
                }

                //checking if connection was closed by server
                if (exceptionMessage == "An existing connection was forcibly closed by the remote host")
                {
                    Application.Restart();
                }

                if (exceptionMessage == "An established connection was aborted by the software in your host machine")
                {
                    Application.Restart();
                }
            }

        }

        //15 mins interval
        private void GetSqlData_Performance3()
        {

            try
            {
                sqlsrc = @"" + SQL_Conn + "";
                for (int i = 0; i < StationIDs.Length; i++)
                {
                    switch (i)
                    {
                        case 0:


                            SqlConnection scon = new SqlConnection(sqlsrc);
                            scon.Open();

                            //sqlqry = "select RT_Performance.* from RT_Performance, Conf_Station where RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active='1' and RT_Performance.AccumType='shift'  and RT_Performance.StationID='"+StationID+"'";
                            sqlqry = "select RT_Performance. OEETarget ,RT_Performance. TargetQuantity ,RT_Performance. TechnicalCycleTime ,RT_Performance. PartType,RT_Performance.Shift ,RT_Performance.AccumType,RT_Performance.InProduction " +
                                    " from RT_Performance Inner Join RT_Status ON RT_Status.StationID = RT_Performance.StationID Inner Join Conf_Station ON RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active = '1' and RT_Performance.AccumType = 'shift'  and RT_Performance.StationID = '" + StationIDs[i] + "'";
                            SqlCommand cmd = new SqlCommand(sqlqry, scon);
                            SqlDataReader dr = cmd.ExecuteReader();
                            if (dr.Read())
                            {
                                //Get data from SQL table
                                OEETarget = float.Parse(dr["OEETarget"].ToString());
                                QuantityTarget = Convert.ToInt32(dr["TargetQuantity"].ToString());
                                TechnicalCycleTime = Convert.ToInt32(dr["TechnicalCycleTime"].ToString());
                                PartType = dr["PartType"].ToString();

                                Shift = dr["Shift"].ToString();
                                Event = dr["AccumType"].ToString();
                                ProductionMode = Convert.ToInt32(dr["InProduction"].ToString());


                                Perf_data2 = "";
                                Perf_data2 = "\n@44 = " + ProductionMode + " " + System.Environment.NewLine + "@39 = " + Shift.Length + "," + Shift + System.Environment.NewLine + "@40= " + Event.Length + "," + Event + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + " ";
                                //Perf_data2_log = Shift + "," + Event + "," + ProductionMode;
                                Perf_data2_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType;
                                if (client.TcpClient.Connected)
                                {
                                    string trigger = "\n@27 = 1 " + System.Environment.NewLine;
                                    client.WriteLineAndGetReply(trigger, TimeSpan.FromMilliseconds(100));

                                    client.WriteLineAndGetReply(Perf_data2, TimeSpan.FromMilliseconds(100));
                                    if (File.Exists(path_data))
                                    {
                                        File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "-- GetSqlData_Performance3() --\t" + Perf_data2_log + Environment.NewLine);
                                    }
                                    trigger = "\n@27 = 0 " + System.Environment.NewLine;
                                    //client.WriteLineAndGetReply(trigger, TimeSpan.FromMilliseconds(100));
                                    client.WriteLine(trigger);
                                }
                                else
                                {
                                    ClientConnect();
                                }

                                File.WriteAllText(path_lastdata, String.Empty);

                                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_lastdata, true))
                                {
                                    file.WriteLine(Perf_data2_log);
                                    // txtMessage.Text = Perf_data2_log;
                                }

                            }

                            scon.Close();
                            break;

                        case 1:

                            SqlConnection scon1 = new SqlConnection(sqlsrc);
                            scon1.Open();

                            sqlqry = "select RT_Performance. OEETarget ,RT_Performance. TargetQuantity ,RT_Performance. TechnicalCycleTime ,RT_Performance. PartType,RT_Performance.Shift ,RT_Performance.AccumType,RT_Performance.InProduction " +
                                    " from RT_Performance Inner Join RT_Status ON RT_Status.StationID = RT_Performance.StationID Inner Join Conf_Station ON RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active = '1' and RT_Performance.AccumType = 'shift'  and RT_Performance.StationID = '" + StationIDs[i] + "'";
                            SqlCommand cmd1 = new SqlCommand(sqlqry, scon1);
                            SqlDataReader dr1 = cmd1.ExecuteReader();
                            if (dr1.Read())
                            {
                                //Get data from SQL table
                                OEETarget = float.Parse(dr1["OEETarget"].ToString());
                                QuantityTarget = Convert.ToInt32(dr1["TargetQuantity"].ToString());
                                TechnicalCycleTime = Convert.ToInt32(dr1["TechnicalCycleTime"].ToString());
                                PartType = dr1["PartType"].ToString();

                                Shift = dr1["Shift"].ToString();
                                Event = dr1["AccumType"].ToString();
                                ProductionMode = Convert.ToInt32(dr1["InProduction"].ToString());


                                Perf_data2 = "";
                                Perf_data2 = "\n@44 = " + ProductionMode + " " + System.Environment.NewLine + "@39 = " + Shift.Length + "," + Shift + System.Environment.NewLine + "@40= " + Event.Length + "," + Event + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + " ";
                                //Perf_data2_log = Shift + "," + Event + "," + ProductionMode;
                                Perf_data2_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType;
                                if (client.TcpClient.Connected)
                                {
                                    string trigger = "\n@27 = 1 " + System.Environment.NewLine;
                                    client.WriteLineAndGetReply(trigger, TimeSpan.FromMilliseconds(100));

                                    client.WriteLineAndGetReply(Perf_data2, TimeSpan.FromMilliseconds(100));
                                    if (File.Exists(path_data))
                                    {
                                        File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "-- GetSqlData_Performance3() --\t" + Perf_data2_log + Environment.NewLine);
                                    }
                                    trigger = "\n@27 = 0 " + System.Environment.NewLine;
                                    //client.WriteLineAndGetReply(trigger, TimeSpan.FromMilliseconds(100));
                                    client.WriteLine(trigger);
                                }
                                else
                                {
                                    ClientConnect();
                                }

                                File.WriteAllText(path_lastdata, String.Empty);
                                Perf_data2_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType;

                                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_lastdata, true))
                                {
                                    file.WriteLine(Perf_data2_log);
                                    // txtMessage.Text = Perf_data2_log;
                                }

                            }

                            scon1.Close();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                txtStatus.Text = "";
                txtStatus.Text = exceptionMessage;

                //log file
                if (File.Exists(path_log))
                {
                    File.AppendAllText(path_log, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during GetSqlData_Performance3() --\t" + exceptionMessage + Environment.NewLine);
                }
                else
                {
                    File.Create(path_log);
                    TextWriter tw = new StreamWriter(path_log);
                    tw.WriteLine(DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during GetSqlData_Performance3() --\t" + exceptionMessage + Environment.NewLine);
                    tw.Close();
                }

                //checking if connection was closed by server
                if (exceptionMessage == "An existing connection was forcibly closed by the remote host")
                {
                    Application.Restart();
                }

                if (exceptionMessage == "An established connection was aborted by the software in your host machine")
                {
                    Application.Restart();
                }
            }

        }

        private void CheckSqlData_Performance()
        {

            ReadFile();

            try
            {
                //sqlsrc = @"Data Source=.\SQLEXPRESS;Initial Catalog=PA;Persist Security Info=True;User ID=sa;Password=111;";
                sqlsrc = @"" + SQL_Conn + "";
                SqlConnection scon = new SqlConnection(sqlsrc);
                scon.Open();

                //sqlqry = "select RT_Performance.* from RT_Performance, Conf_Station where RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active='1' and RT_Performance.AccumType='shift'  and RT_Performance.StationID='"+StationID+"'";
                sqlqry = "select RT_Performance. OEETarget ,RT_Performance. TargetQuantity ,RT_Performance. TechnicalCycleTime ,RT_Performance. PartType,RT_Performance.Shift ,RT_Performance.AccumType,RT_Performance.InProduction " +
                        " from RT_Performance Inner Join RT_Status ON RT_Status.StationID = RT_Performance.StationID Inner Join Conf_Station ON RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active = '1' and RT_Performance.AccumType = 'shift'  and RT_Performance.StationID = '" + StationIDs[0] + "'";
                SqlCommand cmd = new SqlCommand(sqlqry, scon);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    //Get data from SQL table
                    OEETarget = float.Parse(dr["OEETarget"].ToString());
                    QuantityTarget = Convert.ToInt32(dr["TargetQuantity"].ToString());
                    TechnicalCycleTime = Convert.ToInt32(dr["TechnicalCycleTime"].ToString());
                    PartType = dr["PartType"].ToString();

                    Shift = dr["Shift"].ToString();
                    Event = dr["AccumType"].ToString();
                    ProductionMode = Convert.ToInt32(dr["InProduction"].ToString());

                    if (OEETarget > _OEETarget)
                    {
                        string startupdata = "";
                        startupdata = "\n@11 = " + OEETarget + " " + System.Environment.NewLine + "@12 =" + QuantityTarget + " " + System.Environment.NewLine + "@13 = " + TechnicalCycleTime + " " + System.Environment.NewLine + "@16= " + PartType.Length + "," + PartType + System.Environment.NewLine + "@9 = " + Shift.Length + "," + Shift + System.Environment.NewLine + "@10= " + Event.Length + "," + Event + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + " ";
                        string startupdata_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType;

                        if (client.TcpClient.Connected)
                        {
                            string trigger = "\n@27 = 1 " + System.Environment.NewLine;
                            client.WriteLine(trigger);

                            client.WriteLineAndGetReply(startupdata, TimeSpan.FromMilliseconds(100));
                            if (File.Exists(path_data))
                            {
                                File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t" + startupdata_log + Environment.NewLine);
                            }
                            trigger = "\n@27 = 0 " + System.Environment.NewLine;
                            client.WriteLine(trigger);
                        }


                        File.WriteAllText(path_lastdata, String.Empty);

                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_lastdata, true))
                        {
                            file.WriteLine(startupdata_log);
                        }

                    }
                    else if (QuantityTarget > _TargetQty)
                    {
                        string startupdata = "";
                        startupdata = "\n@11 = " + OEETarget + " " + System.Environment.NewLine + "@12 =" + QuantityTarget + " " + System.Environment.NewLine + "@13 = " + TechnicalCycleTime + " " + System.Environment.NewLine + "@16= " + PartType.Length + "," + PartType + System.Environment.NewLine + "@9 = " + Shift.Length + "," + Shift + System.Environment.NewLine + "@10= " + Event.Length + "," + Event + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + " ";
                        string startupdata_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType;

                        if (client.TcpClient.Connected)
                        {
                            string trigger = "\n@27 = 1 " + System.Environment.NewLine;
                            client.WriteLine(trigger);

                            client.WriteLineAndGetReply(startupdata, TimeSpan.FromMilliseconds(100));
                            if (File.Exists(path_data))
                            {
                                File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t" + startupdata_log + Environment.NewLine);
                            }
                            trigger = "\n@27 = 0 " + System.Environment.NewLine;
                            client.WriteLine(trigger);
                        }


                        File.WriteAllText(path_lastdata, String.Empty);

                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_lastdata, true))
                        {
                            file.WriteLine(startupdata_log);
                        }

                    }
                    else if (TechnicalCycleTime > _TechCycleTime)
                    {
                        string startupdata = "";
                        startupdata = "\n@11 = " + OEETarget + " " + System.Environment.NewLine + "@12 =" + QuantityTarget + " " + System.Environment.NewLine + "@13 = " + TechnicalCycleTime + " " + System.Environment.NewLine + "@16= " + PartType.Length + "," + PartType + System.Environment.NewLine + "@9 = " + Shift.Length + "," + Shift + System.Environment.NewLine + "@10= " + Event.Length + "," + Event + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + " ";
                        string startupdata_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType;

                        if (client.TcpClient.Connected)
                        {
                            string trigger = "\n@27 = 1 " + System.Environment.NewLine;
                            client.WriteLine(trigger);

                            client.WriteLineAndGetReply(startupdata, TimeSpan.FromMilliseconds(100));
                            if (File.Exists(path_data))
                            {
                                File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t" + startupdata_log + Environment.NewLine);
                            }
                            trigger = "\n@27 = 0 " + System.Environment.NewLine;
                            client.WriteLine(trigger);
                        }


                        File.WriteAllText(path_lastdata, String.Empty);

                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_lastdata, true))
                        {
                            file.WriteLine(startupdata_log);
                        }

                    }
                    else if (PartType != _PartType.Trim())
                    {
                        string startupdata = "";
                        startupdata = "\n@11 = " + OEETarget + " " + System.Environment.NewLine + "@12 =" + QuantityTarget + " " + System.Environment.NewLine + "@13 = " + TechnicalCycleTime + " " + System.Environment.NewLine + "@16= " + PartType.Length + "," + PartType + System.Environment.NewLine + "@9 = " + Shift.Length + "," + Shift + System.Environment.NewLine + "@10= " + Event.Length + "," + Event + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + " ";
                        string startupdata_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType;

                        if (client.TcpClient.Connected)
                        {
                            string trigger = "\n@27 = 1 " + System.Environment.NewLine;
                            client.WriteLine(trigger);

                            client.WriteLineAndGetReply(startupdata, TimeSpan.FromMilliseconds(100));
                            if (File.Exists(path_data))
                            {
                                File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t" + startupdata_log + Environment.NewLine);
                            }
                            trigger = "\n@27 = 0 " + System.Environment.NewLine;
                            client.WriteLine(trigger);
                        }

                        File.WriteAllText(path_lastdata, String.Empty);

                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_lastdata, true))
                        {
                            file.WriteLine(startupdata_log);
                        }

                    }
                    else if (Shift != _Shift.Trim())
                    {
                        string startupdata = "";
                        startupdata = "\n@11 = " + OEETarget + " " + System.Environment.NewLine + "@12 =" + QuantityTarget + " " + System.Environment.NewLine + "@13 = " + TechnicalCycleTime + " " + System.Environment.NewLine + "@16= " + PartType.Length + "," + PartType + System.Environment.NewLine + "@9 = " + Shift.Length + "," + Shift + System.Environment.NewLine + "@10= " + Event.Length + "," + Event + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + " ";
                        string startupdata_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType;

                        if (client.TcpClient.Connected)
                        {
                            string trigger = "\n@27 = 1 " + System.Environment.NewLine;
                            client.WriteLine(trigger);

                            client.WriteLineAndGetReply(startupdata, TimeSpan.FromMilliseconds(100));
                            if (File.Exists(path_data))
                            {
                                File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t" + startupdata_log + Environment.NewLine);
                            }
                            trigger = "\n@27 = 0 " + System.Environment.NewLine;
                            client.WriteLine(trigger);
                        }

                        File.WriteAllText(path_lastdata, String.Empty);

                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_lastdata, true))
                        {
                            file.WriteLine(startupdata_log);
                        }

                    }
                    else if (Event != _Event.Trim())
                    {
                        string startupdata = "";
                        startupdata = "\n@11 = " + OEETarget + " " + System.Environment.NewLine + "@12 =" + QuantityTarget + " " + System.Environment.NewLine + "@13 = " + TechnicalCycleTime + " " + System.Environment.NewLine + "@16= " + PartType.Length + "," + PartType + System.Environment.NewLine + "@9 = " + Shift.Length + "," + Shift + System.Environment.NewLine + "@10= " + Event.Length + "," + Event + System.Environment.NewLine + "@27 = 0 " + System.Environment.NewLine + " ";
                        string startupdata_log = Shift + "," + Event + "," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + PartType;

                        if (client.TcpClient.Connected)
                        {
                            string trigger = "\n@27 = 1 " + System.Environment.NewLine;
                            client.WriteLine(trigger);

                            client.WriteLineAndGetReply(startupdata, TimeSpan.FromMilliseconds(100));
                            if (File.Exists(path_data))
                            {
                                File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t" + startupdata_log + Environment.NewLine);
                            }
                            trigger = "\n@27 = 0 " + System.Environment.NewLine;
                            client.WriteLine(trigger);
                        }

                        File.WriteAllText(path_lastdata, String.Empty);

                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_lastdata, true))
                        {
                            file.WriteLine(startupdata_log);
                        }

                    }


                }

                scon.Close();
            }
            catch (Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                txtStatus.Text = "";
                txtStatus.Text = exceptionMessage;

                //log file
                if (File.Exists(path_log))
                {
                    File.AppendAllText(path_log, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during CheckSqlData_Performance() --\t" + exceptionMessage + Environment.NewLine);
                }
                else
                {
                    File.Create(path_log);
                    TextWriter tw = new StreamWriter(path_log);
                    tw.WriteLine(DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during CheckSqlData_Performance() --\t" + exceptionMessage + Environment.NewLine);
                    tw.Close();
                }

                //checking if connection was closed by server
                if (exceptionMessage == "An existing connection was forcibly closed by the remote host")
                {
                    Application.Restart();
                }

                if (exceptionMessage == "An established connection was aborted by the software in your host machine")
                {
                    Application.Restart();
                }
            }
        }

        private void GetSqlData_RunRate()
        {
          

            try
            {
                sqlsrc = @"" + SQL_Conn + "";
                string trigger = "";
                for (int i = 0; i < StationIDs.Length; i++)
                {
                    switch (i)
                    {
                        case 0:
                            ReadLastRecordID();

                            SqlConnection scon = new SqlConnection(sqlsrc);
                            scon.Open();
                            //QuantityOK_RR, QuantityNOK_RR, StationID_RR, IntervalTarget_RR
                            //sqlqry_rr = "select RunRate.* from RunRate, Conf_Station where RunRate.StationID = Conf_Station.StationID and Conf_Station.Active='1' and Conf_Station.StoreRunRate='1' and RunRate.StationID='" + StationID + "' and RunRate.RecordID > '" + RecordID_prev + "'";
                            sqlqry_rr = "select RunRate.* from RunRate where  RunRate.StationID='" + StationIDs[i] + "' and RunRate.RecordID > '" + RecordID_prev + "'";
                            SqlCommand cmd = new SqlCommand(sqlqry_rr, scon);
                            SqlDataReader reader = cmd.ExecuteReader();

                            while (reader.Read())
                            {
                                RecordID = Convert.ToInt32(reader["RecordID"].ToString());
                                QuantityOK_RR = Convert.ToInt32(reader["QuantityOK"].ToString());
                                PartCnt_RR = Convert.ToInt32(reader["PartCount"].ToString());
                                IntervalTarget_RR = float.Parse(reader["IntervalTarget"].ToString());
                                time_rr = reader["ValueTimeStamp"].ToString();



                                RR_data = "\n@21= " + QuantityOK_RR + System.Environment.NewLine + "@22 = " + IntervalTarget_RR + " " + System.Environment.NewLine + "@20 = " + PartCnt_RR + " " + System.Environment.NewLine + "@25= " + time_rr.Length + "," + time_rr + System.Environment.NewLine + "@26 = 0 " + System.Environment.NewLine + " ";
                                RR_data_log = "\nStation-" + StationIDs[i] + " -> @26 = 0 , @21= " + QuantityOK_RR + ",@22 = " + IntervalTarget_RR + " , @20 = " + PartCnt_RR + ", @25 " + time_rr + System.Environment.NewLine;

                                File.WriteAllText(path_record, String.Empty);

                                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_record, true))
                                {
                                    file.WriteLine(RecordID);
                                }

                                if (client.TcpClient.Connected)
                                {
                                    trigger = "\n@26 = 1 " + System.Environment.NewLine;
                                    client.WriteLine(trigger);
                                    //client.WriteLineAndGetReply(trigger, TimeSpan.FromMilliseconds(100));

                                    client.WriteLineAndGetReply(RR_data, TimeSpan.FromMilliseconds(100));
                                    if (File.Exists(path_data))
                                    {
                                        File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t" + RR_data_log + Environment.NewLine);
                                    }
                                    //RR_data = "\n@26 = 0 " + System.Environment.NewLine;
                                    //client.WriteLineAndGetReply(RR_data, TimeSpan.FromMilliseconds(100));

                                }


                            }

                            scon.Close();
                            break;

                        case 1:

                            ReadLastRecordID();

                            SqlConnection scon1 = new SqlConnection(sqlsrc);
                            scon1.Open();
                            //QuantityOK_RR, QuantityNOK_RR, StationID_RR, IntervalTarget_RR
                            //sqlqry_rr = "select RunRate.* from RunRate, Conf_Station where RunRate.StationID = Conf_Station.StationID and Conf_Station.Active='1' and Conf_Station.StoreRunRate='1' and RunRate.StationID='" + StationID + "' and RunRate.RecordID > '" + RecordID_prev + "'";
                            sqlqry_rr = "select RunRate.* from RunRate where  RunRate.StationID='" + StationIDs[i] + "' and RunRate.RecordID > '" + RecordID_prev + "'";
                            SqlCommand cmd1 = new SqlCommand(sqlqry_rr, scon1);
                            SqlDataReader reader1 = cmd1.ExecuteReader();

                            while (reader1.Read())
                            {
                                RecordID = Convert.ToInt32(reader1["RecordID"].ToString());
                                QuantityOK_RR = Convert.ToInt32(reader1["QuantityOK"].ToString());
                                PartCnt_RR = Convert.ToInt32(reader1["PartCount"].ToString());
                                IntervalTarget_RR = float.Parse(reader1["IntervalTarget"].ToString());
                                time_rr = reader1["ValueTimeStamp"].ToString();



                                RR_data = "\n@51= " + QuantityOK_RR + System.Environment.NewLine + "@52 = " + IntervalTarget_RR + " " + System.Environment.NewLine + "@50 = " + PartCnt_RR + " " + System.Environment.NewLine + "@55= " + time_rr.Length + "," + time_rr + System.Environment.NewLine + "@56 = 0 " + System.Environment.NewLine + " ";
                                RR_data_log = "\nStation-" + StationIDs[i] + " ->  @56 = 0 , @51= " + QuantityOK_RR + ",@52 = " + IntervalTarget_RR + " , @50 = " + PartCnt_RR + ", @55 " + time_rr + System.Environment.NewLine;

                                File.WriteAllText(path_record, String.Empty);

                                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path_record, true))
                                {
                                    file.WriteLine(RecordID);
                                }

                                if (client.TcpClient.Connected)
                                {
                                    trigger = "\n@56 = 1 " + System.Environment.NewLine;
                                    client.WriteLine(trigger);
                                    //client.WriteLineAndGetReply(trigger, TimeSpan.FromMilliseconds(100));

                                    client.WriteLineAndGetReply(RR_data, TimeSpan.FromMilliseconds(100));
                                    if (File.Exists(path_data))
                                    {
                                        File.AppendAllText(path_data, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t" + RR_data_log + Environment.NewLine);
                                    }
                                    //RR_data = "\n@56 = 0 " + System.Environment.NewLine;
                                    //client.WriteLineAndGetReply(RR_data, TimeSpan.FromMilliseconds(100));

                                }


                            }

                            scon1.Close();
                            break;

                    }
                }

            }
            catch (Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                txtStatus.Text = "";
                txtStatus.Text = exceptionMessage;

                //log file
                if (File.Exists(path_log))
                {
                    File.AppendAllText(path_log, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during GetSqlData_RunRate() --\t" + exceptionMessage + Environment.NewLine);
                }
                else
                {
                    File.Create(path_log);
                    TextWriter tw = new StreamWriter(path_log);
                    tw.WriteLine(DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "   -- during GetSqlData_RunRate() --\t" + exceptionMessage + Environment.NewLine);
                    tw.Close();
                }

                //checking if connection was closed by server
                if (exceptionMessage == "An existing connection was forcibly closed by the remote host")
                {
                    /* timer1.Stop();
                     timer_RR_1.Stop();
                     client.TcpClient.GetStream().Close();
                     client.TcpClient.Close();
                     //client.Disconnect();
                     btnConnect.Enabled = true;
                     btnDisconnect.Enabled = false;
 #pragma warning disable CA1303 // Do not pass literals as localized parameters
                     lblStatus.Text = "DisConnected";
 #pragma warning restore CA1303 // Do not pass literals as localized parameters
                     lblStatus.BackColor = Color.Red;
                     //client = null; */
                    Application.Restart();


                }

                if (exceptionMessage == "An established connection was aborted by the software in your host machine")
                {
                    Application.Restart();
                }
            }
        }

        //send data to local database table RT_DashBoard
        private void SaveLocalSqlData_Performance()
        {
            ReadTxt();
            try
            {
                //sqlsrc = @"Data Source=.\SQLEXPRESS;Initial Catalog=PA;Persist Security Info=True;User ID=sa;Password=111;";
                sqlsrc = @"" + SQL_Conn + "";
                SqlConnection scon = new SqlConnection(sqlsrc);
                scon.Open();
                for (int i = 0; i < StationIDs.Length; i++)
                {
                    //sqlqry = "select RT_Performance.* from RT_Performance, Conf_Station where RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active='1' and RT_Performance.AccumType='shift'  and RT_Performance.StationID='"+StationID+"'";
                    sqlqry = "select RT_Performance.OEErt ,RT_Performance.AvailabilityPercent ,RT_Performance.ThroughputPercent ,RT_Performance. QualityPercent ,RT_Performance.TEEP ,RT_Performance.Downtime ,RT_Performance.QuantityOK ,RT_Performance. QuantityNotOK ,RT_Performance.Shift ,RT_Performance.AccumType ,RT_Performance.EndTimeStamp ,RT_Performance. OEETarget ,RT_Performance. TargetQuantity ,RT_Performance. TechnicalCycleTime ,RT_Performance. InProduction ,RT_Performance. ProgressiveTarget ,RT_Performance. OEEtr ,RT_Performance. PartType ,RT_Performance. LastCycleTime ,RT_Performance. NonProductiveTime, RT_Status.PartCycleTime, Conf_ProductionMode.ProductionMode, Conf_ProductionMode.DowntimeSummary,Conf_ProductionMode.Warning,Conf_ProductionMode.Error, Conf_Station.UnitProduct, Conf_Station.UnitSpeed" +
                        " from RT_Performance Inner Join RT_Status ON RT_Status.StationID = RT_Performance.StationID Inner Join Conf_ProductionMode ON RT_Performance.InProduction = Conf_ProductionMode.RecordID Inner Join Conf_Station ON RT_Performance.StationID = Conf_Station.StationID and Conf_Station.Active = '1' and RT_Performance.AccumType = 'shift'  and RT_Performance.StationID = '" + StationIDs[i] + "'";
                    SqlCommand cmd = new SqlCommand(sqlqry, scon);
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        //Get data from SQL table
                        OEErt = float.Parse(dr["OEErt"].ToString());
                        Availability = float.Parse(dr["AvailabilityPercent"].ToString());
                        Performance = float.Parse(dr["ThroughputPercent"].ToString());
                        Quality = float.Parse(dr["QualityPercent"].ToString());
                        TEEP = float.Parse(dr["TEEP"].ToString());
                        Downtime = (float.Parse(dr["Downtime"].ToString())) / 1000;
                        QuantityOK = float.Parse(dr["QuantityOK"].ToString());
                        QuantityNOK = float.Parse(dr["QuantityNotOK"].ToString());
                        Shift = dr["Shift"].ToString();
                        Event = dr["AccumType"].ToString();
                        OEETarget = float.Parse(dr["OEETarget"].ToString());
                        QuantityTarget = Convert.ToInt32(dr["TargetQuantity"].ToString());
                        TechnicalCycleTime = Convert.ToInt32(dr["TechnicalCycleTime"].ToString());
                        ProductionMode = Convert.ToInt32(dr["InProduction"].ToString());
                        ProgressiveTarget = Convert.ToInt32(dr["ProgressiveTarget"].ToString());
                        PartType = dr["PartType"].ToString();
                        LastCycleTime = float.Parse(dr["PartCycleTime"].ToString());
                        NonProductiveTime = Convert.ToInt32(dr["NonProductiveTime"].ToString());
                        ProductionMode_Name = dr["ProductionMode"].ToString();
                        EndTime = dr["EndTimeStamp"].ToString();
                        //EntryTime = DateTime.Now;//Convert.ToDateTime(dr["EndTimeStamp"].ToString());
                        UnitProduct = dr["UnitProduct"].ToString();
                        UnitSpeed = dr["UnitSpeed"].ToString();

                        DowntimeSummary = Convert.ToBoolean(dr["DowntimeSummary"].ToString());
                        Warning = Convert.ToBoolean(dr["Warning"].ToString());
                        Error = Convert.ToBoolean(dr["Error"].ToString());
                        //checking whether it's downtime,error,warning,etc -->
                        if (DowntimeSummary && Warning && Error)
                        {
                            DownTime_Wa_Er = "True_True_True";
                        }
                        else if (DowntimeSummary && Warning && !Error)
                        {
                            DownTime_Wa_Er = "True_True_False";
                        }
                        else if (DowntimeSummary && !Warning && Error)
                        {
                            DownTime_Wa_Er = "True_False_True";
                        }
                        else if (DowntimeSummary && !Warning && !Error)
                        {
                            DownTime_Wa_Er = "True_False_False";
                        }
                        else
                        {
                            DownTime_Wa_Er = "False_False_False";
                        }
                        //  --->
                        dr.Close();
                        string sql_insert = "";
                        //sql_insert = "INSERT INTO [dbo].[RT_DashBoard] ([StationID] ,[OEErt] ,[Availability] ,[Performance],[Quality],[TEEP] ,[Downtime],[QuantityOK] ,[QuantityNOK] ,[Shift],[Event] ,[OEETarget] ,[QuantityTarget]  ,[TechnicalCycleTime]  ,[ProductionMode],[ProgressiveTarget],[PartType],[LastcycleTime],[NonProductiveTime],[Mode],[DownTime_Wa_Er],[DateAndTime],[UnitProduct],[UnitSpeed],[EntryTime])" +
                        //" VALUES (" + StationID + "," + OEErt + "," + Availability + "," + Performance + "," + Quality + "," + TEEP + "," + Downtime + "," + QuantityOK + "," + QuantityNOK + ",'" + Shift + "','" + Event + "'," + OEETarget + "," + QuantityTarget + "," + TechnicalCycleTime + "," + ProductionMode + "," + ProgressiveTarget + ",'" + PartType + "'," + LastCycleTime + "," + NonProductiveTime + ",'" + ProductionMode_Name + "','" + DownTime_Wa_Er + "','" + EndTime + "','" + UnitProduct + "','" + UnitSpeed + "','"+EntryTime+"')";
                        SqlCommand localcmd = new SqlCommand("Insertdata", scon);
                        localcmd.CommandType = CommandType.StoredProcedure;
                        localcmd.Parameters.Add("@StationID", SqlDbType.Int).Value = StationIDs[i];
                        localcmd.Parameters.Add("@OEErt", SqlDbType.Float).Value = OEErt;
                        localcmd.Parameters.Add("@Availability", SqlDbType.Float).Value = Availability;
                        localcmd.Parameters.Add("@Performance", SqlDbType.Float).Value = Performance;
                        localcmd.Parameters.Add("@Quality", SqlDbType.Float).Value = Quality;
                        localcmd.Parameters.Add("@TEEP", SqlDbType.Float).Value = TEEP;
                        localcmd.Parameters.Add("@Downtime", SqlDbType.Float).Value = Downtime;
                        localcmd.Parameters.Add("@QuantityOK", SqlDbType.Float).Value = QuantityOK;
                        localcmd.Parameters.Add("@QuantityNOK", SqlDbType.Float).Value = QuantityNOK;
                        localcmd.Parameters.Add("@Shift", SqlDbType.VarChar).Value = Shift;
                        localcmd.Parameters.Add("@Event", SqlDbType.VarChar).Value = Event;
                        localcmd.Parameters.Add("@OEETarget", SqlDbType.Float).Value = OEETarget;
                        localcmd.Parameters.Add("@QuantityTarget", SqlDbType.Int).Value = QuantityTarget;
                        localcmd.Parameters.Add("@TechnicalCycleTime", SqlDbType.Int).Value = TechnicalCycleTime;
                        localcmd.Parameters.Add("@ProductionMode", SqlDbType.Int).Value = ProductionMode;
                        localcmd.Parameters.Add("@ProgressiveTarget", SqlDbType.Int).Value = ProgressiveTarget;
                        localcmd.Parameters.Add("@PartType", SqlDbType.VarChar).Value = PartType;
                        localcmd.Parameters.Add("@LastcycleTime", SqlDbType.Float).Value = LastCycleTime;
                        localcmd.Parameters.Add("@NonProductiveTime", SqlDbType.Int).Value = NonProductiveTime;
                        localcmd.Parameters.Add("@Mode", SqlDbType.VarChar).Value = ProductionMode_Name;
                        localcmd.Parameters.Add("@DownTime_Wa_Er", SqlDbType.VarChar).Value = DownTime_Wa_Er;
                        localcmd.Parameters.Add("@DateAndTime", SqlDbType.VarChar).Value = EndTime;
                        localcmd.Parameters.Add("@UnitProduct", SqlDbType.VarChar).Value = UnitProduct;
                        localcmd.Parameters.Add("@UnitSpeed", SqlDbType.VarChar).Value = UnitSpeed;
                        localcmd.Parameters.Add("@ID", SqlDbType.Int);
                        localcmd.Parameters["@ID"].Direction = ParameterDirection.Output;

                        localcmd.ExecuteNonQuery();
                        if ((int)localcmd.Parameters["@ID"].Value > 0)
                        {
                            txtStatus.Text = "New row entered - " + localcmd.Parameters["@ID"].Value.ToString() + "  StaionID - " + StationIDs[i];
                        }
                        else
                        {
                            txtStatus.Text = "Error";
                        }
                    }

                }
                scon.Close();
            }
            catch (Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                txtStatus.Text = "";
                txtStatus.Text = exceptionMessage;
            }
        }


        private void Disconnect_Client()
        {
            timer1.Stop();
            timer_RR_1.Stop();


            client.TcpClient.GetStream().Close();
            client.TcpClient.Close();
            client.Disconnect();

            //log file
            if (File.Exists(path_log))
            {
                File.AppendAllText(path_log, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt") + "--\t  Connection closed" + Environment.NewLine);
            }

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            lblStatus.Text = "DisConnected";
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            lblStatus.BackColor = Color.Red;
            txtStatus.Text = "";
            txtStatus.Text = "Data Transfer stopped";
            txtStatus.Text += "\n Connection closed";
        }

        private void cbCloud_CheckedChanged(object sender, EventArgs e)
        {
            FileInfo myFile = new FileInfo(path_chk);
            string text = File.ReadAllText(path_chk);
            if (cbCloud.Checked == true)
            {
                myFile.Attributes &= ~FileAttributes.Hidden;//make file unhidden before writing
                //File.WriteAllText(path_chk, String.Empty);
                //text = text.Insert(2, "1");
                text = text.Replace(",0", ",1");
                File.WriteAllText(path_chk, text);

                myFile.Attributes |= FileAttributes.Hidden;//re-hide file after writing
            }
            else
            {
                myFile.Attributes &= ~FileAttributes.Hidden;//make file unhidden before writing
                                                            //File.WriteAllText(path_chk, String.Empty);
                text = text.Replace(",1", ",0");
                File.WriteAllText(path_chk, text);

                myFile.Attributes |= FileAttributes.Hidden;//re-hide file after writing
            }
        }


        private void cbLocal_CheckedChanged(object sender, EventArgs e)
        {
            FileInfo myFile = new FileInfo(path_chk);
            string text = File.ReadAllText(path_chk);
            // var aStringBuilder = new StringBuilder(text);
            if (cbLocal.Checked == true)
            {
                myFile.Attributes &= ~FileAttributes.Hidden;//make file unhidden before writing
                                                            //File.WriteAllText(path_chk, String.Empty);
                text = text.Replace("0,", "1,");
                //text = text.Insert(0, "1");
                File.WriteAllText(path_chk, text);

                myFile.Attributes |= FileAttributes.Hidden;//re-hide file after writing
                InitTimer_local();
            }
            else
            {
                myFile.Attributes &= ~FileAttributes.Hidden;//make file unhidden before writing
                                                            //File.WriteAllText(path_chk, String.Empty);
                text = text.Replace("1,", "0,");
                File.WriteAllText(path_chk, text);

                myFile.Attributes |= FileAttributes.Hidden;//re-hide file after writing
                timer_local.Stop();
            }
        }


        private void btnConnect_Click(object sender, EventArgs e)
        {

            ClientConnect();
            Startupdata();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            Disconnect_Client();
        }

        // End of Tab 1 -->

        // <-- Tab 2

        //delete data older than 30 days
        private void btnDelete_Click(object sender, EventArgs e)
        {

            ReadTxt();
            try
            {
                //sqlsrc = @"Data Source=.\SQLEXPRESS;Initial Catalog=PA;Persist Security Info=True;User ID=sa;Password=111;";
                string str = @"" + SQL_Conn + "";
                SqlConnection cn = new SqlConnection(str);
                SqlCommand cmd = new SqlCommand("DeleteOldData", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cn.Open();
                cmd.ExecuteNonQuery();
                cn.Close();
            }
            catch (Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                txtStatus.Text = "";
                txtStatus.Text = "Delete failed due to --> " + exceptionMessage;
            }
        }

        private void btnDeleteLog_Click(object sender, EventArgs e)
        {
            if (File.Exists(path_data))
            {
                if (new FileInfo(path_data).Length > 0)
                {
                    File.WriteAllText(path_data, String.Empty);
                }
            }

            //if (File.Exists(path_log))
            //{
            //    if (new FileInfo(path_log).Length > 0)
            //    {
            //        File.WriteAllText(path_log, String.Empty);
            //    }
            //}
        }

        // --> Tab 2

        // <-- Tab 3
        private void ApplVersion()
        {
            //Use Product Version
            string Pversion = Application.ProductVersion.Substring(0, 5).ToString();
            txtVersion.Text = Pversion;

        }
        // End of Tab 3 -->
    }
}
