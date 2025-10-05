using RabbitMQ.Client;
using System.Collections.Generic;

namespace MessageChannel.RabbitMQ
{
    public sealed class RMQConnectionManager
    {
        private static volatile RMQConnectionManager _instance = null;
        private static readonly object _syncRoot = new object();
        private static readonly object _collectionLock = new object();

        private readonly Dictionary<string, IConnection> _connectionRegistry = new Dictionary<string, IConnection>();
        private readonly Dictionary<string, IChannel> _channelRegistry = new Dictionary<string, IChannel>();

        private RMQConnectionManager() { }

        public static RMQConnectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        _instance ??= new RMQConnectionManager();
                    }
                }

                return _instance;
            }
        }

        public IConnection GetConnection(RMQSettings settings)
        {
            IConnection ret = null;

            string connectionRegistryKey = $"{settings.HostName}:{settings.Port}";

            if (_connectionRegistry.ContainsKey(connectionRegistryKey))
            {
                return _connectionRegistry[connectionRegistryKey];
            }

            // lock and double check that another thread did not create.
            lock (_collectionLock)
            {
                if (_connectionRegistry.ContainsKey(connectionRegistryKey))
                {
                    return _connectionRegistry[connectionRegistryKey];
                }

                var factory = new ConnectionFactory()
                {
                    HostName = settings.HostName,
                    Port = settings.Port,
                    UserName = settings.UserName,
                    Password = settings.Password
                };

                ret = factory.CreateConnectionAsync().Result;
                _connectionRegistry.Add(connectionRegistryKey, ret);
            }

            return ret;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="name"></param>
        /// <param name="channel"></param>
        public void RegisterChannel(string scope, string name, IChannel channel)
        {
            if (IsChannelRegistered(scope, name) == false)
            {
                lock (_collectionLock)
                {
                    if (IsChannelRegistered(scope, name) == false)
                    {
                        _channelRegistry.Add(GetChannelKey(scope, name), channel);
                    }
                }
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsChannelRegistered(string scope, string name)
        {
            return _channelRegistry.ContainsKey(GetChannelKey(scope, name));
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public IChannel GetChannelFromRegistry(string scope, string name)
        {
            if (IsChannelRegistered(scope, name))
            {
                return _channelRegistry[GetChannelKey(scope, name)];
            }

            return null;
        }

        private string GetChannelKey(string scope, string name)
        {
            return $"{scope}:{name}";
        }
    }
}
