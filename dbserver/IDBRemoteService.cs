using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbserver
{
    public interface IDBRemoteService
    {
        // аутентификация
        int GetCheck(string nick, string pass);
        // для проверки связи
        bool check();
        // регистрация
        int Register(string nick, string pass);
    }
}
