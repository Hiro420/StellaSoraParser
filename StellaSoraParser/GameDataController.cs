using System.Text;

namespace StellaSoraParser;

// Thanks to all the retards at YostarGames for making their game half-opensource
public class GameDataController
{
	private int _magicKey;
	private string _binVersion;

	public Dictionary<object, byte[]> LoadCommonBinData(byte[] array)
	{
		var result = new Dictionary<object, byte[]>();

		using (MemoryStream memoryStream = new MemoryStream(array))
		using (BinaryReader binaryReader = new BinaryReader(memoryStream))
		{
			if (binaryReader.ReadInt32() != this._magicKey)
				return result;

			int versionLength = binaryReader.ReadInt16();
			byte[] versionBytes = binaryReader.ReadBytes(versionLength);
			string version = Encoding.UTF8.GetString(versionBytes);
			if (version != this._binVersion)
				return result;

			bool hasPrimaryKey = binaryReader.ReadByte() == 1;
			byte keyType = binaryReader.ReadByte(); // 0 = string, 1 = int, 2 = long
			int entryCount = binaryReader.ReadInt32();

			if (hasPrimaryKey)
			{
				if (keyType == 1)
				{
					for (int i = 0; i < entryCount; i++)
					{
						int key = binaryReader.ReadInt32();
						int dataLength = binaryReader.ReadInt16();
						byte[] data = binaryReader.ReadBytes(dataLength);
						result[key] = data;
					}
				}
				else if (keyType == 2)
				{
					for (int i = 0; i < entryCount; i++)
					{
						long key = binaryReader.ReadInt64();
						int dataLength = binaryReader.ReadInt16();
						byte[] data = binaryReader.ReadBytes(dataLength);
						result[key] = data;
					}
				}
				else
				{
					for (int i = 0; i < entryCount; i++)
					{
						int keyLength = binaryReader.ReadInt16();
						string key = Encoding.UTF8.GetString(binaryReader.ReadBytes(keyLength));
						int dataLength = binaryReader.ReadInt16();
						byte[] data = binaryReader.ReadBytes(dataLength);
						result[key] = data;
					}
				}
			}
			else
			{
				for (int i = 0; i < entryCount; i++)
				{
					int dataLength = binaryReader.ReadInt16();
					byte[] data = binaryReader.ReadBytes(dataLength);
					result[i] = data; // use index as key for no-primary-key entries
				}
			}
		}

		return result;
	}

	public GameDataController()
	{
		this._magicKey = 234324;
		this._binVersion = "0.1.0.2";
	}
}
