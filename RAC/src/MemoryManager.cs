using System.Collections.Generic;

using RAC.Payloads;
using RAC.History;
using RAC.Errors;


namespace RAC
{
    public class MemoryManager
    {

        private Dictionary<string, Payload> storage;
        // TODO: make history private
        public Dictionary<string, OpHistory> history;

        public MemoryManager()
        {
            storage = new Dictionary<string, Payload>();
            history = new Dictionary<string, OpHistory>();
        }

        public void StorePayload(string uid, Payload payload)
        {
            storage.TryAdd(uid, payload);
        }

        public Payload GetPayload(string uid)
        {
            if (storage.TryGetValue(uid, out var pl))
                return pl;
            else
                return null;


        }
    }
}