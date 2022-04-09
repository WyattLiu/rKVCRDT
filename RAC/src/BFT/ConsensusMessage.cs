using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using RAC;
using static RAC.Errors.Log;


namespace RAC.Consensus
{
    public enum ConsensusMessageType
    {
        Propose = 0,
        Write = 1,
        Accept = 2
        
    }

    public class ConsensusMessage
    {

        public int cid {set; get;}

        public MD5 value { private set; get; }
        public ConsensusMessageType type {set;get;}

        public string proof { set; get; }

        public ConsensusMessage(MD5 value)
        {
            this.value = value;
        }
        
    }

}

