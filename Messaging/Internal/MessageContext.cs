using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Linq;

namespace Messaging.Internal
{
    internal class MessageContext: IDisposable
    {
        private readonly ConcurrentDictionary<string, Message> _dic = new ConcurrentDictionary<string, Message>();

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
    }
}
