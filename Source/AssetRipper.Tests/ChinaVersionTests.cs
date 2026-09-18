using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Metadata;
using AssetRipper.Import.AssetCreation;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.IO.Files;
using AssetRipper.IO.Files.SerializedFiles;
using AssetRipper.Primitives;
using AssetRipper.Processing.Scenes;
using AssetRipper.SourceGenerated.Classes.ClassID_141;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Tests;

/// <summary>
/// Chinese Unity builds (versions with a "c" type, eg 2022.3.20f1c1) are based on a much later engine than their
/// version number suggests, so their serialized layouts can differ from international builds with the same version number.
/// </summary>
/// <remarks>
/// The test data is the raw object data from a game built with Unity 2022.3.20f1c1.
/// </remarks>
internal class ChinaVersionTests
{
	private static readonly UnityVersion China2022_3_20 = new(2022, 3, 20, UnityVersionType.China, 1);
	private static readonly UnityVersion China2022_3_62 = new(2022, 3, 62, UnityVersionType.China, 1);

	[Test]
	public void ChinaBuildSettingsReadsWithAuthToken()
	{
		ProcessedAssetCollection collection = CreateChinaCollection();
		GameAssetFactory factory = new(new BaseManager(_ => { }));

		IUnityObjectBase asset = factory.ReadAsset(new AssetInfo(collection, 1, 141), Convert.FromHexString(BuildSettingsHex), null);

		Assert.That(asset, Is.InstanceOf<BuildSettings_2022_3_52>());
		BuildSettings_2022_3_52 buildSettings = (BuildSettings_2022_3_52)asset!;
		Assert.That(buildSettings.Scenes.Count, Is.EqualTo(3));
		Assert.That(buildSettings.Scenes[0].String, Is.EqualTo("Assets/Scenes/VillageScene.unity"));
		Assert.That(buildSettings.AuthToken!.String, Is.EqualTo("2483993"));
		Assert.That(buildSettings.GraphicsAPIs.Count, Is.EqualTo(1));
		Assert.That(buildSettings.GraphicsAPIs[0], Is.EqualTo(2));
	}

	[Test]
	public void ChinaBuildSettings62ReadsWithExtraField()
	{
		ProcessedAssetCollection collection = CreateChinaCollection(China2022_3_62);
		GameAssetFactory factory = new(new BaseManager(_ => { }));

		IUnityObjectBase asset = factory.ReadAsset(new AssetInfo(collection, 1, 141), Convert.FromHexString(BuildSettings62Hex), null);

		Assert.That(asset, Is.InstanceOf<TypeTreeObject>());
		TypeTreeObject typeTree = (TypeTreeObject)asset!;
		Assert.That(typeTree.ReleaseFields["scenes"].AsStringArray, Is.EqualTo(new[] { "Assets/Scenes/Main.unity", "Assets/Altar/DarkEmpireScene.unity" }));
		Assert.That(typeTree.ReleaseFields["m_Version"].AsString, Is.EqualTo("2022.3.62f1c1"));
		Assert.That(typeTree.ReleaseFields["m_ChinaExtraString"].AsString, Is.EqualTo("eef112ed46381c9df4c4743b03c1a1757ab16f4172f3ee9ca9a5011a2923bf3e90582af80f6c3376"));
		Assert.That(typeTree.ReleaseFields["m_AuthToken"].AsString, Is.EqualTo("6438845"));
		Assert.That(typeTree.ReleaseFields["m_GraphicsAPIs"].AsInt32Array, Is.EqualTo(new[] { 2 }));
	}

	[Test]
	public void ChinaBuildSettingsScenesFallback()
	{
		GameAssetFactory factory = new(new BaseManager(_ => { }));

		IUnityObjectBase typedAsset = factory.ReadAsset(new AssetInfo(CreateChinaCollection(China2022_3_20), 1, 141), Convert.FromHexString(BuildSettingsHex), null);
		Assert.That(SceneHelpers.TryGetScenes(typedAsset, out IReadOnlyList<Utf8String>? typedScenes), Is.True);
		Assert.That(typedScenes.Select(scene => scene.String), Is.EqualTo(new[] { "Assets/Scenes/VillageScene.unity", "Assets/Scenes/GameScene.unity", "Assets/Scenes/LoadingScene.unity" }));

		IUnityObjectBase treeAsset = factory.ReadAsset(new AssetInfo(CreateChinaCollection(China2022_3_62), 1, 141), Convert.FromHexString(BuildSettings62Hex), null);
		Assert.That(SceneHelpers.TryGetScenes(treeAsset, out IReadOnlyList<Utf8String>? treeScenes), Is.True);
		Assert.That(treeScenes.Select(scene => scene.String), Is.EqualTo(new[] { "Assets/Scenes/Main.unity", "Assets/Altar/DarkEmpireScene.unity" }));
	}

	[Test]
	public void ChinaUnityConnectSettingsReadsWithChinaUrls()
	{
		ProcessedAssetCollection collection = CreateChinaCollection();
		GameAssetFactory factory = new(new BaseManager(_ => { }));

		IUnityObjectBase asset = factory.ReadAsset(new AssetInfo(collection, 1, 310), Convert.FromHexString(UnityConnectSettingsHex), null);

		Assert.That(asset, Is.InstanceOf<TypeTreeObject>());
		TypeTreeObject typeTree = (TypeTreeObject)asset!;
		Assert.That(typeTree.ReleaseFields["m_Enabled"].AsBoolean, Is.True);
		Assert.That(typeTree.ReleaseFields["m_EventOldUrl"].AsString, Is.EqualTo("https://api.uca.cloud.unity3d.com/v1/events"));
		Assert.That(typeTree.ReleaseFields["m_EventUrl"].AsString, Is.EqualTo("https://cdp.cloud.unity3d.com/v1/events"));
		Assert.That(typeTree.ReleaseFields["m_ConfigUrl"].AsString, Is.EqualTo("https://config.uca.cloud.unity3d.com"));
		Assert.That(typeTree.ReleaseFields["m_DashboardUrl"].AsString, Is.EqualTo("https://dashboard.unity3d.com"));
		Assert.That(typeTree.ReleaseFields["m_ChinaEventUrl"].AsString, Is.EqualTo("https://cdp.cloud.unity.cn/v1/events"));
		Assert.That(typeTree.ReleaseFields["m_ChinaConfigUrl"].AsString, Is.EqualTo("https://cdp.cloud.unity.cn/config"));
		Assert.That(typeTree.ReleaseFields["m_TestInitMode"].AsInt32, Is.Zero);
	}

	private static ProcessedAssetCollection CreateChinaCollection()
	{
		return CreateChinaCollection(China2022_3_20);
	}

	private static ProcessedAssetCollection CreateChinaCollection(UnityVersion version)
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(version);
		collection.SetLayout(version, BuildTarget.NoTarget, TransferInstructionFlags.SerializeGameRelease);
		return collection;
	}

	private const string BuildSettingsHex =
		"03000000200000004173736574732F5363656E65732F56696C6C616765536365" +
		"6E652E756E6974791D0000004173736574732F5363656E65732F47616D655363" +
		"656E652E756E697479000000200000004173736574732F5363656E65732F4C6F" +
		"6164696E675363656E652E756E69747900000000000000000000000000010000" +
		"0100000001010100010100000D000000323032322E332E323066316331000000" +
		"0700000032343833393933000100000002000000";

	private const string BuildSettings62Hex =
		"02000000180000004173736574732F5363656E65732F4D61696E2E756E697479" +
		"220000004173736574732F416C7461722F4461726B456D706972655363656E65" +
		"2E756E6974790000000000000000000000000000000100000000000101010001" +
		"010100000D000000323032322E332E3632663163310000005000000065656631" +
		"3132656434363338316339646634633437343362303363316131373537616231" +
		"3666343137326633656539636139613530313161323932336266336539303538" +
		"3261663830663663333337360700000036343338383435000100000002000000";

	private const string UnityConnectSettingsHex =
		"010000002B00000068747470733A2F2F6170692E7563612E636C6F75642E756E" +
		"69747933642E636F6D2F76312F6576656E7473002700000068747470733A2F2F" +
		"6364702E636C6F75642E756E69747933642E636F6D2F76312F6576656E747300" +
		"2400000068747470733A2F2F636F6E6669672E7563612E636C6F75642E756E69" +
		"747933642E636F6D1D00000068747470733A2F2F64617368626F6172642E756E" +
		"69747933642E636F6D0000002400000068747470733A2F2F6364702E636C6F75" +
		"642E756E6974792E636E2F76312F6576656E74732100000068747470733A2F2F" +
		"6364702E636C6F75642E756E6974792E636E2F636F6E66696700000000000000" +
		"2200000068747470733A2F2F706572662D6576656E74732E636C6F75642E756E" +
		"6974792E636E0000000000000A00000000000000010001000001000000000000" +
		"00000000";
}
