using MemoryQueue.Base.Models;

namespace MemoryQueue.Base
{
    public sealed record MyCommandKey
    {
        public Guid Id { get; set; }

        public int Balance { get; set; } = 0;

        public MyCommand Command { get; set; }

        public MyCommandKey(Guid id)
        {
            Id = id;
        }
    }

    public sealed record MyCommand
    {
        public Guid Id { get; set; }
        public QueueItem Item { get; set; }
        public bool Upsert { get; set; }
        public bool CountAsAnInsert { get; set; }

        public override string ToString()
        {
            return $"MessageId: {Id} || Upsert? {Upsert}";
        }

        public MyCommand(QueueItem item, bool upsert)
        {
            Upsert = upsert;
            Item = item;
            Id = item.Id;
        }
    }
}

