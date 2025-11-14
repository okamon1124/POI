public static class ZoneMapping
{
    public static LogicZone ToLogicZone(ZoneType uiZone)
    {
        switch (uiZone)
        {
            case ZoneType.Hand: return LogicZone.Hand;
            case ZoneType.Main: return LogicZone.Main;
            case ZoneType.Deployment: return LogicZone.Deployment;
            case ZoneType.Advance: return LogicZone.Advance;
            case ZoneType.Combat: return LogicZone.Combat;
            case ZoneType.Environment: return LogicZone.Environment;
            case ZoneType.Equipment: return LogicZone.Equipment;
            default: return LogicZone.UnDefined;
        }
    }
}