using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace HostingExtensions
{
    public static class TransformerFactoryUtilities
    {
        /// <summary>
        /// GetTransformerFactory.  This is not the greatest way to do this,
        /// but it helps demonstrate how
        /// the transformer is configured outside of the queue processing.
        /// 
        /// Come back to this, there are some Type.GetType(type) issues here to work through with assembly resolution
        /// 
        /// </summary>
        public static Type GetTransformerFactory(IServiceProvider serviceProvider, Type defaultType)
        {
            IConfiguration? configuration = serviceProvider?.GetService<IConfiguration>();

            if (configuration == null)
            {
                return defaultType;
            }

            var type = configuration["ConduitConfig:TransformerFactoryType"];

            if (type == null)
            {
                return defaultType;
            }

#pragma warning disable CS8603
            return Type.GetType(type);
#pragma warning restore CS8603
        }
    }
}
