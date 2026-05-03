namespace TowerDefence.Entities;

public abstract class EntityProcessor : Node
{
    public Main Game { get; }

    protected EntityProcessor(Main game) => Game = game;
}
