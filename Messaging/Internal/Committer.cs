using System;
using System.Collections.Generic;
using System.Text;

namespace Messaging.Internal
{
    internal class Committer
    {
        public const string CommitterCount = "CommitterCount";

        private const string CommitterStartIndex = "CommitterStartIndex";

        private const string CommitterEndIndex = "CommitterEndIndex";

        private Committer(int index)
        {
            Index = index;
        }

        public int Index { get; private set; }

        public long StartIndex { get; set; }


        public long EndIndex { get; set; }


        public string StartIndexKey => $"{CommitterStartIndex}{Index}";

        public string EndIndexKey => $"{CommitterEndIndex}{Index}";


        public static Committer Create(int index)
        {
            return new Committer(index);
        }
    }
}
