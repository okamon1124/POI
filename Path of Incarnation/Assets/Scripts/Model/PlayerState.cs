public class PlayerState
{
    public Owner Owner { get; }
    public int Health { get; set; }

    public PlayerState(Owner owner, int startingHealth)
    {
        Owner = owner;
        Health = startingHealth;
    }
}