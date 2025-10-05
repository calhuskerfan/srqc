using System;

namespace MessageChannel
{
    public class ConsumerEventArgs : EventArgs
    {
        public string[] ConsumerTags
        {
            get;
            private set;
        }
  
        public ConsumerEventArgs(string[] consumerTags)
        {
            ConsumerTags = consumerTags;
        }
    }
}
