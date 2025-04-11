using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace srqc.domain
{
    public class RotateLock
    {
        //
        private readonly EventWaitHandle RotateLockHandle = new(true, EventResetMode.AutoReset);

        public void AcquireLock()
        {
            RotateLockHandle.WaitOne();
        }

        public void ReleaseLock() { 
            RotateLockHandle.Set();
        }
    }
}
