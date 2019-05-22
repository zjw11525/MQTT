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
using MQTTnet;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;


namespace MQTTClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MqttClient mqttClient = null;
        public MainWindow()
        {
            InitializeComponent();
            Task.Run(async () => { await ConnectMqttServerAsync(); });
        }
        private async Task ConnectMqttServerAsync()
        {
            if (mqttClient == null)
            {
                mqttClient = new MqttClientFactory().CreateMqttClient() as MqttClient;
                mqttClient.ApplicationMessageReceived += MqttClient_ApplicationMessageReceived;
                mqttClient.Connected += MqttClient_Connected;
                mqttClient.Disconnected += MqttClient_Disconnected;
            }

            try
            {
                var options = new MqttClientTcpOptions
                {
                    Server = "127.0.0.1",
                    ClientId = Guid.NewGuid().ToString().Substring(0, 5),
                    UserName = "u001",
                    Password = "p001",
                    CleanSession = true
                };

                await mqttClient.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke((new Action(() =>
                {
                    txtReceiveMessage.AppendText($"连接到MQTT服务器失败！" + Environment.NewLine + ex.Message + Environment.NewLine);
                })));
            }
        }

        private void MqttClient_Connected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke((new Action(() =>
            {
                txtReceiveMessage.AppendText("已连接到MQTT服务器！" + Environment.NewLine);
            })));
        }

        private void MqttClient_Disconnected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke((new Action(() =>
            {
                txtReceiveMessage.AppendText("已断开MQTT连接！" + Environment.NewLine);
            })));
        }

        private void MqttClient_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((new Action(() =>
            {
                txtReceiveMessage.AppendText($">> {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}{Environment.NewLine}");
            })));
        }

        private void BtnPublish_Click(object sender, RoutedEventArgs e)
        {
            string topic = txtPubTopic.Text.Trim();

            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("发布主题不能为空！");
                return;
            }

            string inputString = txtSendMessage.Text.Trim();
            var appMsg = new MqttApplicationMessage(topic, Encoding.UTF8.GetBytes(inputString), MqttQualityOfServiceLevel.AtMostOnce, false);
            mqttClient.PublishAsync(appMsg);
        }

        private void BtnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            string topic = txtSubTopic.Text.Trim();

            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("订阅主题不能为空！");
                return;
            }

            if (!mqttClient.IsConnected)
            {
                MessageBox.Show("MQTT客户端尚未连接！");
                return;
            }

            mqttClient.SubscribeAsync(new List<TopicFilter> {
                new TopicFilter(topic, MqttQualityOfServiceLevel.AtMostOnce)
            });

            txtReceiveMessage.AppendText($"已订阅[{topic}]主题" + Environment.NewLine);
            txtSubTopic.IsEnabled = false;
            btnSubscribe.IsEnabled = false;
        }
    }
}
