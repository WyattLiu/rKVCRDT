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
        public int cid { set; get; }
        // propoerser of this round of decision
        public int proposer { set; get; }
        // hash of the decided value
        public string value { set; get; }
        public MD5 digest { set; get; }
        public ConsensusStatus status = ConsensusStatus.undecided;

        public int numNodes { set; get; }

        public int recievedPP { set; get; } = 0;
        public int recievedPrepare { set; get; } = 0;
        public int recievedValidPrepare { set; get; } = 0;
        public int recievedCommit { set; get; } = 0;
        public int recievedValidCommit {set; get; } = 0;

    }


}