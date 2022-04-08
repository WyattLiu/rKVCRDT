using System;
using RAC.Network;



namespace RAC
{
    class Program
    {
        //TODO: use proper versioning
        static string VERSION = "10";

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

            var handler0 = Global.server.HandleRequestAsync(Global.server.reqQueue);
            var handler1 = Global.server.SendResponseAsync(Global.server.respQueue);
            var handler2 = Global.server.HandleRequestAsync(Global.server.clusterReqQueue);
            var handler3 = Global.server.SendResponseAsync(Global.server.ClusterRespQueue);

            Global.server.Run();



            return 0;

        }

    }
}
