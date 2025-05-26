using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TriggerZoneHandler))]
public class IslandTrigger : MonoBehaviour
{
    public string islandName;
    public IslandDisplay islandDisplay;
    public float secondsToStay = 2f; // How long the player must stay before triggering

    private TriggerZoneHandler triggerZoneHandler;
    private Coroutine stayCoroutine;
    private bool hasTriggered = false;

    private void Awake()
    {
        triggerZoneHandler = GetComponent<TriggerZoneHandler>();
    }

    private void OnEnable()
    {
        triggerZoneHandler.OnEnter += HandlePlayerEnter;
        triggerZoneHandler.OnExit += HandlePlayerExit;
    }

    private void OnDisable()
    {
        triggerZoneHandler.OnEnter -= HandlePlayerEnter;
        triggerZoneHandler.OnExit -= HandlePlayerExit;
    }

    private void HandlePlayerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (hasTriggered) return; // already triggered once, skip

        // Start waiting to see if player stays long enough
        stayCoroutine = StartCoroutine(WaitAndTrigger());
    }

    private void HandlePlayerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // If they leave before the wait finishes, cancel the coroutine
        if (stayCoroutine != null)
        {
            StopCoroutine(stayCoroutine);
            stayCoroutine = null;
        }
    }

    private IEnumerator WaitAndTrigger()
    {
        yield return new WaitForSeconds(secondsToStay);

        islandDisplay.ShowIslandName(islandName);
        hasTriggered = true; // only trigger once per zone (if desired)

        stayCoroutine = null;
    }
}
