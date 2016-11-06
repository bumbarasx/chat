using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbserver
{
    public class DBRemoteService : MarshalByRefObject, IDBRemoteService
    {
        /// <summary>
        ///  аутентификация
        /// </summary>
        public int GetCheck(string _nick, string _pass)
        {
            int q = 1;
            if(_nick != "test")
            {
                // строка подключения к базе данных
                string sqlConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\ТРРП\04\dbserver\server.mdf;Integrated Security=True";

                using (SqlConnection connection = new SqlConnection(sqlConnectionString))
                {
                    connection.Open();
                    // sql запрос
                    using (SqlCommand command = new SqlCommand("SELECT pass FROM users WHERE nick ='" + _nick + "'", connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // проверка пароля
                            if (_pass == reader.GetString(0)) q = 2;
                        }
                    }
                }
            }            
            return q;
        }
        /// <summary>
        ///  для проверки связи с сервером
        /// </summary>
        public bool check()
        {
            return true;
        }
        /// <summary>
        ///  регистрация
        /// </summary>
        public int Register(string _nick, string _pass)
        {
            int q = 3;
            string sqlConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\ТРРП\04\dbserver\server.mdf;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("INSERT INTO users(nick, pass) VALUES('"+_nick+"', '"+_pass+"')", connection))
                {
                    try
                    {
                        // выполнение sql команды
                        command.ExecuteNonQuery();
                        q = 2;
                    }
                    catch
                    {
                        q = 3;
                    }
                }
            }
            return q;
        }
    }
}
