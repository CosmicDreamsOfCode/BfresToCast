using System.Numerics;
using BfresLibrary;
using BfresLibrary.Helpers;
using BfresToCast.Utils;
using Cast.NET;
using Cast.NET.Nodes;
using Syroot.BinaryData;

namespace BfresToCast.Processors;

public static class ModelProcessor
{
    public static bool Process(Model model, string outDir, ByteOrder byteOrder)
    {
        CastNode root = new CastNode(CastNodeIdentifier.Root);
        ModelNode modelNode = root.AddNode<ModelNode>();
        SkeletonNode skeletonNode = modelNode.AddNode<SkeletonNode>();
        ConsumeSkin(skeletonNode, model);
        Quaternion rot = Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateRotationX(1.5708f));
        modelNode.AddValue("r", new Vector4(rot.X, rot.Y, rot.Z, rot.W));

        List<ulong> materialHashes = ConsumeMaterials(model, modelNode);

        foreach (Shape shape in model.Shapes.Values)
        {
            MeshNode meshNode = new MeshNode();
            meshNode.AddString("n", shape.Name);
            meshNode.AddValue("m", materialHashes[shape.MaterialIndex]);

            RawShapeInfo rawShape = GetRawShape(shape, model, byteOrder);

            // we consume the weights so early to populate VertexInfo's weight values
            // they aren't populated until after we consume the weights,
            // since we don't process RawShapeInfo weights(apart from the buffer) until this
            ConsumeWeights(model, rawShape, meshNode);

            // If this is the case, we need to move sections of this mesh to the transforms of the bones 
            if (shape.VertexSkinCount == 1)
            {
                foreach (ushort boneIndex in shape.SkinBoneIndices)
                {
                    if (model.Skeleton.BoneList[boneIndex].RigidMatrixIndex == -1)
                        continue;

                    MoveVertexToBone(boneIndex);
                }
            }
            else if (shape.VertexSkinCount == 0) // The above applies here too, only we do it based on BoneIndex
            {
                int boneIndex = shape.BoneIndex;
                MoveVertexToBone(boneIndex);
            }

            AddMeshNodeArrays(meshNode, rawShape.VertexInfos);

            int i = 0; // work around because the names of the UVs(and possibly colors) are incorrect sometimes
            // e.g. you can have UV 0 then UV 2, with no UV 1 in between
            meshNode.AddValue("ul", (byte)rawShape.Uvs.Count);
            foreach (KeyValuePair<byte, List<Vector2>> uv in rawShape.Uvs)
            {
                meshNode.AddArray($"u{i}", uv.Value.ToArray());
                i++;
            }
            meshNode.AddValue("cl", (byte)rawShape.VertexColors.Count);

            i = 0;
            foreach (KeyValuePair<byte, List<Vector4>> vertexColor in rawShape.VertexColors)
            {
                meshNode.AddArray($"c{i}", vertexColor.Value);
                i++;
            }

            ConsumeFaces(meshNode, rawShape);
            meshNode.Parent = modelNode;
            continue;

            // Transforms a vertex via the world transform(position relative to world origin) of the bone.
            // Gets the world transform via GetWorldMatrix(Bone, Skeleton)
            void MoveVertexToBone(int boneIndex)
            {
                Bone bone = model.Skeleton.BoneList[boneIndex];

                Matrix4x4 boneMatrix = ModelUtils.GetWorldMatrix(bone, model.Skeleton);
                foreach (VertexInfo vertexInfo in rawShape.GetVertexesWithWeight(boneIndex))
                {
                    vertexInfo.Position = Vector3.Transform(vertexInfo.Position, boneMatrix);
                    vertexInfo.Normal = Vector3.TransformNormal(vertexInfo.Normal, boneMatrix);
                    vertexInfo.Tangent = Vector4.Transform(vertexInfo.Tangent, boneMatrix);
                }
            }
        }

        CastWriter.Save($@"{outDir}/{model.Name}.cast", root);
        Console.WriteLine($"Saved model : {model.Name}");

        return true;
    }
    
    private static void AddMeshNodeArrays(MeshNode node, List<VertexInfo> vertexInfos)
    {
        node.AddArray("vp", vertexInfos.Select(info => info.Position));
        node.AddArray("vn", vertexInfos.Select(info => info.Normal));
        node.AddArray("vt", vertexInfos.Select(info => info.Tangent));
    }
    
    #region Consumption

    /// <summary>
    /// Indexes the identifiers used by Cast for the materials and adds the materials from the Model into the ModelNode
    /// </summary>
    /// <param name="model"></param>
    /// <param name="modelNode"></param>
    /// <returns>A list containing the identifiers of the material nodes.
    /// Each index corresponds with the index of the material in the model</returns>
    static List<ulong> ConsumeMaterials(Model model, ModelNode modelNode)
    {
        List<ulong> materialHashes = new List<ulong>();

        foreach (Material material in model.Materials.Values)
        {
            MaterialNode materialNode = new MaterialNode(material.Name, "pbr");
            materialHashes.Add(materialNode.Hash);
            materialNode.Parent = modelNode;
        }

        return materialHashes;
    }

    /// <summary>
    /// Parses the weights from the model and rawShape into the meshNode
    /// </summary>
    /// <param name="model">The model which contains the shape</param>
    /// <param name="rawShape">The RawShapeInfo containing the data for the shape</param>
    /// <param name="meshNode">The mesh node to parse the results into</param>
    static void ConsumeWeights(Model model, RawShapeInfo rawShape, MeshNode meshNode)
    {
        int boneSets = 1; // TODO: is it possible for models to have more than 1 boneset?
        CastArrayProperty<float> boneWeights = meshNode.AddArray<float>("wv", new List<float>(boneSets * rawShape.Verticies.Count));
        meshNode.AddValue("mi", Math.Max(rawShape.Shape.VertexSkinCount, (byte)1)); // safety feature to ensure

        // We allocate bone indices depending on the type
        CastProperty boneIndices;
        if (model.Skeleton.BoneList.Count <= 0xFF)
            boneIndices = meshNode.AddArray<byte>("wb", new(boneSets * rawShape.Verticies.Count));
        else if (model.Skeleton.BoneList.Count <= 0xFFFF)
            boneIndices = meshNode.AddArray<ushort>("wb", new(boneSets * rawShape.Verticies.Count));
        else
            boneIndices = meshNode.AddArray<uint>("wb", new(boneSets * rawShape.Verticies.Count));

        if (rawShape.Shape.VertexSkinCount == 0)
        {
            for (int i = 0; i < rawShape.Verticies.Count; i++)
            {
                int boneIndex = rawShape.Shape.BoneIndex;

                if (boneIndices is CastArrayProperty<byte> bb)
                {
                    bb.Add((byte)boneIndex);
                }
                else if (boneIndices is CastArrayProperty<ushort> bs)
                {
                    bs.Add((ushort)boneIndex);
                }
                else if (boneIndices is CastArrayProperty<uint> bi)
                {
                    bi.Add((uint)boneIndex);
                }

                rawShape.VertexInfos[i].Weights.Add(boneIndex, 1);
            }
            return;
        }

        for (int i = 0; i < rawShape.BlendIndices.Count; i++)
        {
            Vector4 bone = rawShape.BlendIndices[i];
            Vector4 weight;
            if (i < rawShape.BlendWeights.Count)
                weight = rawShape.BlendWeights[i];
            else
                weight = new Vector4(1, 0, 0, 0);

            if (boneIndices is CastArrayProperty<byte> bb)
            {
                for (int j = 0; j < rawShape.Shape.VertexSkinCount; j++)
                {
                    bb.Values.Add((byte)model.Skeleton.MatrixToBoneList[(int)bone[j]]); // TODO: Fix skin counts > 4
                }
            }
            else if (boneIndices is CastArrayProperty<ushort> bs)
            {
                for (int j = 0; j < rawShape.Shape.VertexSkinCount; j++)
                {
                    bs.Values.Add(model.Skeleton.MatrixToBoneList[(int)bone[j]]);
                }
            }
            else if (boneIndices is CastArrayProperty<uint> bi)
            {
                for (int j = 0; j < rawShape.Shape.VertexSkinCount; j++)
                {
                    bi.Values.Add(model.Skeleton.MatrixToBoneList[(int)bone[j]]);
                }
            }

            void AddBoneWeight(ushort boneId, float weightParam)
            {
                boneWeights.Add(weightParam);
                VertexInfo info = rawShape.VertexInfos[i];
                if (info.Weights.ContainsKey(boneId))
                    return;

                info.Weights.Add(boneId, weightParam);
            }

            for (int j = 0; j < rawShape.Shape.VertexSkinCount; j++)
            {
                if (rawShape.BlendWeights.Count != 0)
                    AddBoneWeight(model.Skeleton.GetBoneIndex(bone[j]), weight[j]);
                else
                    AddBoneWeight(model.Skeleton.GetBoneIndex(bone[j]), 1);
            }
        }
    }

    /// <summary>
    /// Parses the Indicies from the rawShape into the specified MeshNode
    /// </summary>
    /// <param name="root"></param>
    /// <param name="rawShape"></param>
    static void ConsumeFaces(MeshNode root, RawShapeInfo rawShape)
    {
        int faceCount = rawShape.Indicies.Count;
        if (rawShape.Verticies.Count < 0xff)
        {
            CastArrayProperty<byte> faceIndices = root.AddArray<byte>("f", new(faceCount));

            for (int i = 0; i < faceCount / 3; i++)
            {
                var i0 = rawShape.Indicies[i * 3 + 0];
                var i1 = rawShape.Indicies[i * 3 + 1];
                var i2 = rawShape.Indicies[i * 3 + 2];

                if (i0 != i1 && i1 != i2 && i2 != i0)
                {
                    faceIndices.Add((byte)i0);
                    faceIndices.Add((byte)i1);
                    faceIndices.Add((byte)i2);
                }
            }
        }
        else if (rawShape.Verticies.Count <= 0xFFFF)
        {
            var faceIndices = root.AddArray<ushort>("f", new(faceCount));

            for (int i = 0; i < faceCount / 3; i++)
            {
                var i0 = rawShape.Indicies[i * 3 + 0];
                var i1 = rawShape.Indicies[i * 3 + 1];
                var i2 = rawShape.Indicies[i * 3 + 2];

                if (i0 != i1 && i1 != i2 && i2 != i0)
                {
                    faceIndices.Add((ushort)i0);
                    faceIndices.Add((ushort)i1);
                    faceIndices.Add((ushort)i2);
                }
            }
        }
        else
        {
            CastArrayProperty<uint> faceIndices = root.AddArray<uint>("f", new(faceCount));

            for (int i = 0; i < faceCount / 3; i++)
            {
                var i0 = rawShape.Indicies[i * 3 + 0];
                var i1 = rawShape.Indicies[i * 3 + 1];
                var i2 = rawShape.Indicies[i * 3 + 2];

                if (i0 != i1 && i1 != i2 && i2 != i0)
                {
                    faceIndices.Add((uint)i0);
                    faceIndices.Add((uint)i1);
                    faceIndices.Add((uint)i2);
                }
            }
        }
    }

    /// <summary>
    ///  Parses data from the model's skeleton into one of Cast's SkeletonNodes.
    /// Used to convert the model's skeleton and all its bones into cast bones which are inserted into the SkeletonNode
    /// </summary>
    /// <param name="skeleton"></param>
    /// <param name="model"></param>
    static void ConsumeSkin(SkeletonNode skeleton, Model model)
    {
        //check scaling mode used for the skeleton
        byte segmentScaleCompensate = 1;
        if (model.Skeleton.FlagsScaling == SkeletonFlagsScaling.None || model.Skeleton.FlagsScaling == SkeletonFlagsScaling.Standard)
        {
            segmentScaleCompensate = 0;
        }

        foreach (Bone bone in model.Skeleton.BoneList)
        {
            BoneNode boneNode = skeleton.AddNode<BoneNode>();

            boneNode.AddString("n", bone.Name);
            boneNode.AddValue("p", (uint)bone.ParentIndex);

            Matrix4x4 worldMatrix = ModelUtils.GetWorldMatrix(bone, model.Skeleton);
            Quaternion worldRotation = Quaternion.CreateFromRotationMatrix(GetWorldRotationMatrix(bone, model.Skeleton));

            boneNode.AddValue("wp", Vector3.Transform(Vector3.Zero, worldMatrix));
            boneNode.AddValue("wr", new Vector4(worldRotation.X, worldRotation.Y, worldRotation.Z, worldRotation.W));
            boneNode.AddValue("ssc", segmentScaleCompensate);
        }

        skeleton.CalculateLocalTransforms();
    }

    /// <summary>
    /// Gets the world rotation as a Matrix4x4 of the specified bone
    /// </summary>
    /// <param name="bone">The bone ot get the matrix from</param>
    /// <param name="skeleton">The skeleton the bone belongs to</param>
    /// <returns>a 4x4 matrix transform only with the rotation</returns>
    static Matrix4x4 GetWorldRotationMatrix(Bone bone, Skeleton skeleton)
    {
        Matrix4x4 rotMatrix = ModelUtils.GetRotationMatrix(bone);
        int currentParent = bone.ParentIndex;
        while (currentParent != -1)
        {
            rotMatrix *= ModelUtils.GetRotationMatrix(skeleton.BoneList[currentParent]);
            currentParent = skeleton.BoneList[currentParent].ParentIndex;
        }

        return rotMatrix;
    }

    #endregion

    /// <summary>
    /// Gets the raw data from the vertex buffers in a nicely organized fashion
    /// </summary>
    /// <param name="shape">The shape to get the data from</param>
    /// <param name="model">The model the shape belongs to</param>
    /// <param name="resFile">The res file the model belongs to</param>
    /// <returns></returns>
    private static RawShapeInfo GetRawShape(Shape shape, Model model, ByteOrder byteOrder)
    {
        RawShapeInfo rawShape = new RawShapeInfo(shape);
        rawShape.Indicies.AddRange(shape.Meshes[0].GetIndices().Select(u => (int)u));

        VertexBufferHelper helper = new VertexBufferHelper(model.VertexBuffers[shape.VertexBufferIndex], byteOrder);
        foreach (VertexBufferHelperAttrib attribute in helper.Attributes)
        {
            switch (attribute.Name)
            {
                case "_i0":
                {
                    rawShape.BlendIndices.AddRange(attribute.Data.Select(vector4F => new Vector4(vector4F.X, vector4F.Y, vector4F.Z, vector4F.W)));
                }
                    break;
                case "_w0":
                {
                    rawShape.BlendWeights.AddRange(attribute.Data.Select(vector4F => new Vector4(vector4F.X, vector4F.Y, vector4F.Z, vector4F.W)));
                }
                    break;
                case "_p0":
                {
                    rawShape.Verticies.AddRange(attribute.Data.Select(vector4F => new Vector3(vector4F.X, vector4F.Y, vector4F.Z)));
                }
                    break;
                case "_n0":
                {
                    rawShape.Normals.AddRange(attribute.Data.Select(vector4F => new Vector3(vector4F.X, vector4F.Y, vector4F.Z)));
                }
                    break;
                case "_t0":
                {
                    rawShape.Tangents.AddRange(attribute.Data.Select(vector4F => new Vector4(vector4F.X, vector4F.Y, vector4F.Z, vector4F.W)));
                }
                    break;
                default:
                {
                    if (attribute.Name.StartsWith("_u"))
                    {
                        byte uv = byte.Parse(attribute.Name[2..]);
                        if (!rawShape.Uvs.ContainsKey(uv))
                            rawShape.Uvs.Add(uv, new List<Vector2>());

                        rawShape.Uvs[uv].AddRange(attribute.Data.Select(vector4F => new Vector2(vector4F.X, vector4F.Y)));
                    }
                    else if (attribute.Name.StartsWith("_c"))
                    {
                        byte color = byte.Parse(attribute.Name[2..]);
                        if (!rawShape.VertexColors.ContainsKey(color))
                            rawShape.VertexColors.Add(color, new List<Vector4>());

                        rawShape.VertexColors[color].AddRange(attribute.Data.Select(f => new Vector4(f.X, f.Y, f.Z, f.W)));
                    }
                }
                    break;
            }
        }

        rawShape.CreateVertexInfos();
        return rawShape;
    }
}