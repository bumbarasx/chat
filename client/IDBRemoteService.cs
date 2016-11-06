using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbserver
{
    public interface IDBRemoteService
    {
        int GetCheck(string nick, string pass);

        bool check();

        int Register(string nick, string pass);
    }
}
