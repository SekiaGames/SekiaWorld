using System;
using System.Collections.Generic;
using System.IO;

namespace ET
{
	public static class FileHelper
	{
		public static List<string> GetAllFiles(string dir, string searchPattern = "*")
		{
			List<string> list = new List<string>();
			GetAllFiles(list, dir, searchPattern);
			return list;
		}
		
		public static void GetAllFiles(List<string> files, string dir, string searchPattern = "*")
		{
			string[] fls = Directory.GetFiles(dir);
			foreach (string fl in fls)
			{
				files.Add(fl);
			}

			string[] subDirs = Directory.GetDirectories(dir);
			foreach (string subDir in subDirs)
			{
				GetAllFiles(files, subDir, searchPattern);
			}
		}
		
		public static void CleanDirectory(string dir)
		{
			if (!Directory.Exists(dir))
			{
				return;
			}
			foreach (string subdir in Directory.GetDirectories(dir))
			{
				Directory.Delete(subdir, true);		
			}

			foreach (string subFile in Directory.GetFiles(dir))
			{
				File.Delete(subFile);
			}
		}

		public static void CopyDirectory(string srcDir, string tgtDir)
		{
			DirectoryInfo source = new DirectoryInfo(srcDir);
			DirectoryInfo target = new DirectoryInfo(tgtDir);
	
			if (target.FullName.StartsWith(source.FullName, StringComparison.CurrentCultureIgnoreCase))
			{
				throw new Exception("父目录不能拷贝到子目录！");
			}
	
			if (!source.Exists)
			{
				return;
			}
	
			if (!target.Exists)
			{
				target.Create();
			}
	
			FileInfo[] files = source.GetFiles();
	
			for (int i = 0; i < files.Length; i++)
			{
				File.Copy(files[i].FullName, Path.Combine(target.FullName, files[i].Name), true);
			}
	
			DirectoryInfo[] dirs = source.GetDirectories();
	
			for (int j = 0; j < dirs.Length; j++)
			{
				CopyDirectory(dirs[j].FullName, Path.Combine(target.FullName, dirs[j].Name));
			}
		}
		
		public static void ReplaceExtensionName(string srcDir, string extensionName, string newExtensionName)
		{
			if (Directory.Exists(srcDir))
			{
				string[] fls = Directory.GetFiles(srcDir);

				foreach (string fl in fls)
				{
					if (fl.EndsWith(extensionName))
					{
						File.Move(fl, fl.Substring(0, fl.IndexOf(extensionName)) + newExtensionName);
						File.Delete(fl);
					}
				}

				string[] subDirs = Directory.GetDirectories(srcDir);

				foreach (string subDir in subDirs)
				{
					ReplaceExtensionName(subDir, extensionName, newExtensionName);
				}
			}
		}
		/// <summary>
		/// 把文本里的换行统一成 LF（CRLF 和裸 CR 都归一）。已经是纯 LF 时原样返回。
		/// 供导表/生成工具写出源码前调用 —— 见 <see cref="WriteAllTextLf"/> 的说明。
		/// </summary>
		public static string ToLf(string text)
		{
			if (text.IndexOf('\r') < 0)
			{
				return text;
			}
			return text.Replace("\r\n", "\n").Replace('\r', '\n');
		}

		/// <summary>
		/// 按 LF 写出文本文件。
		///
		/// 仓库根的 .gitattributes 规定 `* text=auto eol=lf`，但生成出来的源码却常常是 CRLF，
		/// 原因是**输入侧就是 CRLF**：Template.txt 是 CRLF，proto 文件也是 CRLF
		/// （Proto2CS 按 '\n' 切行时会把行尾的 '\r' 一起带进生成代码）。
		/// 结果就是每次导表都产出一批 CRLF 文件，`git status` 里冒出一堆
		/// 「无内容差异的 M」（内容其实一致，只是行尾不同导致 stat 变了），
		/// 真正想看的改动被淹没在里面。
		///
		/// 所以不要指望「输入文件是 LF」—— 在**写出前**统一归一成 LF，
		/// 这样无论模板/proto 是什么行尾，产出都是 LF。
		/// </summary>
		public static void WriteAllTextLf(string path, string content)
		{
			File.WriteAllText(path, ToLf(content));
		}
	}
}
