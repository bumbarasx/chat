using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using dbserver;
using System.IO;

namespace server
{
    internal class StateObject
    {
        // размер буфера сообщения
        public const int BufferSize = 1024;
        // бефер получаемого сообщения
        public byte[] Buffer = new byte[BufferSize];

        // локальный дескриптор ожидания получения, отправки ключей шифрования
        public AutoResetEvent KeysDone { get; } = new AutoResetEvent(false);
        // локальный дескриптор ожидания получения, отправки сообщения
        public AutoResetEvent ReceiveDone { get; } = new AutoResetEvent(false);
        // клиентский сокет
        public Socket WorkSocket { get; }
        // симметричный ключ и вектор для дополнительного сервера
        public byte[] symmetricKey, symmetricIv;

        public StateObject(Socket socket)
        {
            WorkSocket = socket;
            KeysDone.Reset();
            ReceiveDone.Reset();
        }
    }

    public class AsynchronousSocketListener
    {
        // локальный дескриптор ожидания подключения клиента
        private static readonly ManualResetEvent AllDone = new ManualResetEvent(false);
        // локальный дескриптор ожидания подключения доп. сервера
        private static readonly ManualResetEvent AllDoneServer = new ManualResetEvent(false);
        // локальный дескриптор ожидания получения, отправки ключей шифрования
        private static readonly AutoResetEvent KeysDone = new AutoResetEvent(false);
        // список клиентов сервера
        private static readonly List<Socket> ClientSockets = new List<Socket>();
        // симметричный ключ шифрования
        private static readonly RijndaelManaged Rm = new RijndaelManaged();        
        // порт сервера для клиентов
        private const int Port = 11000;
        // порт сервера для доп. серверов
        private const int PortS = 11001;
        // поток для доп. серверов
        private static Thread _dbs = null;
        /// <summary>
        ///  Основная функция запсука сервера
        /// </summary>
        private static void StartListening()
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
            var localEndPoint = new IPEndPoint(ipAddress, Port);
            Console.WriteLine(ipAddress);

            // создание tcp/ip сокета
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // привязка сокета к локальному хосту и ожидание входящих подключений
            try
            {
                Console.WriteLine("Started...");
                listener.Bind(localEndPoint);
                listener.Listen(10);
                // создание новго потока ожидания входящих
                // подключения от дополнительного сервера
                _dbs = new Thread(listenServers);
                _dbs.Start(new IPEndPoint(ipAddress, PortS));

                while (true)
                {
                    // сброс дескриптора
                    AllDone.Reset();

                    // запуск асинхронного сокета принятия входящего подключения
                    listener.BeginAccept(AcceptCallback, listener);
                    // ожидание пока подключение создастся для продолжения
                    AllDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
        }
        /// <summary>
        ///  привязка сокета к локальному хосту и ожидание входящих подключений
        /// </summary>        
        private static void listenServers(object localEndPoint)
        {
            var listenerS = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            listenerS.Bind((IPEndPoint)localEndPoint);
            listenerS.Listen(10);

            while (true)
            {
                // сброс дескриптора
                AllDoneServer.Reset();

                // запуск асинхронного сокета принятия входящего подключения
                listenerS.BeginAccept(AcceptCallbackServer, listenerS);
                // ожидание пока подключение создастся для продолжения
                AllDoneServer.WaitOne();
            }
        }

        private static void AcceptCallbackServer(IAsyncResult ar)
        {
            //сигнал потоку продолжить свою работу
            AllDoneServer.Set();
            // получение сокета клиента
            var listener = (Socket)ar.AsyncState;
            //закочить принятие подключения
            var handler = listener.EndAccept(ar);
            //добавить в список клиентов сервера
            ClientSockets.Add(handler);
            // создать новый объект для сообщений
            var state = new StateObject(handler);
            // отправка симметричного ключа
            Keys(state);
            KeysDone.Reset();
            // получение симметричного ключа
            var keys = KeysSec(state);            
            KeysDone.WaitOne();
            state.symmetricKey = keys.symmetricKey;
            state.symmetricIv = keys.symmetricIv;

            Console.WriteLine("Socket Server connected {0}", state.WorkSocket.RemoteEndPoint);
            //получение сообщений от дополнительного сервера
            while (state.WorkSocket.Connected)
            {
                state.WorkSocket.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReadMessageServer, state);
                state.ReceiveDone.WaitOne();
            }
        }
        private static _keys KeysSec(StateObject _state)
        {
            var key = new _keys();
            var client = _state.WorkSocket;
            //асимметричный ключ
            var rsa = new RSACryptoServiceProvider();
            var rsaKeyInfo = rsa.ExportParameters(false);
            var buffer = new byte[131];
            //копирование откртого ключа и вектора в одно сообщение
            Array.Copy(rsaKeyInfo.Modulus, buffer, 128);
            Array.Copy(rsaKeyInfo.Exponent, 0, buffer, 128, 3);
            // отправка
            client.BeginSend(buffer, 0, 131, 0, SendKey2, client);
            KeysDone.WaitOne();

            buffer = new byte[256];
            // получение зашифрованного симметричного ключа
            client.BeginReceive(buffer, 0, 256, 0, ReadKey2, client);
            KeysDone.WaitOne();
            // достать из сообщения зашифврованный симметричный ключ и вектор
            byte[] encryptedSymmetricKey = new byte[128], encryptedSymmetricIv = new byte[128];        
            Array.Copy(buffer, encryptedSymmetricKey, 128);
            Array.Copy(buffer, 128, encryptedSymmetricIv, 0, 128);
            // расшифровка симметричного ключа
            key.symmetricKey = rsa.Decrypt(encryptedSymmetricKey, false);
            key.symmetricIv = rsa.Decrypt(encryptedSymmetricIv, false);
            KeysDone.Set();
            return key;            
        }
        public class _keys
        {
            public byte[] symmetricKey, symmetricIv;
        }
        // асинронная отправка ключей
        private static void SendKey2(IAsyncResult ar)
        {
            var handler = (Socket)ar.AsyncState;
            handler.EndSend(ar);
            KeysDone.Set();
        }
        // асинхронное получение ключей
        private static void ReadKey2(IAsyncResult ar)
        {
            var client = (Socket)ar.AsyncState;
            client.EndReceive(ar);
            KeysDone.Set();
        }
        // асинхронное чтение сообщений с дополнительного сервера
        private static void ReadMessageServer(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;

            try
            {
                var bytesRead = state.WorkSocket.EndReceive(ar);
                // перешифровка сообщения симметричными ключами
                byte[] nb = state.Buffer;
                Array.Resize(ref nb, bytesRead);
                string str = DecryptStringFromBytes(nb, state.symmetricKey, state.symmetricIv);
                nb = EncryptStringToBytes(str, Rm.Key, Rm.IV);
                Console.WriteLine(Encoding.ASCII.GetString(
                    state.Buffer, 0, bytesRead));
                // отправка сообщения всем, кроме дополнительного сервера
                foreach (var cl in ClientSockets)
                {
                    if(cl != state.WorkSocket)
                    {
                        cl.BeginSend(nb, 0, bytesRead, 0, SendMessage, cl);
                    }                    
                }
                state.ReceiveDone.Set();
            }
            catch (Exception)
            {
                // удаленияе сокета доп. сервера из клиентов сервера
                ClientSockets.Remove(state.WorkSocket);
                Console.WriteLine("Socket disconnected {0}", state.WorkSocket.RemoteEndPoint);
                state.WorkSocket.Shutdown(SocketShutdown.Both);
                state.WorkSocket.Close();
            }
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            // сигнад дескриптора для потока продалжать работать
            AllDone.Set();
            // получить подключение клиента
            var listener = (Socket)ar.AsyncState;
            var handler = listener.EndAccept(ar);
            // добавить подключение в список клиентов сервера
            ClientSockets.Add(handler);
            // создать новый объект для сообщений
            var state = new StateObject(handler);
            // отправка симметричного ключа, через асимметричный
            Keys(state);

            Console.WriteLine("Socket connected {0}", state.WorkSocket.RemoteEndPoint);
            // получение входящих сообщений
            while (state.WorkSocket.Connected)
            {
                state.WorkSocket.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReadMessage, state);
                state.ReceiveDone.WaitOne();
            }
            
        }

        private static void ReadMessage(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;

            try
            {
                var bytesRead = state.WorkSocket.EndReceive(ar);

                Console.WriteLine(Encoding.ASCII.GetString(
                    state.Buffer, 0, bytesRead));
                // отправка сообщения всем клиентам сервера
                foreach (var cl in ClientSockets)
                {
                    cl.BeginSend(state.Buffer, 0, bytesRead, 0, SendMessage, cl);
                }
                state.ReceiveDone.Set();
            }
            catch (Exception)
            {
                // удаленияе сокета клиента из списка сервера
                ClientSockets.Remove(state.WorkSocket);
                Console.WriteLine("Socket disconnected {0}", state.WorkSocket.RemoteEndPoint);
                state.WorkSocket.Shutdown(SocketShutdown.Both);
                state.WorkSocket.Close();
            }
        }
        // асинхронная отправка сообщения
        private static void SendMessage(IAsyncResult ar)
        {
            var handler = (Socket)ar.AsyncState;
            handler.EndSend(ar);
        }

        private static void Keys(StateObject state)
        {
            // асимметричный ключ
            var rsa = new RSACryptoServiceProvider();
            var rsaKeyInfo = new RSAParameters();
            var buffer = new byte[131];
            // получение открытого асимметричного ключа
            state.WorkSocket.BeginReceive(buffer, 0, 131, 0, ReadKey1, state);
            state.KeysDone.WaitOne();
            // достать асимметричный люч и вектор
            rsaKeyInfo.Modulus = new byte[128];
            rsaKeyInfo.Exponent = new byte[3];
            Array.Copy(buffer, rsaKeyInfo.Modulus, 128);
            Array.Copy(buffer, 128, rsaKeyInfo.Exponent, 0, 3);
            // импортировать открытую часть
            rsa.ImportParameters(rsaKeyInfo);
            
            // зашифровать симметричный ключ           
            var simKeys = new byte[256];
            Array.Copy(rsa.Encrypt(Rm.Key, false), simKeys, 128);
            Array.Copy(rsa.Encrypt(Rm.IV, false), 0, simKeys, 128, 128);
            // отправить симметричный зашифрованный ключ
            state.WorkSocket.BeginSend(simKeys, 0, 256, 0, SendKey1, state);
            state.KeysDone.WaitOne();
        }
        // асинхронное чтение ключей
        private static void ReadKey1(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;
            state.WorkSocket.EndReceive(ar);
            state.KeysDone.Set();
        }
        // асинхронная отправка ключей
        private static void SendKey1(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;
            state.WorkSocket.EndSend(ar);
            state.KeysDone.Set();
        }
        //стартовая функция
        public static int Main()
        {
            StartListening();
            return 0;
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
