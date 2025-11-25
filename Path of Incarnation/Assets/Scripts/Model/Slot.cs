public class Slot
{
    public Zone Zone { get; }
    public int Index { get; }

    public Slot NextSlot { get; set; }

    public CardInstance InSlotCardInstance { get; private set; }

    public bool IsEmpty => InSlotCardInstance == null;

    public event System.Action<Slot> Changed;

    public Slot(Zone zone, int index)
    {
        Zone = zone;
        Index = index;
    }

    public void PlaceCard(CardInstance card)
    {
        InSlotCardInstance = card;
        card.CurrentSlot = this;
        Changed?.Invoke(this);
    }

    public CardInstance RemoveCard()
    {
        var c = InSlotCardInstance;
        InSlotCardInstance = null;

        if (c != null)
            c.CurrentSlot = null;

        Changed?.Invoke(this);
        return c;
    }
}
