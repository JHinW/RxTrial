using Messaging.Internal;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging
{
    class Program
    {
        static void Main(string[] args)
        {
            var ctx = new MessageContext();
            Task.Run(async () =>
            {
                while (true)
                {
                    await ctx.AppendMessageAsync(new Message
                    {
                        Id = Guid.NewGuid().ToString()
                    });
                    await Task.Delay(100);
                }
                

            }).Wait();


            Thread.Sleep(Timeout.Infinite);

            Console.WriteLine("Hello World!");
        }



    }
}
