using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Export.UnityProjects.Project;
using AssetRipper.Import.Configuration;
using AssetRipper.Processing.Scenes;
using System.Diagnostics;


namespace AssetRipper.Export.UnityProjects;

public class ProjectAssetContainer : IExportContainer
{
	public ProjectAssetContainer(ProjectExporter exporter, CoreConfiguration options, IEnumerable<IUnityObjectBase> assets,
		IReadOnlyList<IExportCollection> collections)
	{
		m_exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
		CurrentCollection = null!;

		ExportVersion = options.Version;

		IReadOnlyList<Utf8String>? buildScenes = null;
		foreach (IUnityObjectBase asset in assets)
		{
			if (SceneHelpers.TryGetScenes(asset, out IReadOnlyList<Utf8String>? foundScenes))
			{
				buildScenes = foundScenes;
			}
		}
		m_buildScenes = buildScenes;

		List<SceneExportCollection> scenes = new();
		foreach (IExportCollection collection in collections)
		{
			foreach (IUnityObjectBase asset in collection.Assets)
			{
				CheckIfAlreadyAdded(this, asset, collection);
				m_assetCollections.Add(asset, collection);
			}
			if (collection is SceneExportCollection scene)
			{
				scenes.Add(scene);
			}
		}
		m_scenes = scenes.ToArray();

		[Conditional("DEBUG")]
		static void CheckIfAlreadyAdded(ProjectAssetContainer container, IUnityObjectBase asset, IExportCollection currentCollection)
		{
			if (container.m_assetCollections.TryGetValue(asset, out IExportCollection? previousCollection))
			{
				throw new ArgumentException($"Asset {asset} is already added by {previousCollection}");
			}
		}
	}

	public long GetExportID(IUnityObjectBase asset)
	{
		if (m_assetCollections.TryGetValue(asset, out IExportCollection? collection))
		{
			return collection.GetExportID(this, asset);
		}

		return ExportIdHandler.GetMainExportID(asset);
	}

	public AssetType ToExportType(Type type)
	{
		return m_exporter.ToExportType(type);
	}

	public MetaPtr CreateExportPointer(IUnityObjectBase asset)
	{
		if (m_assetCollections.TryGetValue(asset, out IExportCollection? collection))
		{
			return collection.CreateExportPointer(this, asset, collection == CurrentCollection);
		}

		return MetaPtr.CreateMissingReference(asset.ClassID, AssetType.Meta);
	}

	public UnityGuid ScenePathToGUID(string path)
	{
		foreach (SceneExportCollection scene in m_scenes)
		{
			if (scene.Scene.Path == path)
			{
				return scene.GUID;
			}
		}
		return default;
	}

	public bool IsSceneDuplicate(int sceneIndex) => SceneHelpers.IsSceneDuplicate(sceneIndex, m_buildScenes);

	public IExportCollection CurrentCollection { get; set; }
	public AssetCollection File => CurrentCollection.File;
	public UnityVersion ExportVersion { get; }

	private readonly ProjectExporter m_exporter;
	private readonly Dictionary<IUnityObjectBase, IExportCollection> m_assetCollections = new();

	private readonly IReadOnlyList<Utf8String>? m_buildScenes;
	private readonly SceneExportCollection[] m_scenes;
}
