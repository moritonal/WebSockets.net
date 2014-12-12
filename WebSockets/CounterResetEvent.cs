using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSockets
{
    public class CounterResetEvent
    {
        AutoResetEvent resetEvent = new AutoResetEvent(false);
        long value = 0;

        public CounterResetEvent()
        {

        }

        public void Increment()
        {
            Interlocked.Increment(ref value);
            this.resetEvent.Set();
        }

        public void Decrement()
        {
            while (Interlocked.Read(ref value) <= 0)
            {
                resetEvent.WaitOne();
            }

            Interlocked.Decrement(ref value);
        }

        public void Break()
        {
            value = 1;
            resetEvent.Set();
        }
    }
}
