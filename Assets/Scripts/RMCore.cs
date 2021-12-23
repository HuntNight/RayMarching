using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView] public class RMCore : MonoBehaviour
{
    [SerializeField] private ComputeShader RMComputeShader;
    [SerializeField] private Transform LightPosition;
    [SerializeField] private List<RMFigure> Figures;
    private Camera Camera => Camera.current;
    private RenderTexture RT;
    private ComputeBuffer CB;
    private FigureData[] FGD;
    
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        InitCheck();
        if (Figures.Count == 0)
            return;
        SyncBuffer();
        SetShaderParams();
        RMComputeShader.Dispatch(0, Mathf.CeilToInt(Camera.pixelWidth / 8f), Mathf.CeilToInt(Camera.pixelHeight / 8f), 1);
        Graphics.Blit(RT, dest);
    }

    private void SetShaderParams()
    {
        RMComputeShader.SetTexture(0, "Result", RT);
        RMComputeShader.SetInts("Size", RT.width, RT.height);
        RMComputeShader.SetBuffer(0, "Figures", CB);
        RMComputeShader.SetInt("numFigures", Figures.Count);
        var camPosition = Camera.transform.position;
        var lightPosition = LightPosition.position;
        RMComputeShader.SetFloats("camPosition", camPosition.x, camPosition.y, camPosition.z);
        RMComputeShader.SetFloats("lightPosition", lightPosition.x, lightPosition.y, lightPosition.z);
        RMComputeShader.SetMatrix("cameraToWorld", Camera.cameraToWorldMatrix);
        RMComputeShader.SetMatrix("cameraInverseProjection", Camera.projectionMatrix.inverse);
    }
    private void SyncBuffer()
    {
        if (CB == null)
            return;
        for (var i = 0; i < Figures.Count; i++)
        {
            var c = Figures[i].transform;
            FGD[i] = new FigureData
            {
                Position = c.position,
                EulerAngles = c.eulerAngles,
                Size = c.lossyScale
            };
        }
        CB.SetData(FGD);
    }
    private void InitCheck()
    {
        CheckRT();
        CheckBuffer();
    }
    private void CheckRT()
    {
        if (RT != null && RT.width == Camera.pixelWidth && RT.height == Camera.pixelHeight)
            return;
        
        if (RT != null)
            RT.Release();
        
        RT = new RenderTexture(Camera.pixelWidth, Camera.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        RT.enableRandomWrite = true;
        RT.Create();
    }

    private void CheckBuffer()
    {
        if (CB != null && CB.count == Figures.Count)
            return;

        if (Figures.Count == 0)
            return;
        
        if (CB != null)
            CB.Release();
        
        CB = new ComputeBuffer(Figures.Count, FigureData.GetSize());
        FGD = new FigureData[Figures.Count];
    }
}

struct FigureData
{
    public Vector3 Position;
    public Vector3 EulerAngles;
    public Vector3 Size;

    public static int GetSize()
    {
        return sizeof(float) * 9;
    }
}