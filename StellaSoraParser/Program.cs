using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace StellaSoraParser;

public class MainApp
{
	internal static readonly byte[] CRYPT_TEXT_ASSET_KEY = Encoding.UTF8.GetBytes("#$*`x&*)0L[~!@,/");
	internal static readonly string OutputPath = "BinData";
	internal static readonly string OutputPathLua = "Lua";

	public static void Main(string[] args)
	{
		GameDataController controller = new GameDataController();

		if (!Path.Exists(OutputPath))
		{
			Directory.CreateDirectory(OutputPath);
		}

		if (!Path.Exists(OutputPathLua))
		{
			Directory.CreateDirectory(OutputPathLua);
		}

		foreach (string subdir in new[] { "bin", "language", "text_data" })
			Directory.CreateDirectory(Path.Combine(OutputPath, subdir));

		if (args.Length < 1 || !Path.Exists(args[0]))
		{
			Console.WriteLine($"Usage: StellaSoraParser.exe <path to dumped assets\\assetbundles>");
			return;
		}

		string path = Path.Combine(args[0], "data");
		string luaPath = Path.Combine(args[0], "lua");

		JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
		// jsonSerializerSettings.Formatting = Formatting.Indented;
		jsonSerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());


		foreach (string fileFullPath in Directory.EnumerateFiles(luaPath, "*", SearchOption.AllDirectories))
		{
			byte[] decrypted = XXTeaHelper.Decrypt(File.ReadAllBytes(fileFullPath), CRYPT_TEXT_ASSET_KEY);
			string relativePath = Path.GetRelativePath(luaPath, fileFullPath);
			string outputPath = Path.Combine(OutputPathLua, relativePath.Replace(".bytes", ""));
			string? outputDir = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrEmpty(outputDir))
				Directory.CreateDirectory(outputDir);
			File.WriteAllBytes(outputPath, decrypted);
		}

		foreach (string filepath in Directory.GetFiles(Path.Combine(path, "bin")))
		{
			byte[] decrypted = XXTeaHelper.Decrypt(File.ReadAllBytes(filepath), CRYPT_TEXT_ASSET_KEY);
			string fileName2Proto = Path.GetFileNameWithoutExtension(filepath);
			string targetTypeName = $"StellaSoraParser.Proto.{fileName2Proto}";

			Dictionary<object, byte[]> parsed = controller.LoadCommonBinData(decrypted);
			Type type = Type.GetType(targetTypeName) ?? throw new Exception($"Type {targetTypeName} not found.");
			PropertyInfo parser = type.GetProperty("Parser") ?? throw new Exception($"Parser for {targetTypeName} not found.");
			MethodInfo parseMethod = parser.PropertyType.GetMethod(
				"ParseFrom",
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
				null,
				new[] { typeof(byte[]) },
				null
			) ?? throw new Exception($"Custom ParseFrom(byte[]) method not found.");
			object parserInstance = parser?.GetValue(null) ?? throw new Exception("Parser instance not found.");

			Dictionary<object, object> keyValuePairs = new Dictionary<object, object>();

			foreach (KeyValuePair<object, byte[]> kvp in parsed)
			{
				object key = kvp.Key;
				byte[] value = kvp.Value;

				object message = parseMethod.Invoke(parserInstance, new object[] { value })!;
				keyValuePairs.Add(key, message);
			}

			string asActualJson = JsonConvert.SerializeObject(keyValuePairs, Formatting.Indented, jsonSerializerSettings);
			string outputPath = Path.Combine(OutputPath, "bin", $"{fileName2Proto}.json");

			File.WriteAllText(outputPath, asActualJson);
		}

		foreach (string filepath in Directory.GetFiles(Path.Combine(path, "language")))
		{
			byte[] decrypted = XXTeaHelper.Decrypt(File.ReadAllBytes(filepath), CRYPT_TEXT_ASSET_KEY);
			string fileName = Path.GetFileName(filepath);
			string outputPath = Path.Combine(OutputPath, "language", fileName);
			File.WriteAllBytes(outputPath, decrypted);
		}

		foreach (string filepath in Directory.GetFiles(Path.Combine(path, "text_data", "bubble")))
		{
			byte[] decrypted = XXTeaHelper.Decrypt(File.ReadAllBytes(filepath), CRYPT_TEXT_ASSET_KEY);
			string fileName = Path.GetFileName(filepath);
			string outputPath = Path.Combine(OutputPath, "text_data", fileName);
			File.WriteAllBytes(outputPath, decrypted);
		}
	}
}