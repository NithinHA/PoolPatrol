using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RippleEffect : MonoBehaviour
{
    public int TextureSize = 512;
    public RenderTexture ObjectsRT;
    private RenderTexture CurrRT, PrevRT, TempRT;
    public Shader RippleShader, AddShader;
    [Tooltip("Camera that renders ObjectsRT. Defaults to the one under this object.")]
    public Camera RippleCamera;
    private Material RippleMat, AddMat;

    private static readonly int poolRippleTexId = Shader.PropertyToID("_PoolRippleTex");
    private static readonly int poolRippleVPId = Shader.PropertyToID("_PoolRippleVP");
    private static readonly int poolRippleParamsId = Shader.PropertyToID("_PoolRippleParams");
    
    private Material targetMaterial;
    private int objectsRTId;
    private int currentRTId;
    private int prevRTId;
    private int rippleTexId;

    void Start()
    {
        objectsRTId = Shader.PropertyToID("_ObjectsRT");
        currentRTId = Shader.PropertyToID("_CurrentRT");
        prevRTId = Shader.PropertyToID("_PrevRT");
        rippleTexId = Shader.PropertyToID("_RippleTex");

        // Use RHalf (16-bit float) instead of RFloat (32-bit float) for mobile performance
        CurrRT = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.RHalf);
        PrevRT = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.RHalf);
        TempRT = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.RHalf);
        RippleMat = new Material(RippleShader);
        AddMat = new Material(AddShader);

        targetMaterial = GetComponent<Renderer>().material;
        targetMaterial.SetTexture(rippleTexId, CurrRT);
    }

    void Update()
    {
        AddMat.SetTexture(objectsRTId, ObjectsRT);
        AddMat.SetTexture(currentRTId, CurrRT);
        Graphics.Blit(null, TempRT, AddMat);

        RenderTexture rt0 = TempRT;
        TempRT = CurrRT;
        CurrRT = rt0;

        RippleMat.SetTexture(prevRTId, PrevRT);
        RippleMat.SetTexture(currentRTId, CurrRT);
        Graphics.Blit(null, TempRT, RippleMat);

        // Optimize: Eliminated expensive Graphics.Blit(TempRT, PrevRT) by using a 3-way reference swap.
        RenderTexture oldPrev = PrevRT;
        PrevRT = CurrRT;
        CurrRT = TempRT;
        TempRT = oldPrev;

        targetMaterial.SetTexture(rippleTexId, CurrRT);
        PublishGlobals();
    }

    void OnDisable()
    {
        // Tells the pool shaders the sim is gone, so they stop sampling a stale texture.
        Shader.SetGlobalVector(poolRippleParamsId, Vector4.zero);
    }

    // The pool floor, surface and deck shaders all read the sim, mapped by the ripple camera.
    private void PublishGlobals()
    {
        if (RippleCamera == null) RippleCamera = GetComponentInChildren<Camera>();
        if (RippleCamera == null) return;

        Shader.SetGlobalTexture(poolRippleTexId, CurrRT);
        Shader.SetGlobalMatrix(poolRippleVPId, RippleCamera.projectionMatrix * RippleCamera.worldToCameraMatrix);
        Shader.SetGlobalVector(poolRippleParamsId, new Vector4(1f / TextureSize, 0f, 1f, 0f));
    }
}
