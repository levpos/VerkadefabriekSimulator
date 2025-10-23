using UnityEngine;

[DisallowMultipleComponent]
public class Deur : MonoBehaviour
{
    [Header("Target (optioneel: laat leeg en gebruik Player Tag)")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Door motion")]
    public float openAngle = 90f;
    public float openSpeed = 5f;
    public bool invertAngleDirection = false;

    [Header("Open van boven naar beneden (zoals een oven)")]
    public bool openFromTop = false;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)] public float volume = 0.5f;

    private Quaternion closedRot;
    private Quaternion openRot;
    private bool isOpen = false;
    private bool playerInRange = false;
    private AudioSource audioSource;

    void Start()
    {
        closedRot = transform.localRotation;

        // Rotatie berekenen afhankelijk van type deur
        if (openFromTop)
        {
            // Deur draait omlaag rond de X-as
            float angle = invertAngleDirection ? openAngle : -openAngle;
            openRot = closedRot * Quaternion.Euler(angle, 0f, 0f);
        }
        else
        {
            // Normale zijwaartse deur (zoals een kastdeur)
            float angle = invertAngleDirection ? -openAngle : openAngle;
            openRot = closedRot * Quaternion.Euler(0f, angle, 0f);
        }

        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) player = go.transform;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (openSound != null || closeSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (isOpen)
                CloseDoor();
            else
                OpenDoor();
        }

        Quaternion targetRot = isOpen ? openRot : closedRot;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * openSpeed);
    }

    public void OpenDoor()
    {
        if (isOpen) return;
        isOpen = true;
        if (openSound != null && audioSource != null)
            audioSource.PlayOneShot(openSound, volume);
    }

    public void CloseDoor()
    {
        if (!isOpen) return;
        isOpen = false;
        if (closeSound != null && audioSource != null)
            audioSource.PlayOneShot(closeSound, volume);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = false;
    }
}
