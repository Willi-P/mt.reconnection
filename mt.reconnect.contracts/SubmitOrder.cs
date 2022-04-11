namespace mt.reconnect.contracts
{
    public record SubmitOrder
    {
        public string? OrderId { get; init; }
        public string? Name { get; init; }
    }
}