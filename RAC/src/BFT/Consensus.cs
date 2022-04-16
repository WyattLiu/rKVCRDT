using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using RAC;
using static RAC.Errors.Log;


namespace RAC.Consensus
{
    public enum ConsensusStatus
    {
        undecided = 0,
        pre_prepare = 1,
        prepare = 2,
        commit = 3,
        decided = 4,
        failed = 5
    }

    /// <summary>
    /// Class for an instance of consensus
    /// </summary>
    public class ConsensusInstance
    {

        // id for a certain round of decision
        public string cid { get; }
        // propoerser of this round of decision
        public int proposer { get; }
        // hash of the decided value
        public string value { get; }
        public byte[] digest { get; }
        public ConsensusStatus status = ConsensusStatus.undecided;

        public int numNodes { set; get; } = Global.cluster.numNodes;

        public int recievedPP { set; get; } = 0;
        public int recievedPrepare { set; get; } = 0;
        public int recievedValidPrepare { set; get; } = 0;
        public int recievedCommit { set; get; } = 0;
        public int recievedValidCommit {set; get; } = 0;
        public DateTime startime {set; get;}

        public ConsensusInstance(string cid, int proposer, string value, byte[] digest)
        {
            this.cid = cid;
            this.proposer = proposer;
            this.value = value;
            this.digest = digest;
            this.startime = DateTime.Now;
        }


    }


}