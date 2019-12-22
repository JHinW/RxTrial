using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;

namespace Messaging.Internal
{
    /// <summary>
    /// http://rxwiki.wikidot.com/101samples#toc1
    /// </summary>
    internal class MessageContext: IDisposable
    {
        private readonly ConcurrentDictionary<long, Message> _dic = new ConcurrentDictionary<long, Message>();


        private readonly ConcurrentDictionary<string, long> _buffer = new ConcurrentDictionary<string, long>();


        // private readonly IList<Committer> _committers = new List<Committer>();

        private event EventHandler<MessageEvent> _genericEvent;


        private readonly IObservable<EventPattern<MessageEvent>> _eventAsObservable;

        private readonly IDisposable _disposable = null;

        public MessageContext()
        {
            _eventAsObservable = Observable.FromEventPattern<MessageEvent>(
                ev => _genericEvent += ev,
                ev => _genericEvent -= ev);


           var dis = _eventAsObservable
                .Buffer(TimeSpan.FromMilliseconds(200), 10)
                .SkipWhile(evs => evs.Count() == 0)
                .SubscribeOn(Scheduler.Default)
                .Do(evs =>
            {
                Console.WriteLine("Batch Sum" + evs.Count());

            })
            .SelectMany(async evs  =>
            {
                await Task.Delay(1000* 10);
                return evs;
            })
            .Do(evs =>
            {
                Console.WriteLine("Batch Done!" + evs.Count());
            })
            .Subscribe();

        }


        public async Task<Message> AppendMessageAsync(Message message)
        {

            _genericEvent?.Invoke(null, new MessageEvent
            {
                Message = message
            });

            await Task.Delay(0);

            return message;


        }

        public void Dispose()
        {
            _disposable.Dispose();
        }


        private void Init()
        {
            var count = _buffer.GetOrAdd<long>(Committer.CommitterCount, (key, arg) =>
            {
                return arg;
            }, 1);

            for(var i =0; i< count; i++)
            {
                var committer = Committer.Create(i);

                InitCommitter(committer);
               // _committers.Add(committer);
            }


        }


        private void InitCommitter(Committer committer)
        {
            _buffer.AddOrUpdate<long>(committer.StartIndexKey, (key, arg) =>
            {
                return arg;
            },
            (key, valye, arg) =>
            {
                return arg;
            }

            , -1);


            _buffer.AddOrUpdate<long>(committer.EndIndexKey, (key, arg) =>
            {
                return arg;
            },
            (key, valye, arg) =>
            {
                return arg;
            }

            , -1);

        }


        private void InitListener()
        {
            IObservable<int> ob =
    Observable.Create<int>(o =>
    {
        var cancel = new CancellationDisposable(); // internally creates a new CancellationTokenSource
        NewThreadScheduler.Default.Schedule(() =>
        {
            int i = 0;
            for (; ; )
            {
                Thread.Sleep(200);  // here we do the long lasting background operation
                if (!cancel.Token.IsCancellationRequested)    // check cancel token periodically
                    o.OnNext(i++);
                else
                {
                    Console.WriteLine("Aborting because cancel event was signaled!");
                    o.OnCompleted(); // will not make it to the subscriber
                    return;
                }
            }
        }
        );

        return cancel;
    }
    );
        }
    }
}
