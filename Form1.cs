using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace 串口收发
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        SerialPort sp = new SerialPort();
        int number = 0;
        byte[] conSend = null;
        void ReceiveOrSend()
        {
            if (!sp.IsOpen)//判断端口是否开启
            {
                return;
            }

            int receiveTimeOut = 1000;//接收超时时间,超过则启动超时代码.
            DateTime waitTimeOut = DateTime.Now;//获取当前时间.
            while (true)
            {
                byte[] byRec = new byte[sp.ReadBufferSize];//创建一个字节变量,用来接收.大小为串口对象读取缓冲区大小.
                byte[] bySend = new byte[sp.WriteBufferSize];//创建一个字节变量,用来发送.大小为串口对象发送缓冲区大小.
                List<byte> lb = new List<byte>();//定义一个字节接收序列.
                
                bySend[0] = 0x01;
                bySend[1] = 0x03;
                bySend[2] = 0x01;
                bySend[3] = 0xa4;
                bySend[4] = 0x00;
                bySend[5] = 0x78;
                bySend[6] = 0x05;
                bySend[7] = 0x57;
                try
                {
                    while (true)//循环读取
                    {
                        int byTemp = sp.ReadByte();//单字节读取
                        lb.Add((byte)byTemp);//放入字节序列
                        if (byTemp == 0x0d)//判断是否读取到自定义结束标识.
                        {
                            break;//退出循环读取.
                        }
                    }
                    byRec = lb.ToArray();//字节接收序列转换为解码接收字节数组.
                }
                catch
                {
                }
                    if (byRec[0] == 0x01)//判断接收到字节数组中第一位是否为自定义数据完整标识.这里是如果第一位为1.则代表这是完整数据包.
                    {
                        if (byRec[byRec.Length - 1] == 0x0d)//判断最后一位是否是自定义停止标识.这里定义为0x0d;
                        {
                            sp.DiscardInBuffer();//释放接收缓存.

                            //
                            //处理数据处
                            //接收到数据.
                            string str = Encoding.Default.GetString(byRec, 1, byRec.Length - 1);
                            string mess="";
                                 int recNumber =0;
                            try
                            {
                                recNumber = int.Parse(str.Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries)[1].ToString());
                                mess = DateTime.Now + "接收到" + recNumber;
                            }
                            catch //这里可以判断第二位是什么,如果第二位为定义的一个数,那么就代表是发送的数据.这里使用的是异常.通讯里面用的是分割并转换数字.出异常则代表不是通讯信息
                            {
                                listBox2.Items.Add(str);
                                listBox2.SelectedIndex = listBox2.Items.Count - 1;
                            }

                           listBox1.Items.Add(mess);
                            listBox1.SelectedIndex = listBox1.Items.Count - 1;
                            if (recNumber!=0)
                            {
                                number = recNumber;
                            }
                            number++;
                            if (conSend!=null)
                            {
                                bySend = conSend;
                                conSend = null;
                            }
                            else
                            {
                                str = DateTime.Now + "发送至[" + number + "]";
                                bySend = Encoding.Default.GetBytes(str);
                            }
                            listBox1.Items.Add(str);
                            //以上为数据处理.
                            //
                            //
                            while (true)//循环发送线程.
                            {
                                bySend = Packaging(bySend);
                                sp.Write(bySend, 0, bySend.Length);//发送.
                                sp.DiscardOutBuffer();//清楚发送缓冲区.
                                waitTimeOut = DateTime.Now;//获取当前时间.
                                break;//退出发送循环.
                            }
                        }
                    }

                double timeOutNumber = (DateTime.Now - waitTimeOut).TotalMilliseconds;//获取当前延时时间与设定超时时间差.
                if (timeOutNumber > receiveTimeOut)//判断是否长时间为接收到数据
                {
                    string str = DateTime.Now + "超时启动代码[0]";
                    bySend = Encoding.Default.GetBytes(str);//同正常发送代码段.
                    bySend = Packaging(bySend);
                    sp.Write(bySend, 0, bySend.Length);//发送.
                    sp.DiscardOutBuffer();
                    waitTimeOut = DateTime.Now;//设置当前时间.
                }
                Thread.Sleep(100);//延时100
            }
        }

        byte[] bysT = new byte[1] { 0x01 };
        byte[] bysW = new byte[1] { 0x0d };
        private byte[] Packaging(byte[] send)
        {
            send = bysT.Concat(send).ToArray();
            return send.Concat(bysW).ToArray();
            
        }
        Thread th;
        private void button1_Click(object sender, EventArgs e)
        {
            sp.PortName = textBox1.Text;
            sp.BaudRate = int.Parse(textBox2.Text);
            sp.ReadBufferSize = 1024;
            sp.WriteBufferSize = 1024;
            sp.ReadTimeout = 1000;
            sp.Parity = Parity.None;
            if (!sp.IsOpen)
            {
                sp.Open();
                th = new Thread(ReceiveOrSend);
                th.IsBackground = true;
                th.Start();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                th.Abort();
                sp.Close();
            }
            catch
            {

            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!sp.IsOpen || textBox3.Text=="")
            {
                return;
            }

             conSend = Encoding.Default.GetBytes(textBox3.Text);//同正常发送代码段.
        }
    }
}
