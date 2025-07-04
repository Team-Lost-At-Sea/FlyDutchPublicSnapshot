using Needle.Console;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

[RequireComponent(typeof(FloatPropertyInterpolator))]
public class ZoneSound : MonoBehaviour
{
    public GameObject globalVolumeSetting;
    public bool isMusic;
    public AudioClip envMusicClip;

    // Volume multipliers for each surface type (adjust these values based on the loudness of your recordings)
    public Transform playerCameraHere;    // Reference to the object (camera currently) that has the listener
    [Range(0, 1f)]
    public float soundVolume = 0.1f; // Volume for this track.
    public float fadeDuration = 2;
    public float secondsToStayInZoneBeforePlaying = 1;
    [HideInInspector]
    public AudioSource audioSource;
    private FloatPropertyInterpolator fader;
    private float ambientAndMusicMultiplier = 0.8f; // Private multiplier (decided by developer) to all music volume towards this value.
    private float targetVolume; // Final target volume after formulas and multipliers
    private bool staying = false;
    private float currentStayTime = 0;
    private TriggerZoneHandler triggerZoneHandler;

    private void Awake()
    {
        // Register this sound with the AudioManager
        AudioManager.Instance.RegisterSound(this);
        triggerZoneHandler = GetComponent<TriggerZoneHandler>();
    }

    private void OnEnable()
    {
        AddEventListeners();
    }

    private void OnDisable()
    {
        RemoveEventListeners();
    }

    public void AddEventListeners()
    {
        VolumePrefs.OnMusicVolumeChanged += ChangeMusicVolume;
        triggerZoneHandler.OnEnter += TriggerEntered;
        triggerZoneHandler.OnExit += TriggerExited;
    }

    public void RemoveEventListeners()
    {
        VolumePrefs.OnMusicVolumeChanged -= ChangeMusicVolume;
        triggerZoneHandler.OnEnter -= TriggerEntered;
        triggerZoneHandler.OnExit -= TriggerExited;
    }
    private void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        fader = GetComponent<FloatPropertyInterpolator>();
        fader.SetTarget(audioSource, "volume");
        ChangeVolume(isMusic ? VolumePrefs.musicVolume : 1.0f);
    }

    private void ChangeVolume(float multiplier)
    {
        if (multiplier > 1 || multiplier < 0){
            D.LogError("Attempted to set volume multiplier out of bounds!", gameObject, "Any");
            return;
        }
        D.Log($"Multiplier changed to: {multiplier}", gameObject, "Aud");
        targetVolume = soundVolume * ambientAndMusicMultiplier * multiplier;
        audioSource.volume = targetVolume;
    }

    private void ChangeMusicVolume(float musicMultiplier){
        if (!isMusic) return; // Don't do anything if this zone sound was not marked as music.
        ChangeVolume(musicMultiplier);
    }

    private void Update()
    {
        checkStaying();
        if (!staying && audioSource.volume < 0.001f)
        {
            audioSource.Stop();
        }
    }

    private void TriggerEntered(Collider other)
    {
        if (!other.CompareTag("Player")) return; // if it's not a player entering it, ignore

        D.Log("Entered zonesound!", gameObject, "Aud");

        if (envMusicClip != null && !audioSource.isPlaying)
        { // It won't play if an audio is not found, or it won't play if a music is already playing.
          //startStayCheck();

            staying = true;
        }

    }
    private void TriggerExited(Collider other)
    {
        staying = false;
        currentStayTime = 0;
        fader.SetWithDuration(0.0f, fadeDuration);
    }

    private void playSound()
    {
        D.Log("Starting zonesound!", gameObject, "Aud");
        audioSource.clip = envMusicClip;
        audioSource.volume = 0;
        audioSource.Play();
        fader.SetWithDuration(targetVolume, fadeDuration);
    }
    private float getCurrentPlayingVolume()
    {
        return audioSource.volume;
    }

    private void checkStaying()
    {
        if (staying == false || currentStayTime == -1) return;

        currentStayTime += Time.deltaTime;
        D.Log($"Current Stay time: {currentStayTime}", gameObject, "Aud");
        if (currentStayTime >= secondsToStayInZoneBeforePlaying)
        {
            playSound();
            currentStayTime = -1;
        }
    }


}
