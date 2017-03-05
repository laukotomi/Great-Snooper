using System.Collections.Generic;
using GreatSnooper.Helpers;

namespace GreatSnooper.Channel
{
    public class LastUserMessages
    {
        private LinkedList<string> _lastMessages;
        private LinkedListNode<string> _lastMessageIterator;
        private string _tempMessage = string.Empty;

        public LastUserMessages()
        {
            this._lastMessages = new LinkedList<string>();
        }

        public void Reset()
        {
            this._lastMessages.Clear();
            this._lastMessageIterator = null;
            this._tempMessage = string.Empty;
        }

        public bool TryGetPrevious(string actualText, out string text)
        {
            if (this._lastMessages.Count > 0)
            {
                if (this._lastMessageIterator == null)
                {
                    this._lastMessageIterator = _lastMessages.First;
                    this._tempMessage = actualText;
                    text = this._lastMessageIterator.Value;
                    return true;
                }
                else if (this._lastMessageIterator.Next != null)
                {
                    this._lastMessageIterator = _lastMessageIterator.Next;
                    text = this._lastMessageIterator.Value;
                    return true;
                }
            }

            text = string.Empty;
            return false;
        }

        public bool TryGetNext(string p, out string text)
        {
            if (this._lastMessages.Count > 0 && this._lastMessageIterator != null)
            {
                this._lastMessageIterator = this._lastMessageIterator.Previous;
                if (this._lastMessageIterator == null)
                {
                    text = this._tempMessage;
                    return true;
                }
                else
                {
                    text = this._lastMessageIterator.Value;
                    return true;
                }
            }

            text = string.Empty;
            return false;
        }

        public void Add(string message)
        {
            if (this._lastMessages.Count == 0 || this._lastMessages.First.Value != message)
            {
                if (this._lastMessages.Count == GlobalManager.LastMessageCapacity)
                {
                    this._lastMessages.RemoveLast();
                }

                this._lastMessages.AddFirst(message);
            }
            this._lastMessageIterator = null;
        }
    }
}
