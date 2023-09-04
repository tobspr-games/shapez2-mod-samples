using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <remarks>
/// Sharing the data between the entities statically is not a good design, but this is only an example and it is way harder to add proper support for this specific case
/// </remarks>
internal static class PortalPool
{
    private static readonly HashSet<IPortalReceiver> AvailablePortals = new HashSet<IPortalReceiver>();


    internal static void SubscribePortal(IPortalReceiver portalExitEntity)
    {
        AvailablePortals.Add(portalExitEntity);
    }

    internal static IPortalReceiver GetRandom()
    {
        if (AvailablePortals.Count == 0)
        {
            return null;
        }

        var element = AvailablePortals.Skip(Random.Range(0, AvailablePortals.Count)).First();

        AvailablePortals.Remove(element);
        return element;
    }
}
