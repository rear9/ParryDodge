using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class WaveText : MonoBehaviour
{
    [Header("Wave Settings")]
    public float amplitude = 1f;      // vertical wave height
    public float frequency = 1f;     // horizontal spread of wave
    public float waveSpeed = 1f;    // radians per second, smaller = slower

    [Header("Breathing Settings")]
    public float scaleAmplitude = .5f; // fraction of scale
    public float breathingSpeed = 1f;  // radians per second, smaller = slower

    private TMP_Text tmpText;
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] baseVertices;
    private Vector3 originalScale;

    private float wavePhase = 0f;
    private float breathingPhase = 0f;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Increment phases slowly based on deltaTime
        wavePhase += waveSpeed * Time.deltaTime;
        breathingPhase += breathingSpeed * Time.deltaTime;

        AnimateWave();
        AnimateBreathing();
    }

    private void AnimateWave()
    {
        tmpText.ForceMeshUpdate();
        mesh = tmpText.mesh;
        vertices = mesh.vertices;
        baseVertices = tmpText.textInfo.meshInfo[0].vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 orig = baseVertices[i];
            float wave = Mathf.Sin((orig.x / frequency) + wavePhase) * amplitude;
            vertices[i] = new Vector3(orig.x, orig.y + wave, orig.z);
        }

        mesh.vertices = vertices;
        tmpText.canvasRenderer.SetMesh(mesh);
    }

    private void AnimateBreathing()
    {
        float scaleOffset = 1f + Mathf.Sin(breathingPhase) * scaleAmplitude;
        transform.localScale = originalScale * scaleOffset;
    }
}