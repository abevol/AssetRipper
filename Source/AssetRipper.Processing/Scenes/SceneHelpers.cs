using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Import.AssetCreation;
using AssetRipper.SourceGenerated;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_1001;
using AssetRipper.SourceGenerated.Classes.ClassID_114;
using AssetRipper.SourceGenerated.Classes.ClassID_141;
using AssetRipper.SourceGenerated.Classes.ClassID_2;
using AssetRipper.SourceGenerated.Classes.ClassID_3;
using AssetRipper.SourceGenerated.Extensions;
using System.Text.RegularExpressions;

namespace AssetRipper.Processing.Scenes;

public static partial class SceneHelpers
{
	private const string AssetsName = "Assets/";
	private const string LibaryPackageCacheName = "Library/PackageCache/";
	private const string LevelName = "level";
	private const string MainSceneName = "maindata";

	public static bool TryGetFileNameToSceneIndex(string name, UnityVersion version, out int index)
	{
		if (HasMainData(version))
		{
			if (name == MainSceneName)
			{
				index = 0;
				return true;
			}

			if (SceneNameFormat.IsMatch(name))
			{
				index = int.Parse(name.AsSpan(LevelName.Length)) + 1;
				return true;
			}
		}
		else
		{
			if (SceneNameFormat.IsMatch(name))
			{
				index = int.Parse(name.AsSpan(LevelName.Length));
				return true;
			}
		}

		index = -1;
		return false;
	}

	/// <summary>
	/// Less than 5.3.0
	/// </summary>
	public static bool HasMainData(UnityVersion version) => version.LessThan(5, 3);

	/// <summary>
	/// GameObjects, Classes inheriting from LevelGameManager, MonoBehaviours with GameObjects, Components, and PrefabInstances
	/// </summary>
	public static bool IsSceneCompatible(IUnityObjectBase asset)
	{
		return asset switch
		{
			IGameObject => true,
			ILevelGameManager => true,
			IMonoBehaviour monoBeh => monoBeh.IsComponentOnGameObject(),
			IComponent => true,
			IPrefabInstance => true,
			_ => false,
		};
	}

	public static string SceneIndexToFileName(int index, UnityVersion version)
	{
		if (HasMainData(version))
		{
			if (index == 0)
			{
				return MainSceneName;
			}
			return $"{LevelName}{index - 1}";
		}
		return $"{LevelName}{index}";
	}

	public static bool TryGetScenePath(AssetCollection collection, [NotNullWhen(true)] IReadOnlyList<Utf8String>? scenes, [NotNullWhen(true)] out string? result)
	{
		if (scenes is not null && TryGetFileNameToSceneIndex(collection.Name, collection.OriginalVersion, out int index))
		{
			if (index >= scenes.Count)
			{
				//This can happen in the following situation:
				//1. A game is built with N scenes and published to a distribution platform.
				//2. One of the scenes is removed from the project, for whatever reason.
				//3. The game is built again, with the new scene list.
				//4. When updating the game, the developer forgets to delete the Nth scene file.
				//5. Now, there are N-1 scenes in the BuildSettings, but N scene files for AssetRipper to find.
				result = null;
				return false;
			}
			string scenePath = scenes[index].String;
			string extension = Path.GetExtension(scenePath);
			if (scenePath.StartsWith(AssetsName, StringComparison.Ordinal))
			{
				result = scenePath[..^extension.Length];
				return true;
			}
			else if (scenePath.StartsWith(LibaryPackageCacheName, StringComparison.Ordinal))
			{
				result = scenePath[..^extension.Length];
				return true;
			}
			else if (Path.IsPathRooted(scenePath))
			{
				// pull/uTiny 617
				// NOTE: absolute project path may contain Assets/ in its name so in this case we get incorrect scene path, but there is no way to bypass this issue
				int startIndex = scenePath.IndexOf(AssetsName);
				if (startIndex < 0)
				{
					startIndex = scenePath.IndexOf(LibaryPackageCacheName);
				}
				if (startIndex < 0)
				{
					result = null;
					return false;
				}
				result = scenePath[startIndex..^extension.Length];
				return true;
			}
			else if (scenePath.Length == 0)
			{
				// If a game is built without included scenes, Unity creates one with empty name.
				result = null;
				return false;
			}
			else
			{
				result = Path.Join("Assets", "Scenes", scenePath);
				return true;
			}
		}
		result = null;
		return false;
	}

	public static bool IsSceneDuplicate(int sceneIndex, IReadOnlyList<Utf8String>? scenes)
	{
		if (scenes == null)
		{
			return false;
		}

		string sceneName = scenes[sceneIndex].String;
		for (int i = 0; i < scenes.Count; i++)
		{
			if (scenes[i] == sceneName)
			{
				if (i != sceneIndex)
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Gets the scene list from a BuildSettings asset, whether it is a generated class
	/// or a <see cref="TypeTreeObject"/> fallback for layouts without a generated class.
	/// </summary>
	public static bool TryGetScenes(IUnityObjectBase asset, [NotNullWhen(true)] out IReadOnlyList<Utf8String>? scenes)
	{
		if (asset is IBuildSettings buildSettings)
		{
			scenes = buildSettings.Scenes;
			return true;
		}
		else if (asset is TypeTreeObject tree && tree.ClassID == (int)ClassIDType.BuildSettings && tree.ReleaseFields.ContainsField("scenes"))
		{
			scenes = tree.ReleaseFields["scenes"].AsStringArray.Select(scene => new Utf8String(scene)).ToArray();
			return true;
		}
		else
		{
			scenes = null;
			return false;
		}
	}

	[GeneratedRegex("^level(0|([1-9][0-9]*))$")]
	private static partial Regex SceneNameFormat { get; }
}
