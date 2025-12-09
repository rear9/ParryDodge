using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class WaveText : MonoBehaviour // adapted from old TMP tutorials, Animating Vertex Positions (https://www.youtube.com/watch?v=ZHU3AcyDKik)
                                      // Textmesh Tutorial (https://www.youtube.com/watch?v=FgWVW2PL1bQ)
                                      // Custom text effects in Unity with TextMeshPro (https://www.youtube.com/watch?v=FXMqUdP3XcE)
{
    [Header("Wave Settings")]
    public float amplitude = 1f;      // vertical wave height
    public float frequency = 1f;     // horizontal spread of wave
    public float waveSpeed = 1f;    // speed in radians per second
    private float wavePhase;

    [Header("Breathing Settings")]
    public float scaleAmplitude = .5f; // how much to scale
    public float breathingSpeed = 1f; // in radians per second
    private float breathingPhase;

    private TMP_Text tmpText;
    private Mesh mesh;
    private Vector3[] vertices; // lists of text vertices
    private Vector3[] baseVertices;
    private Vector3 originalScale;


    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        wavePhase += waveSpeed * Time.unscaledDeltaTime; // the part that will animate the text
        breathingPhase += breathingSpeed * Time.unscaledDeltaTime;

        AnimateWave();
        AnimateScale();
    }

    private void AnimateWave()
    {
        tmpText.ForceMeshUpdate();
        mesh = tmpText.mesh; // set values
        vertices = mesh.vertices;
        baseVertices = tmpText.textInfo.meshInfo[0].vertices;

        for (int i = 0; i < vertices.Length; i++) // for each vertex, apply sine function 
        {
            Vector3 orig = baseVertices[i];
            float wave = Mathf.Sin((orig.x / frequency) + wavePhase) * amplitude; // generates a different float value because of the X position of the text vertices
            vertices[i] = new Vector3(orig.x, orig.y + wave, orig.z); // offsets each vertex Y position by the sine wave
        }
        mesh.vertices = vertices; // sets the vertices and projects that new mesh to the screen each frame
        tmpText.canvasRenderer.SetMesh(mesh); 
    }

    private void AnimateScale()
    {
        float scaleOffset = 1f + Mathf.Sin(breathingPhase) * scaleAmplitude; // use sine function for scale as well
        transform.localScale = originalScale * scaleOffset;
    }
}