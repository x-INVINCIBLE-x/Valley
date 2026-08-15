using System.Collections.Generic;

namespace Valley.Level.Generation
{
    /// <summary>
    /// Classic "marble bag" / Tetris-style randomizer: every item in the source collection is drawn
    /// exactly once, in a random order, before any item can repeat. When the bag empties it refills and
    /// reshuffles automatically, and Refill() makes sure the last item drawn from the old bag can't
    /// immediately reappear as the first item of the new one (no back-to-back repeat across the seam).
    /// </summary>
    public class ShuffleBag<T>
    {
        readonly List<T> source;
        readonly List<T> bag = new List<T>();
        readonly System.Random rng;
        bool hasLastDrawn;
        T lastDrawn;

        /// <param name="items">The full set of items the bag draws from each pass.</param>
        /// <param name="seed">0 = time-based random seed. Any other value gives a reproducible draw order.</param>
        public ShuffleBag(IEnumerable<T> items, int seed = 0)
        {
            source = new List<T>(items);
            rng = seed != 0 ? new System.Random(seed) : new System.Random();
            Refill();
        }

        /// <summary>How many items are left to draw before the bag empties and refills.</summary>
        public int Remaining => bag.Count;

        /// <summary>Draws the next item. Refills and reshuffles automatically once the bag is empty.</summary>
        public T Draw()
        {
            if (bag.Count == 0) Refill();

            int lastIndex = bag.Count - 1;
            T drawn = bag[lastIndex];
            bag.RemoveAt(lastIndex);

            lastDrawn = drawn;
            hasLastDrawn = true;
            return drawn;
        }

        /// <summary>Empties the bag so the next Draw() starts a fresh, freshly-shuffled pass.</summary>
        public void Reset()
        {
            bag.Clear();
            hasLastDrawn = false;
        }

        void Refill()
        {
            bag.Clear();
            bag.AddRange(source);
            Shuffle(bag);

            // Draw() pulls from the end of the list, so bag[^1] is what would be drawn next. Swap it
            // with something else if it matches the previous pass's final draw, avoiding a same-item
            // repeat right across the refill boundary.
            if (hasLastDrawn && bag.Count > 1 && EqualityComparer<T>.Default.Equals(bag[bag.Count - 1], lastDrawn))
            {
                int swapIndex = rng.Next(0, bag.Count - 1);
                (bag[bag.Count - 1], bag[swapIndex]) = (bag[swapIndex], bag[bag.Count - 1]);
            }
        }

        void Shuffle(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}