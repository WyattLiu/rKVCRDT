using System;
using System.Collections.Generic;
using RAC.Payloads;
using RAC.Operations;
using RAC.Consensus;
using RAC.History;
using Newtonsoft.Json;
using static RAC.Errors.Log;
using System.Linq;


/// <summary>
/// This is a temporary solution 
/// </summary>
namespace RAC.Consensus
{
    public delegate string PayloadToStrDelegate(Payload pl);
    public delegate T StringToPayloadDelegate<T>(string str);

    public class StateHisotryEntry
    {
        public int nodeid;
        public string opid;
        public string before;
        public string after;
        public string time;
        public HashSet<string> related;
        // use to mark if this op is a reverse op
        public bool isrev = false;

        // graph pointers
        public List<String> aft;
        // prev used for sync'd ops to link
        public List<String> prev;

        // consensus
        public ConsensusInstance consensus;

        public StateHisotryEntry(string uid, string opid, string before, string after, string time, bool isrev)
        {
            this.nodeid = Global.selfNode.nodeid;
            this.opid = opid;
            this.before = before;
            this.after = after;
            this.time = time;
            this.related = new HashSet<string>();
            this.aft = new List<string>();
            this.prev = new List<string>();
            this.isrev = isrev;
        }
    }


    // history of each object
    public class BFTHistory : RAC.History.OpHistory
    {

        List<StateHisotryEntry> undecided {set;get;}

        public BFTHistory(string uid, CompensateMethod compensate) : base(uid, compensate)
        {
        }
    }
}