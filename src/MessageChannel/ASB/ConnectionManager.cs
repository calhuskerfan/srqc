using System.Collections.Generic;
using Azure.Messaging.ServiceBus;

namespace MessageChannel.ASB
{
    public sealed class ConnectionManager
    {
        private static volatile ConnectionManager _instance = null;
        private static object _syncRoot = new object();
        private static object _collectionLock = new object();

        private ConnectionManager() { }

        private Dictionary<string, ServiceBusClient> _connectionRegistry = new Dictionary<string, ServiceBusClient>();

        public static ConnectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        _instance ??= new ConnectionManager();
                    }
                }

                return _instance;
            }
        }

        public ServiceBusClient GetServiceBusClient(string connectionString, ServiceBusClientOptions sbcOptions)
        {
            ServiceBusClient ret = null;

            string key = $"{connectionString}:{sbcOptions.GetHashCode()}";

            if (_connectionRegistry.ContainsKey(key))
            {
                ret = _connectionRegistry[key];
            }
            else
            {
                lock (_collectionLock)
                {
                    if (_connectionRegistry.ContainsKey(key))
                    {
                        ret = _connectionRegistry[key];
                    }
                    else
                    {
                        ServiceBusClient sbc = new ServiceBusClient(connectionString,sbcOptions);
                        _connectionRegistry.Add(key, sbc);
                        ret = sbc;
                    }
                }
            }

            return ret;
        }
    }
}
