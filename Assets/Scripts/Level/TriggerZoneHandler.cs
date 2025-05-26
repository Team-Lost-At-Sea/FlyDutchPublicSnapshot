using System;
using UnityEngine;
using UnityEngine.Events;
using Needle.Console;

public class TriggerZoneHandler : MonoBehaviour
{
    public string tagToCheck = "Player";
    public event Action<Collider> OnExit;
    public event Action<Collider> OnEnter;

    [SerializeField] private UnityEvent[] enterTasks;
    [SerializeField] private UnityEvent[] exitTasks;

    private bool playerInside = false;
    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        SceneCore.commands.OnPlayerTeleport += CheckAndSetPlayerInsideVoidWrapper;
        SceneCore.commands.OnPlayerHighSpeedMove += CheckAndSetPlayerInsideVoidWrapper;
    }

    void Start()
    {
        CheckAndSetPlayerInside(SceneCore.playerCharacter.GetComponent<Collider>());

        if (tagToCheck != "Player")
        {
            D.LogWarning("TriggerZoneHandler currently only handles the 'Player' tag. Extend if needed.", gameObject, LogManager.LogCategory.Any);
        }
    }

    void OnDestroy()
    {
        // Always clean up event subscriptions!
        SceneCore.commands.OnPlayerTeleport -= CheckAndSetPlayerInsideVoidWrapper;
        SceneCore.commands.OnPlayerHighSpeedMove -= CheckAndSetPlayerInsideVoidWrapper;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(tagToCheck))
            return;

        if (!playerInside)
        {
            playerInside = true;
            FireEnter(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(tagToCheck))
            return;

        if (playerInside)
        {
            playerInside = false;
            FireExit(other);
        }
    }

    private void FireEnter(Collider other)
    {
        OnEnter?.Invoke(other);
        foreach (var task in enterTasks)
            task.Invoke();
    }

    private void FireExit(Collider other)
    {
        OnExit?.Invoke(other);
        foreach (var task in exitTasks)
            task.Invoke();
    }

    public void CheckAndSetPlayerInsideVoidWrapper(Collider target)
    {
        CheckAndSetPlayerInside(target);
    }

    public bool CheckAndSetPlayerInside(Collider target)
    {
        if (zoneCollider == null || target == null)
            return false;

        bool inside = zoneCollider.bounds.Intersects(target.bounds);

        if (inside && !playerInside)
        {
            playerInside = true;
            FireEnter(target);
        }
        else if (!inside && playerInside)
        {
            playerInside = false;
            FireExit(target);
        }

        return inside;
    }

    public bool IsPlayerInside() => playerInside;
}
