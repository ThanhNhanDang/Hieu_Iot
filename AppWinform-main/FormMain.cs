using MQTTnet.Client;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AppWinform_main.Database;
using AppWinform_main.MQTT_Util;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using AppWinform_main.DTO;
using AppWinform_main.Entity;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace AppWinform_main
{
    public partial class FormMain : Form
    {

        #region MQTT
        private bool _isStart = false;
        private int _timeIsStart = 3; // 3s
        // Create a MQTT client factory
        private static MqttFactory _factory_MQTT;
        // Create a MQTT client instance
        private static IMqttClient _mqtt_Client;
        private static string _client_ID;
        private MqttClientOptions _options;
        private string _messageFeedbackTheNg = "ng";
        private string _messageFeedbackTheXe = "xe";
        #endregion

        #region RFID 
        #endregion

        #region Thread
        private Thread _checkService = null;
        private byte _bcheckServiceIn = 0;
        private byte _bcheckServiceOut = 0;
        private Thread _clockThread = null; // Khai báo một luồng để cập nhật thời gian
        #endregion

        #region Update Homepage
        private string _pathSave = Application.StartupPath + @"Images\User";
        private string _timeFormat = "dd-MM-yyy, hh-mm-ss tt";
        #endregion
        public FormMain()
        {
            InitializeComponent();
            Initialize_clockThread();
        }

        #region Form Event


        private void FormMain_Load(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                /*  List<DTOTagInfo> l = new List<DTOTagInfo>();
                  l.Add(new DTOTagInfo(
                          "Đặng Văn A",
                          "ABCD1234",
                          "0D742810BFFF4BD884A5D9A7",
                          "E28011700000020E26B66B48",
                          "2000502486029CBE",
                          "20001348D6CD09C4",
                          "00001111",
                          "00002222",
                          "Honda",
                          false
                          ));
                  l.Add(new DTOTagInfo(
                          "Đặng Văn B",
                          "ABCD1235",
                          "017EACEBF7884AC782D1C47A",
                          "E28011700000020E26B6625F",
                          "2000402486021CD7",
                          "2000125FD6CC09C4",
                          "00003333",
                          "00004444",
                          "Z1000",
                          false
                          ));
                  l.Add(new DTOTagInfo(
                          "Đặng Văn C",
                          "ABCD1236",
                          "12D92ECA27B7491AB729CF2D",
                          "E28011700000020E26B6625D",
                          "200040248602E0BE",
                          "2000025DD6CC09C4",
                          "00005555",
                          "00006666",
                          "Yamaha",
                          false
                         ));
                  l.Add(new DTOTagInfo(
                          "Đặng Văn D",
                          "ABCD1237",
                          "E280689400005024860230D7",
                          "E28011700000020E26B66B46",
                          "20005024860230D7",
                          "20000346D6CD09C4",
                          "00007777",
                          "00008888",
                          "BMW",
                         false
                         ));
                  l.Add(new DTOTagInfo(
                          "Đặng Văn E",
                          "ABCD1238",
                          "014990BEB51E40E0AF7EE0D8",
                          "E28011700000020E26B66B4A",
                          "20005022E85490A4",
                          "2000034AD6CD09C4",
                          "00009999",
                          "11110000",
                          "Mercedes",
                          false
                         ));

                  for (int i = 0; i < 5; i++)
                  {
                      SqliteDataAccess.SaveTag(l[i]);
                  }*/

                List<DTOTagInfo> tagList = SqliteDataAccess.LoadTag();
                if (tagList == null)
                {
                    MessageBox.Show("Danh Sách trống");
                }

                ConnectSubscribeToTheMQTTBroker();

            });
            Task.Run(() =>
            {
                Thread.Sleep(_timeIsStart * 1000);
                _isStart = true;
                _checkService = new Thread(new ThreadStart(CheckServiceThrd));
                _checkService.Start();
                _checkService.IsBackground = true;

                _clockThread = new Thread(new ThreadStart(CheckServiceThrd));
                _clockThread.Start();
                _clockThread.IsBackground = true;
            });
        }


        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_mqtt_Client == null)
                return;
            if (_mqtt_Client.IsConnected)
            {
                _mqtt_Client.DisconnectAsync();
                _mqtt_Client.Dispose();
            }
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {

        }
        #endregion

        #region Btn Event

        #region Close Form
        private void btnCloseForm_MouseEnter(object sender, EventArgs e)
        {
            btnCloseForm.BackColor = Color.Red;
            btnCloseForm.Image = Properties.Resources.CloseW;
        }

        private void btnCloseForm_MouseLeave(object sender, EventArgs e)
        {
            btnCloseForm.BackColor = Color.White;
            btnCloseForm.Image = Properties.Resources.Close;
        }

        private void btnCloseForm_Click(object sender, EventArgs e)
        {
            Close();
        }
        #endregion

        #region Restore Down
        private void btnRestoreDown_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
                btnRestoreDown.Image = Properties.Resources.square;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
                btnRestoreDown.Image = Properties.Resources.RestoreDdown;
            }
        }
        #endregion

        #region Minisize Form
        private void btnMinisize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        #endregion

        #region Add User
        private void btnAddUser_Click(object sender, EventArgs e)
        {
            FormSaveImage formSaveImage = new FormSaveImage();
            formSaveImage.ShowDialog();
        }
        #endregion

        #endregion

        #region MQTT

        #region ReConnect/Disconnect
        // Kết nối và đăng ký topic
        private async void ConnectSubscribeToTheMQTTBroker()
        {
            _factory_MQTT = new MqttFactory();
            _mqtt_Client = _factory_MQTT.CreateMqttClient();
            _client_ID = Guid.NewGuid().ToString();

            // Create MQTT client options
            _options = new MqttClientOptionsBuilder()
            .WithTcpServer(Broker_Util.SERVER_IP, Broker_Util.PORT) // MQTT broker address and port
            .WithCredentials(Broker_Util.USER_NAME, Broker_Util.PASSWORD) // Set username and password
            .WithClientId(_client_ID)
            .WithCleanSession()
            .Build();
            #region Using TLS/SSL
            /*  options = new MqttClientOptionsBuilder()
                  .WithTcpServer(broker, port) // MQTT broker address and port
                  .WithCredentials(username, password) // Set username and password
                  .WithClientId(clientId)
                  .WithCleanSession()
                  .WithTls(
                      o =>
                      {
                          // The used public broker sometimes has invalid certificates. This sample accepts all
                          // certificates. This should not be used in live environments.
                          o.CertificateValidationHandler = _ => true;

                          // The default value is determined by the OS. Set manually to force version.
                          o.SslProtocol = SslProtocols.Tls12; ;

                          // Please provide the file path of your certificate file. The current directory is /bin.
                          var certificate = new X509Certificate("/opt/emqxsl-ca.crt", "");
                          o.Certificates = new List<X509Certificate> { certificate };
                      }
                  )
                  .Build();
            */
            #endregion

            await Reconnect_Using_Event();
            // Subscribe
        }

        public async Task Reconnect_Using_Event()
        {
            // Callback function khi kết nối với broker
            _mqtt_Client.ConnectedAsync += _mqtt_Client_ConnectedAsync;

            // Callback function khi NGẮT kết nối với broker
            _mqtt_Client.DisconnectedAsync += _mqtt_Client_DisconnectedAsync;

            // Callback function when a message is received
            _mqtt_Client.ApplicationMessageReceivedAsync += _mqtt_Client_ApplicationMessageReceivedAsync;

            try
            {
                await _mqtt_Client.ConnectAsync(_options);
            }
            catch (MqttCommunicationException)
            {
                // Sự kiện DisconnectedAsync sẽ được kích hoạt
            }

        }

        private async Task _mqtt_Client_ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            ptbMqttStatus.Invoke(new MethodInvoker(() =>
            {
                ptbMqttStatus.Tag = "0";
                ptbMqttStatus.Image = Properties.Resources.server_connect;
            }));

            List<ETagInfoSync> entities = await SqliteDataAccess.SyncDatbase();
            PublishMessage(JsonConvert.SerializeObject(entities), TopicPub.SYNC_DATABASE_SERVICE);
            // Subscribe to a topic
            await _mqtt_Client.SubscribeAsync(TopicSub.IN_MESSAGE, MqttQualityOfServiceLevel.AtLeastOnce);
            await _mqtt_Client.SubscribeAsync(TopicSub.OUT_MESSAGE, MqttQualityOfServiceLevel.AtLeastOnce);
            //await _mqtt_Client.SubscribeAsync(TopicSub.OUT_NOTFOUND_TAG, MqttQualityOfServiceLevel.AtLeastOnce);
            //await _mqtt_Client.SubscribeAsync(TopicSub.IN_NOTFOUND_TAG, MqttQualityOfServiceLevel.AtLeastOnce);
            await _mqtt_Client.SubscribeAsync(TopicSub.READER_STATUS, MqttQualityOfServiceLevel.AtLeastOnce);
            await _mqtt_Client.SubscribeAsync(TopicSub.CALL_SYNC_DATABSE_SERVICE, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        // Hàm xử lý sự kiện ngắt kết nối
        private async Task _mqtt_Client_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            ptbMqttStatus.Invoke(new MethodInvoker(() =>
            {

                if (ptbMqttStatus.Tag.ToString() == "0")
                {
                    if (ptbMqttStatus.Image != null)
                        ptbMqttStatus.Image.Dispose();
                    ptbMqttStatus.Image = Properties.Resources.server_disconnect_2;
                    ptbMqttStatus.Tag = "1";
                }
                else
                {
                    if (ptbMqttStatus.Image != null)
                        ptbMqttStatus.Image.Dispose();
                    ptbMqttStatus.Image = Properties.Resources.server_disconnect;
                    ptbMqttStatus.Tag = "0";
                }
            }));
            Thread.Sleep(1000);

            await _mqtt_Client.ConnectAsync(_options);

        }

        #endregion

        #region MessageReceived
        // Hàm xử lý sự kiện khi nhận message từ publiser
        private async Task _mqtt_Client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            if (!_isStart) return;

            string message = arg.ApplicationMessage.ConvertPayloadToString();
            string topic = arg.ApplicationMessage.Topic;

            if (topic == TopicSub.READER_STATUS)
            {
                char r = message[0];
                char c = message[1];// SYC_R16 (1) Ra
                                    // ZTX_G20 (2) Vao
                                    // C  Connected
                                    // D  Disconnect

                if (r == '2')
                {
                    _bcheckServiceIn = 0;

                    ptbReaderIn1.Invoke(new MethodInvoker(() =>
                    {
                        if (c == 'C')
                        {
                            if (ptbReaderIn1.Tag.ToString() == "0")
                            {
                                ptbReaderIn1.Tag = '1';
                                if (ptbReaderIn1.Image != null)
                                    ptbReaderIn1.Image.Dispose();
                                ptbReaderIn1.Image = Properties.Resources.Connect_rfid;
                                return; //MethodInvoker
                            }
                            return; //MethodInvoker
                        }
                        if (c == 'D')
                        {
                            if (ptbReaderIn1.Tag.ToString() == "1")
                            {
                                ptbReaderIn1.Tag = '0';
                                if (ptbReaderIn1.Image != null)
                                    ptbReaderIn1.Image.Dispose();
                                ptbReaderIn1.Image = Properties.Resources.Disconnect_rfid;
                                return; // MethodInvoker
                            }
                            return; // MethodInvoker
                        }
                    }));
                    return; // if r
                }
                if (r == '1')
                {
                    _bcheckServiceOut = 0;
                    ptbReaderOut1.Invoke(new MethodInvoker(() =>
                    {
                        if (c == 'C')
                        {
                            if (ptbReaderOut1.Tag.ToString() == "0")
                            {
                                ptbReaderOut1.Tag = '1';
                                if (ptbReaderOut1.Image != null)
                                    ptbReaderOut1.Image.Dispose();
                                ptbReaderOut1.Image = Properties.Resources.Connect_rfid;
                                return; // MethodInvoker
                            }
                            return; // MethodInvoker
                        }
                        if (c == 'D')
                        {
                            if (ptbReaderOut1.Tag.ToString() == "1")
                            {
                                ptbReaderOut1.Tag = '0';
                                if (ptbReaderOut1.Image != null)
                                    ptbReaderOut1.Image.Dispose();
                                ptbReaderOut1.Image = Properties.Resources.Disconnect_rfid;
                                return; // MethodInvoker
                            }
                            return; // MethodInvoker
                        }
                    }));
                    return; // if r
                }
                return; // if topic
            }

            if (topic == TopicSub.CALL_SYNC_DATABSE_SERVICE)
            {
                List<ETagInfoSync> entities = await SqliteDataAccess.SyncDatbase();
                PublishMessage(JsonConvert.SerializeObject(entities), TopicPub.SYNC_DATABASE_SERVICE);
                return;
            }

            Reader.TagInfo? tagInfo = JsonConvert.DeserializeObject<Reader.TagInfo>(message);
            tagInfo = tagInfo ?? new Reader.TagInfo();


            /* if (topic == TopicSub.IN_NOTFOUND_TAG)
             {
                 label1.Invoke(new Action(() =>
                 {
                     label1.Text = $"{DateTime.Now.ToString(SqliteDataAccess._timeFormat + ".ffff")} : {tagInfo.tid} [thẻ Không tồn tại] [LỐI VÀO]";
                 }));
                 return;
             }

             if (topic == TopicSub.OUT_NOTFOUND_TAG)
             {
                 label1.Invoke(new Action(() =>
                 {
                     label1.Text = $"{DateTime.Now.ToString(SqliteDataAccess._timeFormat + ".ffff")} : {tagInfo.epc} [thẻ Không tồn tại] [LỐI RA]";
                 }));
                 return;
             }*/

            if (topic == TopicSub.IN_MESSAGE)
            {
                await InHandle(tagInfo);
                return;
            }

            if (topic == TopicSub.OUT_MESSAGE)
            {
                await OutHandle(tagInfo);
                return;
            }
            return;
        }

        #endregion

        #region TagInfo

        private async Task<DTOTagInfo> FindTagInfo(bool isNg, string value)
        {
            string key = "tidXe";
            if (isNg)
                key = "tidNg";
            DTOTagInfo dto = await SqliteDataAccess.FindByKey(key, value);

            if (dto == null)
            {
                return null;
            }
            return dto;
        }
        #endregion

        #region InHandle
        private async Task<bool> InHandle(Reader.TagInfo tagInfo)
        {
            DTOTagInfo dto = await FindTagInfo(tagInfo.isNg, tagInfo.tid);

            if (dto == null) return false;

            if (tagInfo.isNg)
            {
                // nếu Thẻ người đã vào
                if (dto.isInNg)
                {
                    PublishMessage(_messageFeedbackTheNg + tagInfo.tid, TopicPub.IN_MESSAGE_FEEDBACK);
                    return false;
                }

                //Cập nhật thẻ người
                dto = await SqliteDataAccess.UpdateByKey(
                   "TagInfo",
                   new string[] { "lastUpdate", "isInNg" },
                   new string[] { DateTime.UtcNow.ToString(SqliteDataAccess._timeFormat), "1" },
                   "tidNg", tagInfo.tid
                   );
                PublishMessage(_messageFeedbackTheNg + tagInfo.tid, TopicPub.IN_MESSAGE_FEEDBACK);
            }

            else
            {
                // nếu Thẻ Xe đã vào
                if (dto.isInXe)
                {
                    PublishMessage(_messageFeedbackTheXe + tagInfo.tid, TopicPub.IN_MESSAGE_FEEDBACK);
                    return false;
                }

                //Cập nhật thẻ xe
                dto = await SqliteDataAccess.UpdateByKey(
                   "TagInfo",
                   new string[] { "lastUpdate", "isInXe" },
                   new string[] { DateTime.UtcNow.ToString(SqliteDataAccess._timeFormat), "1" },
                   "tidXe", tagInfo.tid
                   );
                PublishMessage(_messageFeedbackTheXe + tagInfo.tid, TopicPub.IN_MESSAGE_FEEDBACK);
            }
            if (dto.isInNg && dto.isInXe)
            {
                UpdateHomepageIn(dto);
                return true;
            }
            return false;
        }
        #endregion

        #region OutHandle
        private async Task<bool> OutHandle(Reader.TagInfo tagInfo)
        {

            DTOTagInfo dto = await FindTagInfo(tagInfo.isNg, tagInfo.tid);

            if (dto == null) return false;

            if (tagInfo.isNg)
            {
                // nếu Thẻ người đã ra
                if (!dto.isInNg)
                {
                    PublishMessage(_messageFeedbackTheNg + tagInfo.tid, TopicPub.OUT_MESSAGE_FEEDBACK);
                    return false;
                }

                //Cập nhật thẻ người
                dto = await SqliteDataAccess.UpdateByKey(
                   "TagInfo",
                   new string[] { "lastUpdate", "isInNg" },
                   new string[] { DateTime.UtcNow.ToString(SqliteDataAccess._timeFormat), "0" },
                   "tidNg", tagInfo.tid
                   );
                PublishMessage(_messageFeedbackTheNg + tagInfo.tid, TopicPub.OUT_MESSAGE_FEEDBACK);
            }

            else
            {
                // nếu Thẻ Xe đã ra khỏi bãi
                if (!dto.isInXe)
                {
                    PublishMessage(_messageFeedbackTheXe + tagInfo.tid, TopicPub.OUT_MESSAGE_FEEDBACK);
                    return false;
                }

                //Cập nhật thẻ xe
                dto = await SqliteDataAccess.UpdateByKey(
                   "TagInfo",
                   new string[] { "lastUpdate", "isInXe" },
                   new string[] { DateTime.UtcNow.ToString(SqliteDataAccess._timeFormat), "0" },
                   "tidXe", tagInfo.tid
                   );

                PublishMessage(_messageFeedbackTheXe + tagInfo.tid, TopicPub.OUT_MESSAGE_FEEDBACK);
            }
            if (!dto.isInNg && !dto.isInXe)
            {
                UpdateHomepageOut(dto);
                return true;
            }
            return false;
        }
        #endregion

        #region Update Hompage
        private void UpdateHomepageIn(DTOTagInfo dto)
        {
            Invoke(new MethodInvoker(() =>
            {

                Image bienSo = new Bitmap($@"{_pathSave}{dto.imgBienSoPath}");
                if (ptbVao2.Image != null)
                    ptbVao2.Image.Dispose();
                ptbVao2.Image = new Bitmap($@"{_pathSave}{dto.imgNgPath}");
                if (ptbVao4.Image != null)
                    ptbVao4.Image.Dispose();
                ptbVao4.Image = new Bitmap($@"{_pathSave}{dto.imgXePath}");
                if (ptbVao5.Image != null)
                    ptbVao5.Image.Dispose();
                ptbVao5.Image = bienSo;
                if (ptbCheckVao.Image != null)
                    ptbCheckVao.Image.Dispose();
                ptbCheckVao.Image = Properties.Resources._checked;

                lbNameNg.Text = dto.nameNg;
                lbPhuongTien.Text = dto.typeXe;
                lbTGVao.Text = dto.lastUpdate.ToString(_timeFormat);
                lbMaThe.Text = dto.tidNg;
                tbBienSo.Text = dto.nameXe;

                if (ptbVaoBottom5.Image != null)
                    ptbVaoBottom5.Image.Dispose();
                ptbVaoBottom5.Image = (ptbVaoBottom4.Image != null) ? (Image)ptbVaoBottom4.Image.Clone() : null;
                if (ptbVaoBottom4.Image != null)
                    ptbVaoBottom4.Image.Dispose();
                ptbVaoBottom4.Image = (ptbVaoBottom3.Image != null) ? (Image)ptbVaoBottom3.Image.Clone() : null;
                if (ptbVaoBottom3.Image != null)
                    ptbVaoBottom3.Image.Dispose();
                ptbVaoBottom3.Image = (ptbVaoBottom2.Image != null) ? (Image)ptbVaoBottom2.Image.Clone() : null;
                if (ptbVaoBottom2.Image != null)
                    ptbVaoBottom2.Image.Dispose();
                ptbVaoBottom2.Image = (ptbVaoBottom1.Image != null) ? (Image)ptbVaoBottom1.Image.Clone() : null;
                if (ptbVaoBottom1.Image != null)
                    ptbVaoBottom1.Image.Dispose();
                ptbVaoBottom1.Image = (Image)bienSo.Clone();
            }));
        }
        private void UpdateHomepageOut(DTOTagInfo dto)
        {
            Invoke(new MethodInvoker(() =>
            {

                Image bienSo = new Bitmap($@"{_pathSave}{dto.imgBienSoPath}");
                if (ptbRa2.Image != null)
                    ptbRa2.Image.Dispose();
                ptbRa2.Image = new Bitmap($@"{_pathSave}{dto.imgNgPath}");
                if (ptbRa4.Image != null)
                    ptbRa4.Image.Dispose();
                ptbRa4.Image = new Bitmap($@"{_pathSave}{dto.imgXePath}");
                if (ptbRa5.Image != null)
                    ptbRa5.Image.Dispose();
                ptbRa5.Image = bienSo;
                if (ptbCheckRa.Image != null)
                    ptbCheckRa.Image.Dispose();
                ptbCheckRa.Image = Properties.Resources._checked;

                lbNameNg1.Text = dto.nameNg;
                lbPhuongTien1.Text = dto.typeXe;
                lbTGGui1.Text = dto.lastUpdate.ToString(_timeFormat);
                lbMaThe1.Text = dto.tidNg;
                tbBienSo1.Text = dto.nameXe;

                if (ptbRaBottom5.Image != null)
                    ptbRaBottom5.Image.Dispose();
                ptbRaBottom5.Image = (ptbRaBottom4.Image != null) ? (Image)ptbRaBottom4.Image.Clone() : null;
                if (ptbRaBottom4.Image != null)
                    ptbRaBottom4.Image.Dispose();
                ptbRaBottom4.Image = (ptbRaBottom3.Image != null) ? (Image)ptbRaBottom3.Image.Clone() : null;
                if (ptbRaBottom3.Image != null)
                    ptbRaBottom3.Image.Dispose();
                ptbRaBottom3.Image = (ptbRaBottom2.Image != null) ? (Image)ptbRaBottom2.Image.Clone() : null;
                if (ptbRaBottom2.Image != null)
                    ptbRaBottom2.Image.Dispose();
                ptbRaBottom2.Image = (ptbRaBottom1.Image != null) ? (Image)ptbRaBottom1.Image.Clone() : null;
                if (ptbRaBottom1.Image != null)
                    ptbRaBottom1.Image.Dispose();
                ptbRaBottom1.Image = (Image)bienSo.Clone();
            }));
        }
        #endregion

        #region PublishMessage
        // Gửi message cho subcriber
        private async void PublishMessage(string message, string topic)
        {
            if (_mqtt_Client == null) return;
            if (!_mqtt_Client.IsConnected) { return; }

            MqttApplicationMessage message_Builder = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag()
                .Build();
            await _mqtt_Client.PublishAsync(message_Builder);
        }
        #endregion

        #endregion

        #region Thread handle
        private void CheckServiceThrd()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (_bcheckServiceIn < 3)
                    _bcheckServiceIn++;
                if (_bcheckServiceOut < 3)
                    _bcheckServiceOut++;

                if (_bcheckServiceIn == 3)
                {
                    ptbReaderIn1.Invoke(new MethodInvoker(() =>
                    {
                        if (ptbReaderIn1.Tag.ToString() == "1")
                        {
                            ptbReaderIn1.Tag = '0';
                            if (ptbReaderIn1.Image != null)
                                ptbReaderIn1.Image.Dispose();
                            ptbReaderIn1.Image = Properties.Resources.Disconnect_rfid;
                            return;
                        }
                        return;
                    }));
                    _bcheckServiceIn++;
                }
                if (_bcheckServiceOut == 3)
                {
                    ptbReaderOut1.Invoke(new MethodInvoker(() =>
                    {
                        if (ptbReaderOut1.Tag.ToString() == "1")
                        {
                            ptbReaderOut1.Tag = '0';
                            if (ptbReaderOut1.Image != null)
                                ptbReaderOut1.Image.Dispose();
                            ptbReaderOut1.Image = Properties.Resources.Disconnect_rfid;
                            return; // MethodInvoker
                        }
                        return; // MethodInvoker
                    }));
                    _bcheckServiceOut++;
                }
            }
        }
        private void Initialize_clockThread()
        {
            _clockThread = new Thread(UpdateClock); // Tạo một luồng mới
            _clockThread.Start(); // Khởi động luồng
        }

        // Phương thức chạy trên luồng để cập nhật thời gian
        private void UpdateClock()
        {
            while (true)
            {
                // Lấy thời gian hiện tại
                DateTime currentTime = DateTime.Now;

                // Format thời gian và hiển thị lên giao diện
                string formattedDate = currentTime.ToString("dd-MM-yyyy");
                string formattedTime = currentTime.ToString("hh:mm:ss tt");
                UpdateTimeLabel(formattedDate, formattedTime); // Gọi phương thức để cập nhật giao diện

                // Chờ 1 giây trước khi cập nhật thời gian tiếp theo
                Thread.Sleep(1000);
            }

        }

        // Phương thức để cập nhật label thời gian trên giao diện
        private void UpdateTimeLabel(string date, string time)
        {
            Invoke(new MethodInvoker(() =>
            {
                lbDate.Text = date; // Cập nhật label ngày tháng năm
                lbTime.Text = time; // Cập nhật label giờ phút giây
            }));
        }
        #endregion

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
