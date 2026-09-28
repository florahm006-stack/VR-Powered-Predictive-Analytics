using UnityEngine;
using VRLab.Data;

namespace VRLab.Charts
{
    /// <summary>
    /// Base class for 3D charts. Charts render into a bounded local volume
    /// (width x height x depth meters) using GPU instancing (Graphics.RenderMeshInstanced)
    /// so thousands of points cost ~1 draw call.
    /// </summary>
    public abstract class Chart3D : MonoBehaviour
    {
        [Header("Volume (local meters)")]
        [SerializeField] protected Vector3 size = new Vector3(1.6f, 1.0f, 1.6f);

        [Header("Rendering")]
        [SerializeField] protected Mesh pointMesh;       // assign a small cube/sphere mesh
        [SerializeField] protected Material pointMaterial;
        [SerializeField, Range(1, 4096)] protected int maxPoints = 2048;

        protected readonly Matrix4x4[] matrices = new Matrix4x4[4096];
        protected int pointCount;
        protected Dataset dataset;

        /// <summary>Number of instanced points currently drawn (for tests/diagnostics).</summary>
        public int PointCount => pointCount;

        public virtual void SetDataset(Dataset ds)
        {
            dataset = ds;
            Rebuild();
        }

        /// <summary>Rebuild all point transforms from the current dataset and draw.</summary>
        public void Rebuild()
        {
            pointCount = 0;
            BuildPoints();
            Draw();
        }

        protected abstract void BuildPoints();

        protected void AddPoint(Vector3 localPos, float scale)
        {
            if (pointCount >= maxPoints) return;
            matrices[pointCount++] = Matrix4x4.TRS(
                transform.TransformPoint(localPos),
                transform.rotation,
                Vector3.one * scale);
        }

        /// <summary>Normalize a raw value into [0,1] given min/max (with zero-span guard).</summary>
        protected static float Normalize(float v, float min, float max)
        {
            float span = max - min;
            if (span <= Mathf.Epsilon) return 0.5f;
            return Mathf.Clamp01((v - min) / span);
        }

        protected void Draw()
        {
            if (pointMesh == null || pointMaterial == null || pointCount == 0) return;
            Graphics.RenderMeshInstanced(
                new RenderParams(pointMaterial),
                pointMesh, 0,
                matrices, pointCount);
        }

        // Redraw every frame is cheap (instancing) and keeps charts correct when the
        // chart transform moves; scenes may instead call Rebuild() on data change only.
        protected virtual void Update() => Draw();

        /// <summary>Test/editor injection of the rendering assets.</summary>
        public void Configure(Mesh mesh, Material material)
        {
            pointMesh = mesh;
            pointMaterial = material;
        }
    }
}
