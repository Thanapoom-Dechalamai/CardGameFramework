using System;

namespace Project.Application.Poker
{
    public readonly struct TexasHoldemPlayerAction
    {
        public TexasHoldemPlayerActionType ActionType { get; }
        public int Amount { get; }

        public TexasHoldemPlayerAction(TexasHoldemPlayerActionType actionType, int amount = 0)
        {
            if (!Enum.IsDefined(typeof(TexasHoldemPlayerActionType), actionType))
                throw new ArgumentOutOfRangeException(nameof(actionType), actionType, "Invalid player action.");

            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Action amount cannot be negative.");

            ActionType = actionType;
            Amount = amount;
        }

        public static TexasHoldemPlayerAction Fold()
        {
            return new TexasHoldemPlayerAction(TexasHoldemPlayerActionType.Fold);
        }

        public static TexasHoldemPlayerAction Check()
        {
            return new TexasHoldemPlayerAction(TexasHoldemPlayerActionType.Check);
        }

        public static TexasHoldemPlayerAction Call()
        {
            return new TexasHoldemPlayerAction(TexasHoldemPlayerActionType.Call);
        }

        public static TexasHoldemPlayerAction Bet(int amount)
        {
            return new TexasHoldemPlayerAction(TexasHoldemPlayerActionType.Bet, amount);
        }

        public static TexasHoldemPlayerAction RaiseTo(int totalStreetBet)
        {
            return new TexasHoldemPlayerAction(TexasHoldemPlayerActionType.Raise, totalStreetBet);
        }

        public static TexasHoldemPlayerAction AllIn()
        {
            return new TexasHoldemPlayerAction(TexasHoldemPlayerActionType.AllIn);
        }
    }
}
