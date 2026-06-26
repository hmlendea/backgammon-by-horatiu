namespace BackgammonByHoratiu.GameLogic.GameManagers
{
    public interface IGameLogicManager
    {
        void LoadContent();

        void UnloadContent();

        void Update(double elapsedMilliseconds);
    }
}
