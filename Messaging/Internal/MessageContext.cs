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


        private CommitterDefintion.StaticCommitter _committer = null;

        private event EventHandler<MessageEvent> _genericEvent;


        private readonly IObservable<EventPattern<MessageEvent>> _eventAsObservable;

        private readonly IDisposable _disposable = null;


        protected object _lock { get; private set; }

        public MessageContext()
        {
            _lock = new object();

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
                var len = evs.Count();

                var space = TryGetFreeSpace(len);
                if(space.Item1)
                {
                    return false;
                }

                var index = 0;
                for(var i = space.Item2; i<= space.Item3; ++i)
                {
                    var key = i ;
                    _dic.AddOrUpdate(key, evs[index].EventArgs.Message, (_key, message) => evs[index].EventArgs.Message);
                    ++index;
                }


                await Task.Delay(1000 * 1);
                return true;
            })
            .Do(isOk =>
            {
                if (isOk)
                {
                    Console.WriteLine("Batch Done!");
                }
                else
                {
                    Console.WriteLine("Batch Failed!");
                }
                
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
            var count = _buffer.GetOrAdd<long>(CommitterDefintion.StaticCommitter.CommitterCount, (key, arg) =>
            {
                return arg;
            }, 1);

            for(var i =0; i< count; i++)
            {
                _committer = CommitterDefintion.StaticCommitter.Create(i);

                InitStaticCommitter(_committer);
            }


        }


        private void InitStaticCommitter(CommitterDefintion.StaticCommitter committer)
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
            IObservable<int> ob = Observable.Create<int>(o =>
                            {
                                var cancel = new CancellationDisposable(); // internally creates a new CancellationTokenSource
                                NewThreadScheduler.Default.Schedule(() =>
                                {
                                    int i = 0;
                                    for (; ; )
                                    {
                                        if (!cancel.Token.IsCancellationRequested)    // check cancel token periodically
                                        {
                                            var startIndex = GetCommitterVal(_committer.StartIndexKey);

                                            var endIndex = GetCommitterVal(_committer.EndIndexKey);



                                            o.OnNext(i++);
                                        }
                                            

                                       
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
                            });
        }



        private long GetCommitterVal(string key)
        {
            if (_buffer.TryGetValue(key, out long val))
            {
                return val;
            }

            return -1;
        }


        private bool SetCommitterVal(string key, long newVal, )
        {
            if (_buffer.TryUpdate(key, val, val))
            {
                return true;
            }

            return false;
        }

        private Message GetMessageVal(long key)
        {
            if (_dic.TryGetValue(key, out Message val))
            {
                return val;
            }

            return null;
        }


        private long PrepareAppend(int messageCount)
        {
            //lock (this.Lock)
            //{

            //}

            //foreach (var staticCommitter in _committers)
            //{
            //    var val = GetCommitterVal(staticCommitter.EndIndexKey);

            //    if(long.MaxValue - val > messageCount)
            //    {
            //        return val + 1;
            //    }
            //    else
            //    {
            //        var startIndex = GetCommitterVal(staticCommitter.StartIndexKey);


            //    }
                
                

            //}

            //var startIndex = this.lastIndex.Next();
            //for (var i = 0; i < messageCount; i++)
            //{
            //    this.inflightAppends.Add(new CommitInfo
            //    {
            //        RecordInfo = new RecordInfo(this.lastIndex.Next()),
            //        IsCommited = false,
            //        AppendTime = DateTime.UtcNow
            //    });

            //    this.lastIndex = this.lastIndex.Next();
            //}

            return NoAvailableIndex;
        }


        private Tuple<bool, long, long> TryGetFreeSpace(int messageCount)
        {
            var startIndex = GetCommitterVal(_committer.StartIndexKey);
            var endIndex = GetCommitterVal(_committer.EndIndexKey);

            if(_committer.InCircle)
            {
                if(startIndex - endIndex > messageCount)
                {
                    return new Tuple<bool, long, long>(true, endIndex + 1, endIndex + messageCount);
                }

                return new Tuple<bool, long, long>(false, -1, -1);
            }
            else
            {

                if (long.MaxValue - endIndex > messageCount)
                {
                    this._committer.SetCircleStatus(true);
                    return new Tuple<bool, long, long>(true, endIndex +1, endIndex + messageCount);
                }
                else
                {
                    if(startIndex > messageCount)
                    {
                        return new Tuple<bool, long, long>(true, -1, messageCount -1);
                    }
                }

                return new Tuple<bool, long, long>(false, -1, -1);

            }
        }


        private Tuple<bool, long, long> TryGetAvailableData(int messageCount = 10)
        {
            var startIndex = GetCommitterVal(_committer.StartIndexKey);
            var endIndex = GetCommitterVal(_committer.EndIndexKey);

            if (_committer.InCircle)
            {
                if (startIndex - endIndex > messageCount)
                {
                    return new Tuple<bool, long, long>(true, endIndex + 1, endIndex + messageCount);
                }

                return new Tuple<bool, long, long>(false, -1, -1);
            }
            else
            {

                if (long.MaxValue - endIndex > messageCount)
                {
                    this._committer.SetCircleStatus(true);
                    return new Tuple<bool, long, long>(true, endIndex + 1, endIndex + messageCount);
                }
                else
                {
                    if (startIndex > messageCount)
                    {
                        return new Tuple<bool, long, long>(true, -1, messageCount - 1);
                    }
                }

                return new Tuple<bool, long, long>(false, -1, -1);

            }
        }


        private const long NoAvailableIndex= -9999;




    }
}
