using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using RAC;
using static RAC.Errors.Log;


namespace RAC.Consensus
{
    public enum ConsensusMessageType
    {
        pre_prepare = 0,
        prepare = 1,
        commit = 2,
        complete = 3

    }

    public class ConsensusMessage
    {

        public int cid { set; get; }
        public int sender { set; get; }

        public string value { set; get; }

        public MD5 digest { set; get; }
        public ConsensusMessageType type { set; get; }

        public string sign { set; get; }

        public ConsensusMessage(ConsensusMessageType type, int sender)
        {
            this.type = type;
            this.sender = sender;
        }

    }

}

