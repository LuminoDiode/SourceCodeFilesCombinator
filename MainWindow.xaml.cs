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
	public partial class MainWindow : Window
	{
		private SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(255, 220, 220));
		private SolidColorBrush OkBrush = new SolidColorBrush(Color.FromRgb(220, 255, 220));

		/// <exception cref="System.FormatException"/>
		private string[] GetFileExtensionsOrShowErrorToUser()
		{
			var inp = this.FileExtsInputTB.Text.Split(" ,.;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			if (inp.Length <= 0 || inp.Any(x => x.Length > 10) || inp.Any(s => s.Any(c => !char.IsLetterOrDigit(c))))
			{
				this.FileExtsInputTB.Background = this.ErrorBrush;
				throw new FormatException("Unable to get file extensions from user input.");
			}
			else
			{
				this.FileExtsInputTB.Background = this.OkBrush;
				return inp;
			}
		}

		/// <exception cref="System.FormatException"/>
		private DirectoryInfo GetSearchDirectoryOrShowErrorToUser()
		{
			var inp = this.FolderInputTB.Text.Trim();
			var dir = new DirectoryInfo(inp);
			if (!dir.Exists)
			{
				this.FolderInputTB.Background = this.ErrorBrush;
				throw new FormatException("Unable to get search directory from user input.");
			}
			else
			{
				this.FolderInputTB.Background = this.OkBrush;
				this.LastCorrectSearchDirFullName = dir.FullName;
				return dir;
			}
		}

		/// <exception cref="System.AggregateException"/>
		/// <exception cref="System.FormatException"/>
		private IEnumerable<FileInfo> GetInputFilesOrShowErrorToUser()
		{
			var exts = this.GetFileExtensionsOrShowErrorToUser();
			var dir = this.GetSearchDirectoryOrShowErrorToUser();

			var fls = dir.GetFiles(string.Empty, this.SearchFilesInSubdirs ?
				SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

			return fls.Where(x => exts.Any(e => x.Extension.EndsWith(e)));
		}

		/// <exception cref="System.AggregateException"/>
		/// <exception cref="System.FormatException"/>
		private IEnumerable<string> GetInputFilesTextsOrShowErrorToUser()
		{
			return GetInputFilesOrShowErrorToUser().Select(x => File.ReadAllText(x.FullName));
		}


		private string GetNameByCurrentVariant(FileInfo fi)
		{
			if (this.FileNameVariantInOutput == FileNameVariant.FULL_PATH) return fi.FullName;
			if (this.FileNameVariantInOutput == FileNameVariant.TO_SHARED_PATH) return fi.FullName.Replace(LastCorrectSearchDirFullName, string.Empty);
			if (this.FileNameVariantInOutput == FileNameVariant.FILE_NAME_ONLY) return fi.Name;

			throw new NotImplementedException();
		}
		private string ProceedSourceCode(FileInfo sourceCodeFile, bool addFileName, bool removeLineIndets, bool addLinesNumbers)
		{
			var alltext = File.ReadAllText(sourceCodeFile.FullName);
			var sb = new StringBuilder(alltext.Length + sourceCodeFile.FullName.Length);

			IEnumerable<string> lines = alltext.Split('\n');

			var lns = lines.ToArray();

			if (this.AddFileNamesInOutput) sb.AppendLine(GetNameByCurrentVariant(sourceCodeFile));

			int lineCounter = 1;
			int maxLineNumberLength = lns.Length.ToString().Length;

			for (int i = 0; i < lns.Length; i++)
			{
				if (this.RemoveEmptyLinesInOutput && string.IsNullOrWhiteSpace(lns[i])) continue;

				if (this.AddLineNumsInOutput)
				{
					var currentLineNumber = lineCounter++.ToString();

					int numOfSpace = 0;
					if (this.CommonLinesNumsLengthInOutput)
						numOfSpace = maxLineNumberLength - currentLineNumber.Length + 1;
					if (numOfSpace < 1) numOfSpace = 1;

					sb.Append(currentLineNumber);
					sb.Append(new String(' ', numOfSpace));
				}

				if (this.RemoveIndentInOutput)
					sb.AppendLine(lns[i].Trim());
				else
					sb.AppendLine(lns[i].TrimEnd());

			}

			return sb.ToString();
		}


		private bool SearchFilesInSubdirs => this.SearchSubDirsCB.IsChecked.Value;
		private bool AddFileNamesInOutput => this.AddFileNamesCB.IsChecked.Value;
		private enum FileNameVariant
		{
			FULL_PATH,
			TO_SHARED_PATH,
			FILE_NAME_ONLY
		}
		private FileNameVariant FileNameVariantInOutput
		{
			get
			{
				if (FullFileNameRB.IsChecked.Value) return FileNameVariant.FULL_PATH;
				if (ShortToSharedFolderRB.IsChecked.Value) return FileNameVariant.TO_SHARED_PATH;
				if (ShortToFileNameRB.IsChecked.Value) return FileNameVariant.FILE_NAME_ONLY;

				return FileNameVariant.FULL_PATH;
			}
		}
		private bool RemoveIndentInOutput => this.RemoveIndentCB.IsChecked.Value;
		private bool AddLineNumsInOutput => this.AddLineNumsCB.IsChecked.Value;
		private bool RemoveEmptyLinesInOutput => this.RemoveEmptyCB.IsChecked.Value;
		private bool CommonLinesNumsLengthInOutput => this.CommonLinesNumsLengthCB.IsChecked.Value;

		private string LastCorrectSearchDirFullName;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void OpenFolderDialogBT_Click(object sender, RoutedEventArgs e)
		{
			var fd = new FolderBrowserDialog();
			fd.ShowDialog();
			if (!string.IsNullOrEmpty(fd.SelectedPath))
			{
				this.FolderInputTB.Text = fd.SelectedPath;
			}
			fd.Dispose();
		}

		private string GenerateOutput(IEnumerable<FileInfo> fis)
		{
			var ToAppendSb = new StringBuilder((int)(fis.Sum(x => x.Length) / 2));

			foreach (var f in fis)
			{
				ToAppendSb.Append(this.ProceedSourceCode(f, this.AddFileNamesInOutput, this.RemoveEmptyLinesInOutput, this.AddFileNamesInOutput));
				ToAppendSb.Append('\n');
			}

			return ToAppendSb.ToString();
		}

		private void ProceedTB_Click(object sender, RoutedEventArgs e)
		{
			this.OutputRTB.Document.Blocks.Clear();

			IEnumerable<FileInfo> files;
			try { files = GetInputFilesOrShowErrorToUser(); } catch { return; }

			this.OutputRTB.AppendText(this.GenerateOutput(files));
		}

		private void ExportToFileTB_Click(object sender, RoutedEventArgs e)
		{
			var fd = new SaveFileDialog();
			fd.Filter = @"Text files (*.txt)|*.txt|All files (*.*)|*.*";
			fd.ShowDialog();
			if (!string.IsNullOrEmpty(fd.FileName))
			{
				IEnumerable<FileInfo> files;
				try { files = GetInputFilesOrShowErrorToUser(); } catch { return; }
				File.WriteAllText(fd.FileName, this.GenerateOutput(files));
			}
		}
	}
}