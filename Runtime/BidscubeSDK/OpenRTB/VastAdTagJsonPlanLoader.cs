namespace BidscubeSDK.OpenRTB
{
    internal static class VastAdTagJsonPlanLoader
    {
        internal enum NestedPlanLoadMode
        {
            None,
            SingleSlot,
            FullPlan
        }

        internal static NestedPlanLoadMode GetNestedPlanLoadMode(ResolvedVideoAdPayload resolved)
        {
            if (resolved?.PlaybackPlan == null || !resolved.PlaybackPlan.IsPlayable)
                return NestedPlanLoadMode.None;

            return resolved.PlaybackPlan.Slots.Count > 1
                ? NestedPlanLoadMode.FullPlan
                : NestedPlanLoadMode.SingleSlot;
        }
    }
}
