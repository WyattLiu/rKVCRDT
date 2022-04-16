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
        request,
        pre_prepare,
        prepare,
        commit,
        complete

    }

    public class ConsensusMessage
    {

        public string cid { set; get; }
        public int sender { set; get; }

        public string value { set; get; }

        public byte[] digest { set; get; }
        public ConsensusMessageType type { set; get; }

        public string sign { set; get; }

        public int sequenceNum {set; get;}

        public ConsensusMessage(string cid, ConsensusMessageType type, int sender, byte[] digest, string sign, int sequenceNum=-1)
        {
            this.cid = cid;
            this.type = type;
            this.sender = sender;
            this.digest = digest;
            this.sign = sign;
            this.sequenceNum = sequenceNum;
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

