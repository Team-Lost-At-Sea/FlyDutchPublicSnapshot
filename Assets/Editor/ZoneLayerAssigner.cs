using UnityEditor;
using UnityEngine;

public class ZoneLayerAssigner
{
    [MenuItem("Tools/Assign Zone Layer To All TriggerZones")]
    private static void AssignZoneLayer()
    {
        int zoneLayer = LayerMask.NameToLayer("Zone");
        if (zoneLayer == -1)
        {
            Debug.LogError("Zone layer not found! Please create a layer named 'Zone' first.");
            return;
        }

        int count = 0;
        var allZones = Object.FindObjectsOfType<TriggerZoneHandler>(true); // true = include inactive objects
        foreach (var zone in allZones)
        {
            if (zone.gameObject.layer != zoneLayer)
            {
                Undo.RecordObject(zone.gameObject, "Assign Zone Layer");
                zone.gameObject.layer = zoneLayer;
                count++;
            }
        }

        Debug.Log($"Assigned 'Zone' layer to {count} TriggerZoneHandler GameObjects.");
    }
}
