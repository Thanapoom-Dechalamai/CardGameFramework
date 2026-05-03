using System;

namespace Project.Domain.Cards
{
    public readonly struct Card : IEquatable<Card>
    {
        public Rank Rank { get; }
        public Suit Suit { get; }

        public Card(Rank rank, Suit suit)
        {
            if (!Enum.IsDefined(typeof(Rank), rank))
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Invalid card rank.");

            if (!Enum.IsDefined(typeof(Suit), suit))
                throw new ArgumentOutOfRangeException(nameof(suit), suit, "Invalid card suit.");

            Rank = rank;
            Suit = suit;
        }

        public bool Equals(Card other)
        {
            return Rank == other.Rank && Suit == other.Suit;
        }

        public override bool Equals(object obj)
        {
            return obj is Card other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Rank, (int)Suit);
        }

        public override string ToString()
        {
            return $"{Rank} of {Suit}";
        }

        public static bool operator ==(Card left, Card right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Card left, Card right)
        {
            return !left.Equals(right);
        }
    }
}