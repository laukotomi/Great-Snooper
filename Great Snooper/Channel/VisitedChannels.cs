namespace GreatSnooper.Channel
{
    using System.Collections.Generic;

    class VisitedChannels
    {
        private List<int> visitedChannels = new List<int>();

        public int Count
        {
            get
            {
                return this.visitedChannels.Count;
            }
        }

        public int GetBeforeLastIndex()
        {
            if (this.visitedChannels.Count > 1)
            {
                return this.visitedChannels[this.visitedChannels.Count - 2];
            }
            return -1;
        }

        public int GetLastIndex()
        {
            if (this.visitedChannels.Count > 0)
            {
                return this.visitedChannels[this.visitedChannels.Count - 1];
            }
            return -1;
        }

        public void HandleRemovedIndex(int index)
        {
            this.visitedChannels.Remove(index);

            for (int i = 0; i < this.visitedChannels.Count; i++)
            {
                if (this.visitedChannels[i] > index)
                {
                    this.visitedChannels[i]--;
                }
            }
        }

        public void Visit(int channelIndex)
        {
            this.visitedChannels.Remove(channelIndex);
            this.visitedChannels.Add(channelIndex);
        }
    }
}