using System;
using RAC.Network;



namespace RAC
{
    class Program
    {
        static string VERSION = "16";

        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Please provide correct json cluster config file");
                return 1;
            }

            Console.WriteLine("Running rac version " + VERSION);

            string nodeconfigfile = args[0];

            Global.init(nodeconfigfile);


            var handler0 = Global.server.HandleRequestAsync(Global.server.clientReqQueue);
            var handler1 = Global.server.SendResponseAsync(Global.server.clientRespQueue);
            var handler2 = Global.server.HandleRequestAsync(Global.server.clusterReqQueue);
            var handler3 = Global.server.SendResponseAsync(Global.server.clusterRespQueue);

            Global.server.Run();



            return 0;

        }

    }
}
