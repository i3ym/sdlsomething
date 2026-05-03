namespace TowerDefence.Towers;

public abstract class TowerProcessor : Node
{
    public Main Game { get; }

    protected TowerProcessor(Main game) => Game = game;
}
