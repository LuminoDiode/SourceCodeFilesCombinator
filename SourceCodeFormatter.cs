using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Collections;
using System.Windows.Forms;

namespace SourceCodeFilesComplier
{
	public class SourceCodeFileFormatter
	{
		public bool AddFileNames { get; set; } = true;
		public bool RemoveLineIndets { get; set; } = true;
		public bool RemoveEmptyLines { get; set; } = true;
		public bool AddLinesNumbers { get; set; } = true;
		public bool UseCommonLinesNumsLength { get; set; } = true;

		public enum FileNameVariant
		{
			FULL_PATH,
			TO_SHARED_PATH,
			FILE_NAME_ONLY
		}
		public FileNameVariant UseFileNameVariant { get; set; } = FileNameVariant.FULL_PATH;

		/// <summary> Directory full name which is user to trim file names while using FileNameVariantInOutput==TO_SHARED_PATH. </summary>
		public string FilesSharedDirectory { get; set; } = string.Empty;


		private string GetNameByCurrentVariant(FileInfo fi)
		{
			if (this.UseFileNameVariant == FileNameVariant.FULL_PATH) return fi.FullName;
			if (this.UseFileNameVariant == FileNameVariant.TO_SHARED_PATH) return fi.FullName.Replace(FilesSharedDirectory, string.Empty);
			if (this.UseFileNameVariant == FileNameVariant.FILE_NAME_ONLY) return fi.Name;

			throw new NotImplementedException();
		}

		public string ProceedSourceCode(FileInfo sourceCodeFile)
		{
			var lines = File.ReadAllLines(sourceCodeFile.FullName);
			var sb = new StringBuilder(lines.Sum(x => x.Length) + sourceCodeFile.FullName.Length);
			int maxLineNumberLength = lines.Length.ToString().Length;

			if (this.AddFileNames) sb.AppendLine(GetNameByCurrentVariant(sourceCodeFile));
			for (int i = 0, lineCounter = 1; i < lines.Length; i++)
			{
				if (this.RemoveEmptyLines && string.IsNullOrWhiteSpace(lines[i])) continue;

				if (this.AddLinesNumbers)
				{
					var currentLineNumber = lineCounter++.ToString();
					int numOfSpace = 0;

					if (this.UseCommonLinesNumsLength)
						numOfSpace = maxLineNumberLength - currentLineNumber.Length + 1;
					if (numOfSpace < 1) numOfSpace = 1;

					sb.Append(currentLineNumber);
					sb.Append(new string(' ', numOfSpace));
				}

				if (this.RemoveLineIndets)
					sb.AppendLine(lines[i].Trim());
				else
					sb.AppendLine(lines[i].TrimEnd());
			}

			return sb.ToString();
		}
	}
}
