namespace BackgammonByHoratiu.Entities
{
    readonly struct DiceQuadruple(int firstDie, int secondDie, int thirdDie, int fourthDie)
    {
        public int FirstDie { get; } = firstDie;
        public int SecondDie { get; } = secondDie;
        public int ThirdDie { get; } = thirdDie;
        public int FourthDie { get; } = fourthDie;

        public bool IsValid => FirstDie != -1;

        public static DiceQuadruple None => new(-1, -1, -1, -1);
    }
}
