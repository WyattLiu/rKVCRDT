using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using RAC;
using static RAC.Errors.Log;
using System.Net;

namespace RAC.Consensus
{

    public class Proposer
    {


        public Proposer(MD5 value)
        {
            ConsensusMessage msg = new ConsensusMessage(value);

            


        }

    }

    public class Accepter
    {

        public void processMessage(ConsensusMessage msg)
        {

            switch (msg.type)
            {
                case ConsensusMessageType.Propose:
                    proposeReceived(msg);
                    break;
                case ConsensusMessageType.Write:
                    writeReceived(msg);
                    break;
                case ConsensusMessageType.Accept:
                    acceptReceived(msg);
                    break;

            }


        }

        public void proposeReceived(ConsensusMessage msg)
        {

        }


        private void writeReceived(ConsensusMessage msg)
        {

        }

        private void acceptReceived(ConsensusMessage msg)
        {

        }


    }


}

    
}