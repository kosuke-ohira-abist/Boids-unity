using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;

public class Boids : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    struct BoidData
    {
        public Vector3 position;
        public Vector3 velocity;
    }

    [System.Serializable]
    struct ObjectParam
    {
        // 他の個体との適用半径
        public float neighborhoodRadius;
        // 力の重み
        public float weight;
    }

    [System.Serializable]
    public struct WallParam
    {
        public float avoidWeight;
        public Vector3 center;
        public Vector3 size;
    }

    const int GRID_WIDTH = 8;
    const int GRID_HEIGHT = 8;
    const int GRID_DEPTH = 8;
    const int THREAD_SIZE = 256;
    public const int MAX_OBJECT_NUM = THREAD_SIZE * 64; // NOTE: オブジェクトの数は都合上THREAD_SIZEの倍数とする
    const float MAX_SPEED = 5.0f;
    const float MAX_FORCE = 0.5f;

    // 個体のパラメータ
    // 結合
    ObjectParam _CohesionParam;
    // 整列
    ObjectParam _AlignmentParam;
    // 分離
    ObjectParam _SeparateParam;
    // 壁のパラメータ
    public WallParam _WallParam;


    // コンピュートシェーダー
    ComputeShader _BoidsCS;
    ComputeBuffer _BoidsGridBuffer;
    ComputeBuffer _BoidsForceBuffer;
    public ComputeBuffer _BoidsDataBuffer;
    GPUData.Kernel _GridKernel;
    GPUData.Kernel _ForceKernel;
    GPUData.Kernel _IntegrateKernel;

    void Start()
    {
        _CohesionParam.neighborhoodRadius = 2.0f;
        _CohesionParam.weight = 1.0f;
        _AlignmentParam.neighborhoodRadius = 2.0f;
        _AlignmentParam.weight = 1.0f;
        _SeparateParam.neighborhoodRadius = 1.0f;
        _SeparateParam.weight = 3.0f;
        _WallParam.avoidWeight = 10.0f;
        _WallParam.center = Vector3.zero;
        _WallParam.size = new Vector3(32f, 32f, 32f);
        _BoidsCS = Resources.Load<ComputeShader>("Shaders/Boids");
        _GridKernel = new GPUData.Kernel(_BoidsCS, "GridCS");
        _ForceKernel = new GPUData.Kernel(_BoidsCS, "ForceCS");
        _IntegrateKernel = new GPUData.Kernel(_BoidsCS, "IntegrateCS");
        initBuffer();
    }

    void Update()
    {
        simulation();
    }

    void OnDestroy()
    {
        releaseBuffer();
    }

    void OnDrawGizmos() 
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(_WallParam.center, _WallParam.size);
    }

    void initBuffer()
    {
        _BoidsGridBuffer = new ComputeBuffer(GRID_WIDTH * GRID_HEIGHT * GRID_DEPTH * MAX_OBJECT_NUM, Marshal.SizeOf(typeof(int)));
        _BoidsForceBuffer = new ComputeBuffer(MAX_OBJECT_NUM, Marshal.SizeOf(typeof(Vector3)));
        _BoidsDataBuffer = new ComputeBuffer(MAX_OBJECT_NUM, Marshal.SizeOf(typeof(BoidData)));
        var forceArray = new Vector3[MAX_OBJECT_NUM];
        var boidsDataArray = new BoidData[MAX_OBJECT_NUM];
        for(var i = 0; i < MAX_OBJECT_NUM; ++i)
        {
            forceArray[i] = Vector3.zero;
            boidsDataArray[i].position = Random.insideUnitSphere * 1.0f;
            boidsDataArray[i].velocity = Random.insideUnitSphere * 0.1f;
        }
        _BoidsForceBuffer.SetData(forceArray);
        _BoidsDataBuffer.SetData(boidsDataArray);
    }

    void simulation()
    {
        // calc grid
        _BoidsCS.SetInt("_MaxObjectNum", MAX_OBJECT_NUM);
        _BoidsCS.SetVector("_WallCenter", _WallParam.center);
        _BoidsCS.SetVector("_WallSize", _WallParam.size);
        _BoidsCS.SetBuffer(_GridKernel.Index, "_BoidDataBufferRead", _BoidsDataBuffer);
        _BoidsCS.SetBuffer(_GridKernel.Index, "_BoidGridBufferWrite", _BoidsGridBuffer);
        _BoidsCS.Dispatch(_GridKernel.Index, GRID_WIDTH, GRID_HEIGHT, GRID_DEPTH);

        // calc force
        _BoidsCS.SetFloat("_CohesionNeighborhoodRadius", _CohesionParam.neighborhoodRadius);
        _BoidsCS.SetFloat("_AlignmentNeighborhoodRadius", _AlignmentParam.neighborhoodRadius);
        _BoidsCS.SetFloat("_SeparateNeighborhoodRadius", _SeparateParam.neighborhoodRadius);
        _BoidsCS.SetFloat("_MaxSpeed", MAX_SPEED);
        _BoidsCS.SetFloat("_MaxSteerForce", MAX_FORCE);
        _BoidsCS.SetFloat("_SeparateWeight", _SeparateParam.weight);
        _BoidsCS.SetFloat("_CohesionWeight", _CohesionParam.weight);
        _BoidsCS.SetFloat("_AlignmentWeight", _AlignmentParam.weight);
        _BoidsCS.SetFloat("_AvoidWallWeight", _WallParam.avoidWeight);
        _BoidsCS.SetBuffer(_ForceKernel.Index, "_BoidGridBufferRead", _BoidsGridBuffer);
        _BoidsCS.SetBuffer(_ForceKernel.Index, "_BoidDataBufferRead", _BoidsDataBuffer);
        _BoidsCS.SetBuffer(_ForceKernel.Index, "_BoidForceBufferWrite", _BoidsForceBuffer);
        _BoidsCS.Dispatch(_ForceKernel.Index, MAX_OBJECT_NUM / (int)_ForceKernel.ThreadX, 1, 1);

        // calc position and velocity
        _BoidsCS.SetFloat("_DeltaTime", Time.deltaTime);
        _BoidsCS.SetBuffer(_IntegrateKernel.Index, "_BoidDataBufferRead", _BoidsDataBuffer);
        _BoidsCS.SetBuffer(_IntegrateKernel.Index, "_BoidForceBufferRead", _BoidsForceBuffer);
        _BoidsCS.SetBuffer(_IntegrateKernel.Index, "_BoidDataBufferWrite", _BoidsDataBuffer);
        _BoidsCS.Dispatch(_IntegrateKernel.Index, MAX_OBJECT_NUM / (int)_IntegrateKernel.ThreadX, 1, 1);
    }

    void releaseBuffer()
    {
        if(_BoidsGridBuffer != null)
        {
            _BoidsGridBuffer.Release();
            _BoidsGridBuffer = null;
        }
        if(_BoidsDataBuffer != null)
        {
            _BoidsDataBuffer.Release();
            _BoidsDataBuffer = null;
        }
        if(_BoidsForceBuffer != null)
        {
            _BoidsForceBuffer.Release();
            _BoidsForceBuffer = null;
        }
    }
}
