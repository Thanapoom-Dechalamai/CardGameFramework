using System.Collections.Generic;

namespace Project.Domain.Cards
{
    public interface ICardShuffler
    {
        IReadOnlyList<Card> Shuffle(IReadOnlyList<Card> cards);
    }
}