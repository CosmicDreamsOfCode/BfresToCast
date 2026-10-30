using System.Numerics;
using BfresLibrary;

namespace BfresToCast;

/// <summary>
/// A simplified interface to <see cref="BfresLibrary.Shape"/> with all the buffers exposed as lists.
/// Also contains additional features such as <see cref="VertexInfos"/> and basic enumeration features.
/// </summary>
public record RawShapeInfo()
{
    public Shape Shape { get; }
    
    /// <summary>
    /// A list of all the compiled Verticies. Each vertex info has most of the important information about a vertex.
    /// These values are used to convert the bfres buffers into cast buffers
    /// </summary>
    public VertexInfo[] VertexInfos { get; set; } = [];
    public List<int> Indicies { get; set; } = [];

    public Dictionary<byte, List<Vector2>> Uvs { get; set; } = new Dictionary<byte, List<Vector2>>();
    public Dictionary<byte, List<Vector4>> VertexColors { get; set; } = new Dictionary<byte, List<Vector4>>();

    public List<float[]> BlendIndices { get; set; } = new List<float[]>();
    public List<float[]> BlendWeights { get; set; } = new List<float[]>();

    public List<string> KeyShapes { get; } = new List<string>();


    public RawShapeInfo(Shape shape) : this()
    {
        Shape = shape;
    }

    /// <summary>
    /// Enumerates over all vertices which have weights for the specified bone.
    /// </summary>
    /// <param name="boneIndex">The index of the bone to search for(this is the actual index found in Skeleton.BonesList, not in the weights)</param>
    /// <param name="weight">If true, will return all bones with a weight greater than 0 for this bone.
    /// If false, will return all bones with a weight of 0 for this bone.</param>
    /// <returns>A bones which meet the weight condition</returns>
    public IEnumerable<VertexInfo> GetVertexesWithWeight(int boneIndex, bool weight = true)
    {
        return VertexInfos.Where(v => v.Weights.ContainsKey(boneIndex) && (v.Weights[boneIndex] > 0 && weight));
    }
}