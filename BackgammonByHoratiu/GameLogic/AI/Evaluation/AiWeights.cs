namespace BackgammonByHoratiu.GameLogic.AI.Evaluation
{
    internal static class AiWeights
    {
        // Phase detection thresholds (AI pip count minus human pip count)
        internal const int BackGamePipThreshold = 40;
        internal const int RacingPipThreshold = 15;

        // Bar pieces are treated as this many pips when calculating pip count
        internal const int BarPipEquivalent = 25;

        // Pip lead multipliers per phase
        internal const int PipLeadRacing = 10;
        internal const int PipLeadBlocking = 3;

        // Outed piece costs and bonuses
        internal const int AiBarPenaltyRacing = 80;
        internal const int AiBarPenaltyBlocking = 60;
        internal const int AiBarPenaltyBackGame = 50;
        internal const int HumanBarBonusRacing = 30;
        internal const int HumanBarBonusBlocking = 60;
        internal const int HumanBarBonusBackGame = 80;

        // Owned point bonuses — Racing phase
        internal const int RacingHomePointBonus = 20;
        internal const int RacingOuterPointBonus = 5;

        // Owned point bonuses — Blocking phase
        internal const int BlockingHomePointBonus = 60;
        internal const int BlockingAnchorBonus = 80;
        internal const int BlockingOuterPointBonus = 25;

        // Owned point bonuses — BackGame phase
        internal const int BackGameAnchorBonus = 120;
        internal const int BackGameHomePointBonus = 70;
        internal const int BackGameOuterPointBonus = 10;

        // Blot exposure multipliers (threat level x multiplier = score penalty)
        internal const int BlotThreatRacing = 8;
        internal const int BlotThreatBlocking = 20;
        internal const int BlotThreatBackGameHome = 5;
        internal const int BlotThreatBackGameOuter = 20;

        // Prime scoring
        internal const int PrimeMultiplierBlocking = 8;
        internal const int PrimeMultiplierBackGame = 10;
        internal const int PrimeMilestone4Bonus = 40;
        internal const int PrimeMilestone5Bonus = 100;
        internal const int PrimeMilestone6Bonus = 200;

        // Consecutive anchor chain reward in human home board (cols 18-23)
        internal const int ConsecutiveAnchorBonus = 20;

        // Home board closure reward when human pieces are on the bar
        internal const int ClosedPointBarBonus = 80;

        // Return hit risk penalty factor
        internal const int ReturnHitRiskFactor = 12;

        // Quadratic stacking penalty factor for bear-off distribution
        internal const int DistributionPenaltyFactor = 6;
    }
}
