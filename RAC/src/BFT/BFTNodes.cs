using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using RAC;
using static RAC.Errors.Log;
using System.Net;

namespace RAC.Consensus
{

    public class BFTNodes
    {
        public int id { set; get; }

        // TODO: shit should be a list?
        public Dictionary<string, ConsensusInstance> currentConsensus { set; get; }

        public BFTNodes(int id)
        {
            this.id = id;
            this.currentConsensus = new Dictionary<string, ConsensusInstance>();
        }

        public string sign(string value)
        {
            // TODO:
            return value;
        }

        public void sendMsg(ConsensusMessage msg, int targetNode)
        {

        }

        public void broadcast(ConsensusMessage msg)
        {

        }

        public void startConsensus(string value)
        {
            MD5 digest = MD5.Create(value);
            string sign = this.sign(digest.ToString());

            ConsensusInstance newConsensus = new ConsensusInstance("0", this.id, value, digest);

            newConsensus.status = ConsensusStatus.prepare;

            this.currentConsensus[newConsensus.cid] = newConsensus;

            ConsensusMessage ppMsg = new ConsensusMessage(newConsensus.cid, ConsensusMessageType.pre_prepare, this.id, digest, sign);
            ppMsg.value = value;

            this.broadcast(ppMsg);
            DEBUG("broadcasting new pre-prepare for " + value);
        }

        public void parseConsensusMessage(string msgStr)
        {
            ConsensusMessage msg = ConsensusMessage.deserialize(msgStr);

            switch (msg.type)
            {
                case ConsensusMessageType.pre_prepare:
                    this.preprepareReceived(msg);
                    break;
                case ConsensusMessageType.prepare:
                    this.prepareReceived(msg);
                    break;
                case ConsensusMessageType.commit:
                    this.acceptReceived(msg);
                    break;

            }
        }


        public void preprepareReceived(ConsensusMessage ppMsg)
        {

            DEBUG("Recieve prepare msg");


            if (!ppMsg.digest.Equals(MD5.Create(ppMsg.value)))
            {
                LOG("ppMsg for " + ppMsg.value + " failed because of digest mismatch");
                return;
            }

            if (this.sign(ppMsg.digest.ToString()) != ppMsg.sign)
            {
                LOG("ppMsg for " + ppMsg.value + " failed because of incorrect signiture");
                return;
            }

            ConsensusInstance newConsensus = new ConsensusInstance(ppMsg.cid, ppMsg.sender, ppMsg.value, ppMsg.digest);
            newConsensus.status = ConsensusStatus.pre_prepare;

            this.currentConsensus[newConsensus.cid] = newConsensus;

            string sign = this.sign(newConsensus.digest.ToString());
            ConsensusMessage prepareMsg = new ConsensusMessage(newConsensus.cid, ConsensusMessageType.prepare, this.id, newConsensus.digest, sign);

            this.broadcast(prepareMsg);
            newConsensus.status = ConsensusStatus.prepare;
        }


        private void prepareReceived(ConsensusMessage prepMsg)
        {

            ConsensusInstance ongoingConsensus = this.currentConsensus[prepMsg.cid];

            // get 2f nodes
            ongoingConsensus.recievedPP++;
            int f = 0;
            if (ongoingConsensus.proposer == this.id)
            {
                f = ongoingConsensus.numNodes / 3 * 2;
            }
            else
            {
                f = (ongoingConsensus.numNodes / 3 * 2) - 1;
            }

            if (ongoingConsensus.status != ConsensusStatus.prepare)
                return;

            if (ongoingConsensus is null)
            {
                // TODO: ?
                LOG("prepMsg for " + prepMsg.cid + " failed because of unvalid current consensus");
                return;
            }

            if (prepMsg.digest != ongoingConsensus.digest)
            {
                LOG("prepMsg for " + prepMsg.cid + " failed because of incorrect digset");
                return;
            }

            // TODO: check other stuffs

            ongoingConsensus.recievedValidPrepare++;

            if (ongoingConsensus.recievedValidPrepare >= f)
            {
                string sign = this.sign(ongoingConsensus.digest.ToString());

                ConsensusMessage commitMsg = new ConsensusMessage(ongoingConsensus.cid, ConsensusMessageType.commit, this.id, ongoingConsensus.digest, sign);

                this.broadcast(commitMsg);

                ongoingConsensus.status = ConsensusStatus.commit;
            }

            // this.currentConsensus[ongoingConsensus.cid] = ongoingConsensus; not needed cuz dic store by reference


        }

        private void acceptReceived(ConsensusMessage commitMsg)
        {

            ConsensusInstance ongoingConsensus = this.currentConsensus[commitMsg.cid];

            ongoingConsensus.recievedCommit++;
            if (ongoingConsensus.status != ConsensusStatus.commit)
                return;

            if (commitMsg.digest != ongoingConsensus.digest)
            {
                LOG("commitMsg for " + commitMsg.cid + " failed because of incorrect digset");
                return;
            }

            ongoingConsensus.recievedValidCommit++;

            if (ongoingConsensus.recievedValidCommit >= ongoingConsensus.numNodes / 3 * 2)
            {
                string sign = this.sign(ongoingConsensus.digest.ToString());

                ConsensusMessage compelteMsg = new ConsensusMessage(ongoingConsensus.cid, ConsensusMessageType.complete, this.id, ongoingConsensus.digest, sign);

                this.sendMsg(compelteMsg, ongoingConsensus.proposer);
                ongoingConsensus.status = ConsensusStatus.decided;
                LOG("Consensus " + ongoingConsensus.cid + " decided");
            }


        }


    }


}