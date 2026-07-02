namespace BackgammonByHoratiu.Entities
{
    readonly struct DiceTriple(int firstDie, int secondDie, int thirdDie)
    {
        public int FirstDie { get; } = firstDie;
        public int SecondDie { get; } = secondDie;
        public int ThirdDie { get; } = thirdDie;

        public bool IsValid => FirstDie != -1;

        public static DiceTriple None => new(-1, -1, -1);
    }
}
