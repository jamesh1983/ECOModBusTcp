using HslCommunication.ModBus;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Modbus.Server
{
    public partial class Server : Form
    {
        private ModBusTcpServer tcpServer;
        private long m_ReceivedTimes { get; set; }

        private MySqlConnection Ali_MySQL_Connection = new MySqlConnection();
        private MySqlDataAdapter Ali_MySQL_DataAdapter = new MySqlDataAdapter();
        private MySqlCommand Ali_MySQL_Command = new MySqlCommand();

        private string ServerIP = "127.0.0.1",
                                UserID = "root",
                                UserPassword = "abc123xyz",
                                DBName = "prodict_database";
        private double ZK_W = -87.0, DC_W = 5.0;
        private int ZK_P = 80, DC_P = 80,
                            ZK_W1 = -86, DC_W1 = 5,
                            ZK_W2 = -85, DC_W2 = 5,
                            ZK_W3 = -84, DC_W3 = 5,
                            ZK_W4 = -83, DC_W4 = 5;
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
            textBox2.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.f") + " :" + 
                tempstring + 
                Environment.NewLine);
            listBox1.DataSource = float_data;
            listBox2.DataSource = bool_data;
            listBox3.DataSource = int_data;
            byte Machine_Unit =Convert.ToByte(int_data[int_data.Length-1]);

            Function_Data_Insert(Machine_Unit, float_data, bool_data, int_data);
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

        // ------------------------------------------------------------------------
        // Function for Data Insert into MysqlDATABase
        // ------------------------------------------------------------------------
        /// <summary>MySQL数据库插入新数据</summary>
        /// <param name="Machine_unit">用于确认插入数据设备ID</param>
        /// <param name="tempfloat">插入的浮点数据</param>
        /// <param name="tempbool">插入的布尔数据</param>
        /// <param name="tempint">插入的整型数据</param>
        private void Function_Data_Insert(byte Machine_unit, float[] tempfloat, bool[] tempbool, int[] tempint)
        {
            string SQL_Connection_String = "Server = " + ServerIP + ";";
            SQL_Connection_String += "User Id = " + UserID + ";";
            SQL_Connection_String += "Password  =" + UserPassword + ";";
            SQL_Connection_String += "Database = " + DBName;

            Ali_MySQL_Connection.ConnectionString = SQL_Connection_String;
            Ali_MySQL_Connection.Open();
            Ali_MySQL_Command.Connection = Ali_MySQL_Connection;

            string SQL_Command_String, Col_Str = null, Val_Str = null, SN_Str = "SN";
            int nf = tempfloat.Length, nb = tempbool.Length, ni = tempint.Length;
            if (Ali_MySQL_Connection.State == ConnectionState.Open)
            {
                SQL_Command_String = "INSERT INTO table_prodict (";
                for (int i = 0; i < nf; i++)
                {
                    int a = i + 1;
                    Col_Str += "Value" + a.ToString() + ", ";
                    Val_Str += tempfloat[i].ToString() + ",";
                    string Status_Update_String = "update table_status set float_data = " + tempfloat[i].ToString() + " where " +
                        "num = " + a.ToString() + " and calog= " + Convert.ToString(Machine_unit);
                    try
                    {
                        Ali_MySQL_Command.CommandText = Status_Update_String;
                        Ali_MySQL_Command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(this, "更新table_status浮点数据失败！" + e.Message, "注意！", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                    }
                }
                for (int i = 0; i < nb; i++)
                {
                    int a = i + 1;
                    Col_Str += "bool" + a.ToString() + ", ";
                    Val_Str += Convert.ToSByte(tempbool[i]).ToString() + ",";
                    string Status_Update_String = "update table_status set bool_data = " + Convert.ToSByte(tempbool[i]).ToString() + " where " +
                        "num = " + a.ToString() + " and calog= " + Convert.ToString(Machine_unit);
                    try
                    {
                        Ali_MySQL_Command.CommandText = Status_Update_String;
                        Ali_MySQL_Command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(this, "更新table_status布尔数据失败！" + e.Message, "注意！", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                    }
                }
                for (int i = 0; i < ni; i++)
                {
                    int a = i + 1;
                    Col_Str += "int_" + a.ToString() + ", ";
                    Val_Str += tempint[i].ToString() + ",";
                    string Status_Update_String = "update table_status set int_data = " + tempint[i].ToString() + " where " +
                        "num = " + a.ToString() + " and calog= " + Convert.ToString(Machine_unit);
                    try
                    {
                        Ali_MySQL_Command.CommandText = Status_Update_String;
                        Ali_MySQL_Command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(this, "更新table_status整型数据失败！" + e.Message, "注意！", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                    }
                }
                Col_Str += SN_Str;
                Val_Str += Convert.ToString(Machine_unit);
                SQL_Command_String += Col_Str + ") VALUES (" + Val_Str + ")";
                Ali_MySQL_Command.CommandText = SQL_Command_String;
                try
                {
                    Ali_MySQL_Command.ExecuteNonQuery();
                    //Ali_MySQL_Connection.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show(this, "插入table_prodict数据库失败！" + e.Message, "注意！", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                }
                string replace_cmd = "REPLACE INTO table_current (";
                replace_cmd += Col_Str + ") VALUES (" + Val_Str + ")";
                Ali_MySQL_Command.CommandText = replace_cmd;
                try
                {
                    if (Ali_MySQL_Connection.State != ConnectionState.Open) Ali_MySQL_Connection.Open();
                    Ali_MySQL_Command.ExecuteNonQuery();
                    //Ali_MySQL_Connection.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show(this, "修改table_current数据库失败！" + e.Message, "注意！", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                }
            }
            else MessageBox.Show("MySQL Connection not Ready.");
            Ali_MySQL_Connection.Close();
        }

        // ------------------------------------------------------------------------
        // Function for Prodiction Calculation
        // ------------------------------------------------------------------------
        /// <summary>计算并保存预测数据</summary>
        /// <param name="data">用于预测计算的基础数据集</param>
        /// <param name="machine_unit">数据集所属的设备序列号</param>
        /// <param name="warning_value">报警值端差/真空度值</param>
        /// <param name="warningpercent">预警百分数（达到百分值既报警）</param>
        /// <param name="warning1">一级报警值（最先达到）</param>
        /// <param name="warning2">二级报警值（次先达到）</param>
        /// <param name="warning3">三级报警值（次后达到）</param>
        /// <param name="warning4">四级报警值（最后达到）</param>
        private void Create_Prodict_Data(float[] data, int machine_unit, int modeType, double warning_value, int warningpercent, int warning1, int warning2, int warning3, int warning4, int period)
        {
            int n = data.Length, i = 0;//获得数据集大小
            float k = 0, b = 0;//拟合后的函数斜率与截距
            float sumxx = 0, sumx = 0, sumxy = 0, sumy = 0;
            float sum_y = 0, sum_x = 0;
            float SSR = 0, nvarX = 0, nvarY = 0, diffxxbar = 0, diffyybar = 0;
            float R_Value = 0;
            float R_Sq = 0;
            float pre_day = 0;
            if (n != 0)
            {
                for (i = 0; i < n; i++)
                {
                    sumxx += (i + 1) * (i + 1);
                    sumx += i + 1;
                    sumxy += (i + 1) * data[i];
                    sumy += data[i];
                }
                float fm = n * sumxx - sumx * sumx;
                float fz1 = n * sumxy - sumx * sumy;
                float fz2 = sumy * sumxx - sumx * sumxy;
                if (fz1 == 0 || fm == 0)
                {
                    k = 0;
                }
                else
                    k = fz1 / fm;//得出斜率值
                                 //    k = k.toFixed(3);//保留三位有效数字
                if (fz2 == 0 || fm == 0)
                {
                    b = 0;
                }
                else
                    b = fz2 / fm;//得出截距值
                                 //    b = b.toFixed(3);//保留三位有效数字

                for (i = 0; i < n; i++)
                {
                    sum_x += i + 1;
                    sum_y += data[i];
                }
                float avr_x = sum_x / n;
                float avr_y = sum_y / n;

                for (i = 0; i < n; i++)
                {
                    diffxxbar = i + 1 - avr_x;
                    diffyybar = data[i] - avr_y;
                    SSR += diffxxbar * diffyybar;
                    nvarX += diffxxbar * diffxxbar;
                    nvarY += diffyybar * diffyybar;
                }
                double SST = Math.Sqrt(nvarX * nvarY);
                if (SST == 0 || SSR == 0)
                {
                    R_Value = 0;
                }
                else
                    R_Value = SSR / Convert.ToSingle(SST);
                //    R_Value = R_Value.toFixed(3);//保留三位有效数字
                R_Sq = R_Value * R_Value;
                pre_day = Convert.ToSingle(warning_value) - b;
                pre_day = pre_day / k;
                pre_day = pre_day - n;
                pre_day = pre_day / 24;
                if ((pre_day <= 0) || (pre_day >= 999))
                {
                    pre_day = 999;
                }
                if (Math.Abs(R_Value) >= 0.3)
                {
                    if (k > 0)
                    {
                        //==================一级报警==================
                        if (pre_day <= warning4)
                        {
                            //pre_bar.value = pre_day + "天";
                            //pre_bar.style.color = "#FF0000";//红色
                            //warning_result.value = "warning";
                            //warning_result.style.color = "#FF0000";//红色
                        }
                        else
                        {
                            //==================二级报警==================
                            if (pre_day <= warning3)
                            {
                                //pre_day = pre_day.toFixed(0);
                                //pre_bar.value = pre_day + "天";
                                //pre_bar.style.color = "#FFA500";//橙色
                                //warning_result.value = "warning";
                                //warning_result.style.color = "#FFA500";//橙色
                            }
                            else
                            {
                                //==================三级报警==================
                                if (pre_day <= warning2)
                                {
                                    //pre_day = pre_day.toFixed(0);
                                    //pre_bar.value = pre_day + "天";
                                    //pre_bar.style.color = "#FFFF00";//黄色
                                    //warning_result.value = "warning";
                                    //warning_result.style.color = "#FFFF00";//黄色
                                }
                                else
                                {
                                    //==================四级报警==================
                                    if (pre_day <= warning1)
                                    {
                                        //pre_day = pre_day.toFixed(0);
                                        //pre_bar.value = pre_day + "天";
                                        //pre_bar.style.color = "#0000FF";//蓝色
                                        //warning_result.value = "warning";
                                        //warning_result.style.color = "#0000FF";//蓝色
                                    }
                                    //==================提请关注==================
                                    else
                                    {
                                        //pre_day = pre_day.toFixed(0);
                                        //pre_bar.value = pre_day + "天（提请关注）";
                                        //pre_bar.style.color = "#000000";//黑色
                                        //warning_result.value = "warning";
                                        //warning_result.style.color = "#000000";//黑色
                                    }
                                }
                            }
                        }
                    }
                    //==================趋势向好==================
                    else
                    {
                        //pre_day = pre_day.toFixed(0);
                        //pre_bar.value = pre_day + "天（趋好）";
                    }
                }
                //===============================================================================================
                else
                {
                    int count = 0;
                    int warning_offset_value = 0;
                    for (i = 0; i < n; i++)
                    {
                        if (Math.Abs(warning_value - warning_offset_value) < Math.Abs(data[i]))
                        {
                            count++;
                        }
                    }
                    float curren_percent = count / n;
                    float warning_percent_value = 0;
                    float warning_per = warning_percent_value / 100;
                    //==============在相关度比较差的前提下判断偏离情况==================
                    if (curren_percent > warning_per)
                    {
                        //warning_result.value = "warning";
                        //warning_result.style.color = "#FFA500";//橙色
                    }
                }
            }
            //int[] xdata = new int[data.Length];//预测days（天）数据
            //float[] ydata = new float[data.Length];
            if (Ali_MySQL_Connection.State != ConnectionState.Open) Ali_MySQL_Connection.Open();
            if (Ali_MySQL_Connection.State == ConnectionState.Open)
            {
                string SQL_Command_String = "INSERT INTO table_result (SN,MD,period,kvalue,bvalue,rsqr,warningvalue,warningpercent,pre_day,calculatedatetime) VALUE (";
                string Val_Str = "";
                //xdata[i] = i + 1;
                //ydata[i] = k * (i + 1) + b;
                Val_Str = Convert.ToString(machine_unit) + ",";
                Val_Str += Convert.ToString(modeType) + ",";
                Val_Str += Convert.ToString(period) + ",";
                Val_Str += k.ToString() + ",";
                Val_Str += b.ToString() + ",";
                Val_Str += R_Sq.ToString() + ",";
                Val_Str += warning_value.ToString() + ",";
                Val_Str += warningpercent.ToString() + ",";
                Val_Str += pre_day.ToString() + ",";
                Val_Str += System.DateTime.Now.ToString("yyyyMMddhhmmss") + ")";
                SQL_Command_String += Val_Str;
                try
                {
                    Ali_MySQL_Command.CommandText = SQL_Command_String;
                    Ali_MySQL_Command.ExecuteNonQuery();
                    Ali_MySQL_Connection.Close();
                }
                catch (Exception e)
                {
                    string tempstring = e.Message;
                    MessageBox.Show("MySQL Update Failed.");
                    //throw;
                }
            }
            else MessageBox.Show("MySQL Connection not Ready.");
        }

        // ------------------------------------------------------------------------
        // Function for Warning Data Calculation
        // ------------------------------------------------------------------------
        /// <summary>计算并保存预测数据</summary>
        /// <param name="SN">设备ID号</param>
        private void Function_Do_Calculation(int SN)
        {
            string SQL_Select =
                    "SELECT avg(Value5-Value6) " +
                    "FROM table_prodict " +
                    "WHERE SN = " + SN.ToString() + " " +
                    "AND Value4> 5 " +
                    "AND DATE_SUB(CURDATE(), INTERVAL 30 DAY)<= date(TIMETAG) " +
                    "GROUP BY year(TIMETAG), month(TIMETAG), date(TIMETAG), hour(TIMETAG)";
            DataSet Origin_Data = Data_Select(ServerIP, UserID, UserPassword, DBName, SQL_Select);
            float[] data_value = new float[Origin_Data.Tables[0].Rows.Count];
            for (int i = 0; i < Origin_Data.Tables[0].Rows.Count; i++)
            {
                data_value[i] = Convert.ToSingle(Origin_Data.Tables[0].Rows[i].ItemArray[0]);
            }
            Create_Prodict_Data(data_value, SN, 2, ZK_W, ZK_P, ZK_W1, ZK_W2, ZK_W3, ZK_W4, 30);

            SQL_Select =
                "SELECT avg(Value5-Value6) " +
                "FROM table_prodict " +
                "WHERE SN = " + SN.ToString() + " " +
                "AND Value4> 5 " +
                "AND DATE_SUB(CURDATE(), INTERVAL 7 DAY)<= date(TIMETAG) " +
                "GROUP BY year(TIMETAG), month(TIMETAG), date(TIMETAG), hour(TIMETAG)";
            Origin_Data = Data_Select(ServerIP, UserID, UserPassword, DBName, SQL_Select);
            data_value = new float[Origin_Data.Tables[0].Rows.Count];
            for (int i = 0; i < Origin_Data.Tables[0].Rows.Count; i++)
            {
                data_value[i] = Convert.ToSingle(Origin_Data.Tables[0].Rows[i].ItemArray[0]);
            }
            Create_Prodict_Data(data_value, SN, 2, ZK_W, ZK_P, ZK_W1, ZK_W2, ZK_W3, ZK_W4, 7);

            SQL_Select =
                "SELECT avg(Value5-Value6) " +
                "FROM table_prodict " +
                "WHERE SN = " + SN.ToString() + " " +
                "AND Value4> 5 " +
                "AND DATE_SUB(CURDATE(), INTERVAL 1 DAY)<= date(TIMETAG) " +
                "GROUP BY year(TIMETAG), month(TIMETAG), date(TIMETAG), hour(TIMETAG)";
            Origin_Data = Data_Select(ServerIP, UserID, UserPassword, DBName, SQL_Select);
            data_value = new float[Origin_Data.Tables[0].Rows.Count];
            for (int i = 0; i < Origin_Data.Tables[0].Rows.Count; i++)
            {
                data_value[i] = Convert.ToSingle(Origin_Data.Tables[0].Rows[i].ItemArray[0]);
            }
            Create_Prodict_Data(data_value, SN, 2, ZK_W, ZK_P, ZK_W1, ZK_W2, ZK_W3, ZK_W4, 1);

            SQL_Select =
                "SELECT avg(Value5-Value6) " +
                "FROM table_prodict " +
                "WHERE SN = " + SN.ToString() + " " +
                "AND Value4> 5 " +
                "AND DATE_SUB(CURDATE(), INTERVAL 30 DAY)<= date(TIMETAG) " +
                "GROUP BY year(TIMETAG), month(TIMETAG), date(TIMETAG), hour(TIMETAG) ";
            Origin_Data = Data_Select(ServerIP, UserID, UserPassword, DBName, SQL_Select);
            data_value = new float[Origin_Data.Tables[0].Rows.Count];
            for (int i = 0; i < Origin_Data.Tables[0].Rows.Count; i++)
            {
                data_value[i] = Convert.ToSingle(Origin_Data.Tables[0].Rows[i].ItemArray[0]);
            }
            Create_Prodict_Data(data_value, SN, 1, DC_W, DC_P, DC_W1, DC_W2, DC_W3, DC_W4, 30);

            SQL_Select =
                "SELECT avg(Value5-Value6) " +
                "FROM table_prodict " +
                "WHERE SN = " + SN.ToString() + " " +
                "AND Value4> 5 " +
                "AND DATE_SUB(CURDATE(), INTERVAL 7 DAY)<= date(TIMETAG) " +
                "GROUP BY year(TIMETAG), month(TIMETAG), date(TIMETAG), hour(TIMETAG) ";
            Origin_Data = Data_Select(ServerIP, UserID, UserPassword, DBName, SQL_Select);
            data_value = new float[Origin_Data.Tables[0].Rows.Count];
            for (int i = 0; i < Origin_Data.Tables[0].Rows.Count; i++)
            {
                data_value[i] = Convert.ToSingle(Origin_Data.Tables[0].Rows[i].ItemArray[0]);
            }
            Create_Prodict_Data(data_value, SN, 1, DC_W, DC_P, DC_W1, DC_W2, DC_W3, DC_W4, 7);

            SQL_Select =
                "SELECT avg(Value5-Value6) " +
                "FROM table_prodict " +
                "WHERE SN = " + SN.ToString() + " " +
                "AND Value4> 5 " +
                "AND DATE_SUB(CURDATE(), INTERVAL 1 DAY)<= date(TIMETAG) " +
                "GROUP BY year(TIMETAG), month(TIMETAG), date(TIMETAG), hour(TIMETAG) ";
            Origin_Data = Data_Select(ServerIP, UserID, UserPassword, DBName, SQL_Select);
            data_value = new float[Origin_Data.Tables[0].Rows.Count];
            for (int i = 0; i < Origin_Data.Tables[0].Rows.Count; i++)
            {
                data_value[i] = Convert.ToSingle(Origin_Data.Tables[0].Rows[i].ItemArray[0]);
            }
            Create_Prodict_Data(data_value, SN, 1, DC_W, DC_P, DC_W1, DC_W2, DC_W3, DC_W4, 1);

        }

        // ------------------------------------------------------------------------
        // Function for Data Select
        // ------------------------------------------------------------------------
        /// <summary>查询数据库数据</summary>
        /// <param name="server">服务器IP地址</param>
        /// <param name="id">用户ID</param>
        /// <param name="password">用户密码</param>
        /// <param name="database">数据库名</param>
        /// <param name="select_string">查询语句</param>
        private DataSet Data_Select(string server, string id, string password, string database, string select_string)
        {
            DataSet dataSet = new DataSet();
            string SQL_Connection_String = "Server = " + server + ";";
            SQL_Connection_String += "User Id = " + id + ";";
            SQL_Connection_String += "Password  =" + password + ";";
            SQL_Connection_String += "Database = " + database;
            try
            {
                Ali_MySQL_Connection.ConnectionString = SQL_Connection_String;
                Ali_MySQL_Connection.Open();
                Ali_MySQL_Command.Connection = Ali_MySQL_Connection;
                string CommandString = select_string;
                Ali_MySQL_Command.CommandText = CommandString;//"SELECT * FROM INFORMATION_SCHEMA.TABLES";
                Ali_MySQL_DataAdapter.SelectCommand = Ali_MySQL_Command;
                Ali_MySQL_DataAdapter.Fill(dataSet);
                Ali_MySQL_Connection.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "表单查询失败！" + ex.Message, "注意！", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            }
            return dataSet;
        }
    }
}
