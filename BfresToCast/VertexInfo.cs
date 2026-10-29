using System.Numerics;

namespace BfresToCast;

/// <summary>
/// All the information of a vertex compiled into a single record, including all of its bone weights.
/// </summary>
public record VertexInfo(int Idx, Vector3 Position, Vector3 Normal, Vector4 Tangent, Dictionary<int, float> Weights)
{
    public Vector3 Position { get; set; } = Position;
    public Vector3 Normal { get; set; } = Normal;
    public Vector4 Tangent { get; set; } = Tangent;
    public Dictionary<int, float> Weights { get; set; } = Weights;
}