using System.Numerics;
using BfresLibrary;

namespace BfresToCast;

/// <summary>
/// A simplified interface to <see cref="BfresLibrary.Shape"/> with all the buffers exposed as lists.
/// Also contains additional features such as <see cref="VertexInfos"/> and basic enumeration features.
/// </summary>
public record RawShapeInfo(List<Vector3> Verticies, List<Vector3> Normals, List<Vector4> Tangents, Dictionary<byte, List<Vector2>> Uvs, List<int> Indicies, List<Vector4> BlendIndices, List<Vector4> BlendWeights, Dictionary<byte, List<Vector4>> VertexColors, Shape Shape)
{
    /// <summary>
    /// A list of all the compiled Verticies. Each vertex info has most of the important information about a vertex.
    /// These values are used to convert the bfres buffers into cast buffers
    /// </summary>
    public List<VertexInfo> VertexInfos { get; private set; } = new List<VertexInfo>();
    public List<int> Indicies { get; set; } = Indicies;


    // TODO: remove these
    // It would be much nicer and easier to replace these outright with VertexInfos
    // rn we are kinda half in half out, as some of our stuff use VertexInfos and some use these exposed buffers
    public List<Vector3> Verticies { get; set; } = Verticies;
    public List<Vector3> Normals { get; set; } = Normals;
    public List<Vector4> Tangents { get; set; } = Tangents;


    public Dictionary<byte, List<Vector2>> Uvs { get; set; } = Uvs;
    public Dictionary<byte, List<Vector4>> VertexColors { get; set; } = VertexColors;

    public List<Vector4> BlendIndices { get; set; } = BlendIndices;
    public List<Vector4> BlendWeights { get; set; } = BlendWeights;


    public RawShapeInfo(Shape shape) : this(new List<Vector3>(), new List<Vector3>(), new List<Vector4>(),
        new Dictionary<byte, List<Vector2>>(), new List<int>(), new List<Vector4>(), new List<Vector4>(), new Dictionary<byte, List<Vector4>>(), shape)
    {
    }

    /// <summary>
    /// Constructs <see cref="VertexInfos"/>. This should not be called outside GetRawShape().
    /// </summary>
    public void CreateVertexInfos()
    {
        for (int i = 0; i < Verticies.Count; i++)
        {
            Vector3 vert = Verticies[i];
            VertexInfo vertexInfo = new VertexInfo(i, vert, Normals.ElementAtOrDefault(i), Tangents.ElementAtOrDefault(i),
                new Dictionary<int, float>());
            VertexInfos.Add(vertexInfo);
        }
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