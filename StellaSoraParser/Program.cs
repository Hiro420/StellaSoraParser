using System.Reflection;
using System.Text;
using Google.Protobuf;
using Newtonsoft.Json;

namespace StellaSoraParser;

public class MainApp
{
	internal static readonly byte[] CRYPT_TEXT_ASSET_KEY = Encoding.UTF8.GetBytes("#$*`x&*)0L[~!@,/");
	internal static readonly string OutputPath = "BinData";

	public static void Main(string[] args)
	{
		GameDataController controller = new GameDataController();

		if (!Path.Exists(OutputPath))
		{
			Directory.CreateDirectory(OutputPath);
		}
		foreach (string subdir in new[] { "bin", "language", "text_data" })
			Directory.CreateDirectory(Path.Combine(OutputPath, subdir));

		if (args.Length < 1 || !Path.Exists(args[0]))
		{
			Console.WriteLine($"Usage: StellaSoraParser.exe <path to assets\\assetbundles\\data>");
			return;
		}

		string path = args[0];

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

				string json = JsonFormatter.Default.Format((IMessage)message);

				// got no native way to do it, so i have no other option...
				object reparsed = JsonConvert.DeserializeObject(json)!;
				keyValuePairs.Add(key, reparsed);

			}

			string asActualJson = JsonConvert.SerializeObject(keyValuePairs, Formatting.Indented);
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