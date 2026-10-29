namespace RestorationToolsTweaks
{
    // The brush of one tool instance. The vanilla values are read once, before the mod changes the tool.
    internal sealed class BrushState
    {
        public float VanillaActiveRadius;
        public float VanillaBrushRadius;
        public int Step;
        public bool PreviewVisible;
        // On the host: the brush scale of the last relayed hit, and the drain factor last logged.
        public float HostScale = 1f;
        public float LoggedDrainFactor = 1f;
    }
}
