using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace URLServerManagerModern.Utilities
{
    public class TaskQueue
    {
        SemaphoreSlim s;
        public TaskQueue()
        {
            s = new SemaphoreSlim(1);
        }

        public async Task<T> Enqueue<T>(Func<Task<T>> task)
        {
            await s.WaitAsync();
            try
            {
                return await task();
            }
            finally
            {
                s.Release();
            }
        }

        public async Task Enqueue(Func<Task> taskGenerator)
        {
            await s.WaitAsync();
            try
            {
                await taskGenerator();
            }
            finally
            {
                s.Release();
            }
        }
    }
}
