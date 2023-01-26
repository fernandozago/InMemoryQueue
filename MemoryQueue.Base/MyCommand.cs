using MemoryQueue.Base.Models;

namespace MemoryQueue.Base
{
    public enum QueueItemDbChangeType
    {
        Insert,
        Update,
        Delete
    }

    public sealed record QueueItemDbChange
    {
        public Guid Id { get; set; }
        public QueueItem Item { get; set; }
        public QueueItemDbChangeType ChangeType { get; set; }
        public bool Upsert => ChangeType != QueueItemDbChangeType.Delete;
        public int Balance { get; set; }

        public override string ToString()
        {
            return $"MessageId: {Id} || Upsert? {ChangeType}";
        }

        public QueueItemDbChange(QueueItem item, bool upsert)
        {
            if (upsert)
            {
                ChangeType = item.Retrying ? QueueItemDbChangeType.Update : QueueItemDbChangeType.Insert;
            }
            else
            {
                ChangeType = QueueItemDbChangeType.Delete;
            }
            Item = item;
            Id = item.Id;
        }
    }
}

