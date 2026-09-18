using AssetRipper.Assets.Metadata;
using AssetRipper.Import.Structure.Assembly.Serializable;
using AssetRipper.Import.Structure.Assembly.TypeTrees;
using AssetRipper.IO.Files.SerializedFiles;
using AssetRipper.Primitives;
using AssetRipper.SourceGenerated;

namespace AssetRipper.Import.AssetCreation;

/// <summary>
/// Creates a BuildSettings asset for newer Chinese Unity builds (eg 2022.3.62f1c1).
/// </summary>
/// <remarks>
/// These builds contain an extra string field between m_Version and m_AuthToken that exists in no international
/// layout. Only its position and string shape are known, so the node is cloned from the neighboring m_AuthToken node.
/// </remarks>
internal static class ChinaBuildSettingsTree
{
	/// <summary>
	/// The international layout on which the China fork's BuildSettings is based.
	/// </summary>
	private static readonly UnityVersion BaseLayoutVersion = new(2022, 3, 52, UnityVersionType.Final, 1);

	public static TypeTreeObject Create(AssetInfo assetInfo)
	{
		if (TypeTreeNodeStruct.TryMakeFromTpk(ClassIDType.BuildSettings, BaseLayoutVersion, out TypeTreeNodeStruct releaseRoot, out TypeTreeNodeStruct editorRoot))
		{
			return TypeTreeObject.Create(assetInfo, InsertExtraStringField(releaseRoot), InsertExtraStringField(editorRoot), ITypeResolver.Null);
		}
		else
		{
			throw new InvalidOperationException($"Could not find the BuildSettings type tree for version {BaseLayoutVersion}.");
		}
	}

	private static TypeTreeNodeStruct InsertExtraStringField(TypeTreeNodeStruct root)
	{
		TypeTreeNodeStruct[] subNodes = root.SubNodes.ToArray();
		int insertionPoint = Array.FindIndex(subNodes, static node => node.Name == "m_AuthToken");
		if (insertionPoint < 0)
		{
			// The base layout doesn't match expectations, so just use it as is.
			return root;
		}

		TypeTreeNodeStruct template = subNodes[insertionPoint];
		TypeTreeNodeStruct[] newSubNodes = new TypeTreeNodeStruct[subNodes.Length + 1];
		Array.Copy(subNodes, 0, newSubNodes, 0, insertionPoint);
		newSubNodes[insertionPoint] = new TypeTreeNodeStruct(template.TypeName, "m_ChinaExtraString", template.Version, template.MetaFlag, template.SubNodes.ToArray());
		Array.Copy(subNodes, insertionPoint, newSubNodes, insertionPoint + 1, subNodes.Length - insertionPoint);
		return new TypeTreeNodeStruct(root.TypeName, root.Name, root.Version, root.MetaFlag, newSubNodes);
	}
}
