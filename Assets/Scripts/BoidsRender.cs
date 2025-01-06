using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(Boids))]
public class BoidsRender : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    struct GPUInstance
    {
        public uint numMeshIndices;
        public uint maxObjectNum;
        public uint startIndex;
        public uint baseVertex;
        public uint startInstanceIndex;
    }
    Vector3 _Scale;
    Boids _BoidsScript;
    [SerializeField]
    Mesh _InstancedMesh;
    [SerializeField]
    Material _InstanceRenderMaterial;
    GPUInstance[] _GPUInstance;
    ComputeBuffer _GPUInstanceBuffer;

    void Start()
    {
        _Scale = new Vector3(0.05f, 0.1f, 0.8f);

        _GPUInstance = new GPUInstance[1];
        _GPUInstance[0].numMeshIndices = 0;
        _GPUInstance[0].maxObjectNum = 0;
        _GPUInstance[0].startIndex = 0;
        _GPUInstance[0].baseVertex = 0;
        _GPUInstance[0].startInstanceIndex = 0;
        _GPUInstanceBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(GPUInstance)), ComputeBufferType.IndirectArguments);

        _BoidsScript = GetComponent<Boids>();
    }

    void Update()
    {
        RenderInstancedMesh();
    }

    void OnDisable()
    {
        if(_GPUInstanceBuffer != null)
        {
            _GPUInstanceBuffer.Release();
            _GPUInstanceBuffer = null;
        }
    }

    void RenderInstancedMesh()
    {
        if(_InstanceRenderMaterial == null || _BoidsScript == null || !SystemInfo.supportsInstancing)
        {
            Debug.Log("Failed to render instanced meshes.");
            return;
        }

        _GPUInstance[0].numMeshIndices = (_InstancedMesh != null) ? (uint)_InstancedMesh.GetIndexCount(0) : 0;
        _GPUInstance[0].maxObjectNum = (uint)Boids.MAX_OBJECT_NUM;
        _GPUInstanceBuffer.SetData(_GPUInstance);

        _InstanceRenderMaterial.SetBuffer("_BoidDataBuffer", _BoidsScript._BoidsDataBuffer);
        _InstanceRenderMaterial.SetVector("_BoidObjectScale", _Scale);
        var bounds = new Bounds(
            _BoidsScript._WallParam.center,
            _BoidsScript._WallParam.size
        );
        Graphics.DrawMeshInstancedIndirect(
            _InstancedMesh,
            0,
            _InstanceRenderMaterial,
            bounds,
            _GPUInstanceBuffer
        );
    }
}
