using System.Text;

namespace PackageDowngrader
{
	internal class Program
	{
		private const string EXTENSION_PACKAGE = ".upk";
		private const string EXTENSION_MAP = ".gear";

		private const int VERSION_OFFSET = 4;
		private const int NAME_OFFSET = 12;
		private const int NAME_TO_GENS_OFFSET = 64;

		private const short VERSION = 828;
		private const short LICENSEE = 0;

		static void Main(string[] args)
		{
			var folder = args[0];

			Console.WriteLine($"Downgrading packages in {folder}");

			LoopFolder(folder);
		}

		static void LoopFolder(string folder_path)
		{
			foreach (var file in Directory.EnumerateFiles(folder_path))
			{
				Console.WriteLine($"Looping file {file}");
				if (Path.GetExtension(file) != EXTENSION_PACKAGE && Path.GetExtension(file) != EXTENSION_MAP)
				{
					continue;
				}

				DowngradeFile(file);
			}

			foreach (var folder in Directory.EnumerateDirectories(folder_path))
			{
				Console.WriteLine($"Looping folder {folder}");
				LoopFolder(folder);
			}
		}

		static void DowngradeFile(string path)
		{
			Console.WriteLine($"Downgrading {Path.GetFileName(path)} to {VERSION}");

			if (!File.Exists(path))
			{
				return;
			}


			using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
			{
				stream.Seek(VERSION_OFFSET, SeekOrigin.Begin);

				long saved_version_offset = -1;
				using (var reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
				{
					// Seek to the name because it's variable length
					stream.Seek(NAME_OFFSET, SeekOrigin.Begin);
					int length = reader.ReadInt32();
					stream.Seek(length + NAME_TO_GENS_OFFSET, SeekOrigin.Current);

					int count = reader.ReadInt32();
					stream.Seek((4 + 4 + 4) * count, SeekOrigin.Current);

					saved_version_offset = stream.Position;
				}

				using (var writer = new BinaryWriter(stream, Encoding.Default, leaveOpen: true))
				{
					stream.Seek(VERSION_OFFSET, SeekOrigin.Begin);
				
					int version = (LICENSEE << 16) | VERSION;
					writer.Write(version);
				
					stream.Seek(saved_version_offset, SeekOrigin.Begin);
					writer.Write(VERSION);
				
					writer.Flush();
				}
			}
		}
	}
}
