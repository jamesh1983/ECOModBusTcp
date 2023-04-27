﻿using HslCommunication.ModBus;
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
            for (int i = 0; i < modbus.Length; i++)
            {
                tempstring += modbus[i].ToString();
            }
            textBox2.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.f") + " :" + 
                tempstring + 
                Environment.NewLine);
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
    }
}
