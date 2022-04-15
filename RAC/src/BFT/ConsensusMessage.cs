using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;

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

        public string cid { set; get; }
        public int sender { set; get; }

        public string value { set; get; }

        public byte[] digest { set; get; }
        public ConsensusMessageType type { set; get; }

        public string sign { set; get; }

        public ConsensusMessage(string cid, ConsensusMessageType type, int sender, byte[] digest, string sign)
        {
            this.cid = cid;
            this.type = type;
            this.sender = sender;
            this.digest = digest;
            this.sign = sign;
        }

        public string serialize()
        {
            return JsonConvert.SerializeObject(this);
        }


        public static ConsensusMessage deserialize(string jsonMsg)
        {
            return JsonConvert.DeserializeObject<ConsensusMessage>(jsonMsg);
        }


        public override string ToString()
        {
            return this.serialize();
        }

    }

}

