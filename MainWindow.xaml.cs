using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace SourceCodeFilesCombinator
{
	public partial class MainWindow : Window
	{
		private readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(255, 220, 220));
		private readonly SolidColorBrush OkBrush = new SolidColorBrush(Color.FromRgb(220, 255, 220));

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
			var exList = new List<Exception>(2);
			string[] exts = null;
			DirectoryInfo dir = null;
			try { exts = this.GetFileExtensionsOrShowErrorToUser(); } catch (Exception ex) { exList.Add(ex); }
			try { dir = this.GetSearchDirectoryOrShowErrorToUser(); } catch (Exception ex) { exList.Add(ex); }
			foreach (var ex in exList) throw ex;

			var fls = dir.GetFiles(string.Empty, (this.SearchSubDirsCB.IsChecked ?? false) ?
				SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

			return fls.Where(x => exts.Any(e => x.Extension.EndsWith(e)));
		}

		private string LastCorrectSearchDirFullName { get; set; }
		public MainWindow()
		{
			this.InitializeComponent();
			this.OutputRTB.FontFamily = new FontFamily("Consolas");
		}

		private void OpenFolderDialogBT_Click(object sender, RoutedEventArgs e)
		{
			var fd = new FolderBrowserDialog();
			fd.ShowDialog();
			if (!string.IsNullOrEmpty(fd.SelectedPath)) this.FolderInputTB.Text = fd.SelectedPath;
			fd.Dispose();
		}

		private string GenerateOutput(IEnumerable<FileInfo> fis)
		{
			SourceCodeFileFormatter.FileNameVariant FileNamesCult = SourceCodeFileFormatter.FileNameVariant.FULL_PATH;
			if (this.ShortToSharedFolderRB.IsChecked ?? false) FileNamesCult = SourceCodeFileFormatter.FileNameVariant.TO_SHARED_PATH;
			else if (this.ShortToFileNameRB.IsChecked ?? false) FileNamesCult = SourceCodeFileFormatter.FileNameVariant.FILE_NAME_ONLY;

			SourceCodeFileFormatter proceeder = new SourceCodeFileFormatter
			{
				AddFileNames = this.AddFileNamesCB.IsChecked ?? false,
				UseFileNameVariant = FileNamesCult,
				RemoveLineIndets = this.RemoveIndentCB.IsChecked ?? false,
				RemoveEmptyLines = this.RemoveEmptyCB.IsChecked ?? false,
				AddLinesNumbers = this.AddLineNumsCB.IsChecked ?? false,
				UseCommonLinesNumsLength = this.CommonLinesNumsLengthCB.IsChecked ?? false,
				FilesSharedDirectory = this.LastCorrectSearchDirFullName
			};

			var outputBuilder = new StringBuilder((int)fis.Sum(x => x.Length / 2));
			foreach (var f in fis) { outputBuilder.AppendLine(proceeder.ProceedSourceCode(f)); }
			return outputBuilder.ToString();
		}

		private void ProceedTB_Click(object sender, RoutedEventArgs e)
		{
			this.OutputRTB.Document.Blocks.Clear();

			IEnumerable<FileInfo> files;
			try { files = this.GetInputFilesOrShowErrorToUser(); } catch { return; }

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
				try { files = this.GetInputFilesOrShowErrorToUser(); } catch { return; }
				File.WriteAllText(fd.FileName, this.GenerateOutput(files));
			}

			fd.Dispose();
		}
	}
}