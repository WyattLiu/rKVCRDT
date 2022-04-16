using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks.Dataflow;

using RAC;
using static RAC.Errors.Log;
using System.Net;
using RAC.Network;
using System.Text;
using System.Linq;

namespace RAC.Consensus
{

    public class BFTNodes
    {
        private const int TIMEOUT = 200;

        public int id { set; get; }
        public int leader { set; get; }

        public int sequenceNum { set; get; } = 0;


        public Dictionary<string, ConsensusInstance> msgPool { set; get; }

        public BFTNodes(int id)
        {
            this.id = id;
            this.msgPool = new Dictionary<string, ConsensusInstance>();
            this.leader = 0;
        }

        public string sign(string value)
        {
            // TODO:
            return value;
        }

        public MessagePacket sendMsg(ConsensusMessage msg, int targetNode)
        {
            MessagePacket packet = new MessagePacket(msg.serialize(), Dest.server, targetNode);
            packet.msgSrc = MsgSrc.bftnode;
            return packet;

        }

        public MessagePacket broadcast(ConsensusMessage msg)
        {
            MessagePacket packet = new MessagePacket(msg.serialize(), Dest.broadcast);
            packet.msgSrc = MsgSrc.bftnode;
            return packet;
        }

        public void checkTimeout()
        {
            DateTime curtime = DateTime.Now;
            foreach (var c in this.msgPool)
            {
                if ((curtime - c.Value.startime).TotalMilliseconds > TIMEOUT)
                {
                    c.Value.status = ConsensusStatus.failed;
                }
            }
        }

        public void consensusRequest(string cid, string value)
        {
            if (this.id == this.leader)
            {
                startConsensus(cid, value);
            }
            else
            {
                ConsensusMessage reqMsg = new ConsensusMessage(cid, ConsensusMessageType.request, this.id, null, null);
                reqMsg.value = value;
                Global.server.respQueue.Post(sendMsg(reqMsg, this.leader));
            }

        }

        public void startConsensus(string cid, string value)
        {
            DEBUG("starting consensus for " + value);
            MD5 md5 = MD5.Create();
            byte[] digest = md5.ComputeHash(Encoding.Unicode.GetBytes(value));
            string sign = this.sign(digest.ToString());
            this.sequenceNum++;

            ConsensusInstance newConsensus = new ConsensusInstance(cid, this.id, value, digest, this.sequenceNum);

            newConsensus.status = ConsensusStatus.prepare;

            this.msgPool[newConsensus.cid] = newConsensus;

            ConsensusMessage ppMsg = new ConsensusMessage(newConsensus.cid, ConsensusMessageType.pre_prepare, this.id, digest, sign);
            ppMsg.value = value;

            DEBUG("broadcasting new pre-prepare for " + newConsensus.cid);

            MessagePacket msg = this.broadcast(ppMsg);
            Global.server.respQueue.Post(msg);
            
        }

        public void parseConsensusMessage(string msgStr)
        {
            ConsensusMessage msg = ConsensusMessage.deserialize(msgStr);
            MessagePacket res = null;
            DEBUG("Recieved new consensus request " + msg.ToString());
            switch (msg.type)
            {
                case ConsensusMessageType.request:
                    res = this.requestRecieved(msg);
                    break;
                case ConsensusMessageType.pre_prepare:
                    res = this.preprepareReceived(msg);
                    break;
                case ConsensusMessageType.prepare:
                    res = this.prepareReceived(msg);
                    break;
                case ConsensusMessageType.commit:
                    res = this.acceptReceived(msg);
                    break;
            }

            if (res is not null) Global.server.respQueue.Post(res);
        }

        public MessagePacket requestRecieved(ConsensusMessage reqMsg)
        {
            startConsensus(reqMsg.cid, reqMsg.value);
            return null;
        }


        public MessagePacket preprepareReceived(ConsensusMessage ppMsg)
        {


            ConsensusInstance newConsensus = new ConsensusInstance(ppMsg.cid, ppMsg.sender, ppMsg.value, ppMsg.digest, ppMsg.sequenceNum);
            newConsensus.status = ConsensusStatus.pre_prepare;

            if (!ppMsg.digest.SequenceEqual(MD5.Create().ComputeHash(Encoding.Unicode.GetBytes(ppMsg.value))))
            {
                LOG("ppMsg for " + newConsensus.cid + " failed because of digest mismatch");
                newConsensus.status = ConsensusStatus.failed;
                return null;
            }

            if (this.sign(ppMsg.digest.ToString()) != ppMsg.sign)
            {
                LOG("ppMsg for " + newConsensus.cid + " failed because of incorrect signiture");
                newConsensus.status = ConsensusStatus.failed;
                return null;
            }

            this.msgPool[newConsensus.cid] = newConsensus;

            string sign = this.sign(newConsensus.digest.ToString());
            ConsensusMessage prepareMsg = new ConsensusMessage(newConsensus.cid, ConsensusMessageType.prepare, this.id, newConsensus.digest, sign);

            newConsensus.status = ConsensusStatus.prepare;

            DEBUG("Broadcast prepare msg for " + newConsensus.cid);
            return this.broadcast(prepareMsg);
        }


        private MessagePacket prepareReceived(ConsensusMessage prepMsg)
        {

            ConsensusInstance ongoingConsensus = this.msgPool[prepMsg.cid];

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
                return null;

            if (ongoingConsensus is null)
            {
                // TODO: ?
                LOG("prepMsg for " + prepMsg.cid + " failed because of unvalid current consensus");
                return null;
            }

            if (!prepMsg.digest.SequenceEqual(ongoingConsensus.digest))
            {
                LOG("prepMsg for " + prepMsg.cid + " failed because of incorrect digest");
                return null;
            }

            // TODO: check other stuffs

            ongoingConsensus.recievedValidPrepare++;

            if (ongoingConsensus.recievedValidPrepare >= f)
            {
                string sign = this.sign(ongoingConsensus.digest.ToString());

                ConsensusMessage commitMsg = new ConsensusMessage(ongoingConsensus.cid, ConsensusMessageType.commit, this.id, ongoingConsensus.digest, sign);


                ongoingConsensus.status = ConsensusStatus.commit;
                DEBUG("Broadcast commit msg for " + ongoingConsensus.cid);
                return this.broadcast(commitMsg);
            } 


            // this.currentConsensus[ongoingConsensus.cid] = ongoingConsensus; not needed cuz dic store by reference
            return null;

        }

        private MessagePacket acceptReceived(ConsensusMessage commitMsg)
        {

            ConsensusInstance ongoingConsensus = this.msgPool[commitMsg.cid];

            ongoingConsensus.recievedCommit++;
            if (ongoingConsensus.status != ConsensusStatus.commit)
                return null;

            if (!commitMsg.digest.SequenceEqual(ongoingConsensus.digest))
            {            
                LOG("commitMsg for " + commitMsg.cid + " incorrect because of incorrect digset");
                return null;
            }

            ongoingConsensus.recievedValidCommit++;

            if (ongoingConsensus.recievedValidCommit >= ongoingConsensus.numNodes / 3 * 2)
            {
                string sign = this.sign(ongoingConsensus.digest.ToString());

                ConsensusMessage compelteMsg = new ConsensusMessage(ongoingConsensus.cid, ConsensusMessageType.complete, this.id, ongoingConsensus.digest, sign);

                
                ongoingConsensus.status = ConsensusStatus.decided;
                DEBUG("Consensus " + ongoingConsensus.cid + " decided");
                return this.sendMsg(compelteMsg, ongoingConsensus.proposer);
            }

            return null;

        }


    }


}