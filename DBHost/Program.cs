using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using dbserver;

namespace DBHost
{
    class Program
    {
        static void Main(string[] args)
        {
            // tcp канал
            var channel = new TcpChannel(51495);
            // регистрация канала
            ChannelServices.RegisterChannel(channel, true);
            // регистрация сервисов
            var service = new dbserver.DBRemoteService();
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(dbserver.DBRemoteService),
                "GetCheck", WellKnownObjectMode.SingleCall);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(dbserver.DBRemoteService),
                "check", WellKnownObjectMode.SingleCall);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(dbserver.DBRemoteService),
                "Register", WellKnownObjectMode.SingleCall);

            Console.WriteLine("Service started");
            Console.ReadLine();
        }
    }
}
