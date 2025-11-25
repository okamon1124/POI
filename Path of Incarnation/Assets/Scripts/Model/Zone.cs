using System.Collections.Generic;

public class Zone
{
    public ZoneType Type { get; }
    public Owner Owner { get; }

    public IReadOnlyList<Slot> Slots => _slots;
    private readonly List<Slot> _slots = new();

    public Zone(ZoneType type, Owner owner, int slotCount)
    {
        Type = type;
        Owner = owner;

        for (int i = 0; i < slotCount; i++)
        {
            _slots.Add(new Slot(this, i));
        }
    }

    public Slot GetFirstEmptySlot()
        => _slots.Find(s => s.IsEmpty);
}
