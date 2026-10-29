using System.Numerics;
using BfresLibrary;

namespace BfresToCast.Utils;

public static class ModelUtils
{
    /// <summary>
    /// Gets the rotation matrix of the specified bone
    /// </summary>
    /// <param name="bone">The bone ot get the matrix from</param>
    /// <returns>a 4x4 matrix transform only with the rotation</returns>
    public static Matrix4x4 GetRotationMatrix(Bone bone)
    {
        Matrix4x4 rotMatrix = Matrix4x4.Identity;

        if (bone.FlagsRotation != BoneFlagsRotation.Quaternion)
            rotMatrix = Matrix4x4.CreateRotationX(bone.Rotation.X) * Matrix4x4.CreateRotationY(bone.Rotation.Y) *
                        Matrix4x4.CreateRotationZ(bone.Rotation.Z);
        else
            rotMatrix = Matrix4x4.CreateFromQuaternion(new Quaternion(bone.Rotation.X, bone.Rotation.Y,
                bone.Rotation.Z, bone.Rotation.W));

        return rotMatrix;
    }

    /// <summary>
    /// Gets the local transformation of the specified Bone
    /// </summary>
    /// <param name="bone">The bone to get the transform of</param>
    /// <returns>The local transform of the bone</returns>
    public static Matrix4x4 GetLocalMatrix(Bone bone)
    {
        Matrix4x4 transMatrix = Matrix4x4.CreateTranslation(bone.Position.X, bone.Position.Y, bone.Position.Z);
        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(bone.Scale.X, bone.Scale.Y, bone.Scale.Z);
        Matrix4x4 rotMatrix = GetRotationMatrix(bone);

        Matrix4x4 localMatrix = scaleMatrix * rotMatrix * transMatrix;
        return localMatrix;
    }

    /// <summary>
    /// Gets the world matrix of the specified Bone
    /// </summary>
    /// <param name="bone">The bone to get the world transform of</param>
    /// <param name="skeleton">The skeleton the bone belongs to</param>
    /// <returns>The local transform of the bone</returns>
    public static Matrix4x4 GetWorldMatrix(Bone bone, Skeleton skeleton)
    {
        Matrix4x4 worldMatrix = GetLocalMatrix(bone);
        int currentParent = bone.ParentIndex;
        while (currentParent != -1)
        {
            worldMatrix *= GetLocalMatrix(skeleton.BoneList[currentParent]);
            currentParent = skeleton.BoneList[currentParent].ParentIndex;
        }

        return worldMatrix;
    }
}