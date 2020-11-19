using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using log4net;
using log4net.Config;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Web.UI.WebControls;
using System.IO;

namespace HEV_Agent_V2
{
    public partial class frmAgentScrew : Telerik.WinControls.UI.RadForm
    {

        DataSet DtSet;
        DataTable TbMachine;
        private static readonly ILog log = LogManager.GetLogger(typeof(frmAgentScrew));
        MqttClient client = null;
        string clientId = "";
        bool active = true;
        bool exit = false;
        RegistryKey HevAgent = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        string LogsPath = "";
        string ServerIp = "";
        string Note = ""; string Ip = "1.1.1.2";

        public frmAgentScrew()
        {
           // client = new MqttClient(IPAddress.Parse(ServerIp));
            KhoiTao();

            DtSet = new System.Data.DataSet();
            TbMachine = DtSet.Tables.Add("Lineeee");
            InitializeComponent();
            BasicConfigurator.Configure();
            client = new MqttClient(IPAddress.Parse(ServerIp));
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.MqttMsgPublished += client_MqttMsgPublished;
            client.MqttMsgSubscribed += client_MqttMsgSubscribed;
            client.MqttMsgUnsubscribed += client_MqttMsgUnsubscribed;
            client.ConnectionClosed += client_Dis;
            //  client.ConnectionClosed += ConnectionClosedEventHandler;;;;;
            //client.Connect
            clientId = Guid.NewGuid().ToString();

            TbMachine = DtSet.Tables.Add("Machine");

            TbMachine.Columns.Add("LastTime", typeof(System.String));
            TbMachine.Columns.Add("Name", typeof(System.String));
            TbMachine.Columns.Add("Status", typeof(System.Int32));
            TbMachine.Columns.Add("ErrorCode", typeof(System.String));
            TbMachine.Columns.Add("Note", typeof(System.String));
            TbMachine.Columns.Add("Ip", typeof(System.String));

            try
            {
                Ip = GetLocalIPAddress();
            }
            catch {
                log.Error("K lay duoc IP: ");
            }


            //Khởi tạo hàng 1 ngay
            var hang = TbMachine.NewRow();
            hang["LastTime"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            hang["Name"] = "FINAL";
            hang["Status"] = 1;
            hang["ErrorCode"] = "1st";
            hang["Note"] = Note;
            hang["Ip"] = Ip;

            TbMachine.Rows.Add(hang);

            radGridView1.BeginUpdate();

            radGridView1.DataSource = TbMachine;
            this.radGridView1.MasterTemplate.Columns[0].Width = 150;
            this.radGridView1.MasterTemplate.Columns[1].Width = 120;
            this.radGridView1.MasterTemplate.Columns[2].Width = 100;
            this.radGridView1.MasterTemplate.Columns[3].Width = 120;
            this.radGridView1.MasterTemplate.Columns[4].Width = 120;
            this.radGridView1.MasterTemplate.Columns[5].Width = 120;


            this.radGridView1.TableElement.RowHeight = 30;
            this.radGridView1.TableElement.TableHeaderHeight = 40;
            radGridView1.EndUpdate();



            //Khởi động cùng windows
            HevAgent.DeleteValue("HEV_FN_Agent", false);
            HevAgent.SetValue("HEV_FN_Agent", Application.ExecutablePath);

            txtLogPath.Text = LogsPath;
            txtServer.Text = ServerIp;
            txtNote.Text = Note;
           


            //Lấy setting
            //Ẩn vẫn chạy



            //Build json gửi lên server
            //Nếu server gọi thì trả lời


        }


        //PhanTich
        private void PhanTichImei(string filep, string fname)
        {

            try
            {
                //Tách ra các thông tin cần thiết trước

                string app_path = System.IO.Directory.GetCurrentDirectory();
                string kq = "";
                int stt = 0;

                File.Copy(@filep, @app_path + @"/Temp/" + @fname, true);
                System.IO.StreamReader file = new System.IO.StreamReader(@app_path + "/Temp/" + @fname);

                while (file.EndOfStream != true)
                {
                    kq = file.ReadLine().Trim().ToLower();

                }
                file.Close();
                //Debug.WriteLine(kq);
                string Code = "OK";

                if (kq.Contains("alarm on"))
                { 
                    stt = 0;
                    //Tách lấy error code
                    string[] ketqua1 = kq.Split('(');
                    string[] ketqua2 = ketqua1[1].Split(')');
                    Code = ketqua2[0];
                }
                if (kq.Contains("alarm off"))
                    stt = 1;

                //Xoa file khi k dùng đến
                File.Delete(@app_path + "/Temp/" + @fname);


                TbMachine.Rows[0]["LastTime"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                TbMachine.Rows[0]["Status"] = stt;
                TbMachine.Rows[0]["ErrorCode"] = Code;


                //Dù sao đi chăng nữa thì cũng Pub lên server thôi mà Man
                //Build Json rồi Pub lên kênh
                if (client.IsConnected)
                {
                   
                    string abc = "[{\"LastTime\":\"" + TbMachine.Rows[0]["LastTime"] + "\",\"Name\":\"IMEI\",\"Status\":" + stt + ",\"ErrorCode\":\"" + Code + "\",\"Note\":\"" + Note + "\",\"Ip\":\"" + Ip + "\"}]";
                    client.Publish("hev", Encoding.UTF8.GetBytes(abc), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                   // Debug.WriteLine("Vừa pub dữ liệu lẻ lên server đó a");

                }
                else
                {
                    //Debug.WriteLine("K kết nối tới server");
                    log.Error("C160. Khong Publish du lieu len Server duoc");
                }

            }

            catch (Exception ex)
            {
                log.Error("Loi khi phan tich file log: "+ex);
            }
            
            finally
            {
                this.radGridView1.CurrentRow = null;
            }


        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public string TbToJson(DataTable table)
        {
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(table);
            return JSONString;
        }

        private void fileSystemWatcher1_Created(object sender, System.IO.FileSystemEventArgs e)
        {
           // Debug.WriteLine(e.Name);;
           // PhanTich(e.Name);
        }
        Font pop = new Font("Consolas", 11f, FontStyle.Bold);
        private void radGridView1_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            GridHeaderCellElement cell = e.CellElement as GridHeaderCellElement;
            if (cell != null)
            {
                cell.Font = pop;
                cell.ForeColor = Color.Black;
                cell.BackColor = Color.AliceBlue;

            }

            if (e.CellElement.Text == "0")
            {
                e.CellElement.Text = "NG";
                e.CellElement.BackColor = Color.Red;
                e.CellElement.TextAlignment = ContentAlignment.MiddleCenter;

            }

            if (e.CellElement.Text == "1")
            {
                e.CellElement.Text = "OK";
                e.CellElement.BackColor = Color.LimeGreen;
                e.CellElement.TextAlignment = ContentAlignment.MiddleCenter;
            }

            if (e.CellElement.Text == "2")
            {
                e.CellElement.Text = "OFF"; e.CellElement.BackColor = Color.Gray;
                e.CellElement.TextAlignment = ContentAlignment.MiddleCenter;
            }

            if (e.CellElement.Text == "3")
                e.CellElement.Text = "WAIT";

        }

        private void frmAgentScrew_Load(object sender, EventArgs e)
        {
            //Khi khởi động lên, cố gắng kết nối tới server
            try
            {
                fileSystemWatcher1.Path = LogsPath;
            }

            catch (Exception ex)
            {
                log.Error(ex);
            }


            backgroundWorker1.RunWorkerAsync();

        }

        //INIT
        private void KhoiTao()
        {
            try { 
                LogsPath = Properties.Settings.Default.LogPath;
                ServerIp = Properties.Settings.Default.Server;
                Note = Properties.Settings.Default.Note;
                
                

            }
            catch (Exception ex)
            {
                log.Error("Co loi trong qua trinh khoi tao "+ex);
                MessageBox.Show("Có lỗi trong quá trình khởi tạo, vui lòng check setting");
            }

        }

        //Nhận tin nhắn
        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            //Nhận
            try
            {
                string result = System.Text.Encoding.UTF8.GetString(e.Message);

                if (result == "report")
                {

                    string data = TbToJson(TbMachine);
                    client.Publish("hev", Encoding.UTF8.GetBytes(data), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                }

            }
            catch (Exception ex)
            {
                log.Error("C252. Loi phan nhan LoaLoa "+ex);
            }
            //string abc = "[{\"LastTime\":\"" + d + "\",\"Name\":\"" + Ten + "\",\"Status\":" + stt + ",\"ErrorCode\":\"" + Code + "\",\"Note\":\"" + Note + "\"}}]";


        }

        private void client_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            // write your code
        }

        //Sub rồi
        private void client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            // write your code
            Console.WriteLine("Subscribed");
        }

        //Published
        void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            //Console.WriteLine("Published: " + e.MessageId);

        }

        //Nếu ngắt kết nối thì sao?
        void client_Dis(object sender, EventArgs e)
        {
            Debug.WriteLine("Tự dưng bị ngắt kết nối");
            //Co gang connect lại
            //  Task.Run(() => PersistConnectionAsync());
        }


        void ConnectionClosedEventHandler(object sender, EventArgs e)
        {

            Debug.WriteLine("Kết nối vừa bị đóng vì cái gì đó");
        }



        private async Task PersistConnectionAsync()
        {
            var connected = client.IsConnected;
            while (active)
            {
                if (!connected)
                {
                    try
                    {
                        client.Connect(clientId);
                        Debug.WriteLine("ReConnected");
                        string[] topic = { "vienkmtt" };
                        client.Subscribe(topic, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                    }
                    catch
                    {
                        Debug.WriteLine("failed reconnect");
                    }
                }

                if (client.IsConnected)
                {
                    // Debug.WriteLine("ReConnected");
                    //  string[] topic = { "vienkmtt" };
                    //   client.Subscribe(topic, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

                }

                await Task.Delay(1000);
                connected = client.IsConnected;
            }
        }

        private void frmAgentScrew_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (client.IsConnected)
                client.Disconnect();

            active = false;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (!client.IsConnected)
                {
                    try
                    {
                        client.Connect(clientId,
                        null, null,
                        false,
                        MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, //aws 
                        true,
                        $"hev_ngatketnoi",
                        "{\"message\":\"e abc vua bi disconnected\"}",
                        true,
                        5);

                        if (client.IsConnected)
                        {
                            Debug.WriteLine("Connected to Server");
                            this.radWaitingBar1.Invoke(new MethodInvoker(() => { this.radWaitingBar1.StartWaiting(); this.radWaitingBar1.Text = "Connected to Server. Monitoring....."; this.radWaitingBar1.ForeColor = Color.Blue; }));

                        }


                        string[] topic = { "loaloa" };
                        client.Subscribe(topic, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                    }
                    catch
                    {
                        Debug.WriteLine("Error. Can't connect to Server");
                        this.radWaitingBar1.Invoke(new MethodInvoker(() => { this.radWaitingBar1.StopWaiting(); this.radWaitingBar1.Text = "Have an error. Can't connect to Server."; this.radWaitingBar1.ForeColor = Color.Red; }));
                    }
                }

                Thread.Sleep(500);
            }



        }

        private void frmAgentScrew_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (exit)
            {
                client.Disconnect();

            }

            else
            {
                e.Cancel = true;
                notifyIcon1.Visible = true;
                this.Hide();
            }


        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //FrmAgent.ActiveForm();
            if (this.WindowState == FormWindowState.Normal)
            {
                Show();
                this.Focus();

            }
            else if (this.WindowState == FormWindowState.Minimized)
            {
                Show();
                this.WindowState = FormWindowState.Normal;
                this.Focus();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {

            if (txtPass.Text == "hev123")
            {
                Properties.Settings.Default.LogPath = txtLogPath.Text;
                Properties.Settings.Default.Note = txtNote.Text;
                Properties.Settings.Default.Server = txtServer.Text.Trim();
                

                Properties.Settings.Default.Save();
                if(client.IsConnected)
                    client.Disconnect();
                KhoiTao();

                TbMachine.Rows[0]["Note"] = txtNote.Text;
                fileSystemWatcher1.Path = txtLogPath.Text;



                // MessageBox.Show("Save OK, Vui lòng bật lại phần mềm để bắt đầu làm việc");
                //  exit = true;
                // Application.Restart();
                // Application.Exit();

            }

            else
                MessageBox.Show("Sai Pass");
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            if (txtPass.Text == "hev123")
            {

                exit = true;
                Application.Exit();

            }

            else
                MessageBox.Show("Sai Pass");
        }

        private void radPageView1_SelectedPageChanged(object sender, EventArgs e)
        {

        }

        private void fileSystemWatcher1_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            if (e.Name.Contains("MonitoringLog")) { 

                //Xem là IMEI Hay Final
                PhanTichImei(e.FullPath, e.Name);

            }
            else
            {
                Debug.WriteLine("no no");
            }    
        }

        private void PhanTichFile(string filep, string fname)
        {

            try {




            }
            catch(Exception ex)
            {
                log.Error(ex);
            }
            

        }
    }
}
