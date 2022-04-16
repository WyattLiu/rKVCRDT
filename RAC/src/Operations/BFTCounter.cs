using System;
using System.Collections.Generic;
using System.Linq;
using RAC.Consensus;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{

    /// <summary>
    /// Reversible counter
    /// Reverse functions:
    ///  Causal reverse
    /// /// </summary>
    public class BFTCounter : Operation<BFTCounterPayload>
    {

        public override string typecode { get ; set; } = "bftc";

        public BFTCounter(string uid, Parameters parameters) : base(uid, parameters)
        {
            try
            {
                this.history = Global.memoryManager.history[uid];
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                Global.memoryManager.history.Add(uid, new RAC.History.OpHistory(uid, this.Compensate));
                this.history = Global.memoryManager.history[uid];

            }
        }

        public override Responses GetValue()
        {   
            // 1. calculate initial value
            // 2. go to tombstone and retract everything
            Responses res;

            if (this.payload is null)
            {
                res = new Responses(Status.fail);
                res.AddResponse(Dest.client, "BFTCounter with id {0} cannot be found");
            } 
            else
            {
                int pos = payload.PVector.Sum();
                int neg = payload.NVector.Sum();
                int compensate = 0;
                DEBUG("actual value: " + (pos - neg).ToString() + " compensate: " + compensate);

                res = new Responses(Status.success);
                res.AddResponse(Dest.client, (pos - neg + compensate).ToString()); 
            }

            noSideEffect = true;
            return res;
        }

        public override Responses SetValue()
        {
            Responses res = new Responses(Status.success);
            BFTCounterPayload pl = new BFTCounterPayload(uid, (int)Config.numReplicas, (int)Config.replicaId);
            BFTCounterPayload oldstate = pl.CloneValues();

            int value = this.parameters.GetParam<int>(0);
            if (value >= 0)
                pl.PVector[pl.replicaid] = value;
            else
                pl.NVector[pl.replicaid] = -value;

            this.payload = pl;
            string opid = this.history.AddNewEntry(oldstate, this.payload, BFTCounterPayload.PayloadToStr);

            GenerateSyncRes(ref res, opid);
            res.AddResponse(Dest.client, opid); 
            
            return res;
        }

        public Responses Increment()
        {   
            BFTCounterPayload oldstate = this.payload.CloneValues();
            this.payload.PVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            string opid = this.history.AddNewEntry(oldstate, this.payload, BFTCounterPayload.PayloadToStr);

            Global.server.bftNode.consensusRequest(opid, "+" + this.parameters.GetParam<int>(0).ToString());

            Responses res = new Responses(Status.success);
            res.AddResponse(Dest.client, opid);
            GenerateSyncRes(ref res, opid);
            return res;

        }

        public Responses Decrement()
        {
            BFTCounterPayload oldstate = this.payload.CloneValues();
            this.payload.NVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            string opid = this.history.AddNewEntry(oldstate, this.payload, BFTCounterPayload.PayloadToStr);

            Global.server.bftNode.consensusRequest(opid, "-" + this.parameters.GetParam<int>(0).ToString());

            Responses res = new Responses(Status.success);
            res.AddResponse(Dest.client, opid); 
            GenerateSyncRes(ref res, opid);

            return res;

        }

        public override Responses Synchronization()
        {
            Responses res = new Responses(Status.success);

            List<int> otherP = this.parameters.GetParam<List<int>>(0);
            List<int> otherN = this.parameters.GetParam<List<int>>(1);
            
            if (this.payload is null)
            {
                BFTCounterPayload pl = new BFTCounterPayload(uid, (int)Config.numReplicas, (int)Config.replicaId);
                this.payload = pl;
            }

            if (otherP.Count != otherN.Count || otherP.Count != payload.PVector.Count)
            {   
                res = new Responses(Status.fail);
                LOG("Sync failed for item: " + this.payload.replicaid);
                return res;
            }

            for (int i = 0; i < otherP.Count; i++)
            {
                this.payload.PVector[i] = Math.Max(this.payload.PVector[i], otherP[i]);
                this.payload.NVector[i] = Math.Max(this.payload.NVector[i], otherN[i]);
            }
            
            DEBUG("Sync successful, new value for " + this.uid + " is " +  
                    (this.payload.PVector.Sum() - this.payload.NVector.Sum()));
            

            res = new Responses(Status.success);

            return res;
        }


        public Responses Reverse()
        {
            Responses res = new Responses(Status.success);
            string opid = this.parameters.GetParam<String>(0);
            string startime = this.history.log[opid].time;
            string curTime = this.history.curTime.ToString(); 
            string reverseop = this.history.AddNewEntry(startime, curTime, true);

            this.history.addTombstone(reverseop);
            
            res.AddResponse(Dest.client);
            return res;
        }

        private void GenerateSyncRes(ref Responses res, string newop)
        {
            Parameters syncPm = new Parameters(2);
            syncPm.AddParam(0, this.payload.PVector);
            syncPm.AddParam(1, this.payload.NVector);

            string broadcast = Parser.BuildCommand(this.typecode, "y", this.uid, syncPm);
            
            res.AddResponse(Dest.broadcast, broadcast, false);
        }

        public override void Compensate(string opid)
        {

            BFTCounterPayload rcafter;
            BFTCounterPayload rcbefore;

            history.GetEntry(opid, BFTCounterPayload.StrToPayload, out rcbefore, out rcafter, out _);

            int compensate = (rcbefore.PVector.Sum() - rcbefore.NVector.Sum()) - 
                        (rcafter.PVector.Sum() - rcafter.NVector.Sum());


            if (compensate != 0)
            {   
                var pChange = rcafter.PVector.Zip(rcbefore.PVector, (one, two) => one - two).ToList();
                var nChange = rcafter.NVector.Zip(rcbefore.NVector, (one, two) => one - two).ToList();
                    
                for (int i = 0; i < this.payload.PVector.Count; i++)
                {
                    this.payload.PVector[i] += nChange[i];
                    this.payload.NVector[i] += pChange[i];
                }
            }

            DEBUG("compensate: " + compensate + " applied on states for " + uid);

        }

    }
}

