using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class OutboxEvent
    {
        public Guid Id { get; set; }
        public OutboxEventType EventType { get; set; }
        public required string Payload { get; set; }
        public OutboxEventStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
