using System.Diagnostics;
using PaintCore;

namespace RestorationToolsTweaks
{
    // Reads one mask of a panel back from the GPU and gives its mean value (0 to 1) inside the panel's UV area.
    // The same base and the same cutoffs as the game's SprayCoverageCounter, but it adds up the values, so
    // partial work (a part of the dirt taken off) counts. The base reads again only after the panel changes.
    internal sealed class WorkCounter : CwPaintableTextureMonitorMask
    {
        private const byte InteriorMaskCutoff = 250;
        private const byte IslandMaskCutoff = 127;

        public float Mean { get; private set; }
        // True when the panel's UV mask has no pixel inside the cutoffs, so the mask can never give a reading.
        public bool Unreadable { get; private set; }
        public double PassMs { get; private set; }
        public double RequestMs { get; private set; }

        protected override void HandleComplete(int boost)
        {
            if (!currentPixels.IsCreated || !maskPixels.IsCreated || currentPixels.Length != maskPixels.Length)
            {
                return;
            }
            long start = Stopwatch.GetTimestamp();
            long sum = 0;
            int count = 0;
            for (int i = 0; i < currentPixels.Length; i++)
            {
                if (maskPixels[i] >= InteriorMaskCutoff)
                {
                    count++;
                    sum += currentPixels[i].r;
                }
            }
            if (count == 0)
            {
                for (int i = 0; i < currentPixels.Length; i++)
                {
                    if (maskPixels[i] > IslandMaskCutoff)
                    {
                        count++;
                        sum += currentPixels[i].r;
                    }
                }
            }
            if (count == 0)
            {
                Unreadable = true;
                return;
            }
            Mean = sum / (255f * count);
            PassMs = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            InvokeOnUpdated();
            RequestMs = 0;
        }

        // RequestMs is the longest Update (where the base starts a GPU read) since the last reading.
        protected override void Update()
        {
            long start = Stopwatch.GetTimestamp();
            base.Update();
            double ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            if (ms > RequestMs)
            {
                RequestMs = ms;
            }
        }
    }
}
