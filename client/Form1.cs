using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client01
{
    public partial class Form1 : Form
    {
        // никнейм пользователя
        private static string nick;
        public Form1(string _nick)
        {
            InitializeComponent();
            nick = _nick;
            Text = nick;
        }
        
        // порт сервера
        private const int Port = 11000;

        // локальный дескриптор ожидания подключения
        private static AutoResetEvent ConnectDone =
            new AutoResetEvent(false);
        // локальный дескриптор ожидания подключения и получения ключа
        private static AutoResetEvent _connect =
            new AutoResetEvent(false);
        // локальный дескриптор ожидания отправки сообщения
        private static AutoResetEvent SendDone =
            new AutoResetEvent(false);
        // локальный дескриптор ожидания получения сообщения
        private static AutoResetEvent ReceiveDone =
            new AutoResetEvent(false);
        // локальный дескриптор ожидания процедуры чтения сообщений
        private static AutoResetEvent _read =
            new AutoResetEvent(false);
        // локальный дескриптор ожидания получения ключа шифрования
        private static AutoResetEvent KeysDone = 
            new AutoResetEvent(false);
        // клиентский сокет
        private static Socket client = null;
        // поток для основных функций
        private static Thread rd = null;
        // симметричный ключ и вектор
        private static byte[] symmetricKey, symmetricIv;
        /// <summary>
        ///  подключение и получение симметричного ключа
        /// </summary>
        private void Connect()
        {
            var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress;
            try
            {
                ipAddress = ipHostInfo.AddressList[4];
            }
            catch
            {
                ipAddress = ipHostInfo.AddressList[4];
            }
                        
            // поиск сервера и подключение к нему
            ConnectDone.Reset();
            client = findSocket(ipAddress.ToString());
            ConnectDone.WaitOne();
            // получение ключа шифрования
            KeysDone.Reset();
            KeysSec(client);
            KeysDone.WaitOne();
            _connect.Set();
        }
        /// <summary>
        ///  поиск сервера
        /// </summary>
        private Socket findSocket(string ip)
        {
            textPole.Invoke((MethodInvoker)delegate
            {
                textPole.Text = "Connecting.";
            });
            //List<string> str = finds(ip);
            Socket temp = null;
            // получение домена локальной сети
            string local = ip.Substring(0, ip.LastIndexOf(".")) + ".";
            int i = 0;
            while(true)
            {
                // создание сокета
                temp = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);
                // пробуем подключиться
                IAsyncResult result = temp.BeginConnect(IPAddress.Parse(ip), Port, null, null);
                // ожидание результата
                bool success = result.AsyncWaitHandle.WaitOne(500, true);

                if (!success)
                {
                    // если неудачно, закрыть сокет
                    temp.Close();
                    textPole.Invoke((MethodInvoker)delegate
                    {
                        textPole.Text += ".";
                    });
                }
                else
                {
                    try
                    {
                        // если удачно, то попытка завершить подключение
                        temp.EndConnect(result);
                        textPole.Invoke((MethodInvoker)delegate
                        {
                            textPole.Text = "Connected to " + temp.RemoteEndPoint + "\n";
                        });
                        // сигнал, что подключение есть
                        ConnectDone.Set();
                        break;
                    }
                    catch
                    {
                        // сервер отверг подключение, закрыть сокет
                        temp.Close();
                    }

                }
            }
            //while(true)
            //{
            //    // создание сокета
            //    temp = new Socket(AddressFamily.InterNetwork,
            //        SocketType.Stream, ProtocolType.Tcp);
            //    // пробуем подключиться
            //    IAsyncResult result = temp.BeginConnect(IPAddress.Parse(local + i++), Port, null, null);
            //    // ожидание результата
            //    bool success = result.AsyncWaitHandle.WaitOne(500, true);

            //    if (!success)
            //    {
            //        // если неудачно, закрыть сокет
            //        temp.Close();
            //        textPole.Invoke((MethodInvoker)delegate
            //        {
            //            textPole.Text += ".";
            //        });                    
            //    }
            //    else
            //    {
            //        try
            //        {
            //            // если удачно, то попытка завершить подключение
            //            temp.EndConnect(result);
            //            textPole.Invoke((MethodInvoker)delegate
            //            {
            //                textPole.Text = "Connected to " + temp.RemoteEndPoint + "\n";
            //            });
            //            // сигнал, что подключение есть
            //            ConnectDone.Set();
            //            break;
            //        }
            //        catch
            //        {
            //            // сервер отверг подключение, закрыть сокет
            //            temp.Close();
            //        }                    
                    
            //    }
            //    // начать поиск с начального адреса
            //    if (i == 255)
            //    {
            //        i = 0;
            //        textPole.Invoke((MethodInvoker)delegate
            //        {
            //            textPole.Text = "Connecting.";
            //        });
            //    }
            //}

            ConnectDone.Set();
            return temp;
        }
        private List<string> finds(string ip)
        {
            List<string> str = new List<string>();
            string local = ip.Substring(0, ip.LastIndexOf(".")) + ".";
            Ping pingSender = new Ping();
            for(int i = 0; i < 255; i++)
            {
                IPAddress address = IPAddress.Parse(local + i);
                PingReply reply = pingSender.Send(address, 10);
                if (reply.Status == IPStatus.Success)
                {
                    str.Add(local + i);
                }
                textPole.Invoke((MethodInvoker)delegate
                {
                    textPole.Text = i.ToString();
                });
            }
            return str;
        }
        /// <summary>
        ///  получение сообщений
        /// </summary>
        private void Read(object client)
        {
            ReceiveDone.Reset();
            // создание объекта для сообщений
            StateObject state = new StateObject { WorkSocket = (Socket)client };


            while (state.WorkSocket.Connected)
            {
                state.WorkSocket.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReadMessage, state);
                ReceiveDone.WaitOne();
            }
        }
        /// <summary>
        ///  асинхронное получение сообщений
        /// </summary>
        private void ReadMessage(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.WorkSocket;

            try
            {
                int bytesRead = handler.EndReceive(ar);
                byte[] nb = state.Buffer;
                Array.Resize(ref nb, bytesRead);
                textPole.Invoke((MethodInvoker)delegate
                {                    
                    // расшифровка и вывод сообщений
                    textPole.Text += DecryptStringFromBytes(nb, symmetricKey, symmetricIv) + "\r\n";
                });
            }
            catch
            {
                // сервер разорвал подключение
                textPole.Invoke((MethodInvoker)delegate
                {
                    textPole.Text += "Disconnected from " + handler.RemoteEndPoint + "\r\n";
                });
                // освобождение сокета
                handler.Disconnect(true);
                // запустить заново
                _read.Set();
            }
            ReceiveDone.Set();
        }

        /// <summary>
        ///  получение симметричного ключа через асимметричный
        /// </summary>
        private static void KeysSec(Socket client)
        {
            // асимметричный
            var rsa = new RSACryptoServiceProvider();
            var rsaKeyInfo = rsa.ExportParameters(false);
            var buffer = new byte[131];
            Array.Copy(rsaKeyInfo.Modulus, buffer, 128);
            Array.Copy(rsaKeyInfo.Exponent, 0, buffer, 128, 3);
            // отправка открытой части асимметричного ключа
            client.BeginSend(buffer, 0, 131, 0, SendKey1, client);
            KeysDone.WaitOne();

            buffer = new byte[256];
            // получение зашифрованного симметричного ключа
            client.BeginReceive(buffer, 0, 256, 0, ReadKey1, client);
            KeysDone.WaitOne();

            byte[] encryptedSymmetricKey = new byte[128], encryptedSymmetricIv = new byte[128];
            // достать из сообщения симметричный ключ и вектор
            Array.Copy(buffer, encryptedSymmetricKey, 128);
            Array.Copy(buffer, 128, encryptedSymmetricIv, 0, 128);
            // расшифровать симметричный ключ и вектор
            symmetricKey = rsa.Decrypt(encryptedSymmetricKey, false);
            symmetricIv = rsa.Decrypt(encryptedSymmetricIv, false);
            KeysDone.Set();
        }
        // асимметричная отправка ключа
        private static void SendKey1(IAsyncResult ar)
        {
            var handler = (Socket)ar.AsyncState;
            handler.EndSend(ar);
            KeysDone.Set();
        }
        // асимметричное получение ключа
        private static void ReadKey1(IAsyncResult ar)
        {
            var client = (Socket)ar.AsyncState;
            client.EndReceive(ar);
            KeysDone.Set();
        }
        public class StateObject
        {
            // клиентский сокет
            public Socket WorkSocket;
            // размер буфера сообщения
            public const int BufferSize = 1024;
            // буфер сообщения
            public byte[] Buffer = new byte[BufferSize];
        }
        // отправка сообщения при нажатии клавиши Enter
        private void textMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(this, new EventArgs());
            }
        }
        // асинхронная отправка сообщения
        private static void SendMessage(IAsyncResult ar)
        {
            Socket handler = (Socket)ar.AsyncState;
            handler.EndSend(ar);
            SendDone.Set();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
                     
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
        // запуск потока после прорисовки формы
        private void Form1_Shown(object sender, EventArgs e)
        {            
            rd = new Thread(load);
            rd.Start();
        }
        /// <summary>
        ///  процедура подключения и чтения сообщений
        /// </summary>
        private void load()
        {
            while (true)
            {
                _connect.Reset();
                Connect();
                _connect.WaitOne();
                _read.Reset();
                Read(client);
                _read.WaitOne();
                Thread.Sleep(7000);
            }
        }
        /// <summary>
        ///  отправка сообщения
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if(textMessage.Text != "")
            {                
                if(textMessage.Text == "курс валют")
                {
                    // обращение к веб-сервису
                    DataTable val_ds;
                    curseValue.DailyInfoSoapClient di = new curseValue.DailyInfoSoapClient();
                    var dt = DateTime.Now;
                    dt.ToString("dd.MM.yyyy");
                    string mes = "";
                    try
                    {
                        // доллар
                        val_ds = di.GetCursDynamic(dt, dt.AddDays(1), "R01235").Tables[0];
                        mes = "$: " + val_ds.Rows[0][3].ToString();
                        // евро
                        val_ds = di.GetCursDynamic(dt, dt.AddDays(1), "R01239").Tables[0];
                        mes += "  €: " + val_ds.Rows[0][3].ToString();
                    }
                    catch
                    {

                    }                    
                    textPole.Text += mes + "\r\n";
                }
                else
                {
                    // отправка сообщения на сервер
                    var buffer = EncryptStringToBytes(nick + ": " + textMessage.Text, symmetricKey, symmetricIv);
                    client.BeginSend(buffer, 0, buffer.Length, 0, SendMessage, client);
                    SendDone.WaitOne();                    
                }
                textMessage.Text = "";
            }
            
        }

        private void textMessage_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(rd != null) // если поток не завершен, то завершить
            {
                rd.Abort();
                rd.Join(300);
            }
            Application.Exit();
        }
        /// <summary>
        ///  расшифровать строку из байтов
        /// </summary>
        static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Rijndael object
            // with the specified key and IV.
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }
            return plaintext;

        }
        /// <summary>
        ///  зашифровать строку в байты
        /// </summary>
        static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an Rijndael object
            // with the specified key and IV.
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }
    }
}
