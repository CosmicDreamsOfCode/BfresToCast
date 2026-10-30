using System.Numerics;

namespace BfresToCast;

/// <summary>
/// All the information of a vertex compiled into a single record, including all of its bone weights.
/// </summary>
public record VertexInfo(int Idx, Vector3 Position, Vector3 Normal, Vector4 Tangent, Dictionary<int, float> Weights)
{
    public record ModelVertData(Vector3 Position, Vector3 Normal, Vector4 Tangent)
    {
        public Vector3 Position { get; set; } = Position;
        public Vector3 Normal { get; set; } = Normal;
        public Vector4 Tangent { get; set; } = Tangent;

        public void Transform(Matrix4x4 trans)
        {
            Position = Vector3.Transform(Position, trans);
            Normal = Vector3.TransformNormal(Normal, trans);
            Tangent = Vector4.Transform(Tangent, trans);
        }
    }

    public ModelVertData BaseData { get; init; } = new ModelVertData(Position, Normal, Tangent);

    public Vector3 Position
    {
        get => BaseData.Position;
        set => BaseData.Position = value;
    }

    public Vector3 Normal
    {
        get => BaseData.Normal;
        set => BaseData.Normal = value;
    }

    public Vector4 Tangent
    {
        get => BaseData.Tangent;
        set => BaseData.Tangent = value;
    }
    
    public Dictionary<int, float> Weights { get; set; } = Weights;
    public Dictionary<string, ModelVertData> ShapeKeys { get; init; } = new Dictionary<string, ModelVertData>();

    public void Transform(Matrix4x4 trans)
    {
        BaseData.Transform(trans);
        foreach (string key in ShapeKeys.Keys)
        {
            ShapeKeys[key].Transform(trans);
        }
    }
}