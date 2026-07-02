namespace BackgammonByHoratiu.Entities
{
    readonly struct DicePair
    {
        public int FirstDie { get; }
        public int SecondDie { get; }

        public bool IsValid => FirstDie != -1;

        public static DicePair None => new(-1, -1);

        public DicePair(int firstDie, int secondDie)
        {
            FirstDie = firstDie;
            SecondDie = secondDie;
        }
    }
}
