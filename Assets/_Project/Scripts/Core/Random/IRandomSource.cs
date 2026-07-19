namespace RandomTowerDefense.Core.Random
{
    public interface IRandomSource
    {
        int NextInt(int exclusiveMaximum);
    }
}
