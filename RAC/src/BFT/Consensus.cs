using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using RAC;
using static RAC.Errors.Log;

// taken inspritation from BFTSmart
// https://github.com/bft-smart/library
namespace RAC.Consensus
{
    /// <summary>
    /// Class for a single decision.
    /// </summary>
    public class Decision
    {


        public Decision()
        {

        }

    }

    public enum ConsensusStatus
    {
        undecided = 0,
        inProgress = 1,
        decided = 2,
        failed = 3
    }

    /// <summary>
    /// Class for an instance of consensus
    /// </summary>
    public class ConsensusInstance
    {

        // id for a certain round of decision
        public int cid { set; get; }
        // propoerser of this round of decision
        public Proposer proposer { set; get; }
        // hash of the decided value
        public MD5 value { set; get; }
        public ConsensusStatus status = ConsensusStatus.undecided;

        public void doConsensus()
        {
            
        }



    }


}