using HslCommunication.ModBus;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Modbus.Server
{
    public partial class Server : Form
    {
        private ModBusTcpServer tcpServer;
        private long m_ReceivedTimes { get; set; }
        public Server()
        {
            InitializeComponent();

            timer.Interval = 1000;
            timer.Tick += Timer_Tick;

            tcpServer = new ModBusTcpServer(); // 实例化服务器接收对象
            tcpServer.LogNet = new HslCommunication.LogNet.LogNetSingle(Application.StartupPath + @"\Logs\log.txt"); // 设置日志文件
            tcpServer.OnDataReceived += TcpServer_OnDataReceived; // 关联数据接收方法
        }
        private void userButton1_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBox1.Text, out int port))
            {
                tcpServer.ServerStart(port); // 绑定端口
                timer.Start(); // 启动服务
                textBox1.Enabled = false;
                userButton1.Enabled = false;
                userButton2.Enabled = true;
            }
            else
            {
                MessageBox.Show("格式输入有误");
            }
        }

        private void TcpServer_OnDataReceived(byte[] object1)
        {
            m_ReceivedTimes++;
            BeginInvoke(new Action<byte[]>(ShowModbusData), object1);
        }

        private void ShowModbusData(byte[] modbus)
        {
            //textBox2.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.f") + " :" +
            //    HslCommunication.BasicFramework.SoftBasic.ByteToHexString(modbus) + Environment.NewLine);
            string tempstring = "";
            float[] float_data = ByteArray_To_FloatArray(modbus,13,80);
            bool[] bool_data = ByteArray_To_BoolArray(modbus,93,8);
            int[] int_data = ByteArray_To_IntArray(modbus,101,112);
            label4.Text = modbus.Length.ToString();
            //int a = 0, b = 0, c = 0;
            for (int i = 0; i < modbus.Length; i++)
            {
                //if (i > 92 && i <= 100) 
                //{
                //    bool_data[b] = modbus[i];
                //tempstring += modbus[i].ToString();
                
                //if (i <= 92 && i > 12)
                //{
                //    float_data[a] = modbus[i];
                //    a++;
                //}
                //    b++;
                //}
                //if (i > 100 && i <= 213)
                //{
                //    int_data[c] = modbus[i];
                //    c++;
                //}
            }
            textBox2.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.f") + " :" + 
                tempstring + 
                Environment.NewLine);
            listBox1.DataSource = float_data;
            listBox2.DataSource = bool_data;
            listBox3.DataSource = int_data;
        }
        
        private Timer timer = new Timer();
        private long times_old = 0;
        private void Timer_Tick(object sender, EventArgs e)
        {
            long times = m_ReceivedTimes - times_old;
            label_times.Text = times.ToString();
            times_old = m_ReceivedTimes;
        }

        private void Server_FormClosing(object sender, FormClosingEventArgs e)
        {
            tcpServer?.ServerClose();
        }

        private void userButton2_Click(object sender, EventArgs e)
        {
            //tcpServer?.ServerClose();
            tcpServer.ServerClose();
            timer.Stop();
            textBox1.Enabled = true;
            userButton1.Enabled = true;
            userButton2.Enabled = false;
        }

        private bool[] ByteArray_To_BoolArray(byte[] sourcearray, int sourceindex, int arraylength)
        {
            bool[] destinationarray = new bool[arraylength*8];
            if (sourcearray.Length >= arraylength)
            {
                byte[] temp_bool = new byte[arraylength];
                destinationarray = new bool[arraylength * 8];
                Array.Copy(sourcearray, sourceindex, temp_bool, 0, arraylength);
                for (int i = 0; i < temp_bool.Length; i++)
                {
                    int tempdata = temp_bool[i];
                    for (int j = 0; j < 8; j++)
                    {
                        int x = Convert.ToInt16(Math.Pow(2, j));
                        tempdata = temp_bool[i] & x;
                        destinationarray[i * 8 + j] = Convert.ToBoolean(tempdata);
                    }
                }
            }
            else MessageBox.Show("数据长度错误！");
            return destinationarray;
        }

        private float[] ByteArray_To_FloatArray(byte[] sourcearray, int sourceindex, int arraylength)
        {
            float[] destinationarray = new float[arraylength / 4];
            if ((sourcearray.Length >= arraylength) && ((arraylength % 4) == 0))
            {
                byte[] temp_byte = new byte[arraylength];
                destinationarray = new float[arraylength / 4];
                Array.Copy(sourcearray, sourceindex, temp_byte, 0, arraylength);
                for (int i = 0; i < arraylength; i = i + 4)
                {
                    byte[] bytedata = { temp_byte[i + 3], temp_byte[i + 2], temp_byte[i + 1], temp_byte[i] };
                    destinationarray[i / 4] = Byte_To_Float(bytedata);
                }
            }
            else MessageBox.Show("数据长度错误！");
            return destinationarray;
        }

        private int[] ByteArray_To_IntArray(byte[] sourcearray, int sourceindex, int arraylength)
        {
            int[] destinationarray = new int[arraylength / 2];
            if ((sourcearray.Length >= arraylength) && ((arraylength % 2) == 0))
            {
                byte[] temp_bool = new byte[arraylength];
                destinationarray = new int[arraylength / 2];
                Array.Copy(sourcearray, sourceindex, temp_bool, 0, arraylength);
                for (int x = 0; x < arraylength; x = x + 2)
                {
                    destinationarray[x / 2] = temp_bool[x] << 8 | temp_bool[x + 1];
                }
            }
            else MessageBox.Show("数据长度错误！");
            return destinationarray;
        }

        private static float Byte_To_Float(byte[] data)
        {
            unsafe
            {
                float a = 0.0F;
                byte i;
                byte[] x = data;
                void* pf;
                fixed (byte* px = x)
                {
                    pf = &a;
                    for (i = 0; i < data.Length; i++)
                    {
                        *((byte*)pf + i) = *(px + i);
                    }
                }
                return a;
            }
        }
    }
}
