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
        public ConsensusInstance currentConsensus { set; get; }

        public BFTNodes()
        {

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

    }


    public class Proposer : BFTNodes
    {


        public Proposer()
        {

        }

        public void startConsensus(string value)
        {
            MD5 digest = MD5.Create(value);
            string sign = this.sign(digest.ToString());

            this.currentConsensus = new ConsensusInstance();
            this.currentConsensus.cid = 0; // TODO:
            this.currentConsensus.value = value;
            this.currentConsensus.digest = digest;
            this.currentConsensus.proposer = this.id;
            this.currentConsensus.status = ConsensusStatus.prepare;

            ConsensusMessage ppMsg = new ConsensusMessage(ConsensusMessageType.pre_prepare, this.id);
            ppMsg.value = value;
            ppMsg.digest = digest;
            ppMsg.sign = sign;
            ppMsg.cid = this.currentConsensus.cid;

            this.broadcast(ppMsg);
            DEBUG("broadcasting new pre-prepare for " + value);
        }

    }

    public class Accepter : BFTNodes
    {
        public Accepter()
        {

        }

        public void processMessage(ConsensusMessage msg)
        {

            switch (msg.type)
            {
                case ConsensusMessageType.pre_prepare:
                    preprepareReceived(msg);
                    break;
                case ConsensusMessageType.prepare:
                    prepareReceived(msg);
                    break;
                case ConsensusMessageType.commit:
                    acceptReceived(msg);
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

            this.currentConsensus = new ConsensusInstance();
            this.currentConsensus.proposer = ppMsg.sender;
            this.currentConsensus.cid = ppMsg.cid;
            this.currentConsensus.status = ConsensusStatus.pre_prepare;
            this.currentConsensus.value = ppMsg.value;
            this.currentConsensus.digest = ppMsg.digest;


            ConsensusMessage prepareMsg = new ConsensusMessage(ConsensusMessageType.prepare, this.id);

            prepareMsg.digest = ppMsg.digest;
            prepareMsg.cid = ppMsg.cid;
            prepareMsg.sign = this.sign(ppMsg.digest.ToString());
            this.broadcast(prepareMsg);
            this.currentConsensus.status = ConsensusStatus.prepare;
        }


        private void prepareReceived(ConsensusMessage prepMsg)
        {
            // get 2f nodes
            this.currentConsensus.recievedPP++;
            int f = 0;
            if (this.currentConsensus.proposer == this.id)
            {
                f = this.currentConsensus.numNodes / 3 * 2;
            }
            else
            {
                f = (this.currentConsensus.numNodes / 3 * 2) - 1;
            }

            if (this.currentConsensus.status != ConsensusStatus.prepare)
                return;

            if (this.currentConsensus is null)
            {
                // TODO: ?
                LOG("prepMsg for " + prepMsg.cid + " failed because of unvalid current consensus");
                return;
            }

            if (prepMsg.digest != currentConsensus.digest)
            {
                LOG("prepMsg for " + prepMsg.cid + " failed because of incorrect digset");
                return;
            }

            // TODO: check other stuffs



            this.currentConsensus.recievedValidPrepare++;



            if (this.currentConsensus.recievedValidPrepare >= f)
            {
                ConsensusMessage commitMsg = new ConsensusMessage(ConsensusMessageType.commit, this.id);

                commitMsg.digest = prepMsg.digest;
                commitMsg.cid = prepMsg.cid;
                commitMsg.sign = this.sign(prepMsg.digest.ToString());

                this.broadcast(commitMsg);

                this.currentConsensus.status = ConsensusStatus.commit;
            }


        }

        private void acceptReceived(ConsensusMessage commitMsg)
        {
            this.currentConsensus.recievedCommit++;
            if (this.currentConsensus.status != ConsensusStatus.commit)
                return;

            if (commitMsg.digest != currentConsensus.digest)
            {
                LOG("commitMsg for " + commitMsg.cid + " failed because of incorrect digset");
                return;
            }

            this.currentConsensus.recievedValidCommit++;

            if (this.currentConsensus.recievedValidCommit >= this.currentConsensus.numNodes / 3 * 2)
            {
                ConsensusMessage completeMsg = new ConsensusMessage(ConsensusMessageType.complete, this.id);
                completeMsg.digest = commitMsg.digest;
                completeMsg.cid = commitMsg.cid;

                this.sendMsg(commitMsg, this.currentConsensus.proposer);

            }


        }


    }


}

    
