using System;
using System.Collections.Generic;
using System.Text;

namespace Messaging.Internal
{
    internal class CommitterDefintion
    {
        
        public class StaticCommitter
        {
            public const string CommitterCount = "CommitterCount";

            private const string CommitterStartIndex = "CommitterStartIndex";

            private const string CommitterEndIndex = "CommitterEndIndex";

            private StaticCommitter(int index)
            {
                Index = index;
            }

            public int Index { get; private set; }

            //public long StartIndex { get; set; }


            //public long EndIndex { get; set; }

            public bool InCircle { get; private set; } = false;


            public string StartIndexKey => $"{CommitterStartIndex}{Index}";

            public string EndIndexKey => $"{CommitterEndIndex}{Index}";


            public static StaticCommitter Create(int index)
            {
                return new StaticCommitter(index);
            }

            public void SetCircleStatus(bool status)
            {
                this.InCircle = status;
            }
        }

        public class RunTimeCommitter
        {
            private RunTimeCommitter(int index, int offset)
            {
                StartIndex = index;
                EndIndex = index + offset;
            }

            public long StartIndex { get; set; }


            public long EndIndex { get; set; }


            public static RunTimeCommitter Create(int index, int offset)
            {
                return new RunTimeCommitter(index, offset);
            }
        }


    }
}
