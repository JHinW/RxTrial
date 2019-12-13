using System;
using System.Collections.Generic;
using System.Text;

namespace Messaging.Internal
{
    internal class MessageEvent: EventArgs
    {
        public Message Message { get; set; }
    }
}
