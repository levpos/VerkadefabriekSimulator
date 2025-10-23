using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class FrituurInteract : MonoBehaviour
{
    [Header("Mandjes en knop")]
    public Transform mandje1;
    public Transform mandje2;
    public Transform draaiknop;

    [Header("Instellingen")]
    public string playerTag = "Player";
    public float liftHoogte = 0.25f;          // Hoe hoog ze omhoog gaan (boven start)
    public float dipDiepte = 0.15f;           // Hoe diep ze onder hun startpositie gaan (in het vet)
    public float bewegingTijd = 1.2f;         // Totale tijd per mandje animatie
    public float knopHoek = 45f;              // graden
    public float knopSnelheid = 2f;           // rotatie snelheid (interpolatie)

    [Header("Timing")]
    public float delayTussenMandjes = 0.2f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool spelerInRange = false;
    private bool isAnimating = false;
    private bool isAan = false; // frituur status (aan/uit)
    private Vector3 mandje1Start, mandje2Start;
    private Quaternion knopStartRot;
    private Quaternion knopTargetRot;

    void Start()
    {
        if (mandje1 != null) mandje1Start = mandje1.localPosition;
        if (mandje2 != null) mandje2Start = mandje2.localPosition;
        if (draaiknop != null)
        {
            knopStartRot = draaiknop.localRotation;
            knopTargetRot = knopStartRot * Quaternion.Euler(0f, 0f, -knopHoek);
        }
    }

    void Update()
    {
        if (spelerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (!isAnimating)
            {
                if (!isAan)
                    StartCoroutine(StartFrituurAnimatie());
                else
                    StartCoroutine(StopFrituurAnimatie());
            }
        }
    }

    // Start: omhoog -> naar dip (onder start)
    private IEnumerator StartFrituurAnimatie()
    {
        isAnimating = true;
        isAan = true;

        // draai knop naar target en laat 'm staan
        StartCoroutine(RoteerKnop(true));

        // mandje1 omhoog then naar dip
        StartCoroutine(BeweegMandje_DualPhase(mandje1, mandje1Start, true));
        yield return new WaitForSeconds(delayTussenMandjes);
        StartCoroutine(BeweegMandje_DualPhase(mandje2, mandje2Start, true));

        // wacht tot alle animaties klaar zijn
        yield return new WaitForSeconds(bewegingTijd + 0.1f + delayTussenMandjes);
        isAnimating = false;
    }

    // Stop: uit vet omhoog -> terug naar start
    private IEnumerator StopFrituurAnimatie()
    {
        isAnimating = true;
        isAan = false;

        // draai knop terug naar start en laat 'm staan
        StartCoroutine(RoteerKnop(false));

        // bij uitschakelen: eerst van dip omhoog (uit vet) en terug naar start
        StartCoroutine(BeweegMandje_DualPhase(mandje1, mandje1Start, false));
        yield return new WaitForSeconds(delayTussenMandjes);
        StartCoroutine(BeweegMandje_DualPhase(mandje2, mandje2Start, false));

        yield return new WaitForSeconds(bewegingTijd + 0.1f + delayTussenMandjes);
        isAnimating = false;
    }

    // Dual-phase animatie:
    // - naarBeneden == true: phase1 -> omhoog (start -> start+liftHoogte), phase2 -> naar dip (start+lift -> start-dip)
    // - naarBeneden == false: phase1 -> omhoog uit dip (start-dip -> start+lift), phase2 -> terug naar start (start+lift -> start)
    private IEnumerator BeweegMandje_DualPhase(Transform mandje, Vector3 startPos, bool naarBeneden)
    {
        if (mandje == null) yield break;

        float half = bewegingTijd * 0.5f;

        if (naarBeneden)
        {
            // PHASE 1: start -> peakAbove
            Vector3 peakAbove = startPos + Vector3.up * liftHoogte;
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / half);
                float eased = ease.Evaluate(p);
                mandje.localPosition = Vector3.LerpUnclamped(startPos, peakAbove, eased);
                yield return null;
            }

            // PHASE 2: peakAbove -> dipBelow (start - dipDiepte)
            Vector3 dipBelow = startPos + Vector3.down * dipDiepte;
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / half);
                float eased = ease.Evaluate(p);
                mandje.localPosition = Vector3.LerpUnclamped(peakAbove, dipBelow, eased);
                yield return null;
            }

            // finale: exacte dip positie
            mandje.localPosition = dipBelow;
        }
        else
        {
            // Assume mandje currently at dipBelow (start - dipDiepte). We animate dip -> peakAbove -> start.
            Vector3 dipBelow = startPos + Vector3.down * dipDiepte;
            Vector3 peakAbove = startPos + Vector3.up * liftHoogte;

            // PHASE 1: dipBelow -> peakAbove (snelle omhoog)
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / half);
                float eased = ease.Evaluate(p);
                mandje.localPosition = Vector3.LerpUnclamped(dipBelow, peakAbove, eased);
                yield return null;
            }

            // PHASE 2: peakAbove -> startPos (zacht terug)
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / half);
                float eased = ease.Evaluate(p);
                mandje.localPosition = Vector3.LerpUnclamped(peakAbove, startPos, eased);
                yield return null;
            }

            // finale: exacte start positie
            mandje.localPosition = startPos;
        }
    }

    // Rotatie van de knop naar target of terug
    private IEnumerator RoteerKnop(bool naarLinks)
    {
        if (draaiknop == null) yield break;

        Quaternion from = draaiknop.localRotation;
        Quaternion to = naarLinks ? knopTargetRot : knopStartRot;
        float t = 0f;
        float dur = 1f / Mathf.Max(0.0001f, knopSnelheid); // snelheid -> duurtijd

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            float eased = ease.Evaluate(p);
            draaiknop.localRotation = Quaternion.Slerp(from, to, eased);
            yield return null;
        }

        draaiknop.localRotation = to;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            spelerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            spelerInRange = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Collider col = GetComponent<Collider>();
        if (col != null)
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
