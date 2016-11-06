using dbserver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client01
{
    public partial class Login : Form
    {
        private static TcpChannel _channel = new TcpChannel(); //канал передачи TCP
        public Login()
        {
            InitializeComponent();

            ChannelServices.RegisterChannel(_channel, true); //регистрация канала
            findDB = new Thread(regChannel); //новый поток для поиска сервера
            findDB.Start();//запуск этого потока
        }

        //триггер для приостановки потока, если сервер найден
        private static readonly AutoResetEvent _server = new AutoResetEvent(false);

        Thread findDB = null;
                
        private static IDBRemoteService _instance; //интерфейс сервера с базой данных

        /// <summary>
        ///  Поиск сервера с базой данных
        /// </summary>
        private void regChannel()
        {
            try
            {
                var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                var ipAddress = ipHostInfo.AddressList[4]; //ip-адрес
                string ip = ipAddress.ToString();
                string local = ip.Substring(0, ip.LastIndexOf(".")) + "."; //получение домена локальной сети
                bool check = false;
                Task t;
                Invoke((MethodInvoker)delegate
                {
                    status.Text = "Search server";
                });
                int i = 1;
                //List<string> str = finds(ip);
                //foreach(string st in str)
                while(true)
                {
                    try
                    {
                        _instance = (IDBRemoteService)Activator.GetObject(typeof(IDBRemoteService),
                                        "tcp://" + ip + ":51495/check"); //подключение интерфейса к адресу
                        t = Task.Run(() =>
                        {
                            try
                            {
                                check = _instance.check(); //вызов функции, если успешно, то сервер найден
                            Invoke((MethodInvoker)delegate
                                {
                                    status.Text = "Server found";
                                });
                            }
                            catch
                            {

                            }
                        });
                        check = t.Wait(100); //ожидание завершений 0.5 секунды
                        if (check)
                        {
                            _server.Reset();
                            _server.WaitOne(); //если сервер найден, то приостановка потока
                            Invoke((MethodInvoker)delegate
                            {
                                status.Text = "Search server";
                            });
                        }
                    }
                    catch
                    {

                    }
                    if (i == 255) i = 1; //начать с начального адреса
                }
            }
            catch
            {
                MessageBox.Show("error ip");
            }
        }
        private List<string> finds(string ip)
        {
            List<string> str = new List<string>();
            string local = ip.Substring(0, ip.LastIndexOf(".")) + ".";
            Ping pingSender = new Ping();
            for (int i = 0; i < 255; i++)
            {
                IPAddress address = IPAddress.Parse(local + i);
                PingReply reply = pingSender.Send(address, 10);
                if (reply.Status == IPStatus.Success)
                {
                    str.Add(local + i);
                }
            }
            return str;
        }
        private void Login_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (findDB != null) //если поток не завершен, то завершить
            {
                findDB.Abort();
                findDB.Join(300);
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
        /// <summary>
        ///  Нажатие кнопки аунтентификации
        /// </summary>
        private void buttonLogin_Click(object sender, EventArgs e)
        {
            try
            {
                int check;
                //вызов процедуры атентификации
                check = _instance.GetCheck(textBoxLogin.Text, textBoxPassword.Text);
                if (check == 1) status.Text = "Wrong login or password";//неверный логин или пароль
                else if (check == 2)//успешно
                {
                    status.Text = "Success";
                    Form1 _form = new Form1(textBoxLogin.Text);//инициализация формы чата
                    Hide();//скрытие этой
                    findDB.Abort();//завершение потока
                    findDB.Join(300);
                    _form.Show();//открытие формы                   
                }                
            }
            catch//сервер не отвечает
            {
                status.Text = "Failed";
                Invoke((MethodInvoker)delegate
                {
                    _server.Set();//продолжить поток поиска сервера
                });                
            }
        }
        /// <summary>
        ///  Нажатие кнопки регистрации
        /// </summary>
        private void buttonRegister_Click(object sender, EventArgs e)
        {
            try
            {
                int check = 0;
                //вызов процедуры регистрации
                check = _instance.Register(textBoxLogin.Text, textBoxPassword.Text);
                if (check == 3) status.Text = "Login is exist";
                else if (check == 2)//успешно
                {
                    status.Text = "Success";
                    Form1 _form = new Form1(textBoxLogin.Text);//инициализация формы чата
                    Hide();//скрытие этой
                    findDB.Abort();//завершение потока
                    findDB.Join(300);
                    _form.Show();//открытие формы
                }
            }
            catch//сервер не отвечает
            {
                status.Text = "Failed";
                Invoke((MethodInvoker)delegate
                {
                    _server.Set();//продолжить поток поиска сервера
                });
            }
        }
    }
}
