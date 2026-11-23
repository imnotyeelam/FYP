// Optional refinement for overlapping shared-interaction volumes. Targets without
// this interface retain the established nearest-target behaviour.
public interface ISharedInteractionPriority
{
    int SharedInteractionPriority { get; }
}
